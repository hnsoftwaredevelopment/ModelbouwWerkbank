namespace Modelbouwer.ViewModels;

public class StockReceiptDialogViewModel
{
	public StockReceiptDialogModel Model { get; }

	public StockReceiptDialogViewModel( StockReceiptDialogModel model )
	{
		Model = model;
	}

	public string Title => Model.IsCorrection ? Lang.StockReceiptCorrectDialogTitle : Lang.StockReceiptDialogTitle;
	public string AmountLabel => Model.IsCorrection ? Lang.StockReceiptCorrectAmountLabel : Lang.StockReceiptReceiveAmountLabel;

	/// <summary>
	/// Returns a message when the input can not be booked, otherwise null.
	/// Normal receipt: more than 0 and at most the open quantity, so a receipt never lowers the stock.
	/// Correction: a total between 0 and the ordered quantity.
	/// </summary>
	public string? Validate()
	{
		if ( Model.IsCorrection )
		{
			if ( Model.ReceiveAmount < 0d || Model.ReceiveAmount > Model.OrderedAmount )
				return string.Format( Lang.StockReceiptCorrectAmountOutOfRange, Format( Model.OrderedAmount ) );

			return null;
		}

		if ( Model.ReceiveAmount <= 0d )
			return Lang.StockReceiptReceiveAmountMustBePositive;

		if ( Model.ReceiveAmount > Model.OpenAmount )
			return string.Format( Lang.StockReceiptReceiveAmountTooHigh, Format( Model.OpenAmount ) );

		return null;
	}

	private static string Format( double value ) => value.ToString( "0.##", CultureInfo.CurrentCulture );
}
