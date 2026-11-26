using MySqlCommand = MySql.Data.MySqlClient.MySqlCommand;
using MySqlConnection = MySql.Data.MySqlClient.MySqlConnection;
using MySqlTransaction = MySql.Data.MySqlClient.MySqlTransaction;

namespace Modelbouwer.Services;

public class StockOrderService : IStockOrderService
{
	private readonly GenericDataService _dataService;

	public StockOrderService( GenericDataService dataService )
	{
		_dataService = dataService;
	}

	public string CompleteOrderListQuery = $@"
SELECT
	{DBNames.OrderViewFieldNameId},
	{DBNames.OrderViewFieldNameSupplierId},
	{DBNames.OrderViewFieldNameSupplierName},
	{DBNames.OrderViewFieldNameCurrencyId},
	{DBNames.OrderViewFieldNameCurrencySymbol},
	{DBNames.OrderViewFieldNameOrderNumber},
	{DBNames.OrderViewFieldNameOrderDate},
	{DBNames.OrderViewFieldNameOrderShippingCosts},
	{DBNames.OrderViewFieldNameOrderOrderCosts},
	{DBNames.OrderViewFieldNameClosed},
	{DBNames.OrderViewFieldNameClosedDate},
	{DBNames.OrderViewFieldNameOrderMemo},
	{DBNames.OrderViewFieldNameHasStockLog},
	(SELECT COUNT(*) FROM {DBNames.Database}.{DBNames.OrderLineTable} ol
	 WHERE ol.{DBNames.OrderLineFieldNameSupplierOrderId} = o.{DBNames.OrderViewFieldNameId}
	   AND ol.{DBNames.OrderLineFieldNameClosed} = 0) AS OpenLineCount,
	(SELECT COUNT(*) FROM {DBNames.Database}.{DBNames.OrderLineTable} ol
	 WHERE ol.{DBNames.OrderLineFieldNameSupplierOrderId} = o.{DBNames.OrderViewFieldNameId}) AS LineCount,
	(SELECT IFNULL(SUM(CASE WHEN IFNULL(ol.{DBNames.OrderLineFieldNameRealRowTotal}, 0) <> 0 THEN ol.{DBNames.OrderLineFieldNameRealRowTotal}
	                        ELSE IFNULL(ol.{DBNames.OrderLineFieldNamePrice}, 0) * IFNULL(ol.{DBNames.OrderLineFieldNameAmount}, 0) END), 0)
	 FROM {DBNames.Database}.{DBNames.OrderLineTable} ol
	 WHERE ol.{DBNames.OrderLineFieldNameSupplierOrderId} = o.{DBNames.OrderViewFieldNameId}) AS LinesTotal
FROM {DBNames.Database}.{DBNames.OrderView} o
ORDER BY {DBNames.OrderViewFieldNameOrderDate} DESC, {DBNames.OrderViewFieldNameId} DESC;";

	public string OrderLinesQuery = $@"
SELECT
	ol.{DBNames.OrderLineFieldNameId} AS {DBNames.OrderLineFieldNameId},
	ol.{DBNames.OrderLineFieldNameSupplierOrderId} AS {DBNames.OrderLineViewFieldNameOrderId},
	o.{DBNames.OrderFieldNameSupplierId} AS {DBNames.OrderLineViewFieldNameSupplierId},
	p.{DBNames.ProductFieldNameId} AS {DBNames.OrderLineViewFieldNameProductId},
	p.{DBNames.ProductFieldNameCode} AS {DBNames.OrderLineViewFieldNameProductCode},
	p.{DBNames.ProductFieldNameName} AS {DBNames.OrderLineViewFieldNameProductName},
	ps.{DBNames.ProductSupplierFieldNameProductNumber} AS {DBNames.ProductSupplierFieldNameProductNumber},
	ol.{DBNames.OrderLineFieldNameSupplierProductName} AS {DBNames.OrderLineFieldNameSupplierProductName},
	ol.{DBNames.OrderLineFieldNameAmount} AS {DBNames.OrderLineFieldNameAmount},
	ol.{DBNames.OrderLineFieldNameOpenAmount} AS {DBNames.OrderLineFieldNameOpenAmount},
	ol.{DBNames.OrderLineFieldNamePrice} AS {DBNames.OrderLineFieldNamePrice},
	ol.{DBNames.OrderLineFieldNameRealRowTotal} AS {DBNames.OrderLineFieldNameRealRowTotal},
	(ol.{DBNames.OrderLineFieldNameAmount} - ol.{DBNames.OrderLineFieldNameOpenAmount}) AS {DBNames.OrderLineViewFieldNameReceived},
	ol.{DBNames.OrderLineFieldNameOpenAmount} AS {DBNames.OrderLineViewFieldNameExpected},
	ol.{DBNames.OrderLineFieldNameClosed} AS {DBNames.OrderLineViewFieldNameClosed},
	ol.{DBNames.OrderLineFieldNameClosedDate} AS {DBNames.OrderLineViewFieldNameClosedDate}
FROM {DBNames.Database}.{DBNames.OrderLineTable} ol
INNER JOIN {DBNames.Database}.{DBNames.OrderTable} o ON ol.{DBNames.OrderLineFieldNameSupplierOrderId} = o.{DBNames.OrderFieldNameId}
INNER JOIN {DBNames.Database}.{DBNames.ProductTable} p ON ol.{DBNames.OrderLineFieldNameProductId} = p.{DBNames.ProductFieldNameId}
LEFT JOIN {DBNames.Database}.{DBNames.ProductSupplierTable} ps ON ps.{DBNames.ProductSupplierFieldNameSupplierId} = o.{DBNames.OrderFieldNameSupplierId}
	AND ps.{DBNames.ProductSupplierFieldNameProductId} = p.{DBNames.ProductFieldNameId}
WHERE ol.{DBNames.OrderLineFieldNameSupplierOrderId} = @OrderId
ORDER BY ol.{DBNames.OrderLineFieldNameId};";

	public string InsertOrderQuery = $@"
INSERT INTO {DBNames.Database}.{DBNames.OrderTable} (
	{DBNames.OrderFieldNameSupplierId},
	{DBNames.OrderFieldNameCurrencyId},
	{DBNames.OrderFieldNameOrderNumber},
	{DBNames.OrderFieldNameOrderDate},
	{DBNames.OrderFieldNameShippingCosts},
	{DBNames.OrderFieldNameOrderCosts},
	{DBNames.OrderFieldNameOrderMemo},
	{DBNames.OrderFieldNameClosed},
	{DBNames.OrderFieldNameClosedDate}
) VALUES (
	@{DBNames.OrderFieldNameSupplierId},
	@{DBNames.OrderFieldNameCurrencyId},
	@{DBNames.OrderFieldNameOrderNumber},
	@{DBNames.OrderFieldNameOrderDate},
	@{DBNames.OrderFieldNameShippingCosts},
	@{DBNames.OrderFieldNameOrderCosts},
	@{DBNames.OrderFieldNameOrderMemo},
	@{DBNames.OrderFieldNameClosed},
	@{DBNames.OrderFieldNameClosedDate}
);
{DBNames.SqlSelectLastId}";

	public string UpdateOrderQuery = $@"
UPDATE {DBNames.Database}.{DBNames.OrderTable}
SET
	{DBNames.OrderFieldNameSupplierId} = @{DBNames.OrderFieldNameSupplierId},
	{DBNames.OrderFieldNameCurrencyId} = @{DBNames.OrderFieldNameCurrencyId},
	{DBNames.OrderFieldNameOrderNumber} = @{DBNames.OrderFieldNameOrderNumber},
	{DBNames.OrderFieldNameOrderDate} = @{DBNames.OrderFieldNameOrderDate},
	{DBNames.OrderFieldNameShippingCosts} = @{DBNames.OrderFieldNameShippingCosts},
	{DBNames.OrderFieldNameOrderCosts} = @{DBNames.OrderFieldNameOrderCosts},
	{DBNames.OrderFieldNameOrderMemo} = @{DBNames.OrderFieldNameOrderMemo},
	{DBNames.OrderFieldNameClosed} = @{DBNames.OrderFieldNameClosed},
	{DBNames.OrderFieldNameClosedDate} = @{DBNames.OrderFieldNameClosedDate}
WHERE {DBNames.OrderFieldNameId} = @{DBNames.OrderFieldNameId};";

	/// <summary>Opens a closed order again (KAN-95). Only the order header changes; lines and stocklog are not touched.</summary>
	public static readonly string ReopenOrderQuery = $@"
UPDATE {DBNames.Database}.{DBNames.OrderTable}
SET
	{DBNames.OrderFieldNameClosed} = 0,
	{DBNames.OrderFieldNameClosedDate} = NULL
WHERE {DBNames.OrderFieldNameId} = @{DBNames.OrderFieldNameId};";

	/// <summary>
	/// Closes the open lines of an order (KAN-95), like "cancel remainder" on the receipt screen: the ordered amount
	/// becomes what was received, the line total follows and nothing stays on order. MySQL and MariaDB assign
	/// left to right, so the row total and the open amount use the new ordered amount.
	/// </summary>
	public static readonly string CloseOpenOrderLinesQuery = $@"
UPDATE {DBNames.Database}.{DBNames.OrderLineTable}
SET
	{DBNames.OrderLineFieldNameAmount} = {DBNames.OrderLineFieldNameAmount} - {DBNames.OrderLineFieldNameOpenAmount},
	{DBNames.OrderLineFieldNameRealRowTotal} = ROUND({DBNames.OrderLineFieldNamePrice} * {DBNames.OrderLineFieldNameAmount}, 2),
	{DBNames.OrderLineFieldNameOpenAmount} = 0,
	{DBNames.OrderLineFieldNameClosed} = 1,
	{DBNames.OrderLineFieldNameClosedDate} = @ClosedDate
WHERE {DBNames.OrderLineFieldNameSupplierOrderId} = @OrderId
  AND {DBNames.OrderLineFieldNameClosed} = 0;";

	/// <summary>Closes the order header (KAN-95).</summary>
	public static readonly string CloseOrderQuery = $@"
UPDATE {DBNames.Database}.{DBNames.OrderTable}
SET
	{DBNames.OrderFieldNameClosed} = 1,
	{DBNames.OrderFieldNameClosedDate} = @ClosedDate
WHERE {DBNames.OrderFieldNameId} = @OrderId;";

	public string DeleteOrderQuery = $@"
DELETE FROM {DBNames.Database}.{DBNames.OrderTable}
WHERE {DBNames.OrderFieldNameId} = @{DBNames.OrderFieldNameId};";

	public string InsertOrderLineQuery = $@"
INSERT INTO {DBNames.Database}.{DBNames.OrderLineTable} (
	{DBNames.OrderLineFieldNameSupplierOrderId},
	{DBNames.OrderLineFieldNameProductId},
	{DBNames.OrderLineFieldNameSupplierProductName},
	{DBNames.OrderLineFieldNameAmount},
	{DBNames.OrderLineFieldNameOpenAmount},
	{DBNames.OrderLineFieldNamePrice},
	{DBNames.OrderLineFieldNameRealRowTotal},
	{DBNames.OrderLineFieldNameClosed},
	{DBNames.OrderLineFieldNameClosedDate}
) VALUES (
	@{DBNames.OrderLineFieldNameSupplierOrderId},
	@{DBNames.OrderLineFieldNameProductId},
	@{DBNames.OrderLineFieldNameSupplierProductName},
	@{DBNames.OrderLineFieldNameAmount},
	@{DBNames.OrderLineFieldNameOpenAmount},
	@{DBNames.OrderLineFieldNamePrice},
	@{DBNames.OrderLineFieldNameRealRowTotal},
	@{DBNames.OrderLineFieldNameClosed},
	@{DBNames.OrderLineFieldNameClosedDate}
);
{DBNames.SqlSelectLastId}";

	public string UpdateOrderLineQuery = $@"
UPDATE {DBNames.Database}.{DBNames.OrderLineTable}
SET
	{DBNames.OrderLineFieldNameSupplierOrderId} = @{DBNames.OrderLineFieldNameSupplierOrderId},
	{DBNames.OrderLineFieldNameProductId} = @{DBNames.OrderLineFieldNameProductId},
	{DBNames.OrderLineFieldNameSupplierProductName} = @{DBNames.OrderLineFieldNameSupplierProductName},
	{DBNames.OrderLineFieldNameAmount} = @{DBNames.OrderLineFieldNameAmount},
	{DBNames.OrderLineFieldNameOpenAmount} = @{DBNames.OrderLineFieldNameOpenAmount},
	{DBNames.OrderLineFieldNamePrice} = @{DBNames.OrderLineFieldNamePrice},
	{DBNames.OrderLineFieldNameRealRowTotal} = @{DBNames.OrderLineFieldNameRealRowTotal},
	{DBNames.OrderLineFieldNameClosed} = @{DBNames.OrderLineFieldNameClosed},
	{DBNames.OrderLineFieldNameClosedDate} = @{DBNames.OrderLineFieldNameClosedDate}
WHERE {DBNames.OrderLineFieldNameId} = @{DBNames.OrderLineFieldNameId};";

	public string DeleteOrderLineQuery = $@"
DELETE FROM {DBNames.Database}.{DBNames.OrderLineTable}
WHERE {DBNames.OrderLineFieldNameId} = @{DBNames.OrderLineFieldNameId};";

	public string InsertProductSupplierQuery = $@"
INSERT INTO {DBNames.Database}.{DBNames.ProductSupplierTable} (
	{DBNames.ProductSupplierFieldNameProductId},
	{DBNames.ProductSupplierFieldNameSupplierId},
	{DBNames.ProductSupplierFieldNameProductNumber},
	{DBNames.ProductSupplierFieldNameProductName},
	{DBNames.ProductSupplierFieldNamePrice},
	{DBNames.ProductSupplierFieldNameProductUrl}
) VALUES (
	@{DBNames.ProductSupplierFieldNameProductId},
	@{DBNames.ProductSupplierFieldNameSupplierId},
	@{DBNames.ProductSupplierFieldNameProductNumber},
	@{DBNames.ProductSupplierFieldNameProductName},
	@{DBNames.ProductSupplierFieldNamePrice},
	@{DBNames.ProductSupplierFieldNameProductUrl}
);
{DBNames.SqlSelectLastId}";

	public string UpdateProductSupplierQuery = $@"
UPDATE {DBNames.Database}.{DBNames.ProductSupplierTable}
SET
	{DBNames.ProductSupplierFieldNameProductNumber} = @{DBNames.ProductSupplierFieldNameProductNumber},
	{DBNames.ProductSupplierFieldNameProductName} = @{DBNames.ProductSupplierFieldNameProductName},
	{DBNames.ProductSupplierFieldNamePrice} = @{DBNames.ProductSupplierFieldNamePrice},
	{DBNames.ProductSupplierFieldNameProductUrl} = @{DBNames.ProductSupplierFieldNameProductUrl}
WHERE {DBNames.ProductSupplierFieldNameId} = @{DBNames.ProductSupplierFieldNameId};";

	public string InsertStocklogReceiptQuery = $@"
INSERT INTO {DBNames.Database}.{DBNames.StocklogTable} (
	{DBNames.StocklogFieldNameProductId},
	{DBNames.StocklogFieldNameSupplyOrderId},
	{DBNames.StocklogFieldNameSupplyOrderlineId},
	{DBNames.StocklogFieldNameAmountReceived},
	{DBNames.StocklogFieldNameLogDate}
) VALUES (
	@{DBNames.StocklogFieldNameProductId},
	@{DBNames.StocklogFieldNameSupplyOrderId},
	@{DBNames.StocklogFieldNameSupplyOrderlineId},
	@{DBNames.StocklogFieldNameAmountReceived},
	@{DBNames.StocklogFieldNameLogDate}
);";

	public Task<int> SaveOrderAsync( StockOrderModel order, IEnumerable<StockOrderLineModel> lines, IEnumerable<int> deletedLineIds, IEnumerable<Modelbouwer.Model.ProductSupplierModel> productSuppliers )
	{
		return SaveOrderAsync( order, lines, deletedLineIds, productSuppliers, CancellationToken.None );
	}

	/// <summary>
	/// Saves order, lines, deleted lines and supplier data in one transaction. Generated ids are written back to
	/// the lines and supplier data only after the commit, so a rolled back attempt leaves them unchanged and a
	/// retry inserts again instead of updating rows that do not exist.
	/// </summary>
	public async Task<int> SaveOrderAsync( StockOrderModel order, IEnumerable<StockOrderLineModel> lines, IEnumerable<int> deletedLineIds, IEnumerable<Modelbouwer.Model.ProductSupplierModel> productSuppliers, CancellationToken cancellationToken )
	{
		List<StockOrderLineModel> lineList = lines.ToList();
		List<int> deletedIds = deletedLineIds.Where( id => id > 0 ).Distinct().ToList();
		List<Modelbouwer.Model.ProductSupplierModel> supplierList = productSuppliers.ToList();
		Dictionary<StockOrderLineModel, int> insertedLineIds = new( ReferenceEqualityComparer.Instance );
		Dictionary<Modelbouwer.Model.ProductSupplierModel, int> insertedProductSupplierIds = new( ReferenceEqualityComparer.Instance );

		int savedOrderId = await _dataService.ExecuteInTransactionAsync<int>( async ( connection, transaction ) =>
		{
			insertedLineIds.Clear();
			insertedProductSupplierIds.Clear();

			int orderId = order.Id;
			if ( orderId <= 0 )
			{
				orderId = ( int ) await ExecuteScalarInTransactionAsync<uint>( connection, transaction, InsertOrderQuery, BuildOrderParameters( order, includeId: false ), cancellationToken );
			}
			else
			{
				await ExecuteNonQueryInTransactionAsync( connection, transaction, UpdateOrderQuery, BuildOrderParameters( order, includeId: true ), cancellationToken );
			}

			foreach ( int lineId in deletedIds )
			{
				await ExecuteNonQueryInTransactionAsync( connection, transaction, DeleteOrderLineQuery, new Dictionary<string, object>
				{
					{ $"@{DBNames.OrderLineFieldNameId}", lineId }
				}, cancellationToken );
			}

			foreach ( var line in lineList )
			{
				if ( line.Id <= 0 )
				{
					var parameters = BuildOrderLineParameters( line, includeId: false );
					parameters [ $"@{DBNames.OrderLineFieldNameSupplierOrderId}" ] = orderId;
					insertedLineIds [ line ] = ( int ) await ExecuteScalarInTransactionAsync<uint>( connection, transaction, InsertOrderLineQuery, parameters, cancellationToken );
				}
				else if ( line.IsDirty || line.SupplyOrderId != orderId )
				{
					var parameters = BuildOrderLineParameters( line, includeId: true );
					parameters [ $"@{DBNames.OrderLineFieldNameSupplierOrderId}" ] = orderId;
					await ExecuteNonQueryInTransactionAsync( connection, transaction, UpdateOrderLineQuery, parameters, cancellationToken );
				}
			}

			foreach ( var productSupplier in supplierList )
			{
				var parameters = new Dictionary<string, object>
				{
					{ $"@{DBNames.ProductSupplierFieldNameProductId}", productSupplier.ProductId },
					{ $"@{DBNames.ProductSupplierFieldNameSupplierId}", productSupplier.SupplierId },
					{ $"@{DBNames.ProductSupplierFieldNameProductNumber}", productSupplier.ProductNumber ?? string.Empty },
					{ $"@{DBNames.ProductSupplierFieldNameProductName}", productSupplier.ProductName ?? string.Empty },
					{ $"@{DBNames.ProductSupplierFieldNamePrice}", productSupplier.Price },
					{ $"@{DBNames.ProductSupplierFieldNameProductUrl}", productSupplier.URL ?? string.Empty }
				};

				if ( productSupplier.ProductSupplierId > 0 )
				{
					parameters.Add( $"@{DBNames.ProductSupplierFieldNameId}", productSupplier.ProductSupplierId );
					await ExecuteNonQueryInTransactionAsync( connection, transaction, UpdateProductSupplierQuery, parameters, cancellationToken );
				}
				else
				{
					insertedProductSupplierIds [ productSupplier ] = ( int ) await ExecuteScalarInTransactionAsync<uint>( connection, transaction, InsertProductSupplierQuery, parameters, cancellationToken );
				}
			}

			return orderId;
		}, cancellationToken );

		// Committed: now the generated ids are real.
		foreach ( var line in lineList )
		{
			line.SupplyOrderId = savedOrderId;
			if ( insertedLineIds.TryGetValue( line, out int lineId ) )
				line.Id = lineId;
		}

		foreach ( var (productSupplier, productSupplierId) in insertedProductSupplierIds )
			productSupplier.ProductSupplierId = productSupplierId;

		return savedOrderId;
	}

	public Task<List<StockOrderModel>> GetAllOrdersAsync()
	{
		return GetAllOrdersAsync( CancellationToken.None );
	}

	public Task<List<StockOrderModel>> GetAllOrdersAsync( CancellationToken cancellationToken )
	{
		return _dataService.ExecuteQueryAsync( CompleteOrderListQuery, reader =>
		{
			return new StockOrderModel
			{
				Id = DatabaseValueConverter.GetInt( reader [ DBNames.OrderViewFieldNameId ] ),
				SupplierId = DatabaseValueConverter.GetInt( reader [ DBNames.OrderViewFieldNameSupplierId ] ),
				SupplierName = DatabaseValueConverter.GetString( reader [ DBNames.OrderViewFieldNameSupplierName ] ),
				CurrencyId = DatabaseValueConverter.GetInt( reader [ DBNames.OrderViewFieldNameCurrencyId ] ),
				CurrencySymbol = DatabaseValueConverter.GetString( reader [ DBNames.OrderViewFieldNameCurrencySymbol ] ),
				OrderNumber = DatabaseValueConverter.GetString( reader [ DBNames.OrderViewFieldNameOrderNumber ] ),
				OrderDate = GetNullableDateTime( reader [ DBNames.OrderViewFieldNameOrderDate ] ),
				ShippingCosts = DatabaseValueConverter.GetDouble( reader [ DBNames.OrderViewFieldNameOrderShippingCosts ] ),
				OrderCosts = DatabaseValueConverter.GetDouble( reader [ DBNames.OrderViewFieldNameOrderOrderCosts ] ),
				Closed = DatabaseValueConverter.GetSByte( reader [ DBNames.OrderViewFieldNameClosed ] ) == 1,
				ClosedDate = GetNullableDateTime( reader [ DBNames.OrderViewFieldNameClosedDate ] ),
				Memo = DatabaseValueConverter.GetString( reader [ DBNames.OrderViewFieldNameOrderMemo ] ),
				HasStockLog = DatabaseValueConverter.GetSByte( reader [ DBNames.OrderViewFieldNameHasStockLog ] ) == 1,
				OpenLineCount = DatabaseValueConverter.GetInt( reader [ "OpenLineCount" ] ),
				LineCount = DatabaseValueConverter.GetInt( reader [ "LineCount" ] ),
				LinesTotal = DatabaseValueConverter.GetDouble( reader [ "LinesTotal" ] )
			};
		}, null, cancellationToken );
	}

	public Task<List<StockOrderLineModel>> GetOrderLinesAsync( int orderId )
	{
		return GetOrderLinesAsync( orderId, CancellationToken.None );
	}

	public Task<List<StockOrderLineModel>> GetOrderLinesAsync( int orderId, CancellationToken cancellationToken )
	{
		return _dataService.ExecuteQueryAsync(
			OrderLinesQuery,
			reader => new StockOrderLineModel
			{
				Id = DatabaseValueConverter.GetInt( reader [ DBNames.OrderLineFieldNameId ] ),
				SupplyOrderId = DatabaseValueConverter.GetInt( reader [ DBNames.OrderLineViewFieldNameOrderId ] ),
				SupplierId = DatabaseValueConverter.GetInt( reader [ DBNames.OrderLineViewFieldNameSupplierId ] ),
				ProductId = DatabaseValueConverter.GetInt( reader [ DBNames.OrderLineViewFieldNameProductId ] ),
				ProductCode = DatabaseValueConverter.GetString( reader [ DBNames.OrderLineViewFieldNameProductCode ] ),
				ProductName = DatabaseValueConverter.GetString( reader [ DBNames.OrderLineViewFieldNameProductName ] ),
				SupplierProductNumber = DatabaseValueConverter.GetString( reader [ DBNames.ProductSupplierFieldNameProductNumber ] ),
				SupplierProductName = DatabaseValueConverter.GetString( reader [ DBNames.OrderLineFieldNameSupplierProductName ] ),
				Amount = DatabaseValueConverter.GetDouble( reader [ DBNames.OrderLineFieldNameAmount ] ),
				OpenAmount = DatabaseValueConverter.GetDouble( reader [ DBNames.OrderLineFieldNameOpenAmount ] ),
				Price = DatabaseValueConverter.GetDouble( reader [ DBNames.OrderLineFieldNamePrice ] ),
				RealRowTotal = DatabaseValueConverter.GetDouble( reader [ DBNames.OrderLineFieldNameRealRowTotal ] ),
				Received = DatabaseValueConverter.GetDouble( reader [ DBNames.OrderLineViewFieldNameReceived ] ),
				Expected = DatabaseValueConverter.GetDouble( reader [ DBNames.OrderLineViewFieldNameExpected ] ),
				Closed = DatabaseValueConverter.GetSByte( reader [ DBNames.OrderLineViewFieldNameClosed ] ) == 1,
				ClosedDate = GetNullableDateTime( reader [ DBNames.OrderLineViewFieldNameClosedDate ] )
			},
			new Dictionary<string, object> { { "@OrderId", orderId } },
			cancellationToken );
	}

	public async Task<int> InsertOrderAsync( StockOrderModel order )
	{
		return await InsertOrderAsync( order, CancellationToken.None );
	}

	public async Task<int> InsertOrderAsync( StockOrderModel order, CancellationToken cancellationToken )
	{
		uint newId = await _dataService.ExecuteScalarAsync<uint>( InsertOrderQuery, BuildOrderParameters( order, includeId: false ), cancellationToken );
		return ( int ) newId;
	}

	public Task<int> InsertOrderWithLinesAsync( StockOrderModel order, IEnumerable<StockOrderLineModel> lines )
	{
		return InsertOrderWithLinesAsync( order, lines, CancellationToken.None );
	}

	public Task<int> InsertOrderWithLinesAsync( StockOrderModel order, IEnumerable<StockOrderLineModel> lines, CancellationToken cancellationToken )
	{
		List<StockOrderLineModel> lineList = lines.ToList();

		return _dataService.ExecuteInTransactionAsync<int>( async ( connection, transaction ) =>
		{
			uint orderId = await ExecuteScalarInTransactionAsync<uint>( connection, transaction, InsertOrderQuery, BuildOrderParameters( order, includeId: false ), cancellationToken );

			foreach ( var line in lineList )
			{
				line.SupplyOrderId = ( int ) orderId;
				uint lineId = await ExecuteScalarInTransactionAsync<uint>( connection, transaction, InsertOrderLineQuery, BuildOrderLineParameters( line, includeId: false ), cancellationToken );
				line.Id = ( int ) lineId;
				// No stocklog entry here: an order does not change the inventory, only a receipt does (KAN-100).
			}

			return ( int ) orderId;
		}, cancellationToken );
	}

	public Task UpdateOrderAsync( StockOrderModel order )
	{
		return UpdateOrderAsync( order, CancellationToken.None );
	}

	public Task UpdateOrderAsync( StockOrderModel order, CancellationToken cancellationToken )
	{
		return _dataService.ExecuteScalarAsync<uint>( UpdateOrderQuery, BuildOrderParameters( order, includeId: true ), cancellationToken );
	}

	public Task DeleteOrderAsync( int orderId )
	{
		return DeleteOrderAsync( orderId, CancellationToken.None );
	}

	/// <summary>Deleting an order always goes through the guarded delete, so receipts are never orphaned.</summary>
	public Task DeleteOrderAsync( int orderId, CancellationToken cancellationToken )
	{
		return DeleteOrderWithLinesAsync( orderId, [ ], cancellationToken );
	}

	public Task DeleteOrderWithLinesAsync( int orderId, IEnumerable<StockOrderLineModel> lines )
	{
		return DeleteOrderWithLinesAsync( orderId, lines, CancellationToken.None );
	}

	#region Delete an order (KAN-137)
	private static readonly string OrderLineIdsOfOrder =
		$"SELECT {DBNames.OrderLineFieldNameId} FROM {DBNames.Database}.{DBNames.OrderLineTable} " +
		$"WHERE {DBNames.OrderLineFieldNameSupplierOrderId} = @OrderId";

	/// <summary>Receipts in the stocklog or received amounts on the lines of the order.</summary>
	public static readonly string CountOrderReceiptsQuery =
		$"SELECT " +
		$"(SELECT COUNT(*) FROM {DBNames.Database}.{DBNames.StocklogTable} " +
		$" WHERE {DBNames.StocklogFieldNameAmountReceived} <> 0 " +
		$"   AND ({DBNames.StocklogFieldNameSupplyOrderId} = @OrderId OR {DBNames.StocklogFieldNameSupplyOrderlineId} IN ({OrderLineIdsOfOrder}))) + " +
		$"(SELECT COUNT(*) FROM {DBNames.Database}.{DBNames.OrderLineTable} " +
		$" WHERE {DBNames.OrderLineFieldNameSupplierOrderId} = @OrderId " +
		$"   AND {DBNames.OrderLineFieldNameAmount} - {DBNames.OrderLineFieldNameOpenAmount} > 0);";

	/// <summary>Stocklog rows of an order without receipts: the corrections orders wrote before KAN-100.</summary>
	/// <remarks>Never deletes a receipt: one recorded after the check stays, so its stock is not lost.</remarks>
	public static readonly string DeleteOrderStocklogQuery =
		$"DELETE FROM {DBNames.Database}.{DBNames.StocklogTable} " +
		$"WHERE IFNULL({DBNames.StocklogFieldNameAmountReceived}, 0) = 0 " +
		$"  AND ({DBNames.StocklogFieldNameSupplyOrderId} = @OrderId OR {DBNames.StocklogFieldNameSupplyOrderlineId} IN ({OrderLineIdsOfOrder}));";

	public static readonly string DeleteOrderLinesOfOrderQuery =
		$"DELETE FROM {DBNames.Database}.{DBNames.OrderLineTable} " +
		$"WHERE {DBNames.OrderLineFieldNameSupplierOrderId} = @OrderId;";

	/// <summary>
	/// Deletes an order with everything that belongs to it: its lines and the old correction rows in the stocklog.
	/// An order with receipts is refused with <see cref="OrderHasReceiptsException"/>. The check reads the database
	/// right before deleting, in the same transaction, so a receipt booked after the screen loaded the order is
	/// seen. It takes no locks, so a receipt committed in the very moment between check and delete is not
	/// refused; it is never deleted itself either (see <see cref="DeleteOrderStocklogQuery"/>), so its stock is
	/// kept. For a single-user application that window is accepted. The lines are taken from the database, the
	/// <paramref name="lines"/> argument is kept for callers only.
	/// </summary>
	public Task DeleteOrderWithLinesAsync( int orderId, IEnumerable<StockOrderLineModel> lines, CancellationToken cancellationToken )
	{
		return _dataService.ExecuteInTransactionAsync( ( connection, transaction ) => DeleteOrderStepsAsync(
			orderId,
			( query, parameters ) => ExecuteScalarInTransactionAsync<long>( connection, transaction, query, parameters, cancellationToken ),
			( query, parameters ) => ExecuteNonQueryInTransactionAsync( connection, transaction, query, parameters, cancellationToken ) ),
			cancellationToken );
	}

	/// <summary>The statements of a guarded order delete, separate from the connection so tests can run them.</summary>
	public async Task DeleteOrderStepsAsync(
		int orderId,
		Func<string, Dictionary<string, object>, Task<long>> countAsync,
		Func<string, Dictionary<string, object>, Task> executeAsync )
	{
		var parameters = new Dictionary<string, object> { { "@OrderId", orderId } };

		if ( await countAsync( CountOrderReceiptsQuery, parameters ) > 0 )
			throw new OrderHasReceiptsException( orderId );

		await executeAsync( DeleteOrderStocklogQuery, parameters );
		await executeAsync( DeleteOrderLinesOfOrderQuery, parameters );
		await executeAsync( DeleteOrderQuery, new Dictionary<string, object>
		{
			{ $"@{DBNames.OrderFieldNameId}", orderId }
		} );
	}
	#endregion

	public Task ReopenOrderAsync( int orderId )
	{
		return ReopenOrderAsync( orderId, CancellationToken.None );
	}

	public Task ReopenOrderAsync( int orderId, CancellationToken cancellationToken )
	{
		return _dataService.ExecuteScalarAsync<uint>( ReopenOrderQuery, new Dictionary<string, object>
		{
			{ $"@{DBNames.OrderFieldNameId}", orderId }
		}, cancellationToken );
	}

	public Task CloseOrderAsync( int orderId, DateTime closedDate )
	{
		return CloseOrderAsync( orderId, closedDate, CancellationToken.None );
	}

	public Task CloseOrderAsync( int orderId, DateTime closedDate, CancellationToken cancellationToken )
	{
		return _dataService.ExecuteInTransactionAsync( ( connection, transaction ) => CloseOrderStepsAsync(
			orderId,
			closedDate,
			( query, parameters ) => ExecuteNonQueryInTransactionAsync( connection, transaction, query, parameters, cancellationToken ) ),
			cancellationToken );
	}

	/// <summary>The statements of closing an order, separate from the connection so tests can run them.</summary>
	public static async Task CloseOrderStepsAsync( int orderId, DateTime closedDate, Func<string, Dictionary<string, object>, Task> executeAsync )
	{
		var parameters = new Dictionary<string, object>
		{
			{ "@OrderId", orderId },
			{ "@ClosedDate", closedDate.Date }
		};

		await executeAsync( CloseOpenOrderLinesQuery, parameters );
		await executeAsync( CloseOrderQuery, parameters );
	}

	public async Task<int> InsertOrderLineAsync( StockOrderLineModel line )
	{
		return await InsertOrderLineAsync( line, CancellationToken.None );
	}

	public async Task<int> InsertOrderLineAsync( StockOrderLineModel line, CancellationToken cancellationToken )
	{
		uint newId = await _dataService.ExecuteScalarAsync<uint>( InsertOrderLineQuery, BuildOrderLineParameters( line, includeId: false ), cancellationToken );
		return ( int ) newId;
	}

	public Task UpdateOrderLineAsync( StockOrderLineModel line )
	{
		return UpdateOrderLineAsync( line, CancellationToken.None );
	}

	public Task UpdateOrderLineAsync( StockOrderLineModel line, CancellationToken cancellationToken )
	{
		return _dataService.ExecuteScalarAsync<uint>( UpdateOrderLineQuery, BuildOrderLineParameters( line, includeId: true ), cancellationToken );
	}

	public Task RegisterReceiptAsync( StockOrderLineModel line, double receivedAmount, DateTime? deliveryDate )
	{
		return RegisterReceiptAsync( line, receivedAmount, deliveryDate, CancellationToken.None );
	}

	public Task RegisterReceiptAsync( StockOrderLineModel line, double receivedAmount, DateTime? deliveryDate, CancellationToken cancellationToken )
	{
		return _dataService.ExecuteInTransactionAsync( async ( connection, transaction ) =>
		{
			await ExecuteNonQueryInTransactionAsync( connection, transaction, UpdateOrderLineQuery, BuildOrderLineParameters( line, includeId: true ), cancellationToken );
			await InsertStocklogReceiptAsync( connection, transaction, line.ProductId, line.SupplyOrderId, line.Id, receivedAmount, deliveryDate, cancellationToken );
		}, cancellationToken );
	}

	public Task DeleteOrderLineAsync( int lineId )
	{
		return DeleteOrderLineAsync( lineId, CancellationToken.None );
	}

	public Task DeleteOrderLineAsync( int lineId, CancellationToken cancellationToken )
	{
		return _dataService.ExecuteScalarAsync<uint>( DeleteOrderLineQuery, new Dictionary<string, object>
		{
			{ $"@{DBNames.OrderLineFieldNameId}", lineId }
		}, cancellationToken );
	}

	private Dictionary<string, object> BuildOrderParameters( StockOrderModel order, bool includeId )
	{
		Dictionary<string, object> parameters = new()
		{
			{ $"@{DBNames.OrderFieldNameSupplierId}", order.SupplierId },
			{ $"@{DBNames.OrderFieldNameCurrencyId}", order.CurrencyId },
			{ $"@{DBNames.OrderFieldNameOrderNumber}", order.OrderNumber ?? string.Empty },
			{ $"@{DBNames.OrderFieldNameOrderDate}", order.OrderDate is DateTime orderDate ? orderDate : DBNull.Value },
			{ $"@{DBNames.OrderFieldNameShippingCosts}", order.ShippingCosts },
			{ $"@{DBNames.OrderFieldNameOrderCosts}", order.OrderCosts },
			{ $"@{DBNames.OrderFieldNameOrderMemo}", order.Memo ?? string.Empty },
			{ $"@{DBNames.OrderFieldNameClosed}", order.Closed },
			{ $"@{DBNames.OrderFieldNameClosedDate}", order.ClosedDate is DateTime closedDate ? closedDate : DBNull.Value }
		};

		if ( includeId )
		{
			parameters.Add( $"@{DBNames.OrderFieldNameId}", order.Id );
		}

		return parameters;
	}

	private Dictionary<string, object> BuildOrderLineParameters( StockOrderLineModel line, bool includeId )
	{
		Dictionary<string, object> parameters = new()
		{
			{ $"@{DBNames.OrderLineFieldNameSupplierOrderId}", line.SupplyOrderId },
			{ $"@{DBNames.OrderLineFieldNameProductId}", line.ProductId },
			{ $"@{DBNames.OrderLineFieldNameSupplierProductName}", line.SupplierProductName ?? string.Empty },
			{ $"@{DBNames.OrderLineFieldNameAmount}", line.Amount },
			{ $"@{DBNames.OrderLineFieldNameOpenAmount}", line.Id > 0 || line.Closed || line.OpenAmount > 0 ? line.OpenAmount : line.Amount },   // only a new line without an open amount starts at the ordered amount
			{ $"@{DBNames.OrderLineFieldNamePrice}", line.Price },
			{ $"@{DBNames.OrderLineFieldNameRealRowTotal}", line.RealRowTotal },
			{ $"@{DBNames.OrderLineFieldNameClosed}", line.Closed },
			{ $"@{DBNames.OrderLineFieldNameClosedDate}", line.ClosedDate is DateTime closedDate ? closedDate : DBNull.Value }
		};

		if ( includeId )
		{
			parameters.Add( $"@{DBNames.OrderLineFieldNameId}", line.Id );
		}

		return parameters;
	}

	private static DateTime? GetNullableDateTime( object value )
	{
		return value == null || value == DBNull.Value
			? null
			: Convert.ToDateTime( value );
	}

	private static async Task<T> ExecuteScalarInTransactionAsync<T>( MySqlConnection connection, MySqlTransaction transaction, string query, Dictionary<string, object> parameters, CancellationToken cancellationToken )
	{
		await using MySqlCommand command = new(query, connection, transaction);

		foreach ( var parameter in parameters )
		{
			command.Parameters.AddWithValue( parameter.Key, parameter.Value ?? DBNull.Value );
		}

		object? result = await command.ExecuteScalarAsync( cancellationToken );

		if ( result == null || result == DBNull.Value )
			return default!;

		Type targetType = Nullable.GetUnderlyingType( typeof( T ) ) ?? typeof( T );
		object converted = Convert.ChangeType( result, targetType );
		return ( T ) converted;
	}

	private static async Task ExecuteNonQueryInTransactionAsync( MySqlConnection connection, MySqlTransaction transaction, string query, Dictionary<string, object> parameters, CancellationToken cancellationToken )
	{
		await using MySqlCommand command = new(query, connection, transaction);

		foreach ( var parameter in parameters )
		{
			command.Parameters.AddWithValue( parameter.Key, parameter.Value ?? DBNull.Value );
		}

		await command.ExecuteNonQueryAsync( cancellationToken );
	}

	private Task InsertStocklogReceiptAsync( MySqlConnection connection, MySqlTransaction transaction, int productId, int supplyOrderId, int supplyOrderLineId, double receivedAmount, DateTime? deliveryDate, CancellationToken cancellationToken )
	{
		if ( productId <= 0 || receivedAmount == 0d )
			return Task.CompletedTask;

		return ExecuteNonQueryInTransactionAsync( connection, transaction, InsertStocklogReceiptQuery, new Dictionary<string, object>
		{
			{ $"@{DBNames.StocklogFieldNameProductId}", productId },
			{ $"@{DBNames.StocklogFieldNameSupplyOrderId}", supplyOrderId > 0 ? supplyOrderId : DBNull.Value },
			{ $"@{DBNames.StocklogFieldNameSupplyOrderlineId}", supplyOrderLineId > 0 ? supplyOrderLineId : DBNull.Value },
			{ $"@{DBNames.StocklogFieldNameAmountReceived}", receivedAmount },
			{ $"@{DBNames.StocklogFieldNameLogDate}", deliveryDate is DateTime date ? date : DateTime.Today }
		}, cancellationToken );
	}
}