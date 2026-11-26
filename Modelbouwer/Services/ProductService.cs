namespace Modelbouwer.Services;

public class ProductService : IProductService
{
	private readonly GenericDataService _dataService;
	public bool ProductUsed { get; set; }

	public ProductService( GenericDataService dataService )
	{
		_dataService = dataService;
	}

	#region Database query's
	public string CompleteProductList = $"" +
		$"SELECT " +
		$"{ DBNames.ProductFieldNameBrandId}, " +
		$"{ DBNames.ProductFieldNameCategoryId}, " +
		$"{ DBNames.ProductFieldNameCode}, " +
		$"{ DBNames.ProductFieldNameDimensions}, " +
		$"{ DBNames.ProductFieldNameHide}, " +
		$"{ DBNames.ProductFieldNameId}, " +
		$"{ DBNames.ProductFieldNameImage}, " +
		$"{ DBNames.ProductFieldNameImageRotationAngle}, " +
		$"{ DBNames.ProductFieldNameMemo}, " +
		$"{ DBNames.ProductFieldNameMinimalStock}, " +
		$"{ DBNames.ProductFieldNameName}, " +
		$"{ DBNames.ProductFieldNamePrice}, " +
		$"{ DBNames.ProductFieldNameProjectCosts}, " +
		$"{ DBNames.ProductFieldNameStandardOrderQuantity}, " +
		$"{ DBNames.ProductFieldNameStorageId}, " +
		$"{ DBNames.ProductFieldNameUnitId}" +
		$" FROM {DBNames.Database}.{DBNames.ProductTable};";

	public string CompleteBrandList = $"" +
		$"SELECT " +
		$"{DBNames.BrandFieldNameId} AS {DBNames.BrandFieldNameId}, " +
		$"{DBNames.BrandFieldNameName} AS {DBNames.BrandFieldNameName}" +
		$" FROM {DBNames.Database}.{DBNames.BrandTable};";

	public string CompleteUnitList = $"" +
		$"SELECT " +
		$"{DBNames.UnitFieldNameUnitId} AS {DBNames.UnitFieldNameUnitId}, " +
		$"{DBNames.UnitFieldNameUnitName} AS {DBNames.UnitFieldNameUnitName}" +
		$" FROM {DBNames.Database}.{DBNames.UnitTable};";

	public string CompleteCategoryList = $"" +
		$"SELECT " +
		$"{DBNames.CategoryFieldNameId} AS {DBNames.CategoryFieldNameId}, " +
		$"{DBNames.CategoryFieldNameParentId} AS {DBNames.CategoryFieldNameParentId}, " +
		$"{DBNames.CategoryFieldNameName} AS {DBNames.CategoryFieldNameName}" +
		$" FROM {DBNames.Database}.{DBNames.CategoryTable};";

	public string AddNewProductQuery =
		$"INSERT INTO {DBNames.Database}.{DBNames.ProductTable} ( " +
		$"{ DBNames.ProductFieldNameBrandId}, " +
		$"{ DBNames.ProductFieldNameCategoryId}, " +
		$"{ DBNames.ProductFieldNameCode}, " +
		$"{ DBNames.ProductFieldNameDimensions}, " +
		$"{ DBNames.ProductFieldNameHide}, " +
		$"{ DBNames.ProductFieldNameImage}, " +
		$"{ DBNames.ProductFieldNameImageRotationAngle}, " +
		$"{ DBNames.ProductFieldNameMemo}, " +
		$"{ DBNames.ProductFieldNameMinimalStock}, " +
		$"{ DBNames.ProductFieldNameName}, " +
		$"{ DBNames.ProductFieldNamePrice}, " +
		$"{ DBNames.ProductFieldNameProjectCosts}, " +
		$"{ DBNames.ProductFieldNameStandardOrderQuantity}, " +
		$"{ DBNames.ProductFieldNameStorageId}, " +
		$"{ DBNames.ProductFieldNameUnitId}" +
		$" ) VALUES ( " +
		$"@{ DBNames.ProductFieldNameBrandId}, " +
		$"@{ DBNames.ProductFieldNameCategoryId}, " +
		$"@{ DBNames.ProductFieldNameCode}, " +
		$"@{ DBNames.ProductFieldNameDimensions}, " +
		$"@{ DBNames.ProductFieldNameHide}, " +
		$"@{ DBNames.ProductFieldNameImage}, " +
		$"@{ DBNames.ProductFieldNameImageRotationAngle}, " +
		$"@{ DBNames.ProductFieldNameMemo}, " +
		$"@{ DBNames.ProductFieldNameMinimalStock}, " +
		$"@{ DBNames.ProductFieldNameName}, " +
		$"@{ DBNames.ProductFieldNamePrice}, " +
		$"@{ DBNames.ProductFieldNameProjectCosts}, " +
		$"@{ DBNames.ProductFieldNameStandardOrderQuantity}, " +
		$"@{ DBNames.ProductFieldNameStorageId}, " +
		$"@{ DBNames.ProductFieldNameUnitId} " +
		$"); { DBNames.SqlSelectLastId};";

	public string UpdateProductQuery =
		$"UPDATE {DBNames.Database}.{DBNames.ProductTable} " +
		$"SET " +
		$"{DBNames.ProductFieldNameBrandId} = @{DBNames.ProductFieldNameBrandId}, " +
		$"{DBNames.ProductFieldNameCategoryId} = @{DBNames.ProductFieldNameCategoryId}, " +
		$"{DBNames.ProductFieldNameCode} = @{DBNames.ProductFieldNameCode}, " +
		$"{DBNames.ProductFieldNameDimensions} = @{DBNames.ProductFieldNameDimensions}, " +
		$"{DBNames.ProductFieldNameHide} = @{DBNames.ProductFieldNameHide}, " +
		$"{DBNames.ProductFieldNameImage} = @{DBNames.ProductFieldNameImage}, " +
		$"{DBNames.ProductFieldNameImageRotationAngle} = @{DBNames.ProductFieldNameImageRotationAngle}, " +
		$"{DBNames.ProductFieldNameMemo} = @{DBNames.ProductFieldNameMemo}, " +
		$"{DBNames.ProductFieldNameMinimalStock} = @{DBNames.ProductFieldNameMinimalStock}, " +
		$"{DBNames.ProductFieldNameName} = @{DBNames.ProductFieldNameName}, " +
		$"{DBNames.ProductFieldNamePrice} = @{DBNames.ProductFieldNamePrice}, " +
		$"{DBNames.ProductFieldNameProjectCosts} = @{DBNames.ProductFieldNameProjectCosts}, " +
		$"{DBNames.ProductFieldNameStandardOrderQuantity} = @{DBNames.ProductFieldNameStandardOrderQuantity}, " +
		$"{DBNames.ProductFieldNameStorageId} = @{DBNames.ProductFieldNameStorageId}, " +
		$"{DBNames.ProductFieldNameUnitId} = @{DBNames.ProductFieldNameUnitId} " +
		$"WHERE {DBNames.ProductFieldNameId} = @{DBNames.ProductFieldNameId};";

	public string DeleteProductQuery =
		$"DELETE FROM {DBNames.Database}.{DBNames.ProductTable} " +
		$"WHERE {DBNames.ProductFieldNameId} = @{DBNames.ProductFieldNameId};";

	public string ProductNameExistsQuery =
		$"SELECT COUNT({DBNames.ProductFieldNameId}) " +
		$"FROM {DBNames.Database}.{DBNames.ProductTable} " +
		$"WHERE LOWER({DBNames.ProductFieldNameName}) = LOWER(@{DBNames.ProductFieldNameName})";

	public string ProductUsedQuery = $"" +
		$"SELECT" +
		$"EXISTS( " +
		$"SELECT 1 " +
		$"FROM {DBNames.Database}.{DBNames.ProductUsageTable} " +
		$"WHERE {DBNames.ProductUsageFieldNameProductId} = @ProductId " +
		$"LIMIT 1 ) " +
		$"OR EXISTS( " +
		$"SELECT 1 " +
		$"FROM {DBNames.Database}.{DBNames.OrderLineTable} " +
		$"WHERE {DBNames.OrderLineFieldNameProductId} = @ProductId " +
		$"LIMIT 1 ) " +
		$"OR EXISTS( " +
		$"SELECT 1 " +
		$"FROM {DBNames.Database}.{DBNames.ProductSupplierTable} " +
		$"WHERE {DBNames.ProductSupplierFieldNameProductId} = @ProductId " +
		$"LIMIT 1 ) " +
		$"AS ProductInUse;";
	#endregion

	public Task<List<ProductModel>> GetAllProductsAsync() => GetAllProductsAsync( CancellationToken.None );

	public Task<List<ProductModel>> GetAllProductsAsync( CancellationToken cancellationToken )
	{
		return _dataService.ExecuteQueryAsync( CompleteProductList, reader =>
		{
			return new ProductModel
			{
				ProductBrandId = DatabaseValueConverter.GetInt( reader [ $"{DBNames.ProductFieldNameBrandId}" ] ),
				ProductCategoryId = DatabaseValueConverter.GetInt( reader [ $"{DBNames.ProductFieldNameCategoryId}" ] ),
				ProductCode = DatabaseValueConverter.GetString( reader [ $"{DBNames.ProductFieldNameCode}" ] ),
				ProductDimensions = DatabaseValueConverter.GetString( reader [ $"{DBNames.ProductFieldNameDimensions}" ] ),
				ProductHide = DatabaseValueConverter.GetInt( reader [ $"{DBNames.ProductFieldNameHide}" ] ),
				ProductId = DatabaseValueConverter.GetInt( reader [ $"{DBNames.ProductFieldNameId}" ] ),
				ProductImage = DatabaseValueConverter.GetBytes( reader [ $"{DBNames.ProductFieldNameImage}" ] ),
				ProductImageRotationAngle = DatabaseValueConverter.GetDouble( reader [ $"{DBNames.ProductFieldNameImageRotationAngle}" ] ),
				ProductMemo = DatabaseValueConverter.GetString( reader [ $"{DBNames.ProductFieldNameMemo}" ] ),
				ProductMinimalStock = DatabaseValueConverter.GetDouble( reader [ $"{DBNames.ProductFieldNameMinimalStock}" ] ),
				ProductName = DatabaseValueConverter.GetString( reader [ $"{DBNames.ProductFieldNameName}" ] ),
				ProductPrice = DatabaseValueConverter.GetDouble( reader [ $"{DBNames.ProductFieldNamePrice}" ] ),
				ProductProjectCosts = DatabaseValueConverter.GetInt( reader [ $"{DBNames.ProductFieldNameProjectCosts}" ] ),
				ProductStandardQuantity = DatabaseValueConverter.GetDouble( reader [ $"{DBNames.ProductFieldNameStandardOrderQuantity}" ] ),
				ProductStorageId = DatabaseValueConverter.GetInt( reader [ $"{DBNames.ProductFieldNameStorageId}" ] ),
				ProductUnitId = DatabaseValueConverter.GetInt( reader [ $"{DBNames.ProductFieldNameUnitId}" ] )
			};
		}, null, cancellationToken );
	}

	public Task<List<BrandModel>> GetAllBrandsAsync() => GetAllBrandsAsync( CancellationToken.None );

	public Task<List<BrandModel>> GetAllBrandsAsync( CancellationToken cancellationToken )
	{
		return _dataService.ExecuteQueryAsync( CompleteBrandList, reader =>
		{
			return new BrandModel
			{
				BrandId = DatabaseValueConverter.GetInt( reader [ $"{DBNames.BrandFieldNameId}" ] ),
				BrandName = DatabaseValueConverter.GetString( reader [ $"{DBNames.BrandFieldNameName}" ] )
			};
		}, null, cancellationToken );
	}

	public Task<List<UnitModel>> GetAllUnitsAsync() => GetAllUnitsAsync( CancellationToken.None );

	public Task<List<UnitModel>> GetAllUnitsAsync( CancellationToken cancellationToken )
	{
		return _dataService.ExecuteQueryAsync( CompleteUnitList, reader =>
		{
			return new UnitModel
			{
				UnitId = DatabaseValueConverter.GetInt( reader [ $"{DBNames.UnitFieldNameUnitId}" ] ),
				UnitName = DatabaseValueConverter.GetString( reader [ $"{DBNames.UnitFieldNameUnitName}" ] )
			};
		}, null, cancellationToken );
	}

	public Task<List<CategoryModel>> GetAllCategoriesAsync() => GetAllCategoriesAsync( CancellationToken.None );

	public Task<List<CategoryModel>> GetAllCategoriesAsync( CancellationToken cancellationToken )
	{
		return _dataService.ExecuteQueryAsync( CompleteCategoryList, reader =>
		{
			return new CategoryModel
			{
				CategoryId = DatabaseValueConverter.GetInt( reader [ $"{DBNames.CategoryFieldNameId}" ] ),
				ParentId = DatabaseValueConverter.GetInt( reader [ $"{DBNames.CategoryFieldNameParentId}" ] ),
				CategoryName = DatabaseValueConverter.GetString( reader [ $"{DBNames.CategoryFieldNameName}" ] )
			};
		}, null, cancellationToken );
	}

	public Task<int> InsertNewProductAsync( Dictionary<string, object?> queryParameters ) => InsertNewProductAsync( queryParameters, CancellationToken.None );

	public async Task<int> InsertNewProductAsync( Dictionary<string, object?> queryParameters, CancellationToken cancellationToken )
	{
		Dictionary<string, object> parameters = new()
		{
			{ $"{DBNames.ProductFieldNameBrandId}", queryParameters[$"@{DBNames.ProductFieldNameBrandId}"] ?? DBNull.Value },
			{ $"{DBNames.ProductFieldNameCategoryId}", queryParameters[$"@{DBNames.ProductFieldNameCategoryId}"] ?? DBNull.Value },
			{ $"{DBNames.ProductFieldNameCode}", queryParameters[$"@{DBNames.ProductFieldNameCode}"] ?? DBNull.Value },
			{ $"{DBNames.ProductFieldNameDimensions}", queryParameters[$"@{DBNames.ProductFieldNameDimensions}"] ?? DBNull.Value },
			{ $"{DBNames.ProductFieldNameHide}", queryParameters[$"@{DBNames.ProductFieldNameHide}"] ?? DBNull.Value },
			{ $"{DBNames.ProductFieldNameImage}", queryParameters[$"@{DBNames.ProductFieldNameImage}"] ?? DBNull.Value },
			{ $"{DBNames.ProductFieldNameImageRotationAngle}", queryParameters[$"@{DBNames.ProductFieldNameImageRotationAngle}"] ?? DBNull.Value },
			{ $"{DBNames.ProductFieldNameMemo}", queryParameters[$"@{DBNames.ProductFieldNameMemo}"] ?? DBNull.Value },
			{ $"{DBNames.ProductFieldNameMinimalStock}", queryParameters[$"@{DBNames.ProductFieldNameMinimalStock}"] ?? DBNull.Value },
			{ $"{DBNames.ProductFieldNameName}" , queryParameters[$"@{DBNames.ProductFieldNameName}"] ?? DBNull.Value },
			{ $"{DBNames.ProductFieldNamePrice}", queryParameters[$"@{DBNames.ProductFieldNamePrice}"] ?? DBNull.Value },
			{ $"{DBNames.ProductFieldNameProjectCosts}", queryParameters[$"@{DBNames.ProductFieldNameProjectCosts}"] ?? DBNull.Value },
			{ $"{DBNames.ProductFieldNameStandardOrderQuantity}", queryParameters[$"@{DBNames.ProductFieldNameStandardOrderQuantity}"] ?? DBNull.Value },
			{ $"{DBNames.ProductFieldNameStorageId}", queryParameters[$"@{DBNames.ProductFieldNameStorageId}"] ?? DBNull.Value },
			{ $"{DBNames.ProductFieldNameUnitId}", queryParameters[$"@{DBNames.ProductFieldNameUnitId}"] ?? DBNull.Value }
		};

		uint newId = await _dataService.ExecuteScalarAsync<uint>( AddNewProductQuery, parameters, cancellationToken );

		return ( int ) newId;
	}

	public Task UpdateProductAsync( Dictionary<string, object?> queryParameters ) => UpdateProductAsync( queryParameters, CancellationToken.None );

	public async Task UpdateProductAsync( Dictionary<string, object?> queryParameters, CancellationToken cancellationToken )
	{
		Dictionary<string, object> parameters = new()
		{
			{ $"{DBNames.ProductFieldNameId}", queryParameters[$"@{DBNames.ProductFieldNameId}"] ?? DBNull.Value },
			{ $"{DBNames.ProductFieldNameBrandId}", queryParameters[$"@{DBNames.ProductFieldNameBrandId}"] ?? DBNull.Value },
			{ $"{DBNames.ProductFieldNameCategoryId}", queryParameters[$"@{DBNames.ProductFieldNameCategoryId}"] ?? DBNull.Value },
			{ $"{DBNames.ProductFieldNameCode}", queryParameters[$"@{DBNames.ProductFieldNameCode}"] ?? DBNull.Value },
			{ $"{DBNames.ProductFieldNameDimensions}", queryParameters[$"@{DBNames.ProductFieldNameDimensions}"] ?? DBNull.Value },
			{ $"{DBNames.ProductFieldNameHide}", queryParameters[$"@{DBNames.ProductFieldNameHide}"] ?? DBNull.Value },
			{ $"{DBNames.ProductFieldNameImage}", queryParameters[$"@{DBNames.ProductFieldNameImage}"] ?? DBNull.Value },
			{ $"{DBNames.ProductFieldNameImageRotationAngle}", queryParameters[$"@{DBNames.ProductFieldNameImageRotationAngle}"] ?? DBNull.Value },
			{ $"{DBNames.ProductFieldNameMemo}", queryParameters[$"@{DBNames.ProductFieldNameMemo}"] ?? DBNull.Value },
			{ $"{DBNames.ProductFieldNameMinimalStock}", queryParameters[$"@{DBNames.ProductFieldNameMinimalStock}"] ?? DBNull.Value },
			{ $"{DBNames.ProductFieldNameName}" , queryParameters[$"@{DBNames.ProductFieldNameName}"] ?? DBNull.Value },
			{ $"{DBNames.ProductFieldNamePrice}", queryParameters[$"@{DBNames.ProductFieldNamePrice}"] ?? DBNull.Value },
			{ $"{DBNames.ProductFieldNameProjectCosts}", queryParameters[$"@{DBNames.ProductFieldNameProjectCosts}"] ?? DBNull.Value },
			{ $"{DBNames.ProductFieldNameStandardOrderQuantity}", queryParameters[$"@{DBNames.ProductFieldNameStandardOrderQuantity}"] ?? DBNull.Value },
			{ $"{DBNames.ProductFieldNameStorageId}", queryParameters[$"@{DBNames.ProductFieldNameStorageId}"] ?? DBNull.Value },
			{ $"{DBNames.ProductFieldNameUnitId}", queryParameters[$"@{DBNames.ProductFieldNameUnitId}"] ?? DBNull.Value }
		};

		await _dataService.ExecuteScalarAsync<uint>( UpdateProductQuery, parameters, cancellationToken );
	}

	public Task DeleteProductAsync( int productId ) => DeleteProductAsync( productId, CancellationToken.None );

	public async Task DeleteProductAsync( int productId, CancellationToken cancellationToken )
	{
		var parameters = new Dictionary<string, object>
		{
			{ $"@{DBNames.ProductFieldNameId}", productId }
		};

		try
		{
			await _dataService.ExecuteScalarAsync<uint>( DeleteProductQuery, parameters, cancellationToken );
		}
		catch ( Exception ex )
		{
			// Try to get a SQL error number (MySqlException has a Number property).
			// We use reflection so unit tests can throw a lightweight exception exposing a Number property.
			int? number = null;
			try
			{
				var prop = ex.GetType().GetProperty( "Number" );
				if ( prop != null && prop.PropertyType == typeof( int ) )
					number = ( int? ) prop.GetValue( ex );
			}
			catch { /* ignore reflection issues */ }

			if ( number == 1451 )
			{
				throw new EntityInUseException(
					$"{Lang.metadataProductDeleteError}." );
			}

			// Not a MySQL foreign key constraint — rethrow original.
			throw;
		}
	}

	public Task<bool> IsProductUsedAsync( int productId ) => IsProductUsedAsync( productId, CancellationToken.None );

	public async Task<bool> IsProductUsedAsync( int productId, CancellationToken cancellationToken )
	{
		var parameters = new Dictionary<string, object>
		{
			{ $"@ProductId", productId }
		};

		var usedCount = await _dataService.ExecuteScalarAsync<int>(
			ProductUsedQuery,
			parameters, cancellationToken);

		return usedCount > 0;
	}

	public Task<bool> NameExistsAsync( string? productName ) => NameExistsAsync( productName, CancellationToken.None );

	public async Task<bool> NameExistsAsync( string? productName, CancellationToken cancellationToken )
	{
		if ( string.IsNullOrWhiteSpace( productName ) )
			return false;

		var parameters = new Dictionary<string, object>
		{
			{ $"@{DBNames.ProductFieldNameName}", productName.Trim() }
		};

		var count = await _dataService.ExecuteScalarAsync<int>( ProductNameExistsQuery, parameters, cancellationToken );
		return count > 0;
	}
}