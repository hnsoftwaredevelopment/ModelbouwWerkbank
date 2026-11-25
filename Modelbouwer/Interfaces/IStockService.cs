namespace Modelbouwer.Interfaces;

public interface IStockService
{
	Task<List<StockManagementModel>> GetCompleteInventoryAsync();
	Task<int> InsertCorrectionAsync( Dictionary<string, object?> queryParameters );
	Task<int> UpdateProductCorrectionAsync( Dictionary<string, object?> queryParameters );

	/// <summary>Receipts, usage and corrections of one product with the resulting stock (KAN-80).</summary>
	Task<List<StockHistoryItemModel>> GetStockHistoryAsync( int productId );
	Task<List<StockHistoryItemModel>> GetStockHistoryAsync( int productId, CancellationToken cancellationToken );
}