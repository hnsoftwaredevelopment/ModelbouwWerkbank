namespace Modelbouwer.Interfaces;

public interface IStockService
{
	Task<List<StockManagementModel>> GetCompleteInventoryAsync();
	Task<List<StockManagementModel>> GetCompleteInventoryAsync( CancellationToken cancellationToken );
	Task<int> InsertCorrectionAsync( Dictionary<string, object?> queryParameters );
	Task<int> InsertCorrectionAsync( Dictionary<string, object?> queryParameters, CancellationToken cancellationToken );
	Task<int> UpdateProductCorrectionAsync( Dictionary<string, object?> queryParameters );
	Task<int> UpdateProductCorrectionAsync( Dictionary<string, object?> queryParameters, CancellationToken cancellationToken );

	/// <summary>Receipts, usage and corrections of one product with the resulting stock (KAN-80).</summary>
	Task<List<StockHistoryItemModel>> GetStockHistoryAsync( int productId );
	Task<List<StockHistoryItemModel>> GetStockHistoryAsync( int productId, CancellationToken cancellationToken );
}