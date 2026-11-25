namespace Modelbouwer.Models;

/// <summary>One stocklog row as read from the database, before it is turned into a history line.</summary>
public sealed class StockLogEntry
{
	public int Id { get; init; }
	public DateTime? LogDate { get; init; }
	public double AmountReceived { get; init; }
	public double AmountUsed { get; init; }
	public double AmountCorrection { get; init; }
	public int? SupplyOrderLineId { get; init; }
	public int? ProductUsageId { get; init; }
	public string? OrderNumber { get; init; }
	public string? SupplierName { get; init; }
	public string? ProjectName { get; init; }
}

public enum StockHistoryKind
{
	Receipt,
	Usage,
	Correction
}

/// <summary>A line on the stock history tab (KAN-80): one mutation with the resulting stock.</summary>
public sealed class StockHistoryItemModel
{
	public DateTime? Date { get; init; }
	public StockHistoryKind Kind { get; init; }
	public string KindText { get; init; } = string.Empty;
	public string Description { get; init; } = string.Empty;
	public double? AmountIn { get; init; }
	public double? AmountOut { get; init; }
	public double Balance { get; init; }

	/// <summary>
	/// Turns stocklog rows into history lines with a running balance. Rows without a date come first, rows with
	/// nothing booked are skipped. The result is in chronological order; the view sorts newest first.
	/// </summary>
	public static List<StockHistoryItemModel> Build( IEnumerable<StockLogEntry> entries )
	{
		var history = new List<StockHistoryItemModel>();
		double balance = 0d;

		foreach ( var entry in entries
			.OrderBy( e => e.LogDate.HasValue )
			.ThenBy( e => e.LogDate )
			.ThenBy( e => e.Id ) )
		{
			double delta = entry.AmountReceived - entry.AmountUsed + entry.AmountCorrection;
			if ( entry.AmountReceived == 0d && entry.AmountUsed == 0d && entry.AmountCorrection == 0d )
				continue;

			balance += delta;
			var (kind, kindText, description) = Describe( entry );
			history.Add( new StockHistoryItemModel
			{
				Date = entry.LogDate,
				Kind = kind,
				KindText = kindText,
				Description = description,
				AmountIn = delta > 0d ? delta : null,
				AmountOut = delta < 0d ? -delta : null,
				Balance = balance
			} );
		}

		return history;
	}

	private static (StockHistoryKind Kind, string KindText, string Description) Describe( StockLogEntry entry )
	{
		if ( entry.ProductUsageId.HasValue || entry.AmountUsed != 0d )
			return (StockHistoryKind.Usage, Lang.StockHistoryKindUsage, entry.ProjectName ?? string.Empty);

		string order = string.IsNullOrWhiteSpace( entry.SupplierName )
			? entry.OrderNumber ?? string.Empty
			: $"{entry.OrderNumber} ({entry.SupplierName})";

		// The stock stays booked when an order is deleted afterwards; say so instead of showing nothing.
		if ( string.IsNullOrWhiteSpace( order ) && entry.SupplyOrderLineId.HasValue )
			order = Lang.StockHistoryOrderDeleted;

		if ( entry.AmountReceived != 0d )
			return (StockHistoryKind.Receipt, Lang.StockHistoryKindReceipt, order);

		// Corrections linked to an order line were written by orders before KAN-100.
		return entry.SupplyOrderLineId.HasValue
			? (StockHistoryKind.Correction, Lang.StockHistoryKindCorrection, string.Format( Lang.StockHistoryOrderCorrection, order ))
			: (StockHistoryKind.Correction, Lang.StockHistoryKindCorrection, string.Empty);
	}
}
