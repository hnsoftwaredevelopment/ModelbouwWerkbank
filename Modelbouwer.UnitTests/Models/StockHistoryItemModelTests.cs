namespace Modelbouwer.UnitTests.Models;

/// <summary>Stock history tab (KAN-80): stocklog rows become history lines with a running balance.</summary>
[TestClass]
public class StockHistoryItemModelTests
{
	private static readonly DateTime March1 = new( 2026, 3, 1 );
	private static readonly DateTime March5 = new( 2026, 3, 5 );
	private static readonly DateTime March9 = new( 2026, 3, 9 );

	[TestMethod]
	public void Build_KeepsChronologicalOrderAndRunningBalance()
	{
		var history = StockHistoryItemModel.Build(
		[
			new StockLogEntry { Id = 3, LogDate = March9, AmountUsed = 2, ProductUsageId = 30, ProjectName = "HMS Victory" },
			new StockLogEntry { Id = 1, LogDate = March1, AmountCorrection = 4 },
			new StockLogEntry { Id = 2, LogDate = March5, AmountReceived = 6, SupplyOrderLineId = 40, OrderNumber = "CW0132", SupplierName = "Cornwall" }
		] );

		CollectionAssert.AreEqual( new[] { 4d, 10d, 8d }, history.Select( item => item.Balance ).ToArray() );
		Assert.AreEqual( StockHistoryKind.Correction, history [ 0 ].Kind );
		Assert.AreEqual( 4d, history [ 0 ].AmountIn );
		Assert.IsNull( history [ 0 ].AmountOut );

		Assert.AreEqual( StockHistoryKind.Receipt, history [ 1 ].Kind );
		Assert.AreEqual( "CW0132 (Cornwall)", history [ 1 ].Description );

		Assert.AreEqual( StockHistoryKind.Usage, history [ 2 ].Kind );
		Assert.AreEqual( "HMS Victory", history [ 2 ].Description );
		Assert.AreEqual( 2d, history [ 2 ].AmountOut );
		Assert.IsNull( history [ 2 ].AmountIn );
	}

	[TestMethod]
	public void Build_NegativeCorrectionIsAnOutgoingAmount()
	{
		var item = StockHistoryItemModel.Build( [ new StockLogEntry { Id = 1, LogDate = March1, AmountCorrection = -3 } ] ).Single();

		Assert.AreEqual( StockHistoryKind.Correction, item.Kind );
		Assert.AreEqual( Lang.StockHistoryKindCorrection, item.KindText );
		Assert.AreEqual( 3d, item.AmountOut );
		Assert.AreEqual( -3d, item.Balance );
		Assert.AreEqual( string.Empty, item.Description );
	}

	[TestMethod]
	public void Build_CorrectionLinkedToAnOrderLineNamesTheOrder()
	{
		// Orders wrote corrections themselves before KAN-100.
		var item = StockHistoryItemModel.Build(
		[
			new StockLogEntry { Id = 1, LogDate = March1, AmountCorrection = 2, SupplyOrderLineId = 40, OrderNumber = "CW0099", SupplierName = "Cornwall" }
		] ).Single();

		Assert.AreEqual( StockHistoryKind.Correction, item.Kind );
		Assert.AreEqual( string.Format( Lang.StockHistoryOrderCorrection, "CW0099 (Cornwall)" ), item.Description );
	}

	[TestMethod]
	public void Build_ReceiptOfADeletedOrderSaysSo()
	{
		var item = StockHistoryItemModel.Build( [ new StockLogEntry { Id = 82, LogDate = March5, AmountReceived = 4, SupplyOrderLineId = 75 } ] ).Single();

		Assert.AreEqual( StockHistoryKind.Receipt, item.Kind );
		Assert.AreEqual( Lang.StockHistoryOrderDeleted, item.Description );
	}

	[TestMethod]
	public void Build_SkipsEmptyRowsAndPutsRowsWithoutDateFirst()
	{
		var history = StockHistoryItemModel.Build(
		[
			new StockLogEntry { Id = 2, LogDate = March5, AmountReceived = 1 },
			new StockLogEntry { Id = 1, LogDate = null, AmountCorrection = 5 },
			new StockLogEntry { Id = 3, LogDate = null }
		] );

		Assert.AreEqual( 2, history.Count, "A row with nothing booked is skipped." );
		Assert.IsNull( history [ 0 ].Date );
		Assert.AreEqual( 6d, history [ 1 ].Balance );
	}

	[TestMethod]
	public void StockHistoryQuery_FiltersOnProductAndJoinsOrderSupplierAndProject()
	{
		StringAssert.Contains( StockService.StockHistoryQuery, $"WHERE sl.{DBNames.StocklogFieldNameProductId} = @ProductId" );
		StringAssert.Contains( StockService.StockHistoryQuery, $"LEFT JOIN {DBNames.Database}.{DBNames.OrderTable} so" );
		StringAssert.Contains( StockService.StockHistoryQuery, $"LEFT JOIN {DBNames.Database}.{DBNames.SupplierTable} sup" );
		StringAssert.Contains( StockService.StockHistoryQuery, $"LEFT JOIN {DBNames.Database}.{DBNames.ProjectTable} prj" );
	}
}
