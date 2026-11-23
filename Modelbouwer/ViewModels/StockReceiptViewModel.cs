using CommunityToolkit.Mvvm.Input;

namespace Modelbouwer.ViewModels;

/// <summary>
/// Receipt screen (KAN-102): orders on the left, order lines on the right. All actions work on the selected line
/// through double click, context menu or toolbar: receive, receive all, cancel remainder, edit supplier data.
/// </summary>
public partial class StockReceiptViewModel : AsyncObservableObject
{
	private readonly IStockOrderService _stockOrderService;
	private readonly ISupplierService _supplierService;
	private bool _suppressSelectedOrderChange;
	private List<StockOrderModel> _allOrders = [];

	public ObservableCollection<StockOrderModel> Orders { get; } = [ ];
	public ObservableCollection<StockOrderLineModel> OpenOrderLines { get; } = [ ];

	[ObservableProperty] private StockOrderModel? _selectedOrder;
	[ObservableProperty] private StockOrderLineModel? _selectedOrderLine;
	[ObservableProperty] private bool _showClosedOrders;
	[ObservableProperty] private bool _isBusy;

	/// <summary>Dialog hooks; tests replace these, the app uses the real windows.</summary>
	public Func<StockReceiptDialogViewModel, bool>? ShowReceiptDialog { get; set; }
	public Func<StockReceiptDateDialogViewModel, bool>? ShowReceiptDateDialog { get; set; }
	public Func<SupplierProductDialogViewModel, bool>? ShowSupplierProductDialog { get; set; }
	public Func<string, bool>? ConfirmAction { get; set; }
	public Action<string>? ShowInformation { get; set; }

	public IAsyncRelayCommand ActivateLineCommand { get; }
	public IAsyncRelayCommand ReceiveLineCommand { get; }
	public IAsyncRelayCommand CorrectReceiptCommand { get; }
	public IAsyncRelayCommand ReceiveAllCommand { get; }
	public IAsyncRelayCommand CancelRemainderCommand { get; }
	public IAsyncRelayCommand EditSupplierDataCommand { get; }

	public StockReceiptViewModel( IStockOrderService stockOrderService, ISupplierService supplierService )
	{
		_stockOrderService = stockOrderService;
		_supplierService = supplierService;

		ActivateLineCommand = new AsyncRelayCommand( ActivateSelectedLineAsync, () => CanChangeSelectedOpenLine() || CanCorrectSelectedLine() );
		ReceiveLineCommand = new AsyncRelayCommand( ReceiveSelectedLineAsync, CanChangeSelectedOpenLine );
		CorrectReceiptCommand = new AsyncRelayCommand( CorrectSelectedReceiptAsync, CanCorrectSelectedLine );
		ReceiveAllCommand = new AsyncRelayCommand( ReceiveAllAsync, CanReceiveAll );
		CancelRemainderCommand = new AsyncRelayCommand( CancelRemainderAsync, CanChangeSelectedOpenLine );
		EditSupplierDataCommand = new AsyncRelayCommand( EditSupplierDataAsync, () => !IsBusy && SelectedOrder != null && SelectedOrderLine != null );

		ObserveBackgroundTask( InitializeAsync() );
	}

	private bool CanChangeSelectedOpenLine() => !IsBusy && SelectedOrder != null && SelectedOrderLine is { Closed: false };
	private bool CanCorrectSelectedLine() => !IsBusy && SelectedOrder != null && SelectedOrderLine is { Received: > 0d };
	private bool CanReceiveAll() => !IsBusy && SelectedOrder != null && OpenOrderLines.Any( line => !line.Closed );

	private void NotifyCommands()
	{
		ActivateLineCommand.NotifyCanExecuteChanged();
		ReceiveLineCommand.NotifyCanExecuteChanged();
		CorrectReceiptCommand.NotifyCanExecuteChanged();
		ReceiveAllCommand.NotifyCanExecuteChanged();
		CancelRemainderCommand.NotifyCanExecuteChanged();
		EditSupplierDataCommand.NotifyCanExecuteChanged();
	}

	partial void OnIsBusyChanged( bool value ) => NotifyCommands();
	partial void OnSelectedOrderLineChanged( StockOrderLineModel? value ) => NotifyCommands();

	partial void OnSelectedOrderChanged( StockOrderModel? value )
	{
		NotifyCommands();
		if ( _suppressSelectedOrderChange || value == null )
			return;

		ObserveBackgroundTask( LoadSelectedOrderAsync( value ) );
	}

	partial void OnShowClosedOrdersChanged( bool value )
	{
		ApplyOrderFilters();
		if ( SelectedOrder != null )
			ObserveBackgroundTask( LoadSelectedOrderAsync( SelectedOrder ) );
	}

	public async Task InitializeAsync()
	{
		_allOrders = await _stockOrderService.GetAllOrdersAsync();
		ApplyOrderFilters();
	}

	public void ApplySelectedOrder( StockOrderModel order, IEnumerable<StockOrderLineModel> lines )
	{
		_suppressSelectedOrderChange = true;
		if ( ReferenceEquals( SelectedOrder, order ) )
			SelectedOrder = null;

		SelectedOrder = order;
		_suppressSelectedOrderChange = false;

		// All lines of the selected order are shown; closed lines are greyed out in the view so a receipt
		// can still be corrected after "entire order fully received".
		OpenOrderLines.Clear();
		foreach ( var line in lines )
		{
			OpenOrderLines.Add( line );
		}

		SelectedOrderLine = null;
		NotifyCommands();
	}

	private async Task LoadSelectedOrderAsync( StockOrderModel order )
	{
		var lines = await _stockOrderService.GetOrderLinesAsync( order.Id );
		ApplySelectedOrder( order, lines );
	}

	#region Double click: receive an open line, adjust the receipt of a closed line
	public Task ActivateSelectedLineAsync()
	{
		if ( CanChangeSelectedOpenLine() )
			return ReceiveSelectedLineAsync();

		if ( CanCorrectSelectedLine() )
			return CorrectSelectedReceiptAsync();

		return Task.CompletedTask;
	}
	#endregion

	#region Adjust the registered receipt of a line
	/// <summary>
	/// Sets the actual total received for a line. The difference is booked as a (possibly negative) receipt,
	/// and the line and order are reopened when something is open again.
	/// </summary>
	public async Task CorrectSelectedReceiptAsync()
	{
		if ( !CanCorrectSelectedLine() )
			return;

		var order = SelectedOrder!;
		var line = SelectedOrderLine!;

		IsBusy = true;
		try
		{
			var dialogVm = new StockReceiptDialogViewModel( StockReceiptDialogModel.CreateCorrection( line ) );
			var confirmed = ShowReceiptDialog?.Invoke( dialogVm ) ?? ShowReceiptDialogWindow( dialogVm );
			if ( !confirmed || dialogVm.Validate() != null )
				return;

			double delta = dialogVm.Model.StockDelta;
			if ( delta == 0d )
				return;

			DateTime deliveryDate = dialogVm.Model.DeliveryDate ?? DateTime.Today;
			ApplyReceipt( line, delta, deliveryDate );
			await _stockOrderService.RegisterReceiptAsync( line, delta, deliveryDate );

			if ( order.Closed && !line.Closed )
			{
				order.Closed = false;
				order.ClosedDate = null;
				await _stockOrderService.UpdateOrderAsync( order );
			}

			await RefreshOrderAsync( order, deliveryDate, line.Id );
		}
		finally
		{
			IsBusy = false;
		}
	}
	#endregion

	#region Receive one line
	public async Task ReceiveSelectedLineAsync()
	{
		if ( !CanChangeSelectedOpenLine() )
			return;

		var order = SelectedOrder!;
		var line = SelectedOrderLine!;

		IsBusy = true;
		try
		{
			var dialogVm = new StockReceiptDialogViewModel( StockReceiptDialogModel.Create( line ) );
			var confirmed = ShowReceiptDialog?.Invoke( dialogVm ) ?? ShowReceiptDialogWindow( dialogVm );
			if ( !confirmed || dialogVm.Validate() != null )
				return;

			double receiveAmount = dialogVm.Model.ReceiveAmount;
			DateTime deliveryDate = dialogVm.Model.DeliveryDate ?? DateTime.Today;

			ApplyReceipt( line, receiveAmount, deliveryDate );
			await _stockOrderService.RegisterReceiptAsync( line, receiveAmount, deliveryDate );
			await RefreshOrderAsync( order, deliveryDate, line.Id );
		}
		finally
		{
			IsBusy = false;
		}
	}

	/// <summary>Adds a delivery to the line and closes it when nothing is open anymore.</summary>
	public static void ApplyReceipt( StockOrderLineModel line, double receiveAmount, DateTime deliveryDate )
	{
		line.Received += receiveAmount;
		line.OpenAmount = Math.Max( line.Amount - line.Received, 0d );
		line.Closed = line.OpenAmount <= 0d;
		line.ClosedDate = line.Closed ? deliveryDate : null;
	}
	#endregion

	#region Receive all open lines
	public async Task ReceiveAllAsync()
	{
		if ( !CanReceiveAll() )
			return;

		var order = SelectedOrder!;
		var openLines = OpenOrderLines.Where( line => !line.Closed ).ToList();

		IsBusy = true;
		try
		{
			StockReceiptDateDialogViewModel dialogVm = new();
			var confirmed = ShowReceiptDateDialog?.Invoke( dialogVm ) ?? ShowReceiptDateDialogWindow( dialogVm );
			if ( !confirmed )
				return;

			DateTime deliveryDate = dialogVm.DeliveryDate ?? DateTime.Today;
			foreach ( var line in openLines )
			{
				double receiveAmount = line.OpenAmount;
				if ( receiveAmount <= 0d )
					continue;

				ApplyReceipt( line, receiveAmount, deliveryDate );
				await _stockOrderService.RegisterReceiptAsync( line, receiveAmount, deliveryDate );
			}

			await RefreshOrderAsync( order, deliveryDate, null );
		}
		finally
		{
			IsBusy = false;
		}
	}
	#endregion

	#region Cancel the remainder of a line
	public async Task CancelRemainderAsync()
	{
		if ( !CanChangeSelectedOpenLine() )
			return;

		var order = SelectedOrder!;
		var line = SelectedOrderLine!;
		string message = string.Format( Lang.StockReceiptCancelRemainderConfirm,
			line.OpenAmount.ToString( "0.##", CultureInfo.CurrentCulture ), line.DisplayProductDescription );

		bool confirmed = ConfirmAction?.Invoke( message )
			?? MessageBox.Show( message, Lang.generalMessageboxWarningTitle, MessageBoxButton.YesNo, MessageBoxImage.Question ) == MessageBoxResult.Yes;
		if ( !confirmed )
			return;

		IsBusy = true;
		try
		{
			// The ordered amount is reduced to what was received. "Received" is stored as ordered minus open,
			// so this keeps it correct, and the line total only charges what was delivered.
			line.Amount = line.Received;
			line.RealRowTotal = Math.Round( line.Price * line.Amount, 2, MidpointRounding.AwayFromZero );
			line.OpenAmount = 0d;
			line.Closed = true;
			line.ClosedDate = DateTime.Today;
			await _stockOrderService.UpdateOrderLineAsync( line );
			await RefreshOrderAsync( order, DateTime.Today, line.Id );
		}
		finally
		{
			IsBusy = false;
		}
	}
	#endregion

	#region Edit supplier data of the product on a line
	public async Task EditSupplierDataAsync()
	{
		if ( IsBusy || SelectedOrder == null || SelectedOrderLine == null )
			return;

		var order = SelectedOrder;
		var line = SelectedOrderLine;

		IsBusy = true;
		try
		{
			var existing = await _supplierService.GetProductSupplierAsync( order.SupplierId, line.ProductId );
			var product = new ProductModel
			{
				ProductId = line.ProductId,
				ProductCode = line.ProductCode,
				ProductName = line.ProductName,
				ProductPrice = line.Price
			};
			var supplier = new SupplierModel { Id = order.SupplierId, Name = order.SupplierName, CurrencyId = order.CurrencyId };
			var model = StockOrderProductDialogModel.Create( product, supplier, existing );

			var dialogVm = new SupplierProductDialogViewModel( model );
			var confirmed = ShowSupplierProductDialog?.Invoke( dialogVm ) ?? ShowSupplierProductDialogWindow( dialogVm );
			if ( !confirmed || !dialogVm.TryConfirm( out _ ) )
				return;

			await _supplierService.UpsertProductSupplierAsync( new Modelbouwer.Model.ProductSupplierModel
			{
				ProductSupplierId = model.ProductSupplierId,
				SupplierId = model.SupplierId,
				ProductId = model.ProductId,
				CurrencyId = model.CurrencyId,
				ProductNumber = model.SupplierProductNumber,
				ProductName = model.SupplierProductName,
				Price = model.UnitPrice,
				URL = model.ProductUrl,
				DefaultSupplier = false
			} );

			// The delivered line follows the corrected supplier data.
			line.SupplierProductNumber = model.SupplierProductNumber;
			line.SupplierProductName = model.SupplierProductName;
			line.Price = model.UnitPrice;
			line.RealRowTotal = Math.Round( line.Price * line.Amount, 2, MidpointRounding.AwayFromZero );
			await _stockOrderService.UpdateOrderLineAsync( line );
		}
		finally
		{
			IsBusy = false;
		}
	}
	#endregion

	#region Helpers
	private async Task RefreshOrderAsync( StockOrderModel order, DateTime deliveryDate, int? reselectLineId )
	{
		var refreshedLines = await _stockOrderService.GetOrderLinesAsync( order.Id );
		await CloseOrderWhenAllLinesAreClosedAsync( order, refreshedLines, deliveryDate );
		ApplyOrderFilters();
		ApplySelectedOrder( order, refreshedLines );

		if ( reselectLineId is int id )
			SelectedOrderLine = OpenOrderLines.FirstOrDefault( line => line.Id == id );

		if ( !OpenOrderLines.Any( line => !line.Closed ) )
			ShowInformationMessage( Lang.StockReceiptNothingOpen );
	}

	private async Task CloseOrderWhenAllLinesAreClosedAsync( StockOrderModel order, IReadOnlyCollection<StockOrderLineModel> lines, DateTime deliveryDate )
	{
		if ( order.Closed || lines.Count == 0 || lines.Any( line => !line.Closed ) )
			return;

		order.Closed = true;
		order.ClosedDate = deliveryDate;
		await _stockOrderService.UpdateOrderAsync( order );
	}

	private void ApplyOrderFilters()
	{
		// The selected order stays in the list after it closes, so its receipts can still be adjusted.
		// Capture it first: clearing the list can reset the grid's selection binding.
		var selected = SelectedOrder;
		var filtered = _allOrders
			.Where( order => ShowClosedOrders || !order.Closed || ReferenceEquals( order, selected ) )
			.ToList();

		Orders.Clear();
		foreach ( var order in filtered )
		{
			Orders.Add( order );
		}
	}

	private void ShowInformationMessage( string message )
	{
		if ( ShowInformation != null )
			ShowInformation( message );
		else
			MessageBox.Show( message, Lang.generalMessageboxWarningTitle, MessageBoxButton.OK, MessageBoxImage.Information );
	}

	public void ReplaceOrdersForTest( IEnumerable<StockOrderModel> orders )
	{
		_allOrders = orders.ToList();
		ApplyOrderFilters();
	}

	private static bool ShowReceiptDialogWindow( StockReceiptDialogViewModel viewModel )
	{
		StockReceiptDialog dialog = new( viewModel ) { Owner = Application.Current?.MainWindow };
		return dialog.ShowDialog() == true;
	}

	private static bool ShowReceiptDateDialogWindow( StockReceiptDateDialogViewModel viewModel )
	{
		StockReceiptDateDialog dialog = new( viewModel ) { Owner = Application.Current?.MainWindow };
		return dialog.ShowDialog() == true;
	}

	private static bool ShowSupplierProductDialogWindow( SupplierProductDialogViewModel viewModel )
	{
		SupplierProductDialog dialog = new( viewModel ) { Owner = Application.Current?.MainWindow };
		return dialog.ShowDialog() == true;
	}
	#endregion
}
