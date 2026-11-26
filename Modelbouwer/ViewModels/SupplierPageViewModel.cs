using CommunityToolkit.Mvvm.Input;

namespace Modelbouwer.ViewModels;

public partial class SupplierPageViewModel : EntityPageViewModel<SupplierModel>
{
	private readonly ISupplierService _dataService;
	private readonly ICountryService _countryService;
	private readonly ICurrencyService _currencyService;
	private readonly IContactService _contactService;
	private readonly IContactTypeService _contactTypeService;
	private readonly IStockOrderService _stockOrderService;

	private int? _lastSelectedSupplierId;

	// Constructor
	public SupplierPageViewModel
		(
			ISupplierService dataService,
			ICountryService countryService,
			ICurrencyService currencyService,
			IContactService contactService,
			IContactTypeService contactTypeService,
			IStockOrderService stockOrderService,
			IEntityValidator<SupplierModel> validator
		) : base( validator )
	{
		_stockOrderService = stockOrderService;
		_dataService = dataService;
		_countryService = countryService;
		_currencyService = currencyService;
		_contactService = contactService ?? throw new ArgumentNullException( nameof( contactService ) );
		_contactTypeService = contactTypeService ?? throw new ArgumentNullException( nameof( contactTypeService ) );

		ObserveBackgroundTask( LoadComboBoxesContentAsync() );
		ObserveBackgroundTask( LoadContactsAndFunctionsAsync() );
		ObserveBackgroundTask( ReloadAsync() );
	}

	#region Collections & Selected Items
	public ObservableCollection<CountryModel> SupplierCountry { get; } = [ ];
	public ObservableCollection<CurrencyModel> SupplierCurrency { get; } = [ ];
	public ObservableCollection<ContactTypeModel> SupplierContactFunctions { get; } = [ ];
	public ObservableCollection<SupplierContactModel> Contacts { get; } = [ ];
	public ObservableCollection<SupplierContactModel> FilteredContacts { get; } = [ ];

	private CountryModel? _selectedCountry;
	public CountryModel? SelectedCountry
	{
		get => _selectedCountry;
		set
		{
			if ( SetProperty( ref _selectedCountry, value ) && SelectedItem != null && value != null )
			{
				SelectedItem.CountryId = value.CountryId;
			}
		}
	}

	private CurrencyModel? _selectedCurrency;
	public CurrencyModel? SelectedCurrency
	{
		get => _selectedCurrency;
		set
		{
			if ( SetProperty( ref _selectedCurrency, value ) && SelectedItem != null && value != null )
			{
				SelectedItem.CurrencyId = value.CurrencyId;
			}
		}
	}

	private SupplierContactModel? _selectedContact;
	public SupplierContactModel? SelectedContact
	{
		get => _selectedContact;
		set
		{
			if ( SetProperty( ref _selectedContact, value ) )
			{
				if ( value != null )
				{
					// Zet de combobox SelectedContactFunction automatisch
					SelectedContactFunction = SupplierContactFunctions.FirstOrDefault( ct => ct.ContactTypeId == value.ContactTypeId );
				}
			}
		}
	}

	private ContactTypeModel? _selectedContactFunction;
	public ContactTypeModel? SelectedContactFunction
	{
		get => _selectedContactFunction;
		set
		{
			if ( SetProperty( ref _selectedContactFunction, value ) && SelectedContact != null && value != null )
			{
				SelectedContact.ContactTypeId = value.ContactTypeId;
			}
		}
	}
	#endregion

	#region Load Methods
	private async Task LoadComboBoxesContentAsync()
	{
		SupplierCountry.Clear();
		SupplierCurrency.Clear();

		var countriesTask = _countryService.GetAllCountriesAsync();
		var currenciesTask = _currencyService.GetAllCurrenciesAsync();

		await Task.WhenAll( countriesTask, currenciesTask );

		var countries = await countriesTask;
		foreach ( var country in countries )
		{
			SupplierCountry.Add( country );
		}

		var currencies = await currenciesTask;
		foreach ( var currency in currencies )
		{
			SupplierCurrency.Add( currency );
		}
	}

	private async Task LoadContactsAndFunctionsAsync()
	{
		var contactTypesTask = _contactTypeService.GetAllContactTypesAsync();
		var contactsTask = _contactService.GetAllContactsAsync();

		await Task.WhenAll( contactTypesTask, contactsTask );

		var types = await contactTypesTask;
		SupplierContactFunctions.Clear();
		foreach ( var t in types )
			SupplierContactFunctions.Add( t );

		var allContacts = await contactsTask;
		Contacts.Clear();
		foreach ( var c in allContacts )
			Contacts.Add( c );

		// Koppel ContactType lookup
		foreach ( var contact in Contacts )
		{
			contact.ContactTypeList = SupplierContactFunctions;
			contact.RefreshContactTypeName();
		}

		// Update filtered view
		UpdateFilteredContacts();
	}
	#endregion

	#region SelectedSupplier + Filter
	//public SupplierModel? SelectedSupplier => SelectedItem;

	private void UpdateFilteredContacts()
	{
		FilteredContacts.Clear();

		if ( SelectedItem == null )
		{
			RaiseContactCounters();
			return;
		}

		foreach ( var c in Contacts.Where( c => c.SupplierId == SelectedItem.Id ) )
			FilteredContacts.Add( c );


		RaiseContactCounters();
	}

	private void RaiseContactCounters()
	{
		OnPropertyChanged( nameof( TotalContactCount ) );
	}

	public int TotalContactCount => FilteredContacts.Count;
	#endregion

	#region CRUD Contacts
	private IRelayCommand? _addContactCommand;
	public IRelayCommand AddContactCommand => _addContactCommand ??= new RelayCommand( AddContact );

	private IRelayCommand? _deleteContactCommand;
	public IRelayCommand DeleteContactCommand => _deleteContactCommand ??= new RelayCommand( DeleteContact, () => SelectedContact != null );

	private IRelayCommand? _saveContactCommand;
	public IRelayCommand SaveContactCommand => _saveContactCommand ??= new RelayCommand( SaveContact );

	#region Relay command for going to the supplier website
	[RelayCommand( CanExecute = nameof( CanOpenWebsite ) )]
	private void OpenWebsite()
	{
		if ( string.IsNullOrWhiteSpace( SelectedItem?.Url ) )
			return;

		ProcessStartInfo startInfo = new()
		{
			FileName = SelectedItem.Url,
			UseShellExecute = true
		};

		Process.Start( startInfo );
	}

	private bool CanOpenWebsite()
	{
		return !string.IsNullOrWhiteSpace( SelectedItem?.Url );
	}
	#endregion


	private void AddContact()
	{
		if ( SelectedItem == null )
			return;

		var newContact = new SupplierContactModel
		{
			SupplierContactId = 0,
			SupplierId = SelectedItem.Id,
			Name = string.Empty,
			ContactTypeId = SupplierContactFunctions.FirstOrDefault()?.ContactTypeId ?? 0
		};
		Contacts.Add( newContact );
		FilteredContacts.Add( newContact );

		SelectedContact = newContact;

		RaiseContactCounters();
	}

	private void DeleteContact()
	{
		if ( SelectedContact == null )
			return;

		Contacts.Remove( SelectedContact );
		FilteredContacts.Remove( SelectedContact );

		RaiseContactCounters();
	}

	private void SaveContact()
	{
		// Implement contact save logic here
		// This would typically call a service method to persist the contact
	}
	#endregion


	// Commands
	public IRelayCommand AddSupplierCommand => AddCommand;
	public IAsyncRelayCommand SaveSupplierCommand => SaveCommand;
	public IRelayCommand DeleteSupplierCommand => DeleteCommand;
	public new IRelayCommand ClearSearchCommand => _clearSearchCommand ??= new RelayCommand( () => SearchText = string.Empty );


	private IRelayCommand? _clearSearchCommand;


	// Override SelectedItem changed om DefaultSupplier te zetten
	protected override void OnSelectedItemChanged( SupplierModel? oldValue, SupplierModel? newValue )
	{
		base.OnSelectedItemChanged( oldValue, newValue );

		SelectedCurrency = SupplierCurrency.FirstOrDefault( c => c.CurrencyId == newValue?.CurrencyId );

		SelectedCountry = SupplierCountry.FirstOrDefault( c => c.CountryId == newValue?.CountryId );

		UpdateFilteredContacts();
		ObserveBackgroundTask( LoadOrderHistoryAsync( newValue ) );

		OpenWebsiteCommand.NotifyCanExecuteChanged();
	}

	#region Order history tab (KAN-81)
	private int _orderHistoryVersion;
	private int _orderLinesVersion;

	/// <summary>Orders of the selected supplier, newest first.</summary>
	public ObservableCollection<StockOrderModel> SupplierOrders { get; } = [ ];
	public ObservableCollection<StockOrderLineModel> SupplierOrderLines { get; } = [ ];
	[ObservableProperty] private StockOrderModel? _selectedSupplierOrder;
	[ObservableProperty] private bool _hasSupplierOrders;
	[ObservableProperty] private int _supplierOrderCount;
	[ObservableProperty] private double _supplierOrdersTotal;

	partial void OnSelectedSupplierOrderChanged( StockOrderModel? value ) => ObserveBackgroundTask( LoadOrderLinesAsync( value ) );

	private async Task LoadOrderHistoryAsync( SupplierModel? supplier )
	{
		int version = ++_orderHistoryVersion;

		// The previous supplier's orders must not be shown as this supplier's while they load.
		SupplierOrders.Clear();
		SelectedSupplierOrder = null;
		HasSupplierOrders = false;
		SupplierOrderCount = 0;
		SupplierOrdersTotal = 0d;

		if ( supplier == null || supplier.Id <= 0 )
			return;

		var orders = await _stockOrderService.GetAllOrdersAsync();
		if ( version != _orderHistoryVersion )
			return;

		// The order list already comes newest first.
		foreach ( var order in orders.Where( order => order.SupplierId == supplier.Id ) )
			SupplierOrders.Add( order );

		HasSupplierOrders = SupplierOrders.Count > 0;
		SupplierOrderCount = SupplierOrders.Count;
		SupplierOrdersTotal = Math.Round( SupplierOrders.Sum( order => order.GrandTotal ), 2, MidpointRounding.AwayFromZero );
		SelectedSupplierOrder = SupplierOrders.FirstOrDefault();
	}

	private async Task LoadOrderLinesAsync( StockOrderModel? order )
	{
		int version = ++_orderLinesVersion;
		SupplierOrderLines.Clear();
		if ( order == null )
			return;

		var lines = await _stockOrderService.GetOrderLinesAsync( order.Id );
		if ( version != _orderLinesVersion )
			return;

		foreach ( var line in lines )
			SupplierOrderLines.Add( line );
	}
	#endregion


	// Properties voor UI binding
	public ObservableCollection<SupplierModel> Suppliers => Items;
	public int TotalSupplierCount => TotalItemCount;
	public int VisibleSupplierCount
	{
		get => base.VisibleItemCount;
		set => base.VisibleItemCount = value;
	}

	// Filtering
	public bool FilterSupplier( object obj )
	{
		if ( obj is not SupplierModel Supplier )
			return false;

		if ( string.IsNullOrWhiteSpace( SearchText ) )
			return true;

		return Supplier.Name?.Contains( SearchText, StringComparison.CurrentCultureIgnoreCase ) == true;
	}

	// Abstract overrides voor CRUD
	protected override Task<List<SupplierModel>> LoadItemsAsync( CancellationToken cancellationToken ) => _dataService.GetAllSuppliersAsync( cancellationToken );
	protected override Task<int> InsertAsync( SupplierModel item, CancellationToken cancellationToken ) => _dataService.InsertNewSupplierAsync( CreateParameters( item ), cancellationToken );
	protected override Task UpdateAsync( SupplierModel item, CancellationToken cancellationToken )
	{
		if ( SelectedItem == null )
			return Task.CompletedTask;

		_lastSelectedSupplierId = SelectedItem.Id;

		return _dataService.UpdateSupplierAsync( CreateParameters( SelectedItem ), cancellationToken );
	}
	protected override async Task DeleteAsync( SupplierModel item, CancellationToken cancellationToken )
	{
		if ( item == null )
			return;

		var result = MessageBox.Show(
			$"{Lang.toolbarButtonActionDeleteMessageQuestionPrefix} '{item.Name}' {Lang.toolbarButtonActionDeleteMessageQuestionSuffix}",
			$"{Lang.toolbarButtonActionDeleteMessageButtonText}",
			MessageBoxButton.YesNo,
			MessageBoxImage.Warning
		);

		if ( result != MessageBoxResult.Yes )
			return;
		try
		{
			await _dataService.DeleteSupplierAsync( item.Id, cancellationToken );
		}
		catch ( EntityInUseException ex )
		{
			MessageBox.Show(
				ex.Message,
				Lang.generalMessageboxWarningTitle,
				MessageBoxButton.OK,
				MessageBoxImage.Information
			);
		}
	}

	protected override int GetId( SupplierModel item ) => item.Id;
	protected override void SetId( SupplierModel item, int id ) => item.Id = id;

	protected override SupplierModel CreateNewItem() => new()
	{
		Id = 0,
		Name = string.Empty
	};

	protected override void OnItemsLoaded()
	{
		base.OnItemsLoaded();

		foreach ( var supplier in Suppliers )
		{
			supplier.CountryList = SupplierCountry;
		}

		OnPropertyChanged( nameof( TotalSupplierCount ) );
		OnPropertyChanged( nameof( TotalContactCount ) );

		if ( _lastSelectedSupplierId.HasValue )
		{
			var match = Suppliers.FirstOrDefault( p => p.Id == _lastSelectedSupplierId.Value );

			if ( match != null )
			{
				SelectedItem = match;
				return;
			}

			_lastSelectedSupplierId = null;
		}

		// Default Supplier selection (Highest Id)
		SelectSupplierWithHighestId();
	}

	private void SelectSupplierWithHighestId()
	{
		if ( Suppliers.Count == 0 )
		{
			SelectedItem = null;
			return;
		}

		SelectedItem = Suppliers
			.OrderByDescending( p => p.Id )
			.First();
	}

	// Parameter dictionary voor save
	private static Dictionary<string, object?> CreateParameters( SupplierModel c ) => new()
	{
		{ $"@{DBNames.SupplierFieldNameId}", c.Id },
		{ $"@{DBNames.SupplierFieldNameCode}", c.Code },
		{ $"@{DBNames.SupplierFieldNameName}", c.Name?.Trim() },
		{ $"@{DBNames.SupplierFieldNameAddress1}", c.Address1 },
		{ $"@{DBNames.SupplierFieldNameAddress2}", c.Address2 },
		{ $"@{DBNames.SupplierFieldNameZip}", c.Zip },
		{ $"@{DBNames.SupplierFieldNameCity}", c.City },
		{ $"@{DBNames.SupplierFieldNameUrl}", c.Url },
		{ $"@{DBNames.SupplierFieldNameShippingCosts}", c.ShippingCosts },
		{ $"@{DBNames.SupplierFieldNameMinShippingCosts}", c.MinShippingCosts },
		{ $"@{DBNames.SupplierFieldNameOrderCosts}", c.OrderCosts },
		{ $"@{DBNames.SupplierFieldNameMinOrderCosts}", c.MinOrderCosts },
		{ $"@{DBNames.SupplierFieldNameCurrencyId}", c.CurrencyId == 0 ? null : c.CurrencyId },
		{ $"@{DBNames.SupplierFieldNameCountryId}", c.CountryId == 0 ? null : c.CountryId },
		{ $"@{DBNames.SupplierFieldNameGeneralEmail}", c.Mail },
		{ $"@{DBNames.SupplierFieldNameGeneralPhone}", c.Phone },
		{ $"@{DBNames.SupplierFieldNameMemo}", c.Memo }
	};

	// Contact management methods


}