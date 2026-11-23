namespace Modelbouwer.UnitTests.ViewModels;

[TestClass]
public class StockReceiptDialogViewModelTests
{
	private static StockOrderLineModel Line( double amount, double received ) => new()
	{
		Id = 40,
		SupplyOrderId = 25,
		ProductId = 5,
		ProductCode = "P-005",
		ProductName = "Wheel Set",
		Amount = amount,
		Received = received,
		OpenAmount = amount - received
	};

	[TestMethod]
	public void Create_PrefillsReceiveAmountWithOpenAmountAndToday()
	{
		var model = StockReceiptDialogModel.Create( Line( 4, 2 ) );

		Assert.AreEqual( 4d, model.OrderedAmount );
		Assert.AreEqual( 2d, model.AlreadyReceivedAmount );
		Assert.AreEqual( 2d, model.OpenAmount );
		Assert.AreEqual( 2d, model.ReceiveAmount, "This delivery defaults to everything that is still open." );
		Assert.AreEqual( DateTime.Today, model.DeliveryDate );
	}

	[TestMethod]
	public void RemainingAfterReceipt_FollowsReceiveAmount()
	{
		var model = StockReceiptDialogModel.Create( Line( 10, 2 ) );

		model.ReceiveAmount = 3;

		Assert.AreEqual( 5d, model.RemainingAfterReceipt );
	}

	[TestMethod]
	public void Validate_AcceptsPartialAndFullReceipt()
	{
		var vm = new StockReceiptDialogViewModel( StockReceiptDialogModel.Create( Line( 4, 2 ) ) );

		vm.Model.ReceiveAmount = 1;
		Assert.IsNull( vm.Validate() );

		vm.Model.ReceiveAmount = 2;
		Assert.IsNull( vm.Validate() );
	}

	[TestMethod]
	public void Validate_RejectsZeroAndNegativeAmounts()
	{
		var vm = new StockReceiptDialogViewModel( StockReceiptDialogModel.Create( Line( 4, 2 ) ) );

		vm.Model.ReceiveAmount = 0;
		Assert.AreEqual( Lang.StockReceiptReceiveAmountMustBePositive, vm.Validate() );

		vm.Model.ReceiveAmount = -1;
		Assert.AreEqual( Lang.StockReceiptReceiveAmountMustBePositive, vm.Validate() );
	}

	[TestMethod]
	public void Validate_RejectsMoreThanOpenAmount()
	{
		var vm = new StockReceiptDialogViewModel( StockReceiptDialogModel.Create( Line( 4, 2 ) ) );

		vm.Model.ReceiveAmount = 3;

		Assert.IsNotNull( vm.Validate() );
	}
	[TestMethod]
	public void CreateCorrection_PrefillsWithRegisteredTotalAndComputesDelta()
	{
		var model = StockReceiptDialogModel.CreateCorrection( Line( 5, 5 ) );

		Assert.IsTrue( model.IsCorrection );
		Assert.AreEqual( 5d, model.ReceiveAmount );

		model.ReceiveAmount = 3;
		Assert.AreEqual( -2d, model.StockDelta );
		Assert.AreEqual( 2d, model.RemainingAfterReceipt );
	}

	[TestMethod]
	public void Validate_Correction_AllowsZeroToOrderedAndRejectsOutside()
	{
		var vm = new StockReceiptDialogViewModel( StockReceiptDialogModel.CreateCorrection( Line( 5, 5 ) ) );

		vm.Model.ReceiveAmount = 0;
		Assert.IsNull( vm.Validate() );
		vm.Model.ReceiveAmount = 5;
		Assert.IsNull( vm.Validate() );
		vm.Model.ReceiveAmount = 6;
		Assert.IsNotNull( vm.Validate() );
		vm.Model.ReceiveAmount = -1;
		Assert.IsNotNull( vm.Validate() );
	}
}