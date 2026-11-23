namespace Modelbouwer.UnitTests.ViewModels;

[TestClass]
public class StockReceiptViewModelTests
{
	private Mock<IStockOrderService> _mockStockOrderService = null!;
	private Mock<ISupplierService> _mockSupplierService = null!;
	private StockReceiptViewModel _viewModel = null!;
	private List<StockOrderLineModel> _linesInDatabase = [];

	[TestInitialize]
	public void Setup()
	{
		_mockStockOrderService = new Mock<IStockOrderService>();
		_mockSupplierService = new Mock<ISupplierService>();
		_linesInDatabase = [];

		_mockStockOrderService.Setup( s => s.GetAllOrdersAsync() ).ReturnsAsync( new List<StockOrderModel>() );
		_mockStockOrderService.Setup( s => s.GetOrderLinesAsync( It.IsAny<int>() ) ).ReturnsAsync( () => _linesInDatabase.ToList() );
		_mockStockOrderService.Setup( s => s.RegisterReceiptAsync( It.IsAny<StockOrderLineModel>(), It.IsAny<double>(), It.IsAny<DateTime?>() ) ).Returns( Task.CompletedTask );
		_mockStockOrderService.Setup( s => s.UpdateOrderLineAsync( It.IsAny<StockOrderLineModel>() ) ).Returns( Task.CompletedTask );
		_mockStockOrderService.Setup( s => s.UpdateOrderAsync( It.IsAny<StockOrderModel>() ) ).Returns( Task.CompletedTask );

		_viewModel = new StockReceiptViewModel( _mockStockOrderService.Object, _mockSupplierService.Object )
		{
			ShowInformation = _ => { },
			ConfirmAction = _ => true
		};
	}

	private static StockOrderLineModel Line( int id, double amount, double received ) => new()
	{
		Id = id,
		SupplyOrderId = 25,
		ProductId = id,
		ProductCode = $"P-{id}",
		ProductName = $"Product {id}",
		SupplierProductName = $"Product {id}",
		Amount = amount,
		Received = received,
		OpenAmount = amount - received,
		Price = 2,
		Closed = amount - received <= 0
	};

	private StockOrderModel SelectOrder( params StockOrderLineModel [ ] lines )
	{
		var order = new StockOrderModel { Id = 25, SupplierId = 1, CurrencyId = 1, SupplierName = "Cornwall", OrderNumber = "CW0132" };
		_linesInDatabase = lines.ToList();
		_viewModel.ReplaceOrdersForTest( [ order ] );
		_viewModel.ApplySelectedOrder( order, lines );
		return order;
	}

	[TestMethod]
	public void ApplySelectedOrder_ShowsOpenAndClosedLinesOfTheSelectedOrder()
	{
		SelectOrder( Line( 1, 4, 0 ), Line( 2, 2, 2 ) );

		Assert.AreEqual( 2, _viewModel.OpenOrderLines.Count );
	}

	[TestMethod]
	public void ApplySelectedOrder_WhenClosedOrdersAreShown_ShowsClosedOrderLines()
	{
		_viewModel.ShowClosedOrders = true;
		SelectOrder( Line( 1, 4, 0 ), Line( 2, 2, 2 ) );

		Assert.AreEqual( 2, _viewModel.OpenOrderLines.Count );
	}

	[TestMethod]
	public async Task SelectedOrder_WhenBackgroundLoadFails_StoresAsyncError()
	{
		var expected = new InvalidOperationException( "receipt lines failed" );
		_mockStockOrderService.Setup( s => s.GetOrderLinesAsync( 25 ) ).ThrowsAsync( expected );

		_viewModel.SelectedOrder = new StockOrderModel { Id = 25, OrderNumber = "SO-025" };

		await WaitUntilAsync( () => ReferenceEquals( expected, _viewModel.LastAsyncError ) );
		Assert.AreSame( expected, _viewModel.LastAsyncError );
	}

	[TestMethod]
	public async Task ReceiveLine_PartialThenRest_BooksEachDeliveryAndClosesLine()
	{
		// Reproduces KAN-101: 4 ordered, 2 received, then the remaining 2.
		var line = Line( 1, 4, 0 );
		SelectOrder( line );
		_viewModel.SelectedOrderLine = line;
		_viewModel.ShowReceiptDialog = vm => { vm.Model.ReceiveAmount = 2; return true; };

		await _viewModel.ReceiveLineCommand.ExecuteAsync( null );

		Assert.AreEqual( 2d, line.Received );
		Assert.AreEqual( 2d, line.OpenAmount );
		Assert.IsFalse( line.Closed );

		_viewModel.SelectedOrderLine = _viewModel.OpenOrderLines.Single( l => l.Id == 1 );
		_viewModel.ShowReceiptDialog = vm =>
		{
			Assert.AreEqual( 2d, vm.Model.ReceiveAmount, "Second dialog must be prefilled with what is still open." );
			return true;
		};

		await _viewModel.ReceiveLineCommand.ExecuteAsync( null );

		Assert.AreEqual( 4d, line.Received );
		Assert.AreEqual( 0d, line.OpenAmount );
		Assert.IsTrue( line.Closed );
		_mockStockOrderService.Verify( s => s.RegisterReceiptAsync( line, 2d, It.IsAny<DateTime?>() ), Times.Exactly( 2 ) );
	}

	[TestMethod]
	public async Task ReceiveLine_WhenInputInvalid_BooksNothing()
	{
		var line = Line( 1, 4, 2 );
		SelectOrder( line );
		_viewModel.SelectedOrderLine = line;
		_viewModel.ShowReceiptDialog = vm => { vm.Model.ReceiveAmount = -1; return true; };

		await _viewModel.ReceiveLineCommand.ExecuteAsync( null );

		_mockStockOrderService.Verify( s => s.RegisterReceiptAsync( It.IsAny<StockOrderLineModel>(), It.IsAny<double>(), It.IsAny<DateTime?>() ), Times.Never );
		Assert.AreEqual( 2d, line.Received );
	}

	[TestMethod]
	public void ReceiveLine_IsDisabledForClosedLine()
	{
		_viewModel.ShowClosedOrders = true;
		var closed = Line( 2, 2, 2 );
		SelectOrder( closed );
		_viewModel.SelectedOrderLine = closed;

		Assert.IsFalse( _viewModel.ReceiveLineCommand.CanExecute( null ) );
		Assert.IsFalse( _viewModel.CancelRemainderCommand.CanExecute( null ) );
	}

	[TestMethod]
	public async Task ReceiveAll_BooksOpenAmountOfEveryOpenLineAndClosesOrder()
	{
		var first = Line( 1, 4, 1 );
		var second = Line( 2, 3, 0 );
		var order = SelectOrder( first, second );
		_viewModel.ShowReceiptDateDialog = _ => true;

		await _viewModel.ReceiveAllCommand.ExecuteAsync( null );

		_mockStockOrderService.Verify( s => s.RegisterReceiptAsync( first, 3d, It.IsAny<DateTime?>() ), Times.Once );
		_mockStockOrderService.Verify( s => s.RegisterReceiptAsync( second, 3d, It.IsAny<DateTime?>() ), Times.Once );
		Assert.IsTrue( order.Closed );
		_mockStockOrderService.Verify( s => s.UpdateOrderAsync( order ), Times.Once );
	}

	[TestMethod]
	public async Task ReceiveLine_WhenAnotherLineStillOpen_DoesNotCloseOrder()
	{
		var first = Line( 1, 2, 0 );
		var second = Line( 2, 3, 0 );
		var order = SelectOrder( first, second );
		_viewModel.SelectedOrderLine = first;
		_viewModel.ShowReceiptDialog = _ => true;

		await _viewModel.ReceiveLineCommand.ExecuteAsync( null );

		Assert.IsTrue( first.Closed );
		Assert.IsFalse( order.Closed );
		_mockStockOrderService.Verify( s => s.UpdateOrderAsync( It.IsAny<StockOrderModel>() ), Times.Never );
	}

	[TestMethod]
	public async Task CancelRemainder_ClosesLineWithoutBookingStockAndLowersOrderedAmountToReceived()
	{
		var line = Line( 1, 4, 2 );
		SelectOrder( line );
		_viewModel.SelectedOrderLine = line;

		await _viewModel.CancelRemainderCommand.ExecuteAsync( null );

		Assert.AreEqual( 2d, line.Amount, "Received is stored as ordered minus open, so ordered must drop to received." );
		Assert.AreEqual( 4d, line.RealRowTotal, "Only the delivered quantity is charged." );
		Assert.AreEqual( 2d, line.Received );
		Assert.AreEqual( 0d, line.OpenAmount );
		Assert.IsTrue( line.Closed );
		_mockStockOrderService.Verify( s => s.UpdateOrderLineAsync( line ), Times.Once );
		_mockStockOrderService.Verify( s => s.RegisterReceiptAsync( It.IsAny<StockOrderLineModel>(), It.IsAny<double>(), It.IsAny<DateTime?>() ), Times.Never );
	}

	[TestMethod]
	public async Task CancelRemainder_WhenNotConfirmed_ChangesNothing()
	{
		var line = Line( 1, 4, 2 );
		SelectOrder( line );
		_viewModel.SelectedOrderLine = line;
		_viewModel.ConfirmAction = _ => false;

		await _viewModel.CancelRemainderCommand.ExecuteAsync( null );

		Assert.AreEqual( 2d, line.OpenAmount );
		_mockStockOrderService.Verify( s => s.UpdateOrderLineAsync( It.IsAny<StockOrderLineModel>() ), Times.Never );
	}

	[TestMethod]
	public async Task EditSupplierData_UpsertsSupplierProductAndUpdatesLine()
	{
		var line = Line( 1, 4, 0 );
		SelectOrder( line );
		_viewModel.SelectedOrderLine = line;
		_mockSupplierService.Setup( s => s.GetProductSupplierAsync( 1, 1 ) ).ReturnsAsync( ( Modelbouwer.Model.ProductSupplierModel? ) null );
		_mockSupplierService.Setup( s => s.UpsertProductSupplierAsync( It.IsAny<Modelbouwer.Model.ProductSupplierModel>() ) ).ReturnsAsync( 70 );
		_viewModel.ShowSupplierProductDialog = vm =>
		{
			vm.Model.SupplierProductNumber = "CW-123";
			vm.Model.SupplierProductName = "Walnut dowel 3mm";
			vm.Model.UnitPrice = 2.5;
			return true;
		};

		await _viewModel.EditSupplierDataCommand.ExecuteAsync( null );

		_mockSupplierService.Verify( s => s.UpsertProductSupplierAsync( It.Is<Modelbouwer.Model.ProductSupplierModel>( ps =>
			ps.SupplierId == 1 && ps.ProductId == 1 && ps.ProductNumber == "CW-123" && ps.ProductName == "Walnut dowel 3mm" && ps.Price == 2.5 ) ), Times.Once );
		Assert.AreEqual( "Walnut dowel 3mm", line.SupplierProductName );
		Assert.AreEqual( 2.5, line.Price );
		Assert.AreEqual( 10d, line.RealRowTotal );
		_mockStockOrderService.Verify( s => s.UpdateOrderLineAsync( line ), Times.Once );
	}

	[TestMethod]
	public async Task ReceiveAll_KeepsClosedOrderAndLinesVisibleForCorrections()
	{
		var first = Line( 1, 4, 0 );
		var second = Line( 2, 3, 0 );
		var order = SelectOrder( first, second );
		_viewModel.ShowReceiptDateDialog = _ => true;

		await _viewModel.ReceiveAllCommand.ExecuteAsync( null );

		Assert.IsTrue( order.Closed );
		CollectionAssert.Contains( _viewModel.Orders, order, "The closed order stays in the list while it is selected." );
		Assert.AreSame( order, _viewModel.SelectedOrder );
		Assert.AreEqual( 2, _viewModel.OpenOrderLines.Count );
	}

	[TestMethod]
	public async Task CorrectReceipt_AfterEntireOrderReceived_ReopensLineAndOrderAndBooksNegativeDifference()
	{
		// Large order received in full, but 3 of 5 pieces of one line actually arrived.
		var first = Line( 1, 5, 0 );
		var second = Line( 2, 2, 0 );
		var order = SelectOrder( first, second );
		_viewModel.ShowReceiptDateDialog = _ => true;
		await _viewModel.ReceiveAllCommand.ExecuteAsync( null );

		_viewModel.SelectedOrderLine = _viewModel.OpenOrderLines.Single( l => l.Id == 1 );
		_viewModel.ShowReceiptDialog = vm =>
		{
			Assert.IsTrue( vm.Model.IsCorrection );
			Assert.AreEqual( 5d, vm.Model.ReceiveAmount, "Correction starts at the registered total." );
			vm.Model.ReceiveAmount = 3;
			return true;
		};

		await _viewModel.ActivateLineCommand.ExecuteAsync( null );

		Assert.AreEqual( 3d, first.Received );
		Assert.AreEqual( 2d, first.OpenAmount );
		Assert.IsFalse( first.Closed );
		Assert.IsFalse( order.Closed, "The order reopens because a line is open again." );
		_mockStockOrderService.Verify( s => s.RegisterReceiptAsync( first, -2d, It.IsAny<DateTime?>() ), Times.Once );
		Assert.IsTrue( _viewModel.ReceiveLineCommand.CanExecute( null ), "The remaining 2 can be received later." );
	}

	[TestMethod]
	public async Task CorrectReceipt_WithUnchangedTotal_BooksNothing()
	{
		var line = Line( 1, 4, 4 );
		_viewModel.ShowClosedOrders = true;
		SelectOrder( line );
		_viewModel.SelectedOrderLine = line;
		_viewModel.ShowReceiptDialog = _ => true;

		await _viewModel.CorrectReceiptCommand.ExecuteAsync( null );

		_mockStockOrderService.Verify( s => s.RegisterReceiptAsync( It.IsAny<StockOrderLineModel>(), It.IsAny<double>(), It.IsAny<DateTime?>() ), Times.Never );
	}

	[TestMethod]
	public async Task ActivateLine_OnOpenLine_OpensNormalReceipt()
	{
		var line = Line( 1, 4, 1 );
		SelectOrder( line );
		_viewModel.SelectedOrderLine = line;
		bool? wasCorrection = null;
		_viewModel.ShowReceiptDialog = vm => { wasCorrection = vm.Model.IsCorrection; return false; };

		await _viewModel.ActivateLineCommand.ExecuteAsync( null );

		Assert.AreEqual( false, wasCorrection );
	}

	private static async Task WaitUntilAsync( Func<bool> condition )
	{
		for ( int attempt = 0; attempt < 40; attempt++ )
		{
			if ( condition() )
				return;

			await Task.Delay( 25 );
		}

		Assert.Fail( "Condition was not met before the timeout." );
	}
}
