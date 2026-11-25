namespace Modelbouwer.Interfaces;

public interface IStockOrderService
{
	/// <summary>
	/// Saves an order with all pending changes in one transaction (KAN-103): the order header (insert or update),
	/// new lines (Id &lt;= 0), changed lines (IsDirty), deleted lines and the supplier data of products.
	/// Returns the order id.
	/// </summary>
	Task<int> SaveOrderAsync( StockOrderModel order, IEnumerable<StockOrderLineModel> lines, IEnumerable<int> deletedLineIds, IEnumerable<Modelbouwer.Model.ProductSupplierModel> productSuppliers );
	Task<int> SaveOrderAsync( StockOrderModel order, IEnumerable<StockOrderLineModel> lines, IEnumerable<int> deletedLineIds, IEnumerable<Modelbouwer.Model.ProductSupplierModel> productSuppliers, CancellationToken cancellationToken );
	Task<List<StockOrderModel>> GetAllOrdersAsync();
	Task<List<StockOrderModel>> GetAllOrdersAsync( CancellationToken cancellationToken );
	Task<List<StockOrderLineModel>> GetOrderLinesAsync( int orderId );
	Task<List<StockOrderLineModel>> GetOrderLinesAsync( int orderId, CancellationToken cancellationToken );
	Task<int> InsertOrderAsync( StockOrderModel order );
	Task<int> InsertOrderAsync( StockOrderModel order, CancellationToken cancellationToken );
	Task<int> InsertOrderWithLinesAsync( StockOrderModel order, IEnumerable<StockOrderLineModel> lines );
	Task<int> InsertOrderWithLinesAsync( StockOrderModel order, IEnumerable<StockOrderLineModel> lines, CancellationToken cancellationToken );
	Task UpdateOrderAsync( StockOrderModel order );
	Task UpdateOrderAsync( StockOrderModel order, CancellationToken cancellationToken );
	Task DeleteOrderAsync( int orderId );
	Task DeleteOrderAsync( int orderId, CancellationToken cancellationToken );
	Task DeleteOrderWithLinesAsync( int orderId, IEnumerable<StockOrderLineModel> lines );
	Task DeleteOrderWithLinesAsync( int orderId, IEnumerable<StockOrderLineModel> lines, CancellationToken cancellationToken );
	Task<int> InsertOrderLineAsync( StockOrderLineModel line );
	Task<int> InsertOrderLineAsync( StockOrderLineModel line, CancellationToken cancellationToken );
	Task UpdateOrderLineAsync( StockOrderLineModel line );
	Task UpdateOrderLineAsync( StockOrderLineModel line, CancellationToken cancellationToken );
	Task RegisterReceiptAsync( StockOrderLineModel line, double receivedAmount, DateTime? deliveryDate );
	Task RegisterReceiptAsync( StockOrderLineModel line, double receivedAmount, DateTime? deliveryDate, CancellationToken cancellationToken );
	Task DeleteOrderLineAsync( int lineId );
	Task DeleteOrderLineAsync( int lineId, CancellationToken cancellationToken );
}
