using Modelbouwer.Interfaces;
using Modelbouwer.Models;
using Modelbouwer.ViewModels;

using Moq;

namespace Modelbouwer.UnitTests.ViewModels;

[TestClass]
public class SupplierPageViewModelTests
{
	private Mock<ISupplierService> _mockSupplierService = null!;
	private Mock<ICountryService> _mockCountryService = null!;
	private Mock<ICurrencyService> _mockCurrencyService = null!;
	private Mock<IContactService> _mockContactService = null!;
	private Mock<IContactTypeService> _mockContactTypeService = null!;
	private Mock<IEntityValidator<SupplierModel>> _mockValidator = null!;
	private Mock<IStockOrderService> _mockStockOrderService = null!;
	private SupplierPageViewModel _viewModel = null!;

	[TestInitialize]
	public void Setup()
	{
		_mockSupplierService = new Mock<ISupplierService>();
		_mockCountryService = new Mock<ICountryService>();
		_mockCurrencyService = new Mock<ICurrencyService>();
		_mockContactService = new Mock<IContactService>();
		_mockContactTypeService = new Mock<IContactTypeService>();
		_mockValidator = new Mock<IEntityValidator<SupplierModel>>();
		_mockStockOrderService = new Mock<IStockOrderService>();
		_mockStockOrderService.Setup( s => s.GetAllOrdersAsync() ).ReturnsAsync( new List<StockOrderModel>() );
		_mockStockOrderService.Setup( s => s.GetOrderLinesAsync( It.IsAny<int>() ) ).ReturnsAsync( new List<StockOrderLineModel>() );

		// Setup default returns for async methods
		_mockSupplierService.Setup( s => s.GetAllSuppliersAsync() ).ReturnsAsync( new List<SupplierModel>() );
		_mockCountryService.Setup( s => s.GetAllCountriesAsync() ).ReturnsAsync( new List<CountryModel>() );
		_mockCurrencyService.Setup( s => s.GetAllCurrenciesAsync() ).ReturnsAsync( new List<CurrencyModel>() );
		_mockContactService.Setup( s => s.GetAllContactsAsync() ).ReturnsAsync( new List<SupplierContactModel>() );
		_mockContactTypeService.Setup( s => s.GetAllContactTypesAsync() ).ReturnsAsync( new List<ContactTypeModel>() );

		_viewModel = new SupplierPageViewModel(
			_mockSupplierService.Object,
			_mockCountryService.Object,
			_mockCurrencyService.Object,
			_mockContactService.Object,
			_mockContactTypeService.Object,
			_mockStockOrderService.Object,
			_mockValidator.Object
		);
	}

	#region Order history tab (KAN-81)
	private static List<StockOrderModel> OrdersOfTwoSuppliers() =>
	[
		new() { Id = 26, SupplierId = 1, OrderNumber = "CW0140", OrderDate = new DateTime( 2026, 11, 20 ), LinesTotal = 10, ShippingCosts = 5, LineCount = 2 },
		new() { Id = 24, SupplierId = 2, OrderNumber = "OT0001", OrderDate = new DateTime( 2026, 10, 1 ), LinesTotal = 99, LineCount = 1 },
		new() { Id = 19, SupplierId = 1, OrderNumber = "CW0132", OrderDate = new DateTime( 2026, 5, 4 ), LinesTotal = 20.5, OrderCosts = 1, LineCount = 3, Closed = true }
	];

	private static async Task WaitForAsync( Func<bool> condition )
	{
		for ( int i = 0; i < 40 && !condition(); i++ )
			await Task.Delay( 25 );
	}

	[TestMethod]
	public async Task SelectingSupplier_ShowsOnlyItsOrdersNewestFirstWithTotals()
	{
		_mockStockOrderService.Setup( s => s.GetAllOrdersAsync() ).ReturnsAsync( OrdersOfTwoSuppliers() );
		_mockStockOrderService.Setup( s => s.GetOrderLinesAsync( 26 ) ).ReturnsAsync( new List<StockOrderLineModel>
		{
			new() { Id = 90, SupplyOrderId = 26, ProductId = 5, Amount = 2, OpenAmount = 2, Price = 2.5, RealRowTotal = 5 },
			new() { Id = 91, SupplyOrderId = 26, ProductId = 8, Amount = 1, OpenAmount = 0, Price = 5, RealRowTotal = 5 }
		} );

		_viewModel.SelectedItem = new SupplierModel { Id = 1, Name = "Cornwall" };
		await WaitForAsync( () => _viewModel.SupplierOrderLines.Count == 2 );

		CollectionAssert.AreEqual( new[] { "CW0140", "CW0132" }, _viewModel.SupplierOrders.Select( order => order.OrderNumber ).ToArray() );
		Assert.AreEqual( 2, _viewModel.SupplierOrderCount );
		Assert.AreEqual( 36.5, _viewModel.SupplierOrdersTotal, "Lines, shipping and order costs of both orders." );
		Assert.IsTrue( _viewModel.HasSupplierOrders );
		Assert.AreEqual( 26, _viewModel.SelectedSupplierOrder?.Id, "The newest order is selected and its lines are shown." );
		Assert.AreEqual( 2, _viewModel.SupplierOrderLines.Count );
	}

	[TestMethod]
	public void DisplayRowTotal_FallsBackToPriceTimesAmountForOlderOrders()
	{
		Assert.AreEqual( 7.5, new StockOrderLineModel { Price = 2.5, Amount = 3, RealRowTotal = 0 }.DisplayRowTotal );
		Assert.AreEqual( 7d, new StockOrderLineModel { Price = 2.5, Amount = 3, RealRowTotal = 7 }.DisplayRowTotal, "A stored line total wins." );
	}

	[TestMethod]
	public void OrderListQuery_TotalsOlderLinesWithoutStoredTotal()
	{
		var service = new StockOrderService( new Mock<GenericDataService>().Object );

		StringAssert.Contains( service.CompleteOrderListQuery, "AS LineCount" );
		StringAssert.Contains( service.CompleteOrderListQuery, "AS LinesTotal" );
		StringAssert.Contains( service.CompleteOrderListQuery, "ELSE IFNULL(ol.Price, 0) * IFNULL(ol.Amount, 0)" );
	}

	[TestMethod]
	public async Task SelectingAnotherOrder_ShowsItsLines()
	{
		_mockStockOrderService.Setup( s => s.GetAllOrdersAsync() ).ReturnsAsync( OrdersOfTwoSuppliers() );
		_mockStockOrderService.Setup( s => s.GetOrderLinesAsync( 19 ) ).ReturnsAsync( new List<StockOrderLineModel> { new() { Id = 70, SupplyOrderId = 19 } } );
		_viewModel.SelectedItem = new SupplierModel { Id = 1, Name = "Cornwall" };
		await WaitForAsync( () => _viewModel.SelectedSupplierOrder != null );

		_viewModel.SelectedSupplierOrder = _viewModel.SupplierOrders.Single( order => order.Id == 19 );
		await WaitForAsync( () => _viewModel.SupplierOrderLines.Count == 1 );

		Assert.AreEqual( 70, _viewModel.SupplierOrderLines.Single().Id );
	}

	[TestMethod]
	public async Task SelectingSupplierWithoutOrders_ClearsThePreviousHistory()
	{
		_mockStockOrderService.Setup( s => s.GetAllOrdersAsync() ).ReturnsAsync( OrdersOfTwoSuppliers() );
		_viewModel.SelectedItem = new SupplierModel { Id = 1, Name = "Cornwall" };
		await WaitForAsync( () => _viewModel.HasSupplierOrders );

		_viewModel.SelectedItem = new SupplierModel { Id = 3, Name = "New supplier" };
		await WaitForAsync( () => !_viewModel.HasSupplierOrders );

		Assert.AreEqual( 0, _viewModel.SupplierOrders.Count );
		Assert.AreEqual( 0, _viewModel.SupplierOrderLines.Count );
		Assert.AreEqual( 0d, _viewModel.SupplierOrdersTotal );
		Assert.IsNull( _viewModel.SelectedSupplierOrder );
	}
	#endregion

	[TestMethod]
	public void Constructor_InitializesCollections()
	{
		// Assert
		Assert.IsNotNull( _viewModel.SupplierCountry );
		Assert.IsNotNull( _viewModel.SupplierCurrency );
		Assert.IsNotNull( _viewModel.SupplierContactFunctions );
		Assert.IsNotNull( _viewModel.Contacts );
		Assert.IsNotNull( _viewModel.FilteredContacts );
		Assert.IsNotNull( _viewModel.Suppliers );
	}

	[TestMethod]
	public void SupplierPageViewModel_CommandPropertiesAreNotAmbiguousForReflection()
	{
		Assert.IsNotNull( typeof( SupplierPageViewModel ).GetProperty( nameof( SupplierPageViewModel.AddContactCommand ) ) );
		Assert.IsNotNull( typeof( SupplierPageViewModel ).GetProperty( nameof( SupplierPageViewModel.DeleteContactCommand ) ) );
		Assert.IsNotNull( typeof( SupplierPageViewModel ).GetProperty( nameof( SupplierPageViewModel.SaveContactCommand ) ) );
	}

	[TestMethod]
	public async Task Constructor_WhenSupplierLoadFails_StoresAsyncError()
	{
		var expected = new InvalidOperationException( "Unable to load suppliers." );
		var supplierService = new Mock<ISupplierService>();
		supplierService
			.Setup( service => service.GetAllSuppliersAsync() )
			.Returns( Task.FromException<List<SupplierModel>>( expected ) );

		var viewModel = CreateViewModel( supplierService: supplierService.Object );

		await WaitUntilAsync( () => ReferenceEquals( expected, viewModel.LastAsyncError ) );
		Assert.AreSame( expected, viewModel.LastAsyncError );
	}

	[TestMethod]
	public async Task Constructor_WhenCountryLoadFails_StoresAsyncError()
	{
		var expected = new InvalidOperationException( "Unable to load countries." );
		var countryService = new Mock<ICountryService>();
		countryService
			.Setup( service => service.GetAllCountriesAsync() )
			.Returns( Task.FromException<List<CountryModel>>( expected ) );

		var viewModel = CreateViewModel( countryService: countryService.Object );

		await WaitUntilAsync( () => ReferenceEquals( expected, viewModel.LastAsyncError ) );
		Assert.AreSame( expected, viewModel.LastAsyncError );
	}

	[TestMethod]
	public async Task Constructor_WhenContactTypeLoadFails_StoresAsyncError()
	{
		var expected = new InvalidOperationException( "Unable to load contact types." );
		var contactTypeService = new Mock<IContactTypeService>();
		contactTypeService
			.Setup( service => service.GetAllContactTypesAsync() )
			.Returns( Task.FromException<List<ContactTypeModel>>( expected ) );

		var viewModel = CreateViewModel( contactTypeService: contactTypeService.Object );

		await WaitUntilAsync( () => ReferenceEquals( expected, viewModel.LastAsyncError ) );
		Assert.AreSame( expected, viewModel.LastAsyncError );
	}

	[TestMethod]
	public void Suppliers_ReturnsItemsCollection()
	{
		// Assert
		Assert.AreSame( _viewModel.Items, _viewModel.Suppliers );
	}

	[TestMethod]
	public void TotalSupplierCount_ReturnsTotalItemCount()
	{
		// Arrange
		_viewModel.Items.Add( new SupplierModel { Id = 1 } );
		_viewModel.Items.Add( new SupplierModel { Id = 2 } );

		// Act
		var count = _viewModel.TotalSupplierCount;

		// Assert
		Assert.AreEqual( 2, count );
	}

	[TestMethod]
	public void TotalContactCount_ReturnsFilteredContactsCount()
	{
		// Arrange
		_viewModel.FilteredContacts.Add( new SupplierContactModel() );
		_viewModel.FilteredContacts.Add( new SupplierContactModel() );

		// Act
		var count = _viewModel.TotalContactCount;

		// Assert
		Assert.AreEqual( 2, count );
	}

	[TestMethod]
	public void SelectedCountry_UpdatesSupplierCountryId()
	{
		// Arrange
		var supplier = new SupplierModel { Id = 1 };
		var country = new CountryModel { CountryId = 5, CountryName = "Test Country" };
		_viewModel.SelectedItem = supplier;

		// Act
		_viewModel.SelectedCountry = country;

		// Assert
		Assert.AreEqual( 5, supplier.CountryId );
	}

	[TestMethod]
	public void SelectedCurrency_UpdatesSupplierCurrencyId()
	{
		// Arrange
		var supplier = new SupplierModel { Id = 1 };
		var currency = new CurrencyModel { CurrencyId = 3, CurrencyName = "Test Currency" };
		_viewModel.SelectedItem = supplier;

		// Act
		_viewModel.SelectedCurrency = currency;

		// Assert
		Assert.AreEqual( 3, supplier.CurrencyId );
	}

	[TestMethod]
	public void SelectedContactFunction_UpdatesContactTypeId()
	{
		// Arrange
		var contact = new SupplierContactModel { SupplierContactId = 1 };
		var contactType = new ContactTypeModel { ContactTypeId = 7 };
		_viewModel.SelectedContact = contact;

		// Act
		_viewModel.SelectedContactFunction = contactType;

		// Assert
		Assert.AreEqual( 7, contact.ContactTypeId );
	}

	[TestMethod]
	public void FilterSupplier_WithEmptySearchText_ReturnsTrue()
	{
		// Arrange
		var supplier = new SupplierModel { Name = "Test Supplier" };
		_viewModel.SearchText = "";

		// Act
		var result = _viewModel.FilterSupplier(supplier);

		// Assert
		Assert.IsTrue( result );
	}

	[TestMethod]
	public void FilterSupplier_WithMatchingSearchText_ReturnsTrue()
	{
		// Arrange
		var supplier = new SupplierModel { Name = "Test Supplier" };
		_viewModel.SearchText = "Test";

		// Act
		var result = _viewModel.FilterSupplier(supplier);

		// Assert
		Assert.IsTrue( result );
	}

	[TestMethod]
	public void FilterSupplier_WithNonMatchingSearchText_ReturnsFalse()
	{
		// Arrange
		var supplier = new SupplierModel { Name = "Test Supplier" };
		_viewModel.SearchText = "XYZ";

		// Act
		var result = _viewModel.FilterSupplier(supplier);

		// Assert
		Assert.IsFalse( result );
	}

	[TestMethod]
	public void FilterSupplier_IsCaseInsensitive()
	{
		// Arrange
		var supplier = new SupplierModel { Name = "Test Supplier" };
		_viewModel.SearchText = "test";

		// Act
		var result = _viewModel.FilterSupplier(supplier);

		// Assert
		Assert.IsTrue( result );
	}

	[TestMethod]
	public void FilterSupplier_WithNullSupplierName_ReturnsFalse()
	{
		// Arrange
		var supplier = new SupplierModel { Name = null };
		_viewModel.SearchText = "Test";

		// Act
		var result = _viewModel.FilterSupplier(supplier);

		// Assert
		Assert.IsFalse( result );
	}

	[TestMethod]
	public void FilterSupplier_WithNonSupplierObject_ReturnsFalse()
	{
		// Arrange
		var notASupplier = new object();
		_viewModel.SearchText = "Test";

		// Act
		var result = _viewModel.FilterSupplier(notASupplier);

		// Assert
		Assert.IsFalse( result );
	}

	[TestMethod]
	public void GetId_ReturnsSupplierId()
	{
		// Arrange
		var supplier = new SupplierModel { Id = 42 };

		// Act
		var id = _viewModel.GetType()
			.GetMethod("GetId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
			?.Invoke(_viewModel, new object[] { supplier });

		// Assert
		Assert.AreEqual( 42, id );
	}

	[TestMethod]
	public void SetId_SetsSupplierId()
	{
		// Arrange
		var supplier = new SupplierModel { Id = 0 };

		// Act
		_viewModel.GetType()
			.GetMethod( "SetId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance )
			?.Invoke( _viewModel, new object [ ] { supplier, 99 } );

		// Assert
		Assert.AreEqual( 99, supplier.Id );
	}

	[TestMethod]
	public void CreateNewItem_ReturnsSupplierWithDefaultValues()
	{
		// Act
		var method = _viewModel.GetType()
			.GetMethod("CreateNewItem", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
		var supplier = method?.Invoke(_viewModel, null) as SupplierModel;

		// Assert
		Assert.IsNotNull( supplier );
		Assert.AreEqual( 0, supplier.Id );
		Assert.AreEqual( string.Empty, supplier.Name );
	}

	[TestMethod]
	public async Task LoadItemsAsync_CallsGetAllSuppliersAsync()
	{
		// Arrange
		var suppliers = new List<SupplierModel>
		{
			new SupplierModel { Id = 1, Name = "Supplier 1" }
		};
		_mockSupplierService.Setup( s => s.GetAllSuppliersAsync() ).ReturnsAsync( suppliers );

		// Act
		var method = _viewModel.GetType()
			.GetMethod("LoadItemsAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
		var task = method?.Invoke(_viewModel, null) as Task<List<SupplierModel>>;
		var result = await task!;

		// Assert
		Assert.IsNotNull( result );
		Assert.AreEqual( 1, result.Count );
		_mockSupplierService.Verify( s => s.GetAllSuppliersAsync(), Times.AtLeastOnce );
	}

	[TestMethod]
	public void AddSupplierCommand_ReturnsAddCommand()
	{
		// Assert
		Assert.IsNotNull( _viewModel.AddSupplierCommand );
		Assert.AreSame( _viewModel.AddCommand, _viewModel.AddSupplierCommand );
	}

	[TestMethod]
	public void SaveSupplierCommand_ReturnsSaveCommand()
	{
		// Assert
		Assert.IsNotNull( _viewModel.SaveSupplierCommand );
		Assert.AreSame( _viewModel.SaveCommand, _viewModel.SaveSupplierCommand );
	}

	[TestMethod]
	public void DeleteSupplierCommand_ReturnsDeleteCommand()
	{
		// Assert
		Assert.IsNotNull( _viewModel.DeleteSupplierCommand );
		Assert.AreSame( _viewModel.DeleteCommand, _viewModel.DeleteSupplierCommand );
	}

	[TestMethod]
	public void VisibleSupplierCount_CanBeSetAndRetrieved()
	{
		// Act
		_viewModel.VisibleSupplierCount = 10;

		// Assert
		Assert.AreEqual( 10, _viewModel.VisibleSupplierCount );
	}

	[TestMethod]
	public void AddContactCommand_CreatesNewContact()
	{
		// Arrange
		var supplier = new SupplierModel { Id = 1, Name = "Test Supplier" };
		_viewModel.SelectedItem = supplier;

		// Act
		_viewModel.AddContactCommand.Execute( null );

		// Assert
		Assert.IsTrue( _viewModel.FilteredContacts.Count > 0 );
		Assert.AreEqual( 1, _viewModel.FilteredContacts [ 0 ].SupplierId );
	}

	[TestMethod]
	public void AddContactCommand_WithNoSelectedSupplier_DoesNotAddContact()
	{
		// Arrange
		_viewModel.SelectedItem = null;
		var initialCount = _viewModel.FilteredContacts.Count;

		// Act
		_viewModel.AddContactCommand.Execute( null );

		// Assert
		Assert.AreEqual( initialCount, _viewModel.FilteredContacts.Count );
	}

	[TestMethod]
	public void DeleteContactCommand_RemovesContact()
	{
		// Arrange
		var supplier = new SupplierModel { Id = 1 };
		var contact = new SupplierContactModel { SupplierContactId = 1, SupplierId = 1 };
		_viewModel.SelectedItem = supplier;
		_viewModel.Contacts.Add( contact );
		_viewModel.FilteredContacts.Add( contact );
		_viewModel.SelectedContact = contact;

		// Act
		_viewModel.DeleteContactCommand.Execute( null );

		// Assert
		Assert.AreEqual( 0, _viewModel.FilteredContacts.Count );
		Assert.AreEqual( 0, _viewModel.Contacts.Count );
	}

	[TestMethod]
	public void OpenWebsiteCommand_CanExecute_WithValidUrl()
	{
		// Arrange
		var supplier = new SupplierModel { Id = 1, Url = "https://example.com" };
		_viewModel.SelectedItem = supplier;

		// Act
		var canExecute = _viewModel.OpenWebsiteCommand.CanExecute(null);

		// Assert
		Assert.IsTrue( canExecute );
	}

	[TestMethod]
	public void OpenWebsiteCommand_CannotExecute_WithNullUrl()
	{
		// Arrange
		var supplier = new SupplierModel { Id = 1, Url = null };
		_viewModel.SelectedItem = supplier;

		// Act
		var canExecute = _viewModel.OpenWebsiteCommand.CanExecute(null);

		// Assert
		Assert.IsFalse( canExecute );
	}

	[TestMethod]
	public void OpenWebsiteCommand_CannotExecute_WithEmptyUrl()
	{
		// Arrange
		var supplier = new SupplierModel { Id = 1, Url = "" };
		_viewModel.SelectedItem = supplier;

		// Act
		var canExecute = _viewModel.OpenWebsiteCommand.CanExecute(null);

		// Assert
		Assert.IsFalse( canExecute );
	}

	[TestMethod]
	public void SelectedContact_UpdatesSelectedContactFunction()
	{
		// Arrange
		var contactType = new ContactTypeModel { ContactTypeId = 5, ContactTypeName = "Manager" };
		_viewModel.SupplierContactFunctions.Add( contactType );

		var contact = new SupplierContactModel
		{
			SupplierContactId = 1,
			ContactTypeId = 5
		};

		// Act
		_viewModel.SelectedContact = contact;

		// Assert
		Assert.AreEqual( contactType, _viewModel.SelectedContactFunction );
	}

	[TestMethod]
	public void OnSelectedItemChanged_UpdatesFilteredContacts()
	{
		// Arrange
		var supplier1 = new SupplierModel { Id = 1, Name = "Supplier 1" };
		var supplier2 = new SupplierModel { Id = 2, Name = "Supplier 2" };

		var contact1 = new SupplierContactModel { SupplierContactId = 1, SupplierId = 1 };
		var contact2 = new SupplierContactModel { SupplierContactId = 2, SupplierId = 2 };

		_viewModel.Contacts.Add( contact1 );
		_viewModel.Contacts.Add( contact2 );

		// Act
		_viewModel.SelectedItem = supplier1;

		// Assert
		Assert.AreEqual( 1, _viewModel.FilteredContacts.Count );
		Assert.AreEqual( contact1, _viewModel.FilteredContacts [ 0 ] );
	}

	[TestMethod]
	public void SearchText_PropertyChangedRaisesNotification()
	{
		// Arrange
		var propertyChanged = false;
		_viewModel.PropertyChanged += ( s, e ) =>
		{
			if ( e.PropertyName == nameof( _viewModel.SearchText ) )
				propertyChanged = true;
		};

		// Act
		_viewModel.SearchText = "Test";

		// Assert
		Assert.IsTrue( propertyChanged );
	}

	[TestMethod]
	public void SupplierPageViewModel_LoadComboBoxesStartsIndependentServiceCallsBeforeAwaiting()
	{
		var source = LoadSource( "Modelbouwer", "ViewModels", "SupplierPageViewModel.cs" );

		AssertMethodContains( source, "private async Task LoadComboBoxesContentAsync()", "var countriesTask = _countryService.GetAllCountriesAsync();" );
		AssertMethodContains( source, "private async Task LoadComboBoxesContentAsync()", "var currenciesTask = _currencyService.GetAllCurrenciesAsync();" );
		AssertMethodContains( source, "private async Task LoadComboBoxesContentAsync()", "await Task.WhenAll( countriesTask, currenciesTask );" );
	}

	[TestMethod]
	public void SupplierPageViewModel_LoadContactsAndFunctionsStartsIndependentServiceCallsBeforeAwaiting()
	{
		var source = LoadSource( "Modelbouwer", "ViewModels", "SupplierPageViewModel.cs" );

		AssertMethodContains( source, "private async Task LoadContactsAndFunctionsAsync()", "var contactTypesTask = _contactTypeService.GetAllContactTypesAsync();" );
		AssertMethodContains( source, "private async Task LoadContactsAndFunctionsAsync()", "var contactsTask = _contactService.GetAllContactsAsync();" );
		AssertMethodContains( source, "private async Task LoadContactsAndFunctionsAsync()", "await Task.WhenAll( contactTypesTask, contactsTask );" );
	}

	private static SupplierPageViewModel CreateViewModel(
		ISupplierService? supplierService = null,
		ICountryService? countryService = null,
		ICurrencyService? currencyService = null,
		IContactService? contactService = null,
		IContactTypeService? contactTypeService = null )
	{
		var defaultSupplierService = new Mock<ISupplierService>();
		var defaultCountryService = new Mock<ICountryService>();
		var defaultCurrencyService = new Mock<ICurrencyService>();
		var defaultContactService = new Mock<IContactService>();
		var defaultContactTypeService = new Mock<IContactTypeService>();
		var validator = new Mock<IEntityValidator<SupplierModel>>();

		defaultSupplierService.Setup( service => service.GetAllSuppliersAsync() ).ReturnsAsync( [ ] );
		defaultCountryService.Setup( service => service.GetAllCountriesAsync() ).ReturnsAsync( [ ] );
		defaultCurrencyService.Setup( service => service.GetAllCurrenciesAsync() ).ReturnsAsync( [ ] );
		defaultContactService.Setup( service => service.GetAllContactsAsync() ).ReturnsAsync( [ ] );
		defaultContactTypeService.Setup( service => service.GetAllContactTypesAsync() ).ReturnsAsync( [ ] );

		return new SupplierPageViewModel(
			supplierService ?? defaultSupplierService.Object,
			countryService ?? defaultCountryService.Object,
			currencyService ?? defaultCurrencyService.Object,
			contactService ?? defaultContactService.Object,
			contactTypeService ?? defaultContactTypeService.Object,
			DefaultStockOrderService(),
			validator.Object );
	}

	private static IStockOrderService DefaultStockOrderService()
	{
		var stockOrderService = new Mock<IStockOrderService>();
		stockOrderService.Setup( s => s.GetAllOrdersAsync() ).ReturnsAsync( new List<StockOrderModel>() );
		stockOrderService.Setup( s => s.GetOrderLinesAsync( It.IsAny<int>() ) ).ReturnsAsync( new List<StockOrderLineModel>() );
		return stockOrderService.Object;
	}

	private static string LoadSource( params string [ ] relativeSegments )
	{
		var directory = AppContext.BaseDirectory;
		while ( directory != null && !File.Exists( Path.Combine( directory, "ModelbouwWerkbank.slnx" ) ) )
		{
			directory = Directory.GetParent( directory )?.FullName;
		}

		var repositoryRoot = directory ?? throw new DirectoryNotFoundException( "Could not locate repository root." );
		var path = Path.Combine( [ repositoryRoot, .. relativeSegments ] );

		return File.ReadAllText( path );
	}

	private static void AssertMethodContains( string source, string methodSignature, string expectedContent )
	{
		var methodStart = source.IndexOf( methodSignature, StringComparison.Ordinal );
		Assert.IsTrue( methodStart >= 0, $"Method '{methodSignature}' was not found." );

		var nextMethod = source.IndexOf( "\n\tprivate ", methodStart + methodSignature.Length, StringComparison.Ordinal );
		if ( nextMethod < 0 )
			nextMethod = source.Length;

		var methodBody = source.Substring( methodStart, nextMethod - methodStart );
		StringAssert.Contains( methodBody, expectedContent );
	}

	private static async Task WaitUntilAsync( Func<bool> condition )
	{
		using var timeout = new CancellationTokenSource( TimeSpan.FromSeconds( 2 ) );

		while ( !condition() )
		{
			if ( timeout.IsCancellationRequested )
				Assert.Fail( "Condition was not met before timeout." );

			await Task.Delay( 10 );
		}
	}
}