using Modelbouwer.Model;

namespace Modelbouwer.UnitTests.ViewModels;

/// <summary>Order screen (KAN-103): everything stays on screen until Save, which stores it in one transaction.</summary>
[TestClass]
public class StockOrderViewModelTests
{
	private Mock<IStockOrderService> _orders = null!;
	private Mock<IProductService> _products = null!;
	private Mock<IStockService> _stock = null!;
	private Mock<ISupplierService> _suppliers = null!;
	private StockOrderViewModel _vm = null!;
	private List<string> _messages = null!;
	private bool _confirmAnswer;

	private static readonly SupplierModel Cornwall = new() { Id = 1, Name = "Cornwall", CurrencyId = 2, ShippingCosts = 7.5, OrderCosts = 1 };
	private static readonly SupplierModel Other = new() { Id = 2, Name = "Other", CurrencyId = 1 };

	// Product 5: minimum 10, stock 6, nothing on order, order multiple 3 -> shortage 4, suggestion 6.
	// Product 8: minimum 2, stock 5 -> no shortage.
	private static ProductModel Product5() => new() { ProductId = 5, ProductCode = "P-005", ProductName = "Walnut dowel 3 mm", ProductPrice = 1.5, ProductMinimalStock = 10, ProductStandardQuantity = 3 };
	private static ProductModel Product8() => new() { ProductId = 8, ProductCode = "P-008", ProductName = "Brass rod", ProductPrice = 2, ProductMinimalStock = 2 };

	[TestInitialize]
	public void Setup()
	{
		_orders = new Mock<IStockOrderService>();
		_products = new Mock<IProductService>();
		_stock = new Mock<IStockService>();
		_suppliers = new Mock<ISupplierService>();
		_messages = [];
		_confirmAnswer = true;

		_orders.Setup( s => s.GetAllOrdersAsync() ).ReturnsAsync( new List<StockOrderModel>() );
		_orders.Setup( s => s.GetOrderLinesAsync( It.IsAny<int>() ) ).ReturnsAsync( new List<StockOrderLineModel>() );
		_orders.Setup( s => s.SaveOrderAsync( It.IsAny<StockOrderModel>(), It.IsAny<IEnumerable<StockOrderLineModel>>(), It.IsAny<IEnumerable<int>>(), It.IsAny<IEnumerable<ProductSupplierModel>>() ) ).ReturnsAsync( 25 );
		_products.Setup( s => s.GetAllProductsAsync() ).ReturnsAsync( () => new List<ProductModel> { Product5(), Product8() } );
		_stock.Setup( s => s.GetCompleteInventoryAsync() ).ReturnsAsync( () => new List<StockManagementModel>
		{
			new() { ProductId = 5, ProductInventory = 6, ProductInOrder = 0, ProductMinimalStock = 10 },
			new() { ProductId = 8, ProductInventory = 5, ProductInOrder = 0, ProductMinimalStock = 2 }
		} );
		_suppliers.Setup( s => s.GetAllSuppliersAsync() ).ReturnsAsync( new List<SupplierModel> { Cornwall, Other } );
		_suppliers.Setup( s => s.GetAllCurrenciesAsync() ).ReturnsAsync( new List<CurrencyModel>() );
		_suppliers.Setup( s => s.GetAllProductSuppliersAsync() ).ReturnsAsync( () => new List<ProductSupplierModel>
		{
			new() { ProductSupplierId = 70, SupplierId = 1, ProductId = 5, ProductNumber = "CW-DOW3", ProductName = "Walnut Dowel 3mm", Price = 1.25 }
		} );
	}

	private async Task CreateAsync()
	{
		_vm = new StockOrderViewModel( _orders.Object, _products.Object, _stock.Object, _suppliers.Object )
		{
			ShowMessage = message => _messages.Add( message ),
			ConfirmAction = _ => _confirmAnswer
		};
		await _vm.InitializeAsync();
	}

	private StockOrderProductRowModel Row( int productId ) => _vm.ProductRows.Single( row => row.ProductId == productId );

	private async Task SelectSupplierAndAddAsync( int productId )
	{
		_vm.EditableOrder.SupplierId = 1;
		_vm.SelectedProductRow = Row( productId );
		await _vm.AddSelectedProductAsync();
	}

	#region Start and header
	[TestMethod]
	public async Task Constructor_StartsWithEmptyNewOrderWithoutChanges()
	{
		await CreateAsync();

		Assert.IsTrue( _vm.IsNewOrder );
		Assert.AreEqual( 0, _vm.EditableOrder.Id );
		Assert.AreEqual( DateTime.Today, _vm.EditableOrder.OrderDate );
		Assert.AreEqual( 0, _vm.OrderLines.Count );
		Assert.IsFalse( _vm.HasUnsavedChanges );
	}

	[TestMethod]
	public async Task SelectingSupplier_FillsCurrencyAndCostsButKeepsManualCosts()
	{
		await CreateAsync();

		_vm.EditableOrder.ShippingCosts = 3;
		_vm.EditableOrder.SupplierId = 1;

		Assert.AreEqual( 2, _vm.EditableOrder.CurrencyId );
		Assert.AreEqual( 3d, _vm.EditableOrder.ShippingCosts, "Manually entered costs stay." );
		Assert.AreEqual( 1d, _vm.EditableOrder.OrderCosts );
	}

	[TestMethod]
	public async Task ValidateOrderForSave_RequiresSupplierButNotOrderNumber()
	{
		await CreateAsync();

		Assert.AreEqual( Lang.StockOrderSupplierRequired, _vm.ValidateOrderForSave() );

		_vm.EditableOrder.SupplierId = 1;
		_vm.EditableOrder.OrderNumber = null;

		Assert.IsNull( _vm.ValidateOrderForSave(), "A concept order without order number can be saved." );
	}

	[TestMethod]
	public async Task Supplier_IsLockedOnceTheOrderHasLines()
	{
		await CreateAsync();
		Assert.IsTrue( _vm.CanChangeSupplier );

		await SelectSupplierAndAddAsync( 5 );

		Assert.IsFalse( _vm.CanChangeSupplier );
	}
	#endregion

	#region Product list
	[TestMethod]
	public async Task ProductRows_UseStockOverviewShortageRulesAndOrderMultiple()
	{
		await CreateAsync();

		Assert.AreEqual( 4d, Row( 5 ).ShortInventory );
		Assert.AreEqual( 6d, Row( 5 ).SuggestedAmount, "Shortage 4 rounded up to multiple 3." );
		Assert.AreEqual( 0d, Row( 8 ).ShortInventory );
		Assert.AreEqual( 1d, Row( 8 ).SuggestedAmount, "No shortage and no multiple: 1." );
	}

	[TestMethod]
	public async Task SelectingSupplier_GroupsProductsAndShowsSupplierArticleNumber()
	{
		await CreateAsync();

		_vm.EditableOrder.SupplierId = 1;

		Assert.IsTrue( Row( 5 ).IsFromSelectedSupplier );
		Assert.AreEqual( "CW-DOW3", Row( 5 ).SupplierProductNumber );
		Assert.AreEqual( Lang.StockOrderGroupSupplierProducts, Row( 5 ).GroupName );
		Assert.IsFalse( Row( 8 ).IsFromSelectedSupplier );
		Assert.AreEqual( Lang.StockOrderGroupOtherProducts, Row( 8 ).GroupName );
	}

	[TestMethod]
	public async Task Filters_SearchAlsoOnSupplierNumberAndShortagesOnly()
	{
		await CreateAsync();
		_vm.EditableOrder.SupplierId = 1;

		_vm.SearchText = "cw-dow";
		Assert.AreEqual( 5, _vm.ProductRows.Single().ProductId );

		_vm.SearchText = string.Empty;
		_vm.ShowOnlyShortages = true;
		Assert.AreEqual( 5, _vm.ProductRows.Single().ProductId );
	}
	#endregion

	#region Adding products
	[TestMethod]
	public async Task AddProduct_WithoutSupplier_ShowsMessageAndAddsNothing()
	{
		await CreateAsync();
		_vm.SelectedProductRow = Row( 5 );

		await _vm.AddSelectedProductAsync();

		Assert.AreEqual( 0, _vm.OrderLines.Count );
		CollectionAssert.Contains( _messages, Lang.StockOrderSelectSupplierFirst );
	}

	[TestMethod]
	public async Task AddProduct_WithKnownSupplierData_AddsLineWithoutPopupAndUpdatesShortage()
	{
		await CreateAsync();
		bool dialogShown = false;
		_vm.ShowSupplierProductDialog = _ => dialogShown = true;

		await SelectSupplierAndAddAsync( 5 );

		Assert.IsFalse( dialogShown );
		var line = _vm.OrderLines.Single();
		Assert.AreEqual( "CW-DOW3", line.SupplierProductNumber );
		Assert.AreEqual( "Walnut Dowel 3mm", line.SupplierProductName );
		Assert.AreEqual( 6d, line.Amount );
		Assert.AreEqual( 1.25, line.Price, "Supplier price wins over product price." );
		Assert.AreEqual( 7.5, line.RealRowTotal );
		Assert.AreEqual( 0d, Row( 5 ).ShortInventory, "The unsaved line counts as on order." );
		Assert.IsTrue( _vm.HasUnsavedChanges );
	}

	[TestMethod]
	public async Task AddProduct_WithoutSupplierData_AsksAndCancelAddsNothing()
	{
		await CreateAsync();
		_vm.ShowSupplierProductDialog = _ => false;

		await SelectSupplierAndAddAsync( 8 );

		Assert.AreEqual( 0, _vm.OrderLines.Count );
	}

	[TestMethod]
	public async Task AddProduct_WithoutSupplierData_UsesPopupDataAndSavesItWithTheOrder()
	{
		await CreateAsync();
		_vm.ShowSupplierProductDialog = dialog =>
		{
			Assert.AreEqual( "Brass rod", dialog.Model.SupplierProductName, "Description is prefilled with the own product name." );
			dialog.Model.SupplierProductNumber = "CW-BR2";
			dialog.Model.UnitPrice = 2.4;
			return true;
		};

		await SelectSupplierAndAddAsync( 8 );
		_vm.EditableOrder.OrderNumber = "CW0200";
		await _vm.SaveOrderAsync();

		_orders.Verify( s => s.SaveOrderAsync(
			It.Is<StockOrderModel>( o => o.SupplierId == 1 && o.OrderNumber == "CW0200" ),
			It.Is<IEnumerable<StockOrderLineModel>>( lines => lines.Single().ProductId == 8 && lines.Single().Price == 2.4 ),
			It.Is<IEnumerable<int>>( ids => !ids.Any() ),
			It.Is<IEnumerable<ProductSupplierModel>>( ps => ps.Single().ProductId == 8 && ps.Single().ProductNumber == "CW-BR2" && ps.Single().SupplierId == 1 ) ),
			Times.Once );
	}

	[TestMethod]
	public async Task AddProduct_Twice_RaisesTheExistingLine()
	{
		await CreateAsync();

		await SelectSupplierAndAddAsync( 5 );
		_vm.SelectedProductRow = Row( 5 );
		await _vm.AddSelectedProductAsync();

		Assert.AreEqual( 1, _vm.OrderLines.Count );
		Assert.IsTrue( _vm.OrderLines [ 0 ].Amount > 6d );
	}

	[TestMethod]
	public async Task AddShortages_AddsOnlyShortProductsOfThisSupplier()
	{
		await CreateAsync();
		_vm.EditableOrder.SupplierId = 1;

		await _vm.AddShortagesAsync();

		Assert.AreEqual( 5, _vm.OrderLines.Single().ProductId );
		Assert.AreEqual( 6d, _vm.OrderLines.Single().Amount );

		await _vm.AddShortagesAsync();
		CollectionAssert.Contains( _messages, Lang.StockOrderNoShortages, "Nothing left to add the second time." );
		Assert.AreEqual( 1, _vm.OrderLines.Count );
	}
	#endregion

	#region Editing lines in the grid
	private static StockOrderLineModel SavedLine( double amount, double received ) => new()
	{
		Id = 40,
		SupplyOrderId = 25,
		SupplierId = 1,
		ProductId = 5,
		ProductCode = "P-005",
		ProductName = "Walnut dowel 3 mm",
		SupplierProductNumber = "CW-DOW3",
		SupplierProductName = "Walnut Dowel 3mm",
		Amount = amount,
		Received = received,
		OpenAmount = amount - received,
		Price = 1.25,
		RealRowTotal = 1.25 * amount,
		Closed = amount - received <= 0
	};

	private async Task LoadSavedOrderAsync( params StockOrderLineModel [ ] lines )
	{
		await CreateAsync();
		_vm.ApplySelectedOrder( new StockOrderModel { Id = 25, SupplierId = 1, SupplierName = "Cornwall", OrderNumber = "CW0132", OrderDate = DateTime.Today }, lines );
	}

	[TestMethod]
	public async Task EditAmount_RecalculatesOpenAmountTotalAndMarksChanged()
	{
		var line = SavedLine( 4, 0 );
		await LoadSavedOrderAsync( line );

		line.Amount = 10;

		Assert.AreEqual( 10d, line.OpenAmount );
		Assert.AreEqual( 12.5, line.RealRowTotal );
		Assert.AreEqual( 12.5, _vm.EditableOrder.LinesTotal );
		Assert.IsTrue( line.IsDirty );
		Assert.IsTrue( _vm.HasUnsavedChanges );
	}

	[TestMethod]
	public async Task EditAmount_BelowReceived_IsResetToReceived()
	{
		var line = SavedLine( 4, 2 );
		await LoadSavedOrderAsync( line );

		line.Amount = 1;

		Assert.AreEqual( 2d, line.Amount );
		Assert.AreEqual( 0d, line.OpenAmount );
		Assert.AreEqual( 1, _messages.Count );
	}

	[TestMethod]
	public async Task EditPrice_BecomesTheNewSupplierPriceOnSave()
	{
		var line = SavedLine( 4, 0 );
		await LoadSavedOrderAsync( line );

		line.Price = 1.4;
		await _vm.SaveOrderAsync();

		_orders.Verify( s => s.SaveOrderAsync(
			It.IsAny<StockOrderModel>(),
			It.Is<IEnumerable<StockOrderLineModel>>( lines => lines.Single().RealRowTotal == 5.6 ),
			It.IsAny<IEnumerable<int>>(),
			It.Is<IEnumerable<ProductSupplierModel>>( ps => ps.Single().ProductSupplierId == 70 && ps.Single().Price == 1.4 ) ),
			Times.Once );
	}

	[TestMethod]
	public async Task DeleteLine_WithReceipts_IsRefused()
	{
		var line = SavedLine( 4, 2 );
		await LoadSavedOrderAsync( line );
		_vm.SelectedOrderLine = line;

		_vm.DeleteSelectedOrderLine();

		Assert.AreEqual( 1, _vm.OrderLines.Count );
		CollectionAssert.Contains( _messages, Lang.StockOrderLineHasReceipts );
	}

	[TestMethod]
	public async Task DeleteLine_SavedLine_IsDeletedOnSave()
	{
		var line = SavedLine( 4, 0 );
		await LoadSavedOrderAsync( line );
		_vm.SelectedOrderLine = line;

		_vm.DeleteSelectedOrderLine();
		await _vm.SaveOrderAsync();

		_orders.Verify( s => s.SaveOrderAsync(
			It.IsAny<StockOrderModel>(),
			It.Is<IEnumerable<StockOrderLineModel>>( lines => !lines.Any() ),
			It.Is<IEnumerable<int>>( ids => ids.Single() == 40 ),
			It.IsAny<IEnumerable<ProductSupplierModel>>() ),
			Times.Once );
	}
	#endregion

	#region Save, switch and delete
	[TestMethod]
	public async Task Save_ReloadsTheSavedOrderAndClearsChanges()
	{
		var saved = new StockOrderModel { Id = 25, SupplierId = 1, SupplierName = "Cornwall", OrderNumber = "CW0132" };
		_orders.Setup( s => s.GetAllOrdersAsync() ).ReturnsAsync( new List<StockOrderModel> { saved } );
		_orders.Setup( s => s.GetOrderLinesAsync( 25 ) ).ReturnsAsync( new List<StockOrderLineModel> { SavedLine( 6, 0 ) } );
		await CreateAsync();
		await SelectSupplierAndAddAsync( 5 );

		await _vm.SaveOrderAsync();

		Assert.IsFalse( _vm.IsNewOrder );
		Assert.IsFalse( _vm.HasUnsavedChanges );
		Assert.AreSame( saved, _vm.SelectedOrder );
		Assert.AreEqual( 40, _vm.OrderLines.Single().Id );
		_products.Verify( s => s.GetAllProductsAsync(), Times.AtLeast( 2 ), "Stock figures are reloaded after saving." );
	}

	[TestMethod]
	public async Task SwitchingOrder_WithUnsavedChanges_AndNotConfirmed_KeepsTheChanges()
	{
		await CreateAsync();
		await SelectSupplierAndAddAsync( 5 );
		_confirmAnswer = false;

		_vm.SelectedOrder = new StockOrderModel { Id = 30, OrderNumber = "OTHER" };

		Assert.AreEqual( 1, _vm.OrderLines.Count );
		Assert.IsTrue( _vm.HasUnsavedChanges );
		_orders.Verify( s => s.GetOrderLinesAsync( 30 ), Times.Never );
	}

	[TestMethod]
	public async Task NewOrder_WithUnsavedChanges_AndConfirmed_StartsEmpty()
	{
		await CreateAsync();
		await SelectSupplierAndAddAsync( 5 );

		_vm.NewOrderCommand.Execute( null );

		Assert.AreEqual( 0, _vm.OrderLines.Count );
		Assert.IsFalse( _vm.HasUnsavedChanges );
		Assert.AreEqual( 0d, Row( 5 ).PendingInOrder );
	}

	[TestMethod]
	public async Task DeleteOrder_WithReceipts_IsRefused()
	{
		await LoadSavedOrderAsync( SavedLine( 4, 2 ) );

		await _vm.DeleteOrderCommand.ExecuteAsync( null );

		CollectionAssert.Contains( _messages, Lang.StockOrderOrderHasReceipts );
		_orders.Verify( s => s.DeleteOrderWithLinesAsync( It.IsAny<int>(), It.IsAny<IEnumerable<StockOrderLineModel>>() ), Times.Never );
	}

	[TestMethod]
	public async Task DeleteOrder_WhenServiceFindsReceipts_ShowsMessageAndKeepsTheOrder()
	{
		// A receipt booked elsewhere after the order was loaded: the screen still shows no receipts.
		await LoadSavedOrderAsync( SavedLine( 4, 0 ) );
		_orders.Setup( s => s.GetAllOrdersAsync() ).ReturnsAsync( new List<StockOrderModel> { new() { Id = 25, SupplierId = 1, SupplierName = "Cornwall", OrderDate = DateTime.Today } } );
		_orders.SetupSequence( s => s.GetOrderLinesAsync( 25 ) )
			.ReturnsAsync( new List<StockOrderLineModel> { SavedLine( 4, 0 ) } )    // read by the delete
			.ReturnsAsync( new List<StockOrderLineModel> { SavedLine( 4, 2 ) } );   // stored state after the receipt
		_orders.Setup( s => s.DeleteOrderWithLinesAsync( 25, It.IsAny<IEnumerable<StockOrderLineModel>>() ) )
			.ThrowsAsync( new OrderHasReceiptsException( 25 ) );

		await _vm.DeleteOrderCommand.ExecuteAsync( null );

		CollectionAssert.Contains( _messages, Lang.StockOrderOrderHasReceipts );
		Assert.AreEqual( 25, _vm.EditableOrder.Id, "The order stays on screen." );
		Assert.AreEqual( 2d, _vm.OrderLines.Single().Received, "The stored receipt is reloaded, so a later save cannot overwrite it." );
		Assert.IsFalse( _vm.IsWorking );
	}

	[TestMethod]
	public async Task DeleteOrder_RefusedAndReloadFails_SaveOnlyRetriesTheReload()
	{
		var line = SavedLine( 4, 0 );
		await LoadSavedOrderAsync( line );
		line.Price = 1.4;   // a draft change on the order that was about to be deleted
		bool failOrderList = true;
		_orders.Setup( s => s.GetAllOrdersAsync() ).Returns( () => failOrderList
			? Task.FromException<List<StockOrderModel>>( new InvalidOperationException( "connection lost" ) )
			: Task.FromResult( new List<StockOrderModel> { new() { Id = 25, SupplierId = 1, SupplierName = "Cornwall", OrderDate = DateTime.Today } } ) );
		_orders.Setup( s => s.GetOrderLinesAsync( 25 ) ).ReturnsAsync( () => new List<StockOrderLineModel> { SavedLine( 4, 2 ) } );
		_orders.Setup( s => s.DeleteOrderWithLinesAsync( 25, It.IsAny<IEnumerable<StockOrderLineModel>>() ) )
			.ThrowsAsync( new OrderHasReceiptsException( 25 ) );

		await AssertThrowsAsync<InvalidOperationException>( () => _vm.DeleteOrderCommand.ExecuteAsync( null ) );
		Assert.IsFalse( _vm.IsWorking );

		failOrderList = false;
		await _vm.SaveOrderAsync();

		_orders.Verify( s => s.SaveOrderAsync( It.IsAny<StockOrderModel>(), It.IsAny<IEnumerable<StockOrderLineModel>>(), It.IsAny<IEnumerable<int>>(), It.IsAny<IEnumerable<ProductSupplierModel>>() ), Times.Never,
			"The stale editor is never written back." );
		Assert.AreEqual( 2d, _vm.OrderLines.Single().Received, "Save reloaded the stored order instead." );
	}

	[TestMethod]
	public async Task DeleteOrder_Confirmed_DeletesAndStartsNewOrder()
	{
		await LoadSavedOrderAsync( SavedLine( 4, 0 ) );
		_orders.Setup( s => s.GetOrderLinesAsync( 25 ) ).ReturnsAsync( new List<StockOrderLineModel> { SavedLine( 4, 0 ) } );
		_orders.Setup( s => s.DeleteOrderWithLinesAsync( 25, It.IsAny<IEnumerable<StockOrderLineModel>>() ) ).Returns( Task.CompletedTask );

		await _vm.DeleteOrderCommand.ExecuteAsync( null );

		_orders.Verify( s => s.DeleteOrderWithLinesAsync( 25, It.IsAny<IEnumerable<StockOrderLineModel>>() ), Times.Once );
		Assert.IsTrue( _vm.IsNewOrder );
	}

	// Reopen a closed order (KAN-95)
	private async Task LoadClosedOrderAsync()
	{
		await CreateAsync();
		_vm.ApplySelectedOrder( new StockOrderModel { Id = 25, SupplierId = 1, SupplierName = "Cornwall", OrderNumber = "CW0132", OrderDate = DateTime.Today, Closed = true, ClosedDate = DateTime.Today }, [ SavedLine( 4, 4 ) ] );
	}

	private void SetupStoredOrder( bool closed )
	{
		_orders.Setup( s => s.GetAllOrdersAsync() ).ReturnsAsync( () => new List<StockOrderModel>
		{
			new() { Id = 25, SupplierId = 1, SupplierName = "Cornwall", OrderNumber = "CW0132", OrderDate = DateTime.Today, Closed = closed, ClosedDate = closed ? DateTime.Today : null }
		} );
		_orders.Setup( s => s.GetOrderLinesAsync( 25 ) ).ReturnsAsync( () => new List<StockOrderLineModel> { SavedLine( 4, 4 ) } );
	}

	[TestMethod]
	public async Task ReopenOrder_OnlyAvailableForAStoredClosedOrder()
	{
		await CreateAsync();
		Assert.IsFalse( _vm.ReopenOrderCommand.CanExecute( null ), "A new order cannot be reopened." );

		_vm.ApplySelectedOrder( new StockOrderModel { Id = 25, SupplierId = 1, OrderDate = DateTime.Today }, [ SavedLine( 4, 0 ) ] );
		Assert.IsFalse( _vm.ReopenOrderCommand.CanExecute( null ), "An open order is already open." );

		_vm.ApplySelectedOrder( new StockOrderModel { Id = 26, SupplierId = 1, OrderDate = DateTime.Today, Closed = true }, [ SavedLine( 4, 4 ) ] );
		Assert.IsTrue( _vm.ReopenOrderCommand.CanExecute( null ) );
		Assert.IsFalse( _vm.CanEditOrder );
	}

	[TestMethod]
	public async Task ReopenOrder_Confirmed_OpensOrderAndKeepsReceivedLinesClosed()
	{
		await LoadClosedOrderAsync();
		SetupStoredOrder( closed: false );
		_orders.Setup( s => s.ReopenOrderAsync( 25 ) ).Returns( Task.CompletedTask );

		await _vm.ReopenOrderCommand.ExecuteAsync( null );

		_orders.Verify( s => s.ReopenOrderAsync( 25 ), Times.Once );
		_orders.Verify( s => s.SaveOrderAsync( It.IsAny<StockOrderModel>(), It.IsAny<IEnumerable<StockOrderLineModel>>(), It.IsAny<IEnumerable<int>>(), It.IsAny<IEnumerable<ProductSupplierModel>>() ), Times.Never,
			"Reopening writes only the order header, not the lines." );
		Assert.IsFalse( _vm.EditableOrder.Closed );
		Assert.IsNull( _vm.EditableOrder.ClosedDate );
		Assert.IsTrue( _vm.CanEditOrder, "Lines can be added again." );
		Assert.IsTrue( _vm.OrderLines.Single().Closed, "The received line stays closed." );
		Assert.AreEqual( 4d, _vm.OrderLines.Single().Received );
		Assert.IsFalse( _vm.ReopenOrderCommand.CanExecute( null ) );
		Assert.IsFalse( _vm.IsWorking );
		Assert.IsFalse( _vm.HasUnsavedChanges );
	}

	[TestMethod]
	public async Task ReopenOrder_NotConfirmed_DoesNothing()
	{
		await LoadClosedOrderAsync();
		_confirmAnswer = false;

		await _vm.ReopenOrderCommand.ExecuteAsync( null );

		_orders.Verify( s => s.ReopenOrderAsync( It.IsAny<int>() ), Times.Never );
		Assert.IsTrue( _vm.EditableOrder.Closed );
	}

	[TestMethod]
	public async Task ReopenOrder_WhenServiceFails_OrderStaysClosedAndScreenIsReleased()
	{
		await LoadClosedOrderAsync();
		_orders.Setup( s => s.ReopenOrderAsync( 25 ) ).ThrowsAsync( new InvalidOperationException( "connection lost" ) );

		await AssertThrowsAsync<InvalidOperationException>( () => _vm.ReopenOrderCommand.ExecuteAsync( null ) );

		Assert.IsTrue( _vm.EditableOrder.Closed );
		Assert.IsFalse( _vm.IsWorking );
		Assert.IsTrue( _vm.ReopenOrderCommand.CanExecute( null ), "The user can try again." );
	}

	[TestMethod]
	public async Task ReopenOrder_WhenReloadFails_SaveOnlyRetriesTheReload()
	{
		await LoadClosedOrderAsync();
		_orders.Setup( s => s.ReopenOrderAsync( 25 ) ).Returns( Task.CompletedTask );
		_orders.Setup( s => s.GetAllOrdersAsync() ).ThrowsAsync( new InvalidOperationException( "connection lost" ) );

		await AssertThrowsAsync<InvalidOperationException>( () => _vm.ReopenOrderCommand.ExecuteAsync( null ) );
		Assert.IsFalse( _vm.IsWorking );

		SetupStoredOrder( closed: false );
		await _vm.SaveOrderAsync();

		_orders.Verify( s => s.SaveOrderAsync( It.IsAny<StockOrderModel>(), It.IsAny<IEnumerable<StockOrderLineModel>>(), It.IsAny<IEnumerable<int>>(), It.IsAny<IEnumerable<ProductSupplierModel>>() ), Times.Never );
		Assert.IsFalse( _vm.EditableOrder.Closed, "Save reloaded the reopened order." );
	}

	// Close an order by hand (KAN-95)
	[TestMethod]
	public async Task CloseOrder_OnlyAvailableForAStoredOpenOrder()
	{
		await CreateAsync();
		Assert.IsFalse( _vm.CloseOrderCommand.CanExecute( null ), "A new order cannot be closed." );

		await LoadClosedOrderAsync();
		Assert.IsFalse( _vm.CloseOrderCommand.CanExecute( null ), "A closed order is already closed." );

		await LoadSavedOrderAsync( SavedLine( 4, 0 ) );
		Assert.IsTrue( _vm.CloseOrderCommand.CanExecute( null ) );
	}

	[TestMethod]
	public async Task CloseOrder_WithOpenLines_AsksWithLineCountAndClosesTheOrder()
	{
		await LoadSavedOrderAsync( SavedLine( 4, 1 ) );
		string? asked = null;
		_vm.ConfirmAction = message => { asked = message; return true; };
		var closedLine = SavedLine( 1, 1 );
		_orders.Setup( s => s.CloseOrderAsync( 25, DateTime.Today ) ).Returns( Task.CompletedTask );
		_orders.Setup( s => s.GetAllOrdersAsync() ).ReturnsAsync( () => new List<StockOrderModel>
		{
			new() { Id = 25, SupplierId = 1, SupplierName = "Cornwall", OrderNumber = "CW0132", OrderDate = DateTime.Today, Closed = true, ClosedDate = DateTime.Today }
		} );
		_orders.Setup( s => s.GetOrderLinesAsync( 25 ) ).ReturnsAsync( () => new List<StockOrderLineModel> { closedLine } );

		await _vm.CloseOrderCommand.ExecuteAsync( null );

		Assert.AreEqual( string.Format( Lang.StockOrderCloseOrderWithOpenLinesConfirm, "CW0132", "Cornwall", 1 ), asked );
		_orders.Verify( s => s.CloseOrderAsync( 25, DateTime.Today ), Times.Once );
		Assert.IsTrue( _vm.EditableOrder.Closed );
		Assert.IsFalse( _vm.CanEditOrder );
		Assert.IsTrue( _vm.ReopenOrderCommand.CanExecute( null ), "A closed order can be reopened again." );
		Assert.IsFalse( _vm.IsWorking );
	}

	[TestMethod]
	public async Task CloseOrder_WithoutOpenLines_AsksSimpleQuestion()
	{
		await LoadSavedOrderAsync( SavedLine( 4, 4 ) );
		string? asked = null;
		_vm.ConfirmAction = message => { asked = message; return false; };

		await _vm.CloseOrderCommand.ExecuteAsync( null );

		Assert.AreEqual( string.Format( Lang.StockOrderCloseOrderConfirm, "CW0132", "Cornwall" ), asked );
		_orders.Verify( s => s.CloseOrderAsync( It.IsAny<int>(), It.IsAny<DateTime>() ), Times.Never, "Not confirmed." );
	}

	[TestMethod]
	public async Task CloseOrder_WithUnsavedChanges_AsksToSaveFirst()
	{
		var line = SavedLine( 4, 0 );
		await LoadSavedOrderAsync( line );
		line.Amount = 6;
		Assert.IsTrue( _vm.HasUnsavedChanges );

		await _vm.CloseOrderCommand.ExecuteAsync( null );

		CollectionAssert.Contains( _messages, Lang.StockOrderCloseOrderSaveFirst );
		_orders.Verify( s => s.CloseOrderAsync( It.IsAny<int>(), It.IsAny<DateTime>() ), Times.Never );
	}

	[TestMethod]
	public async Task CloseOrder_WhenAnotherOrderLoadsDuringTheDialog_DoesNothing()
	{
		await LoadSavedOrderAsync( SavedLine( 4, 0 ) );
		_vm.ConfirmAction = _ =>
		{
			// A selection load that was already running finishes while the dialog is open.
			_vm.ApplySelectedOrder( new StockOrderModel { Id = 26, SupplierId = 1, OrderDate = DateTime.Today }, [ SavedLine( 2, 0 ) ] );
			return true;
		};

		await _vm.CloseOrderCommand.ExecuteAsync( null );

		_orders.Verify( s => s.CloseOrderAsync( It.IsAny<int>(), It.IsAny<DateTime>() ), Times.Never, "Order 26 was never confirmed." );
	}

	[TestMethod]
	public async Task ReopenOrder_WhenAnotherOrderLoadsDuringTheDialog_DoesNothing()
	{
		await LoadClosedOrderAsync();
		_vm.ConfirmAction = _ =>
		{
			_vm.ApplySelectedOrder( new StockOrderModel { Id = 26, SupplierId = 1, OrderDate = DateTime.Today, Closed = true }, [ SavedLine( 2, 2 ) ] );
			return true;
		};

		await _vm.ReopenOrderCommand.ExecuteAsync( null );

		_orders.Verify( s => s.ReopenOrderAsync( It.IsAny<int>() ), Times.Never );
	}

	[TestMethod]
	public async Task CloseOrder_WhenReloadFails_BlocksEditingAndSaveOnlyReloads()
	{
		var line = SavedLine( 4, 1 );
		await LoadSavedOrderAsync( line );
		_orders.Setup( s => s.CloseOrderAsync( 25, It.IsAny<DateTime>() ) ).Returns( Task.CompletedTask );
		_orders.Setup( s => s.GetAllOrdersAsync() ).ThrowsAsync( new InvalidOperationException( "connection lost" ) );

		await AssertThrowsAsync<InvalidOperationException>( () => _vm.CloseOrderCommand.ExecuteAsync( null ) );

		Assert.IsFalse( _vm.IsWorking );
		Assert.IsFalse( _vm.CanEditOrder, "The editor still shows the open lines; they must not be edited." );
		Assert.IsFalse( _vm.AddShortagesCommand.CanExecute( null ) );

		_orders.Setup( s => s.GetAllOrdersAsync() ).ReturnsAsync( () => new List<StockOrderModel>
		{
			new() { Id = 25, SupplierId = 1, SupplierName = "Cornwall", OrderNumber = "CW0132", OrderDate = DateTime.Today, Closed = true, ClosedDate = DateTime.Today }
		} );
		_orders.Setup( s => s.GetOrderLinesAsync( 25 ) ).ReturnsAsync( () => new List<StockOrderLineModel> { SavedLine( 1, 1 ) } );
		await _vm.SaveOrderAsync();

		_orders.Verify( s => s.SaveOrderAsync( It.IsAny<StockOrderModel>(), It.IsAny<IEnumerable<StockOrderLineModel>>(), It.IsAny<IEnumerable<int>>(), It.IsAny<IEnumerable<ProductSupplierModel>>() ), Times.Never,
			"The open lines are never written back over the closed ones." );
		Assert.IsTrue( _vm.EditableOrder.Closed, "Save reloaded the closed order." );
	}

	[TestMethod]
	public async Task CloseOrder_WhenServiceFails_OrderStaysOpenAndScreenIsReleased()
	{
		await LoadSavedOrderAsync( SavedLine( 4, 0 ) );
		_orders.Setup( s => s.CloseOrderAsync( 25, It.IsAny<DateTime>() ) ).ThrowsAsync( new InvalidOperationException( "connection lost" ) );

		await AssertThrowsAsync<InvalidOperationException>( () => _vm.CloseOrderCommand.ExecuteAsync( null ) );

		Assert.IsFalse( _vm.EditableOrder.Closed );
		Assert.IsFalse( _vm.IsWorking );
		Assert.IsTrue( _vm.CloseOrderCommand.CanExecute( null ) );
	}

	[TestMethod]
	public async Task SelectedOrder_WhenBackgroundLoadFails_StoresAsyncError()
	{
		await CreateAsync();
		var expected = new InvalidOperationException( "order lines failed" );
		_orders.Setup( s => s.GetOrderLinesAsync( 30 ) ).ThrowsAsync( expected );

		_vm.SelectedOrder = new StockOrderModel { Id = 30 };

		for ( int i = 0; i < 40 && !ReferenceEquals( expected, _vm.LastAsyncError ); i++ )
			await Task.Delay( 25 );
		Assert.AreSame( expected, _vm.LastAsyncError );
	}

	[TestMethod]
	public async Task Save_WhileSaving_IgnoresSecondSaveAndLocksTheScreen()
	{
		var gate = new TaskCompletionSource<int>();
		_orders.Setup( s => s.SaveOrderAsync( It.IsAny<StockOrderModel>(), It.IsAny<IEnumerable<StockOrderLineModel>>(), It.IsAny<IEnumerable<int>>(), It.IsAny<IEnumerable<ProductSupplierModel>>() ) )
			.Returns( gate.Task );
		await CreateAsync();
		await SelectSupplierAndAddAsync( 5 );

		var firstSave = _vm.SaveOrderAsync();

		Assert.IsTrue( _vm.IsWorking );
		Assert.IsFalse( _vm.CanEditOrder, "Lines cannot be edited while saving." );
		Assert.IsFalse( _vm.CanSwitchOrder );
		Assert.IsFalse( _vm.SaveOrderCommand.CanExecute( null ) );
		Assert.IsFalse( _vm.NewOrderCommand.CanExecute( null ) );
		Assert.IsFalse( _vm.DeleteOrderCommand.CanExecute( null ) );

		await _vm.SaveOrderAsync();
		_orders.Verify( s => s.SaveOrderAsync( It.IsAny<StockOrderModel>(), It.IsAny<IEnumerable<StockOrderLineModel>>(), It.IsAny<IEnumerable<int>>(), It.IsAny<IEnumerable<ProductSupplierModel>>() ), Times.Once );

		gate.SetResult( 25 );
		await firstSave;

		Assert.IsFalse( _vm.IsWorking );
		Assert.IsTrue( _vm.SaveOrderCommand.CanExecute( null ), "A later save is possible again." );
	}

	[TestMethod]
	public async Task Save_WhenReloadAfterCommitFails_NextSaveUpdatesTheSameOrder()
	{
		bool failOrderListOnce = false;
		_orders.Setup( s => s.GetAllOrdersAsync() ).Returns( () =>
		{
			if ( !failOrderListOnce )
				return Task.FromResult( new List<StockOrderModel>() );

			failOrderListOnce = false;
			return Task.FromException<List<StockOrderModel>>( new InvalidOperationException( "connection lost" ) );
		} );
		var savedOrderIds = new List<int>();
		_orders.Setup( s => s.SaveOrderAsync( It.IsAny<StockOrderModel>(), It.IsAny<IEnumerable<StockOrderLineModel>>(), It.IsAny<IEnumerable<int>>(), It.IsAny<IEnumerable<ProductSupplierModel>>() ) )
			.Callback<StockOrderModel, IEnumerable<StockOrderLineModel>, IEnumerable<int>, IEnumerable<ProductSupplierModel>>( ( order, _, _, _ ) => savedOrderIds.Add( order.Id ) )
			.ReturnsAsync( 25 );
		await CreateAsync();
		await SelectSupplierAndAddAsync( 5 );
		failOrderListOnce = true;

		await AssertThrowsAsync<InvalidOperationException>( _vm.SaveOrderAsync );

		Assert.AreEqual( 25, _vm.EditableOrder.Id, "The commit is recorded before the reload." );
		Assert.IsFalse( _vm.IsNewOrder );
		Assert.IsFalse( _vm.HasUnsavedChanges );
		Assert.IsFalse( _vm.IsWorking );

		var storedOrder = new StockOrderModel { Id = 25, SupplierId = 1, SupplierName = "Cornwall", OrderDate = DateTime.Today };
		_orders.Setup( s => s.GetAllOrdersAsync() ).ReturnsAsync( new List<StockOrderModel> { storedOrder } );

		await _vm.SaveOrderAsync();

		CollectionAssert.AreEqual( new List<int> { 0 }, savedOrderIds, "Without new changes the retry only reloads; nothing is written again." );
		Assert.AreSame( storedOrder, _vm.SelectedOrder );

		// After the reload, a real change is saved as an update of order 25.
		_vm.EditableOrder.OrderNumber = "CW0201";
		await _vm.SaveOrderAsync();
		CollectionAssert.AreEqual( new List<int> { 0, 25 }, savedOrderIds );
	}

	[TestMethod]
	public async Task Save_WhileAnotherOrderIsStillLoading_KeepsTheSavedOrderInTheEditor()
	{
		var slowLoad = new TaskCompletionSource<List<StockOrderLineModel>>();
		_orders.Setup( s => s.GetOrderLinesAsync( 30 ) ).Returns( slowLoad.Task );
		var saveGate = new TaskCompletionSource<int>();
		_orders.Setup( s => s.SaveOrderAsync( It.IsAny<StockOrderModel>(), It.IsAny<IEnumerable<StockOrderLineModel>>(), It.IsAny<IEnumerable<int>>(), It.IsAny<IEnumerable<ProductSupplierModel>>() ) )
			.Returns( saveGate.Task );
		var orderA = new StockOrderModel { Id = 25, SupplierId = 1, SupplierName = "Cornwall" };
		_orders.Setup( s => s.GetAllOrdersAsync() ).ReturnsAsync( new List<StockOrderModel> { orderA } );
		await LoadSavedOrderAsync( SavedLine( 4, 0 ) );

		_vm.SelectedOrder = new StockOrderModel { Id = 30, SupplierId = 2 };   // order B starts loading
		var save = _vm.SaveOrderAsync();                                        // saving A starts before B arrives
		Assert.AreEqual( 25, _vm.SelectedOrder?.Id, "The dropdown goes back to the order that is being saved." );
		slowLoad.SetResult( [ new StockOrderLineModel { Id = 90, SupplyOrderId = 30, ProductId = 8, Amount = 1, OpenAmount = 1 } ] );
		await Task.Delay( 50 );

		Assert.AreEqual( 25, _vm.EditableOrder.Id, "The late load of B does not replace the editor while A is saved." );
		Assert.IsFalse( _vm.OrderLines.Any( line => line.Id == 90 ) );

		saveGate.SetResult( 25 );
		await save;
		Assert.AreEqual( 25, _vm.EditableOrder.Id );
	}

	[TestMethod]
	public async Task PendingSupplierData_IsKeptPerSupplier()
	{
		await CreateAsync();
		int dialogs = 0;
		_vm.ShowSupplierProductDialog = dialog =>
		{
			dialogs++;
			dialog.Model.SupplierProductNumber = dialogs == 1 ? "CW-BR2" : "OT-BR2";
			dialog.Model.UnitPrice = dialogs == 1 ? 2.4 : 3.1;
			return true;
		};

		await SelectSupplierAndAddAsync( 8 );
		_vm.SelectedOrderLine = _vm.OrderLines.Single();
		_vm.DeleteSelectedOrderLine();
		_vm.EditableOrder.SupplierId = 2;
		_vm.SelectedProductRow = Row( 8 );
		await _vm.AddSelectedProductAsync();

		Assert.AreEqual( 2, dialogs, "The data entered for Cornwall is not used for the other supplier." );
		Assert.AreEqual( "OT-BR2", _vm.OrderLines.Single().SupplierProductNumber );
		Assert.AreEqual( 3.1, _vm.OrderLines.Single().Price );

		await _vm.SaveOrderAsync();
		_orders.Verify( s => s.SaveOrderAsync(
			It.IsAny<StockOrderModel>(),
			It.IsAny<IEnumerable<StockOrderLineModel>>(),
			It.IsAny<IEnumerable<int>>(),
			It.Is<IEnumerable<ProductSupplierModel>>( ps => ps.Single().SupplierId == 2 && ps.Single().ProductNumber == "OT-BR2" ) ),
			Times.Once );
	}

	[TestMethod]
	public async Task AddProduct_ToLineWithoutSupplierData_AppliesConfirmedPriceToThatLine()
	{
		await CreateAsync();
		var line = new StockOrderLineModel { Id = 41, SupplyOrderId = 25, SupplierId = 1, ProductId = 8, ProductCode = "P-008", ProductName = "Brass rod", Amount = 3, OpenAmount = 3, Price = 2, RealRowTotal = 6 };
		_vm.ApplySelectedOrder( new StockOrderModel { Id = 25, SupplierId = 1, SupplierName = "Cornwall", OrderDate = DateTime.Today }, [ line ] );
		_vm.ShowSupplierProductDialog = dialog =>
		{
			dialog.Model.SupplierProductNumber = "CW-BR2";
			dialog.Model.SupplierProductName = "Brass rod 2mm";
			dialog.Model.UnitPrice = 2.4;
			return true;
		};

		_vm.SelectedProductRow = Row( 8 );
		await _vm.AddSelectedProductAsync();

		Assert.AreEqual( 4d, line.Amount );
		Assert.AreEqual( 2.4, line.Price );
		Assert.AreEqual( "CW-BR2", line.SupplierProductNumber );
		Assert.AreEqual( 9.6, line.RealRowTotal );
	}

	[TestMethod]
	public async Task OnlyShortages_FollowsTheLinesOnScreen()
	{
		await CreateAsync();
		_vm.ShowOnlyShortages = true;
		Assert.IsTrue( _vm.ProductRows.Any( row => row.ProductId == 5 ) );

		await SelectSupplierAndAddAsync( 5 );
		Assert.IsFalse( _vm.ProductRows.Any( row => row.ProductId == 5 ), "The shortage is covered by the new line." );

		_vm.SelectedOrderLine = _vm.OrderLines.Single();
		_vm.DeleteSelectedOrderLine();
		Assert.IsTrue( _vm.ProductRows.Any( row => row.ProductId == 5 ), "Removing the line brings the shortage back." );
	}

	[TestMethod]
	public async Task SelectedOrder_WhenLoadFails_DropdownReturnsToTheOrderOnScreen()
	{
		await LoadSavedOrderAsync( SavedLine( 4, 0 ) );
		var failure = new InvalidOperationException( "order lines failed" );
		_orders.Setup( s => s.GetOrderLinesAsync( 30 ) ).ThrowsAsync( failure );

		_vm.SelectedOrder = new StockOrderModel { Id = 30 };

		for ( int i = 0; i < 40 && !ReferenceEquals( failure, _vm.LastAsyncError ); i++ )
			await Task.Delay( 25 );
		Assert.AreSame( failure, _vm.LastAsyncError );
		Assert.AreEqual( 25, _vm.SelectedOrder?.Id );
		Assert.AreEqual( 25, _vm.EditableOrder.Id );
	}

	[TestMethod]
	public async Task DeleteOrder_LocksTheScreenAndDeletesTheOrderItStartedWith()
	{
		await LoadSavedOrderAsync( SavedLine( 4, 0 ) );
		var gate = new TaskCompletionSource<List<StockOrderLineModel>>();
		_orders.Setup( s => s.GetOrderLinesAsync( 25 ) ).Returns( gate.Task );
		_orders.Setup( s => s.DeleteOrderWithLinesAsync( It.IsAny<int>(), It.IsAny<IEnumerable<StockOrderLineModel>>() ) ).Returns( Task.CompletedTask );

		var delete = _vm.DeleteOrderCommand.ExecuteAsync( null );
		Assert.IsTrue( _vm.IsWorking );

		_vm.SelectedOrder = new StockOrderModel { Id = 30 };
		Assert.AreEqual( 25, _vm.SelectedOrder?.Id, "Switching order is refused while deleting." );

		gate.SetResult( [ SavedLine( 4, 0 ) ] );
		await delete;

		_orders.Verify( s => s.DeleteOrderWithLinesAsync( 25, It.IsAny<IEnumerable<StockOrderLineModel>>() ), Times.Once );
		_orders.Verify( s => s.GetOrderLinesAsync( 30 ), Times.Never );
		Assert.IsFalse( _vm.IsWorking );
	}

	private static async Task AssertThrowsAsync<TException>( Func<Task> action ) where TException : Exception
	{
		try
		{
			await action();
		}
		catch ( TException )
		{
			return;
		}

		Assert.Fail( $"Expected {typeof( TException ).Name}." );
	}
	#endregion
}
