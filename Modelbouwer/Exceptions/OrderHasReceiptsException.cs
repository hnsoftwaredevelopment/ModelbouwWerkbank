namespace Modelbouwer.Exceptions;

/// <summary>An order with receipts cannot be deleted: the stock it brought in would lose its origin (KAN-137).</summary>
public class OrderHasReceiptsException : EntityInUseException
{
	public OrderHasReceiptsException( int orderId )
		: base( Lang.StockOrderOrderHasReceipts )
	{
		OrderId = orderId;
	}

	public int OrderId { get; }
}
