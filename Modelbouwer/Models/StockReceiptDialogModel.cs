namespace Modelbouwer.Models;

/// <summary>
/// Input for a receipt on one order line (KAN-101, KAN-102).
/// Normal mode: the user enters the quantity of THIS delivery, prefilled with the open quantity.
/// Correction mode: the user enters the actual TOTAL received, prefilled with what is registered now.
/// </summary>
public partial class StockReceiptDialogModel : ObservableObject
{
	[ObservableProperty] private int _orderLineId;
	[ObservableProperty] private int _orderId;
	[ObservableProperty] private int _productId;
	[ObservableProperty] private string? _productNumber;
	[ObservableProperty] private string? _productDescription;
	[ObservableProperty] private double _orderedAmount;
	[ObservableProperty] private double _alreadyReceivedAmount;
	[ObservableProperty] private double _openAmount;
	[ObservableProperty] private DateTime? _deliveryDate = DateTime.Today;
	[ObservableProperty] private bool _isCorrection;

	[ObservableProperty]
	[NotifyPropertyChangedFor( nameof( RemainingAfterReceipt ) )]
	private double _receiveAmount;

	/// <summary>Open quantity after the receipt or correction is booked.</summary>
	public double RemainingAfterReceipt => IsCorrection
		? Math.Max( OrderedAmount - ReceiveAmount, 0d )
		: Math.Max( OpenAmount - ReceiveAmount, 0d );

	/// <summary>Quantity to book on the stock: the delivery itself, or the difference with the registered total.</summary>
	public double StockDelta => IsCorrection ? ReceiveAmount - AlreadyReceivedAmount : ReceiveAmount;

	public static StockReceiptDialogModel Create( StockOrderLineModel line ) => Create( line, false );

	public static StockReceiptDialogModel CreateCorrection( StockOrderLineModel line ) => Create( line, true );

	private static StockReceiptDialogModel Create( StockOrderLineModel line, bool isCorrection )
	{
		return new StockReceiptDialogModel
		{
			OrderLineId = line.Id,
			OrderId = line.SupplyOrderId,
			ProductId = line.ProductId,
			ProductNumber = line.DisplayProductNumber,
			ProductDescription = line.DisplayProductDescription,
			OrderedAmount = line.Amount,
			AlreadyReceivedAmount = line.Received,
			OpenAmount = line.OpenAmount,
			IsCorrection = isCorrection,
			ReceiveAmount = isCorrection ? line.Received : line.OpenAmount,
			DeliveryDate = isCorrection && line.ClosedDate is DateTime closed ? closed : DateTime.Today
		};
	}
}
