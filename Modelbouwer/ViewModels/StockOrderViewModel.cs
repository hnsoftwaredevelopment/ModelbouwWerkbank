using System.ComponentModel;

using CommunityToolkit.Mvvm.Input;

using Modelbouwer.Model;

namespace Modelbouwer.ViewModels;

/// <summary>
/// Order screen (KAN-103). Pick a product from the product list (double click, Enter or context menu) and it
/// appears in the order lines, where quantity and unit price can be changed directly. Nothing is written to the
/// database until Save; Save stores the order, its lines and the supplier data of the products in one transaction.
/// </summary>
public partial class StockOrderViewModel : AsyncObservableObject
{
	private readonly IStockOrderService _stockOrderService;
	private readonly IProductService _productService;
	private readonly IStockService _stockService;
	private readonly ISupplierService _supplierService;

	private bool _suppressSelectedOrderChange;
	private bool _suppressEditableOrderTracking;
	private bool _suppressLineTracking;
	private bool _shippingCostsOverridden;
	private bool _orderCostsOverridden;
	private int _selectionLoadVersion;
	private StockOrderModel? _trackedEditableOrder;
	private StockOrderModel? _loadedOrder;
	private IRelayCommand? _clearSearchCommand;

	private List<StockOrderModel> _allOrders = [];
	private List<StockOrderProductRowModel> _allRows = [];
	private Dictionary<(int SupplierId, int ProductId), ProductSupplierModel> _productSuppliers = [];
	private readonly Dictionary<int, ProductSupplierModel> _pendingProductSuppliers = [];
	private readonly List<int> _deletedLineIds = [];
	private Dictionary<int, double> _savedOpenByProduct = [];

	public ObservableCollection<StockOrderModel> Orders { get; } = [ ];
	public ObservableCollection<StockOrderLineModel> OrderLines { get; } = [ ];
	public ObservableCollection<StockOrderProductRowModel> ProductRows { get; } = [ ];
	public ObservableCollection<SupplierModel> Suppliers { get; } = [ ];
	public ObservableCollection<CurrencyModel> Currencies { get; } = [ ];

	[ObservableProperty] private StockOrderModel _editableOrder = new();
	[ObservableProperty] private StockOrderModel? _selectedOrder;
	[ObservableProperty] private StockOrderLineModel? _selectedOrderLine;
	[ObservableProperty] private StockOrderProductRowModel? _selectedProductRow;
	[ObservableProperty] private bool _isNewOrder;
	[ObservableProperty] private bool _hasUnsavedChanges;
	[ObservableProperty] private bool _isSavingOrder;
	[ObservableProperty] private bool _isEditingOrderLine;
	[ObservableProperty] private bool _showClosedOrders;
	[ObservableProperty] private bool _showOnlyShortages;
	[ObservableProperty] private string? _searchText;

	public bool IsClosedOrder => EditableOrder.Closed;
	public bool CanEditOrder => !EditableOrder.Closed;

	/// <summary>The supplier decides article numbers and prices, so it is fixed once the order has lines.</summary>
	public bool CanChangeSupplier => CanEditOrder && OrderLines.Count == 0;

	/// <summary>Dialog and message hooks; tests replace these, the app uses real windows.</summary>
	public Func<SupplierProductDialogViewModel, bool>? ShowSupplierProductDialog { get; set; }
	public Func<string, bool>? ConfirmAction { get; set; }
	public Action<string>? ShowMessage { get; set; }

	public IRelayCommand NewOrderCommand { get; }
	public IAsyncRelayCommand SaveOrderCommand { get; }
	public IAsyncRelayCommand DeleteOrderCommand { get; }
	public IAsyncRelayCommand AddProductToOrderCommand { get; }
	public IAsyncRelayCommand AddShortagesCommand { get; }
	public IAsyncRelayCommand EditLineSupplierDataCommand { get; }
	public IRelayCommand DeleteOrderLineCommand { get; }
	public IRelayCommand ClearSearchCommand => _clearSearchCommand ??= new RelayCommand( () => SearchText = string.Empty );

	public StockOrderViewModel(
		IStockOrderService stockOrderService,
		IProductService productService,
		IStockService stockService,
		ISupplierService supplierService )
	{
		_stockOrderService = stockOrderService;
		_productService = productService;
		_stockService = stockService;
		_supplierService = supplierService;

		NewOrderCommand = new RelayCommand( RequestNewOrder );
		SaveOrderCommand = new AsyncRelayCommand( cancellationToken => SaveOrderAsync( cancellationToken ), CanSaveOrder );
		DeleteOrderCommand = new AsyncRelayCommand( DeleteOrderAsync );
		AddProductToOrderCommand = new AsyncRelayCommand( AddSelectedProductAsync, CanAddProduct );
		AddShortagesCommand = new AsyncRelayCommand( AddShortagesAsync, CanEditOrderLineCommand );
		EditLineSupplierDataCommand = new AsyncRelayCommand( EditSelectedLineSupplierDataAsync, () => CanEditOrderLineCommand() && SelectedOrderLine != null );
		DeleteOrderLineCommand = new RelayCommand( DeleteSelectedOrderLine, () => CanEditOrderLineCommand() && SelectedOrderLine != null );

		OrderLines.CollectionChanged += ( _, _ ) => OnPropertyChanged( nameof( CanChangeSupplier ) );

		ResetToNewOrder();
		ObserveBackgroundTask( InitializeAsync() );
	}

	private bool CanSaveOrder() => !IsSavingOrder;
	private bool CanEditOrderLineCommand() => CanEditOrder && !IsEditingOrderLine;
	private bool CanAddProduct() => CanEditOrderLineCommand() && SelectedProductRow != null;

	private void NotifyOrderLineCommandsCanExecuteChanged()
	{
		AddProductToOrderCommand.NotifyCanExecuteChanged();
		AddShortagesCommand.NotifyCanExecuteChanged();
		EditLineSupplierDataCommand.NotifyCanExecuteChanged();
		DeleteOrderLineCommand.NotifyCanExecuteChanged();
	}

	partial void OnIsSavingOrderChanged( bool value ) => SaveOrderCommand.NotifyCanExecuteChanged();
	partial void OnIsEditingOrderLineChanged( bool value ) => NotifyOrderLineCommandsCanExecuteChanged();
	partial void OnSelectedOrderLineChanged( StockOrderLineModel? value ) => NotifyOrderLineCommandsCanExecuteChanged();
	partial void OnSelectedProductRowChanged( StockOrderProductRowModel? value ) => AddProductToOrderCommand.NotifyCanExecuteChanged();
	partial void OnSearchTextChanged( string? value ) => ApplyProductFilter();
	partial void OnShowOnlyShortagesChanged( bool value ) => ApplyProductFilter();
	partial void OnShowClosedOrdersChanged( bool value ) => ApplyOrderFilters();

	#region Order selection
	partial void OnSelectedOrderChanged( StockOrderModel? value )
	{
		if ( _suppressSelectedOrderChange || value == null || ReferenceEquals( value, _loadedOrder ) )
			return;

		if ( HasUnsavedChanges && !Confirm( Lang.StockOrderDiscardChangesConfirm ) )
		{
			RestoreSelectedOrder();
			return;
		}

		int loadVersion = ++_selectionLoadVersion;
		ObserveBackgroundTask( LoadSelectedOrderAsync( value, loadVersion ) );
	}

	/// <summary>Puts the dropdown back on the order that is on screen (after "keep my changes").</summary>
	private void RestoreSelectedOrder()
	{
		void Restore()
		{
			_suppressSelectedOrderChange = true;
			SelectedOrder = _loadedOrder;
			_suppressSelectedOrderChange = false;
		}

		// Deferred, because a ComboBox ignores a selection change made while it is still raising its own.
		var dispatcher = Application.Current?.Dispatcher;
		if ( dispatcher != null )
			dispatcher.BeginInvoke( Restore, System.Windows.Threading.DispatcherPriority.Background );
		else
			Restore();
	}

	private async Task LoadSelectedOrderAsync( StockOrderModel order, int loadVersion )
	{
		var lines = await _stockOrderService.GetOrderLinesAsync( order.Id );

		if ( loadVersion != _selectionLoadVersion || SelectedOrder?.Id != order.Id )
			return;

		ApplySelectedOrder( order, lines );
	}

	public void ApplySelectedOrder( StockOrderModel order, IEnumerable<StockOrderLineModel> lines )
	{
		_shippingCostsOverridden = true;
		_orderCostsOverridden = true;

		_suppressSelectedOrderChange = true;
		SelectedOrder = order;
		_suppressSelectedOrderChange = false;
		_loadedOrder = order;

		EditableOrder = new StockOrderModel
		{
			Id = order.Id,
			SupplierId = order.SupplierId,
			CurrencyId = order.CurrencyId,
			SupplierName = order.SupplierName,
			CurrencySymbol = order.CurrencySymbol,
			OrderNumber = order.OrderNumber,
			OrderDate = order.OrderDate,
			ShippingCosts = order.ShippingCosts,
			OrderCosts = order.OrderCosts,
			Memo = order.Memo,
			Closed = order.Closed,
			ClosedDate = order.ClosedDate,
			HasStockLog = order.HasStockLog
		};

		ReplaceLines( lines );
		_savedOpenByProduct = OrderLines
			.GroupBy( line => line.ProductId )
			.ToDictionary( group => group.Key, group => group.Sum( line => line.OpenAmount ) );

		IsNewOrder = false;
		FinishLoad();
	}

	/// <summary>Starts a new order after asking to discard unsaved changes.</summary>
	public void RequestNewOrder()
	{
		if ( HasUnsavedChanges && !Confirm( Lang.StockOrderDiscardChangesConfirm ) )
			return;

		ResetToNewOrder();
	}

	public void ResetToNewOrder()
	{
		_shippingCostsOverridden = false;
		_orderCostsOverridden = false;

		EditableOrder = new StockOrderModel { Id = 0, OrderDate = DateTime.Today };

		_suppressSelectedOrderChange = true;
		SelectedOrder = null;
		_suppressSelectedOrderChange = false;
		_loadedOrder = null;
		_selectionLoadVersion++;

		ReplaceLines( [] );
		_savedOpenByProduct = [];
		IsNewOrder = true;
		FinishLoad();
	}

	private void ReplaceLines( IEnumerable<StockOrderLineModel> lines )
	{
		foreach ( var line in OrderLines )
			line.PropertyChanged -= OrderLine_PropertyChanged;

		OrderLines.Clear();
		foreach ( var line in lines )
			AttachLine( line );

		SelectedOrderLine = null;
		_deletedLineIds.Clear();
		_pendingProductSuppliers.Clear();
	}

	private void AttachLine( StockOrderLineModel line )
	{
		line.PropertyChanged += OrderLine_PropertyChanged;
		OrderLines.Add( line );
	}

	private void FinishLoad()
	{
		RefreshSupplierOnRows();
		RefreshPendingInOrder();
		RecalculateTotals();
		HasUnsavedChanges = false;
		OnPropertyChanged( nameof( IsClosedOrder ) );
		OnPropertyChanged( nameof( CanEditOrder ) );
		OnPropertyChanged( nameof( CanChangeSupplier ) );
		NotifyOrderLineCommandsCanExecuteChanged();
	}
	#endregion

	#region Order header
	partial void OnEditableOrderChanged( StockOrderModel value )
	{
		if ( _trackedEditableOrder != null )
			_trackedEditableOrder.PropertyChanged -= EditableOrder_PropertyChanged;

		_trackedEditableOrder = value;
		_trackedEditableOrder.PropertyChanged += EditableOrder_PropertyChanged;
	}

	private void EditableOrder_PropertyChanged( object? sender, PropertyChangedEventArgs e )
	{
		if ( _suppressEditableOrderTracking )
			return;

		switch ( e.PropertyName )
		{
			case nameof( StockOrderModel.SupplierId ):
				ApplySupplierDefaults();
				RefreshSupplierOnRows();
				ApplyProductFilter();
				HasUnsavedChanges = true;
				break;
			case nameof( StockOrderModel.ShippingCosts ):
				_shippingCostsOverridden = true;
				HasUnsavedChanges = true;
				break;
			case nameof( StockOrderModel.OrderCosts ):
				_orderCostsOverridden = true;
				HasUnsavedChanges = true;
				break;
			case nameof( StockOrderModel.CurrencyId ):
			case nameof( StockOrderModel.OrderNumber ):
			case nameof( StockOrderModel.OrderDate ):
			case nameof( StockOrderModel.Memo ):
				HasUnsavedChanges = true;
				break;
		}
	}

	private SupplierModel? GetEditableOrderSupplier() => Suppliers.FirstOrDefault( s => s.Id == EditableOrder.SupplierId );

	private void ApplySupplierDefaults()
	{
		var supplier = GetEditableOrderSupplier();
		if ( supplier == null )
			return;

		_suppressEditableOrderTracking = true;
		try
		{
			EditableOrder.CurrencyId = supplier.CurrencyId;
			EditableOrder.SupplierName = supplier.Name;

			if ( !_shippingCostsOverridden )
				EditableOrder.ShippingCosts = supplier.ShippingCosts;

			if ( !_orderCostsOverridden )
				EditableOrder.OrderCosts = supplier.OrderCosts;
		}
		finally
		{
			_suppressEditableOrderTracking = false;
		}
	}

	private void RecalculateTotals()
	{
		EditableOrder.LinesTotal = OrderLines.Sum( line => line.RealRowTotal );
	}

	public string? ValidateOrderForSave()
	{
		if ( EditableOrder.Closed )
			return Lang.StockOrderOrderHasReceipts;

		if ( EditableOrder.SupplierId <= 0 )
			return Lang.StockOrderSupplierRequired;

		if ( EditableOrder.OrderDate == null )
			return Lang.StockOrderOrderDateRequired;

		return null;
	}
	#endregion

	#region Reference data and product list
	public async Task InitializeAsync()
	{
		await LoadReferenceDataAsync();
		await LoadOrdersAsync( null );
	}

	public async Task InitializeAsync( CancellationToken cancellationToken )
	{
		await LoadReferenceDataAsync();
		await LoadOrdersAsync( cancellationToken );
	}

	private async Task LoadReferenceDataAsync()
	{
		var inventoryTask = _stockService.GetCompleteInventoryAsync();
		var productsTask = _productService.GetAllProductsAsync();
		var suppliersTask = _supplierService.GetAllSuppliersAsync();
		var currenciesTask = _supplierService.GetAllCurrenciesAsync();
		var productSuppliersTask = _supplierService.GetAllProductSuppliersAsync();

		await PerformanceTrace.MeasureAsync(
			$"{nameof( StockOrderViewModel )}.{nameof( LoadReferenceDataAsync )}",
			() => Task.WhenAll( inventoryTask, productsTask, suppliersTask, currenciesTask, productSuppliersTask ) );

		var inventory = ( await inventoryTask ).ToDictionary( item => item.ProductId );
		_allRows = ( await productsTask ).Select( product =>
		{
			var row = new StockOrderProductRowModel( product )
			{
				MinimalStock = product.ProductMinimalStock,
				StandardOrderQuantity = product.ProductStandardQuantity
			};
			if ( inventory.TryGetValue( product.ProductId, out var stock ) )
			{
				row.Inventory = stock.ProductInventory;
				row.InOrder = stock.ProductInOrder;
				if ( stock.ProductMinimalStock > 0 )
					row.MinimalStock = stock.ProductMinimalStock;
			}
			return row;
		} ).ToList();

		_productSuppliers = ( await productSuppliersTask )
			.GroupBy( ps => (ps.SupplierId, ps.ProductId) )
			.ToDictionary( group => group.Key, group => group.First() );

		ReplaceCollection( Suppliers, await suppliersTask );
		ReplaceCollection( Currencies, await currenciesTask );

		RefreshSupplierOnRows();
		RefreshPendingInOrder();
		ApplyProductFilter();
	}

	private static void ReplaceCollection<T>( ObservableCollection<T> target, IEnumerable<T> items )
	{
		target.Clear();
		foreach ( var item in items )
			target.Add( item );
	}

	private ProductSupplierModel? GetProductSupplier( int productId )
	{
		if ( _pendingProductSuppliers.TryGetValue( productId, out var pending ) )
			return pending;

		return _productSuppliers.TryGetValue( (EditableOrder.SupplierId, productId), out var saved ) ? saved : null;
	}

	/// <summary>Marks which products the selected supplier delivers, for grouping and the article number column.</summary>
	private void RefreshSupplierOnRows()
	{
		bool hasSupplier = EditableOrder.SupplierId > 0;
		foreach ( var row in _allRows )
		{
			var productSupplier = hasSupplier ? GetProductSupplier( row.ProductId ) : null;
			row.HasSelectedSupplier = hasSupplier;
			row.IsFromSelectedSupplier = productSupplier != null;
			row.SupplierProductNumber = productSupplier?.ProductNumber;
		}
	}

	/// <summary>Lines on screen that are not saved yet count as "in order" for the shortage icons.</summary>
	private void RefreshPendingInOrder()
	{
		var openOnScreen = OrderLines
			.GroupBy( line => line.ProductId )
			.ToDictionary( group => group.Key, group => group.Sum( line => line.OpenAmount ) );

		foreach ( var row in _allRows )
		{
			openOnScreen.TryGetValue( row.ProductId, out double onScreen );
			_savedOpenByProduct.TryGetValue( row.ProductId, out double saved );
			row.PendingInOrder = onScreen - saved;
		}
	}

	private void ApplyProductFilter()
	{
		IEnumerable<StockOrderProductRowModel> filtered = _allRows;

		if ( ShowOnlyShortages )
			filtered = filtered.Where( row => row.ShortInventory > 0d );

		if ( !string.IsNullOrWhiteSpace( SearchText ) )
		{
			filtered = filtered.Where( row =>
				row.ProductCode?.Contains( SearchText, StringComparison.CurrentCultureIgnoreCase ) == true
				|| row.ProductName?.Contains( SearchText, StringComparison.CurrentCultureIgnoreCase ) == true
				|| row.SupplierProductNumber?.Contains( SearchText, StringComparison.CurrentCultureIgnoreCase ) == true );
		}

		ReplaceCollection( ProductRows, filtered.ToList() );
	}

	private async Task LoadOrdersAsync( CancellationToken? cancellationToken )
	{
		_allOrders = await PerformanceTrace.MeasureAsync(
			$"{nameof( StockOrderViewModel )}.{nameof( LoadOrdersAsync )}",
			() => cancellationToken.HasValue
				? _stockOrderService.GetAllOrdersAsync( cancellationToken.Value )
				: _stockOrderService.GetAllOrdersAsync() );
		ApplyOrderFilters();
	}

	private void ApplyOrderFilters()
	{
		// The order on screen stays in the list, also when it is closed.
		var loaded = _loadedOrder;
		var filtered = _allOrders
			.Where( order => ShowClosedOrders || !order.Closed || ( loaded != null && order.Id == loaded.Id ) )
			.ToList();

		_suppressSelectedOrderChange = true;
		try
		{
			ReplaceCollection( Orders, filtered );
			if ( loaded != null )
			{
				_loadedOrder = Orders.FirstOrDefault( order => order.Id == loaded.Id ) ?? loaded;
				SelectedOrder = _loadedOrder;
			}
		}
		finally
		{
			_suppressSelectedOrderChange = false;
		}
	}

	public void ReplaceOrdersForTest( IEnumerable<StockOrderModel> orders )
	{
		_allOrders = orders.ToList();
		ApplyOrderFilters();
	}
	#endregion

	#region Order lines: add, edit in the grid, delete
	public async Task AddSelectedProductAsync()
	{
		if ( IsEditingOrderLine )
			return;

		if ( !CanEditOrder || SelectedProductRow == null )
			return;

		IsEditingOrderLine = true;
		try
		{
			await AddProductCoreAsync( SelectedProductRow, askForMissingSupplierData: true );
		}
		finally
		{
			IsEditingOrderLine = false;
		}
	}

	/// <summary>
	/// Adds a product with the suggested quantity. When the product is already on an open line, that line is
	/// raised instead. The supplier data popup only appears when article number or description is missing.
	/// </summary>
	private async Task<bool> AddProductCoreAsync( StockOrderProductRowModel row, bool askForMissingSupplierData )
	{
		var supplier = GetEditableOrderSupplier();
		if ( supplier == null )
		{
			Message( Lang.StockOrderSelectSupplierFirst );
			return false;
		}

		var productSupplier = GetProductSupplier( row.ProductId );
		bool missingData = productSupplier == null
			|| string.IsNullOrWhiteSpace( productSupplier.ProductNumber )
			|| string.IsNullOrWhiteSpace( productSupplier.ProductName );

		if ( missingData && askForMissingSupplierData )
		{
			var model = StockOrderProductDialogModel.Create( row.Product, supplier, productSupplier );
			var dialogVm = new SupplierProductDialogViewModel( model );
			if ( !( ShowSupplierProductDialog?.Invoke( dialogVm ) ?? ShowSupplierProductDialogWindow( dialogVm ) ) || !dialogVm.TryConfirm( out _ ) )
				return false;

			productSupplier = SetPendingProductSupplier( row.ProductId, model.ProductSupplierId, model.SupplierProductNumber, model.SupplierProductName, model.UnitPrice, model.ProductUrl );
		}

		double amount = row.SuggestedAmount;
		var existing = OrderLines.FirstOrDefault( line => line.ProductId == row.ProductId && !line.Closed );
		if ( existing != null )
		{
			existing.Amount += amount;   // the change handler recalculates open amount, total and shortage
		}
		else
		{
			double price = productSupplier is { Price: > 0 } ? productSupplier.Price : row.ProductPrice;
			var line = new StockOrderLineModel
			{
				SupplyOrderId = EditableOrder.Id,
				SupplierId = supplier.Id,
				ProductId = row.ProductId,
				ProductCode = row.ProductCode,
				ProductName = row.ProductName,
				SupplierProductNumber = productSupplier?.ProductNumber,
				SupplierProductName = string.IsNullOrWhiteSpace( productSupplier?.ProductName ) ? row.ProductName : productSupplier.ProductName,
				Amount = amount,
				OpenAmount = amount,
				Price = price,
				RealRowTotal = Math.Round( price * amount, 2, MidpointRounding.AwayFromZero ),
				IsDirty = true
			};
			AttachLine( line );
			SelectedOrderLine = line;
		}

		RefreshSupplierOnRows();
		RefreshPendingInOrder();
		RecalculateTotals();
		HasUnsavedChanges = true;
		return true;
	}

	/// <summary>Bestel advies (KAN-82): adds every product of this supplier with a shortage that is not on the order yet.</summary>
	public async Task AddShortagesAsync()
	{
		if ( IsEditingOrderLine || !CanEditOrder )
			return;

		if ( GetEditableOrderSupplier() == null )
		{
			Message( Lang.StockOrderSelectSupplierFirst );
			return;
		}

		var candidates = _allRows
			.Where( row => row.IsFromSelectedSupplier && row.ShortInventory > 0d )
			.Where( row => !OrderLines.Any( line => line.ProductId == row.ProductId && !line.Closed ) )
			.ToList();

		if ( candidates.Count == 0 )
		{
			Message( Lang.StockOrderNoShortages );
			return;
		}

		IsEditingOrderLine = true;
		try
		{
			foreach ( var row in candidates )
				await AddProductCoreAsync( row, askForMissingSupplierData: false );
		}
		finally
		{
			IsEditingOrderLine = false;
		}
	}

	/// <summary>Quantity or unit price changed directly in the grid.</summary>
	private void OrderLine_PropertyChanged( object? sender, PropertyChangedEventArgs e )
	{
		if ( _suppressLineTracking || sender is not StockOrderLineModel line )
			return;

		if ( e.PropertyName is not ( nameof( StockOrderLineModel.Amount ) or nameof( StockOrderLineModel.Price ) ) )
			return;

		_suppressLineTracking = true;
		try
		{
			if ( e.PropertyName == nameof( StockOrderLineModel.Amount ) )
			{
				if ( line.Amount < line.Received )
				{
					Message( string.Format( Lang.StockOrderAmountBelowReceived, line.Received.ToString( "0.##", CultureInfo.CurrentCulture ) ) );
					line.Amount = line.Received;
				}
				else if ( line.Amount <= 0d )
				{
					Message( Lang.StockOrderAmountMustBePositive );
					line.Amount = Math.Max( line.Received, 1d );
				}

				line.OpenAmount = Math.Max( line.Amount - line.Received, 0d );
			}
			else
			{
				if ( line.Price < 0d )
					line.Price = 0d;

				// A changed price is also the new price of this supplier.
				var productSupplier = GetProductSupplier( line.ProductId );
				SetPendingProductSupplier( line.ProductId, productSupplier?.ProductSupplierId ?? 0,
					productSupplier?.ProductNumber ?? line.SupplierProductNumber,
					productSupplier?.ProductName ?? line.SupplierProductName, line.Price, productSupplier?.URL );
			}

			line.RealRowTotal = Math.Round( line.Price * line.Amount, 2, MidpointRounding.AwayFromZero );
			line.IsDirty = true;
		}
		finally
		{
			_suppressLineTracking = false;
		}

		RefreshPendingInOrder();
		RecalculateTotals();
		HasUnsavedChanges = true;
	}

	public async Task EditSelectedLineSupplierDataAsync()
	{
		var line = SelectedOrderLine;
		var supplier = GetEditableOrderSupplier();
		if ( line == null || supplier == null || !CanEditOrder )
			return;

		var row = _allRows.FirstOrDefault( r => r.ProductId == line.ProductId );
		var product = row?.Product ?? new ProductModel { ProductId = line.ProductId, ProductCode = line.ProductCode, ProductName = line.ProductName, ProductPrice = line.Price };
		var model = StockOrderProductDialogModel.Create( product, supplier, GetProductSupplier( line.ProductId ) );
		model.SupplierProductNumber = line.SupplierProductNumber ?? model.SupplierProductNumber;
		model.SupplierProductName = line.SupplierProductName ?? model.SupplierProductName;
		model.UnitPrice = line.Price;

		var dialogVm = new SupplierProductDialogViewModel( model );
		if ( !( ShowSupplierProductDialog?.Invoke( dialogVm ) ?? ShowSupplierProductDialogWindow( dialogVm ) ) || !dialogVm.TryConfirm( out _ ) )
			return;

		SetPendingProductSupplier( line.ProductId, model.ProductSupplierId, model.SupplierProductNumber, model.SupplierProductName, model.UnitPrice, model.ProductUrl );

		_suppressLineTracking = true;
		try
		{
			line.SupplierProductNumber = model.SupplierProductNumber;
			line.SupplierProductName = model.SupplierProductName;
			line.Price = model.UnitPrice;
			line.RealRowTotal = Math.Round( line.Price * line.Amount, 2, MidpointRounding.AwayFromZero );
			line.IsDirty = true;
		}
		finally
		{
			_suppressLineTracking = false;
		}

		RefreshSupplierOnRows();
		RecalculateTotals();
		HasUnsavedChanges = true;
		await Task.CompletedTask;
	}

	public void DeleteSelectedOrderLine()
	{
		var line = SelectedOrderLine;
		if ( line == null || !CanEditOrder )
			return;

		if ( line.Received > 0d )
		{
			Message( Lang.StockOrderLineHasReceipts );
			return;
		}

		line.PropertyChanged -= OrderLine_PropertyChanged;
		OrderLines.Remove( line );
		if ( line.Id > 0 )
			_deletedLineIds.Add( line.Id );

		SelectedOrderLine = null;
		RefreshPendingInOrder();
		RecalculateTotals();
		HasUnsavedChanges = true;
	}

	private ProductSupplierModel SetPendingProductSupplier( int productId, int productSupplierId, string? number, string? name, double price, string? url )
	{
		var productSupplier = new ProductSupplierModel
		{
			ProductSupplierId = productSupplierId > 0 ? productSupplierId : GetProductSupplier( productId )?.ProductSupplierId ?? 0,
			SupplierId = EditableOrder.SupplierId,
			ProductId = productId,
			CurrencyId = EditableOrder.CurrencyId,
			ProductNumber = number,
			ProductName = name,
			Price = price,
			URL = url,
			DefaultSupplier = false
		};
		_pendingProductSuppliers [ productId ] = productSupplier;
		return productSupplier;
	}
	#endregion

	#region Save and delete
	public Task SaveOrderAsync() => SaveOrderCoreAsync( null );

	public Task SaveOrderAsync( CancellationToken cancellationToken ) => SaveOrderCoreAsync( cancellationToken );

	private async Task SaveOrderCoreAsync( CancellationToken? cancellationToken )
	{
		if ( IsSavingOrder )
			return;

		var validationMessage = ValidateOrderForSave();
		if ( validationMessage != null )
		{
			Message( validationMessage );
			return;
		}

		IsSavingOrder = true;
		try
		{
			var lines = OrderLines.ToList();
			var productSuppliers = _pendingProductSuppliers.Values.ToList();
			int orderId = cancellationToken.HasValue
				? await _stockOrderService.SaveOrderAsync( EditableOrder, lines, _deletedLineIds.ToList(), productSuppliers, cancellationToken.Value )
				: await _stockOrderService.SaveOrderAsync( EditableOrder, lines, _deletedLineIds.ToList(), productSuppliers );

			// Reload, so stock figures, "in order" and supplier data come from the database again.
			await LoadReferenceDataAsync();
			await LoadOrdersAsync( cancellationToken );

			var savedLines = cancellationToken.HasValue
				? await _stockOrderService.GetOrderLinesAsync( orderId, cancellationToken.Value )
				: await _stockOrderService.GetOrderLinesAsync( orderId );
			var savedOrder = _allOrders.FirstOrDefault( order => order.Id == orderId )
				?? new StockOrderModel { Id = orderId, SupplierId = EditableOrder.SupplierId, CurrencyId = EditableOrder.CurrencyId, OrderNumber = EditableOrder.OrderNumber, OrderDate = EditableOrder.OrderDate };

			ApplySelectedOrder( savedOrder, savedLines );
			ApplyOrderFilters();
		}
		finally
		{
			IsSavingOrder = false;
		}
	}

	private async Task DeleteOrderAsync()
	{
		if ( IsNewOrder || EditableOrder.Id <= 0 )
		{
			RequestNewOrder();
			return;
		}

		if ( OrderLines.Any( line => line.Received > 0d ) || EditableOrder.Closed )
		{
			Message( Lang.StockOrderOrderHasReceipts );
			return;
		}

		if ( !Confirm( string.Format( Lang.StockOrderDeleteOrderConfirm, EditableOrder.OrderNumber, EditableOrder.SupplierName ) ) )
			return;

		var lines = await _stockOrderService.GetOrderLinesAsync( EditableOrder.Id );
		await _stockOrderService.DeleteOrderWithLinesAsync( EditableOrder.Id, lines );

		ResetToNewOrder();
		await LoadReferenceDataAsync();
		await LoadOrdersAsync( null );
	}
	#endregion

	#region Helpers
	private bool Confirm( string message )
	{
		return ConfirmAction?.Invoke( message )
			?? MessageBox.Show( message, Lang.generalMessageboxWarningTitle, MessageBoxButton.YesNo, MessageBoxImage.Question ) == MessageBoxResult.Yes;
	}

	private void Message( string message )
	{
		if ( ShowMessage != null )
			ShowMessage( message );
		else
			MessageBox.Show( message, Lang.generalMessageboxWarningTitle, MessageBoxButton.OK, MessageBoxImage.Information );
	}

	private static bool ShowSupplierProductDialogWindow( SupplierProductDialogViewModel viewModel )
	{
		SupplierProductDialog dialog = new( viewModel ) { Owner = Application.Current?.MainWindow };
		return dialog.ShowDialog() == true;
	}
	#endregion
}
