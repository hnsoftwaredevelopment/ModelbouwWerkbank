namespace Modelbouwer.Services;

public class CategoryService : ICategoryService
{
	private readonly GenericDataService _dataService;

	public CategoryService( GenericDataService dataService )
	{
		_dataService = dataService;
	}

	#region Database query's
	public string CompleteCategoryList = $"" +
		$"SELECT " +
		$"{DBNames.CategoryFieldNameId} AS {DBNames.CategoryFieldNameId}, " +
		$"{DBNames.CategoryFieldNameParentId} AS {DBNames.CategoryFieldNameParentId}, " +
		$"{DBNames.CategoryFieldNameName} AS {DBNames.CategoryFieldNameName}" +
		$" FROM {DBNames.Database}.{DBNames.CategoryTable};";

	public string AddNewCategoryQuery =
		$"INSERT INTO {DBNames.Database}.{DBNames.CategoryTable} " +
		$"({DBNames.CategoryFieldNameParentId}, {DBNames.CategoryFieldNameName}) " +
		$"VALUES " +
		$"(@{DBNames.CategoryFieldNameParentId}, @{DBNames.CategoryFieldNameName});" +
		$"{DBNames.SqlSelectLastId}";

	public string UpdateCategoryQuery =
		$"UPDATE {DBNames.Database}.{DBNames.CategoryTable} " +
		$"SET " +
		$"{DBNames.CategoryFieldNameParentId} = @{DBNames.CategoryFieldNameParentId}, " +
		$"{DBNames.CategoryFieldNameName} = @{DBNames.CategoryFieldNameName}" +
		$" WHERE {DBNames.CategoryFieldNameId} = @{DBNames.CategoryFieldNameId};";

	public string DeleteCategoryQuery =
		$"DELETE FROM {DBNames.Database}.{DBNames.CategoryTable} " +
		$"WHERE {DBNames.CategoryFieldNameId} = @{DBNames.CategoryFieldNameId};";

	public string CategoryNameExistsQuery =
		$"SELECT COUNT(*) " +
		$"FROM {DBNames.Database}.{DBNames.CategoryTable} " +
		$"WHERE {DBNames.CategoryFieldNameName} = @{DBNames.CategoryFieldNameName} " +
		$"AND ( " +
		$"( {DBNames.CategoryFieldNameParentId} = @{DBNames.CategoryFieldNameParentId} ) " +
		$"OR ( {DBNames.CategoryFieldNameParentId} IS NULL AND @{DBNames.CategoryFieldNameParentId} IS NULL ) );";

	#endregion

	public Task<List<CategoryModel>> GetAllCategorysAsync() => GetAllCategorysAsync( CancellationToken.None );

	public Task<List<CategoryModel>> GetAllCategorysAsync( CancellationToken cancellationToken )
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

	public Task<int> InsertNewCategoryAsync( Dictionary<string, object?> queryParameters ) => InsertNewCategoryAsync( queryParameters, CancellationToken.None );

	public async Task<int> InsertNewCategoryAsync( Dictionary<string, object?> queryParameters, CancellationToken cancellationToken )
	{
		Dictionary<string, object> parameters = new()
		{
			{ $"@{DBNames.CategoryFieldNameParentId}", queryParameters[$"@{DBNames.CategoryFieldNameParentId}"] ?? DBNull.Value },
			{ $"@{DBNames.CategoryFieldNameName}", queryParameters[$"@{DBNames.CategoryFieldNameName}"] ?? DBNull.Value }
		};

		uint newId = await _dataService.ExecuteScalarAsync<uint>( AddNewCategoryQuery, parameters, cancellationToken );

		return ( int ) newId;
	}

	public Task UpdateCategoryAsync( Dictionary<string, object?> queryParameters ) => UpdateCategoryAsync( queryParameters, CancellationToken.None );

	public async Task UpdateCategoryAsync( Dictionary<string, object?> queryParameters, CancellationToken cancellationToken )
	{
		Dictionary<string, object> parameters = new()
		{
			{ $"@{DBNames.CategoryFieldNameId}", queryParameters[$"@{DBNames.CategoryFieldNameId}"] ?? DBNull.Value },
			{ $"@{DBNames.CategoryFieldNameParentId}", queryParameters[$"@{DBNames.CategoryFieldNameParentId}"] ?? DBNull.Value },
			{ $"@{DBNames.CategoryFieldNameName}", queryParameters[$"@{DBNames.CategoryFieldNameName}"] ?? DBNull.Value }
		};

		await _dataService.ExecuteScalarAsync<uint>( UpdateCategoryQuery, parameters, cancellationToken );
	}

	public Task DeleteCategoryAsync( int categoryId ) => DeleteCategoryAsync( categoryId, CancellationToken.None );

	public async Task DeleteCategoryAsync( int categoryId, CancellationToken cancellationToken )
	{
		var parameters = new Dictionary<string, object>
		{
			{ $"@{DBNames.CategoryFieldNameId}", categoryId }
		};

		try
		{
			await _dataService.ExecuteScalarAsync<uint>( DeleteCategoryQuery, parameters, cancellationToken );
		}
		catch ( Exception ex ) when ( DatabaseErrors.IsRowStillReferenced( ex ) )
		{
			throw new EntityInUseException(
				$"{Lang.metadataCategoryDeleteError}." );
		}
	}

	public Task<bool> NameExistsAsync( string? categoryName, int? parentId ) => NameExistsAsync( categoryName, parentId, CancellationToken.None );

	public async Task<bool> NameExistsAsync( string? categoryName, int? parentId, CancellationToken cancellationToken )
	{
		if ( string.IsNullOrWhiteSpace( categoryName ) )
			return false;

		if ( parentId == 0 )
			parentId = null;

		var parameters = new Dictionary<string, object>
		{
			{ $"@{DBNames.CategoryFieldNameParentId}", ( object? ) parentId ?? DBNull.Value },
			{ $"@{DBNames.CategoryFieldNameName}", categoryName }
		};

		var count = await _dataService.ExecuteScalarAsync<int>( CategoryNameExistsQuery, parameters, cancellationToken );
		return count > 0;
	}
}