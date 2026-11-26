namespace Modelbouwer.Services;

public class WorkTypeService : IWorkTypeService
{
	private readonly GenericDataService _dataService;
	public bool WorkTypeUsed { get; set; }

	public WorkTypeService( GenericDataService dataService )
	{
		_dataService = dataService;
	}

	#region Database query's
	public string CompleteWorkTypeList = $"" +
		$"SELECT " +
		$"{DBNames.WorkTypeFieldNameId} AS {DBNames.WorkTypeFieldNameId}, " +
		$"{DBNames.WorkTypeFieldNameParentId} AS {DBNames.WorkTypeFieldNameParentId}, " +
		$"{DBNames.WorkTypeFieldNameName} AS {DBNames.WorkTypeFieldNameName}" +
		$" FROM {DBNames.Database}.{DBNames.WorkTypeTable};";

	public string AddNewWorkTypeQuery =
		$"INSERT INTO {DBNames.Database}.{DBNames.WorkTypeTable} " +
		$"({DBNames.WorkTypeFieldNameParentId}, {DBNames.WorkTypeFieldNameName}) " +
		$"VALUES " +
		$"(@{DBNames.WorkTypeFieldNameParentId}, @{DBNames.WorkTypeFieldNameName});" +
		$"{DBNames.SqlSelectLastId}";

	public string UpdateWorkTypeQuery =
		$"UPDATE {DBNames.Database}.{DBNames.WorkTypeTable} " +
		$"SET " +
		$"{DBNames.WorkTypeFieldNameParentId} = @{DBNames.WorkTypeFieldNameParentId}, " +
		$"{DBNames.WorkTypeFieldNameName} = @{DBNames.WorkTypeFieldNameName}" +
		$" WHERE {DBNames.WorkTypeFieldNameId} = @{DBNames.WorkTypeFieldNameId};";

	public string DeleteWorkTypeQuery =
		$"DELETE FROM {DBNames.Database}.{DBNames.WorkTypeTable} " +
		$"WHERE {DBNames.WorkTypeFieldNameId} = @{DBNames.WorkTypeFieldNameId};";

	public string WorkTypeNameExistsQuery =
		$"SELECT COUNT(*) " +
		$"FROM {DBNames.Database}.{DBNames.WorkTypeTable} " +
		$"WHERE {DBNames.WorkTypeFieldNameName} = @{DBNames.WorkTypeFieldNameName} " +
		$"AND ( " +
		$"( {DBNames.WorkTypeFieldNameParentId} = @{DBNames.WorkTypeFieldNameParentId} ) " +
		$"OR ( {DBNames.WorkTypeFieldNameParentId} IS NULL AND @{DBNames.WorkTypeFieldNameParentId} IS NULL ) );";

	public string WorkTypeUsedQuery =
		$"SELECT COUNT(*){DBNames.TimeFieldNameWorkTypeId}) FROM {DBNames.Database}.{DBNames.TimeTable} WHERE {DBNames.TimeFieldNameWorkTypeId} = @WorktypeId";
	#endregion

	public Task<List<WorkTypeModel>> GetAllWorkTypesAsync() => GetAllWorkTypesAsync( CancellationToken.None );

	public Task<List<WorkTypeModel>> GetAllWorkTypesAsync( CancellationToken cancellationToken )
	{
		return _dataService.ExecuteQueryAsync( CompleteWorkTypeList, reader =>
		{
			return new WorkTypeModel
			{
				WorktypeId = DatabaseValueConverter.GetInt( reader [ $"{DBNames.WorkTypeFieldNameId}" ] ),
				ParentId = DatabaseValueConverter.GetInt( reader [ $"{DBNames.WorkTypeFieldNameParentId}" ] ),
				WorktypeName = DatabaseValueConverter.GetString( reader [ $"{DBNames.WorkTypeFieldNameName}" ] )
			};
		}, null, cancellationToken );
	}

	public Task<int> InsertNewWorkTypeAsync( Dictionary<string, object?> queryParameters ) => InsertNewWorkTypeAsync( queryParameters, CancellationToken.None );

	public async Task<int> InsertNewWorkTypeAsync( Dictionary<string, object?> queryParameters, CancellationToken cancellationToken )
	{
		Dictionary<string, object> parameters = new()
		{
			{ $"@{DBNames.WorkTypeFieldNameParentId}", queryParameters[$"@{DBNames.WorkTypeFieldNameParentId}"] ?? DBNull.Value },
			{ $"@{DBNames.WorkTypeFieldNameName}", queryParameters[$"@{DBNames.WorkTypeFieldNameName}"] ?? DBNull.Value }
		};

		uint newId = await _dataService.ExecuteScalarAsync<uint>( AddNewWorkTypeQuery, parameters, cancellationToken );

		return ( int ) newId;
	}

	public Task UpdateWorkTypeAsync( Dictionary<string, object?> queryParameters ) => UpdateWorkTypeAsync( queryParameters, CancellationToken.None );

	public async Task UpdateWorkTypeAsync( Dictionary<string, object?> queryParameters, CancellationToken cancellationToken )
	{
		Dictionary<string, object> parameters = new()
		{
			{ $"@{DBNames.WorkTypeFieldNameId}", queryParameters[$"@{DBNames.WorkTypeFieldNameId}"] ?? DBNull.Value },
			{ $"@{DBNames.WorkTypeFieldNameParentId}", queryParameters[$"@{DBNames.WorkTypeFieldNameParentId}"] ?? DBNull.Value },
			{ $"@{DBNames.WorkTypeFieldNameName}", queryParameters[$"@{DBNames.WorkTypeFieldNameName}"] ?? DBNull.Value }
		};

		await _dataService.ExecuteScalarAsync<uint>( UpdateWorkTypeQuery, parameters, cancellationToken );
	}

	public Task DeleteWorkTypeAsync( int worktypeId ) => DeleteWorkTypeAsync( worktypeId, CancellationToken.None );

	public async Task DeleteWorkTypeAsync( int worktypeId, CancellationToken cancellationToken )
	{
		var parameters = new Dictionary<string, object>
		{
			{ $"@{DBNames.WorkTypeFieldNameId}", worktypeId }
		};

		try
		{
			await _dataService.ExecuteScalarAsync<uint>( DeleteWorkTypeQuery, parameters, cancellationToken );
		}
		catch ( MySqlException ex ) when ( ex.Number == 1451 )
		{
			throw new EntityInUseException(
				$"{Lang.metadataWorkTypeDeleteError}." );
		}
	}

	public Task<bool> IsWorkTypeUsedAsync( int worktypeId ) => IsWorkTypeUsedAsync( worktypeId, CancellationToken.None );

	public async Task<bool> IsWorkTypeUsedAsync( int worktypeId, CancellationToken cancellationToken )
	{
		var parameters = new Dictionary<string, object>
		{
			{ $"@{DBNames.TimeFieldNameWorkTypeId}", worktypeId }
		};

		var usedCount = await _dataService.ExecuteScalarAsync<int>(
			WorkTypeUsedQuery,
			parameters, cancellationToken);

		return usedCount > 0;
	}

	public Task<bool> NameExistsAsync( string? worktypeName, int? parentId ) => NameExistsAsync( worktypeName, parentId, CancellationToken.None );

	public async Task<bool> NameExistsAsync( string? worktypeName, int? parentId, CancellationToken cancellationToken )
	{
		if ( string.IsNullOrWhiteSpace( worktypeName ) )
			return false;

		if ( parentId == 0 )
			parentId = null;

		var parameters = new Dictionary<string, object>
		{
			{ $"@{DBNames.WorkTypeFieldNameParentId}", ( object? ) parentId ?? DBNull.Value },
			{ $"@{DBNames.WorkTypeFieldNameName}", worktypeName }
		};


		var count = await _dataService.ExecuteScalarAsync<int>( WorkTypeNameExistsQuery, parameters, cancellationToken );
		return count > 0;
	}
}