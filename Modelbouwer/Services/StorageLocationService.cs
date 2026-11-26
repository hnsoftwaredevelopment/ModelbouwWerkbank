namespace Modelbouwer.Services;

public class StorageLocationService : IStorageLocationService
{
	private readonly GenericDataService _dataService;
	public bool StorageLocationUsed { get; set; }

	public StorageLocationService( GenericDataService dataService )
	{
		_dataService = dataService;
	}

	#region Database query's
	public string CompleteStorageLocationList = $"" +
		$"SELECT " +
		$"{DBNames.StorageFieldNameId} AS {DBNames.StorageFieldNameId}, " +
		$"{DBNames.StorageFieldNameParentId} AS {DBNames.StorageFieldNameParentId}, " +
		$"{DBNames.StorageFieldNameName} AS {DBNames.StorageFieldNameName}" +
		$" FROM {DBNames.Database}.{DBNames.StorageTable};";

	public string AddNewStorageLocationQuery =
		$"INSERT INTO {DBNames.Database}.{DBNames.StorageTable} " +
		$"({DBNames.StorageFieldNameParentId}, {DBNames.StorageFieldNameName}) " +
		$"VALUES " +
		$"(@{DBNames.StorageFieldNameParentId}, @{DBNames.StorageFieldNameName});" +
		$"{DBNames.SqlSelectLastId}";

	public string UpdateStorageLocationQuery =
		$"UPDATE {DBNames.Database}.{DBNames.StorageTable} " +
		$"SET " +
		$"{DBNames.StorageFieldNameParentId} = @{DBNames.StorageFieldNameParentId}, " +
		$"{DBNames.StorageFieldNameName} = @{DBNames.StorageFieldNameName}" +
		$" WHERE {DBNames.StorageFieldNameId} = @{DBNames.StorageFieldNameId};";

	public string DeleteStorageLocationQuery =
		$"DELETE FROM {DBNames.Database}.{DBNames.StorageTable} " +
		$"WHERE {DBNames.StorageFieldNameId} = @{DBNames.StorageFieldNameId};";

	public string StorageNameExistsQuery =
		$"SELECT COUNT(*) " +
		$"FROM {DBNames.Database}.{DBNames.StorageTable} " +
		$"WHERE {DBNames.StorageFieldNameName} = @{DBNames.StorageFieldNameName} " +
		$"AND ( " +
		$"( {DBNames.StorageFieldNameParentId} = @{DBNames.StorageFieldNameParentId} ) " +
		$"OR ( {DBNames.StorageFieldNameParentId} IS NULL AND @{DBNames.StorageFieldNameParentId} IS NULL ) );";

	public string StorageLocationUsedQuery =
		$"SELECT COUNT(*){DBNames.ProductFieldNameStorageId}) FROM {DBNames.Database}.{DBNames.ProductTable} WHERE {DBNames.ProductFieldNameStorageId} = @StorageLocationId";
	#endregion

	public Task<List<StorageLocationModel>> GetAllStorageLocationsAsync() => GetAllStorageLocationsAsync( CancellationToken.None );

	public Task<List<StorageLocationModel>> GetAllStorageLocationsAsync( CancellationToken cancellationToken )
	{
		return _dataService.ExecuteQueryAsync( CompleteStorageLocationList, reader =>
		{
			return new StorageLocationModel
			{
				StorageId = DatabaseValueConverter.GetInt( reader [ $"{DBNames.StorageFieldNameId}" ] ),
				ParentId = DatabaseValueConverter.GetInt( reader [ $"{DBNames.StorageFieldNameParentId}" ] ),
				StorageName = DatabaseValueConverter.GetString( reader [ $"{DBNames.StorageFieldNameName}" ] )
			};
		}, null, cancellationToken );
	}

	public Task<int> InsertNewStorageLocationAsync( Dictionary<string, object?> queryParameters ) => InsertNewStorageLocationAsync( queryParameters, CancellationToken.None );

	public async Task<int> InsertNewStorageLocationAsync( Dictionary<string, object?> queryParameters, CancellationToken cancellationToken )
	{
		Dictionary<string, object> parameters = new()
		{
			{ $"@{DBNames.StorageFieldNameParentId}", queryParameters[$"@{DBNames.StorageFieldNameParentId}"] ?? DBNull.Value },
			{ $"@{DBNames.StorageFieldNameName}", queryParameters[$"@{DBNames.StorageFieldNameName}"] ?? DBNull.Value }
		};

		uint newId = await _dataService.ExecuteScalarAsync<uint>( AddNewStorageLocationQuery, parameters, cancellationToken );

		return ( int ) newId;
	}

	public Task UpdateStorageLocationAsync( Dictionary<string, object?> queryParameters ) => UpdateStorageLocationAsync( queryParameters, CancellationToken.None );

	public async Task UpdateStorageLocationAsync( Dictionary<string, object?> queryParameters, CancellationToken cancellationToken )
	{
		Dictionary<string, object> parameters = new()
		{
			{ $"@{DBNames.StorageFieldNameId}", queryParameters[$"@{DBNames.StorageFieldNameId}"] ?? DBNull.Value },
			{ $"@{DBNames.StorageFieldNameParentId}", queryParameters[$"@{DBNames.StorageFieldNameParentId}"] ?? DBNull.Value },
			{ $"@{DBNames.StorageFieldNameName}", queryParameters[$"@{DBNames.StorageFieldNameName}"] ?? DBNull.Value }
		};

		await _dataService.ExecuteScalarAsync<uint>( UpdateStorageLocationQuery, parameters, cancellationToken );
	}

	public Task DeleteStorageLocationAsync( int storagelocationId ) => DeleteStorageLocationAsync( storagelocationId, CancellationToken.None );

	public async Task DeleteStorageLocationAsync( int storagelocationId, CancellationToken cancellationToken )
	{
		var parameters = new Dictionary<string, object>
		{
			{ $"@{DBNames.StorageFieldNameId}", storagelocationId }
		};

		try
		{
			await _dataService.ExecuteScalarAsync<uint>( DeleteStorageLocationQuery, parameters, cancellationToken );
		}
		catch ( MySqlException ex ) when ( ex.Number == 1451 )
		{
			throw new EntityInUseException(
				$"{Lang.metadataStorageLocationDeleteError}." );
		}
	}

	public Task<bool> IsStorageLocationUsedAsync( int storagelocationId ) => IsStorageLocationUsedAsync( storagelocationId, CancellationToken.None );

	public async Task<bool> IsStorageLocationUsedAsync( int storagelocationId, CancellationToken cancellationToken )
	{
		var parameters = new Dictionary<string, object>
		{
			{ $"@{DBNames.ProductFieldNameStorageId}", storagelocationId }
		};

		var usedCount = await _dataService.ExecuteScalarAsync<int>(
			StorageLocationUsedQuery,
			parameters, cancellationToken);

		return usedCount > 0;
	}

	public Task<bool> NameExistsAsync( string? StorageName, int? parentId ) => NameExistsAsync( StorageName, parentId, CancellationToken.None );

	public async Task<bool> NameExistsAsync( string? StorageName, int? parentId, CancellationToken cancellationToken )
	{
		if ( string.IsNullOrWhiteSpace( StorageName ) )
			return false;

		if ( parentId == 0 )
			parentId = null;

		var parameters = new Dictionary<string, object>
		{
			{ $"@{DBNames.StorageFieldNameParentId}", ( object? ) parentId ?? DBNull.Value },
			{ $"@{DBNames.StorageFieldNameName}", StorageName }
		};


		var count = await _dataService.ExecuteScalarAsync<int>( StorageNameExistsQuery, parameters, cancellationToken );
		return count > 0;
	}
}