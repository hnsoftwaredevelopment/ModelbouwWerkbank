namespace Modelbouwer.Services;

public class ProjectService : IProjectService
{
	private readonly GenericDataService _dataService;

	public ProjectService( GenericDataService dataService )
	{
		_dataService = dataService;
	}

	#region Database query's
	public string CompleteProjectList = $"" +
		$"SELECT " +
		$"{ DBNames.ProjectFieldNameId} AS {DBNames.ProjectFieldNameId}, " +
		$"{ DBNames.ProjectFieldNameCode}, " +
		$"{ DBNames.ProjectFieldNameName}, " +
		$"{ DBNames.ProjectFieldNameStartDate}, " +
		$"{ DBNames.ProjectFieldNameEndDate}, " +
		$"{ DBNames.ProjectFieldNameExpectedTime}, " +
		$"{ DBNames.ProjectFieldNameClosed}, " +
		$"{ DBNames.ProjectFieldNameImage}, " +
		$"{ DBNames.ProjectFieldNameImageRotationAngle}, " +
		$"{ DBNames.ProjectFieldNameMemo}" +
		$" FROM {DBNames.Database}.{DBNames.ProjectTable};";

	public string AddNewProjectQuery =
		$"INSERT INTO {DBNames.Database}.{DBNames.ProjectTable} " +
		$"( {DBNames.ProjectFieldNameName}, " +
		$"{DBNames.ProjectFieldNameName}, " +
		$"{DBNames.ProjectFieldNameCode}, " +
		$"{DBNames.ProjectFieldNameStartDate}, " +
		$"{DBNames.ProjectFieldNameEndDate}, " +
		$"{DBNames.ProjectFieldNameExpectedTime}, " +
		$"{DBNames.ProjectFieldNameClosed}, " +
		$"{DBNames.ProjectFieldNameImage}, " +
		$"{DBNames.ProjectFieldNameImageRotationAngle}, " +
		$"{DBNames.ProjectFieldNameMemo} ) " +
		$"VALUES " +
		$"( @{DBNames.ProjectFieldNameName}, " +
		$"@{DBNames.ProjectFieldNameCode}, " +
		$"@{DBNames.ProjectFieldNameStartDate}, " +
		$"@{DBNames.ProjectFieldNameEndDate}, " +
		$"@{DBNames.ProjectFieldNameExpectedTime}, " +
		$"@{DBNames.ProjectFieldNameClosed}, " +
		$"@{DBNames.ProjectFieldNameImage}, " +
		$"@{DBNames.ProjectFieldNameImageRotationAngle}, " +
		$"@{DBNames.ProjectFieldNameMemo} )" +
		$"{DBNames.SqlSelectLastId}";

	public string UpdateProjectQuery =
		$"UPDATE {DBNames.Database}.{DBNames.ProjectTable} " +
		$"SET " +
		$"{DBNames.ProjectFieldNameName} = @{DBNames.ProjectFieldNameName}, " +
		$"{DBNames.ProjectFieldNameCode} = @{DBNames.ProjectFieldNameCode}, " +
		$"{DBNames.ProjectFieldNameStartDate} = @{DBNames.ProjectFieldNameStartDate}, " +
		$"{DBNames.ProjectFieldNameEndDate} = @{DBNames.ProjectFieldNameEndDate}, " +
		$"{DBNames.ProjectFieldNameExpectedTime} = @{DBNames.ProjectFieldNameExpectedTime}, " +
		$"{DBNames.ProjectFieldNameClosed} = @{DBNames.ProjectFieldNameClosed}, " +
		$"{DBNames.ProjectFieldNameImage} = @{DBNames.ProjectFieldNameImage}, " +
		$"{DBNames.ProjectFieldNameImageRotationAngle} = @{DBNames.ProjectFieldNameImageRotationAngle}, " +
		$"{DBNames.ProjectFieldNameMemo} = @{DBNames.ProjectFieldNameMemo} " +
		$"WHERE {DBNames.ProjectFieldNameId} = @{DBNames.ProjectFieldNameId};";

	public string DeleteProjectQuery =
		$"DELETE FROM {DBNames.Database}.{DBNames.ProjectTable} " +
		$"WHERE {DBNames.ProjectFieldNameId} = @{DBNames.ProjectFieldNameId};";

	public string ProjectNameExistsQuery =
		$"SELECT COUNT({DBNames.ProjectFieldNameId}) " +
		$"FROM {DBNames.Database}.{DBNames.ProjectTable} " +
		$"WHERE LOWER({DBNames.ProjectFieldNameName}) = LOWER(@{DBNames.ProjectFieldNameName})";

	public string LastWorkDateOnProjectQuery =
		$"SELECT MAX( {DBNames.TimeFieldNameWorkDate} ) " +
		$"FROM {DBNames.Database}.{DBNames.TimeTable} WHERE {DBNames.TimeFieldNameProjectId} =  @{DBNames.TimeFieldNameProjectId};";

	public string FirstWorkDateAndHourTotalsOnProjectQueryWithProjectId =
		$"SELECT MIN( {DBNames.TimeViewFieldNameWorkDate} ) AS StartDate, " +
		$"IFNULL(SUM({DBNames.TimeViewFieldNameElapsedMinutes}), 0) / 60 AS TotalHours " +
		$"FROM {DBNames.Database}.{DBNames.TimeView} " +
		$"WHERE {DBNames.TimeViewFieldNameProjectId} =  @{DBNames.TimeViewFieldNameProjectId};";

	public string GetProjectExpectedEndDateQuery = $"CALL {DBNames.Database}.{DBNames.SPProjectExpectedEndDate}(@ProjectId);";
	#endregion

	public Task<List<ProjectModel>> GetAllProjectsAsync() => GetAllProjectsAsync( CancellationToken.None );

	public Task<List<ProjectModel>> GetAllProjectsAsync( CancellationToken cancellationToken )
	{
		return _dataService.ExecuteQueryAsync( CompleteProjectList, reader =>
		{
			return new ProjectModel
			{
				ProjectId = DatabaseValueConverter.GetInt( reader [ $"{DBNames.ProjectFieldNameId}" ] ),
				ProjectCode = DatabaseValueConverter.GetString( reader [ $"{DBNames.ProjectFieldNameCode}" ] ),
				ProjectName = DatabaseValueConverter.GetString( reader [ $"{DBNames.ProjectFieldNameName}" ] ),
				ProjectStartDate = DatabaseValueConverter.GetDateOnly( reader [ $"{DBNames.ProjectFieldNameStartDate}" ] ),
				ProjectEndDate = DatabaseValueConverter.GetDateOnly( reader [ $"{DBNames.ProjectFieldNameEndDate}" ] ),
				ProjectExpectedTime = DatabaseValueConverter.GetInt( reader [ $"{DBNames.ProjectFieldNameExpectedTime}" ] ),
				ProjectClosed = DatabaseValueConverter.GetSByte( reader [ $"{DBNames.ProjectFieldNameClosed}" ] ) == 1,
				ProjectImage = DatabaseValueConverter.GetBytes( reader [ $"{DBNames.ProjectFieldNameImage}" ] ),
				ProjectImageRotationAngle = DatabaseValueConverter.GetDouble( reader [ $"{DBNames.ProjectFieldNameImageRotationAngle}" ] ),
				ProjectMemo = DatabaseValueConverter.GetString( reader [ $"{DBNames.ProjectFieldNameMemo}" ] )
			};
		}, null, cancellationToken );
	}

	public Task<int> InsertNewProjectAsync( Dictionary<string, object?> queryParameters ) => InsertNewProjectAsync( queryParameters, CancellationToken.None );

	public async Task<int> InsertNewProjectAsync( Dictionary<string, object?> queryParameters, CancellationToken cancellationToken )
	{
		Dictionary<string, object> parameters = new()
		{
			{ $"@{DBNames.ProjectFieldNameCode}", queryParameters[$"@{DBNames.ProjectFieldNameCode}"] ?? DBNull.Value },
			{ $"@{DBNames.ProjectFieldNameName}", queryParameters[$"@{DBNames.ProjectFieldNameName}"] ?? DBNull.Value },
			{ $"@{DBNames.ProjectFieldNameStartDate}", queryParameters[$"@{DBNames.ProjectFieldNameStartDate}"] ?? DBNull.Value },
			{ $"@{DBNames.ProjectFieldNameEndDate}", queryParameters[$"@{DBNames.ProjectFieldNameEndDate}"] ?? DBNull.Value },
			{ $"@{DBNames.ProjectFieldNameExpectedTime}", queryParameters[$"@{DBNames.ProjectFieldNameExpectedTime}"] ?? DBNull.Value },
			{ $"@{DBNames.ProjectFieldNameClosed}", queryParameters[$"@{DBNames.ProjectFieldNameClosed}"] ?? DBNull.Value },
			{ $"@{DBNames.ProjectFieldNameImage}", queryParameters[$"@{DBNames.ProjectFieldNameImage}"] ?? DBNull.Value },
			{ $"@{DBNames.ProjectFieldNameImageRotationAngle}", queryParameters[$"@{DBNames.ProjectFieldNameImageRotationAngle}"] ?? DBNull.Value },
			{ $"@{DBNames.ProjectFieldNameMemo}", queryParameters[$"@{DBNames.ProjectFieldNameMemo}"] ?? DBNull.Value }
		};

		uint newId = await _dataService.ExecuteScalarAsync<uint>( AddNewProjectQuery, parameters, cancellationToken );

		return ( int ) newId;
	}

	public Task UpdateProjectAsync( Dictionary<string, object?> queryParameters ) => UpdateProjectAsync( queryParameters, CancellationToken.None );

	public async Task UpdateProjectAsync( Dictionary<string, object?> queryParameters, CancellationToken cancellationToken )
	{
		Dictionary<string, object> parameters = new()
		{
			{ $"@{DBNames.ProjectFieldNameId}", queryParameters[$"@{DBNames.ProjectFieldNameId}"] ?? DBNull.Value },
			{ $"@{DBNames.ProjectFieldNameCode}", queryParameters[$"@{DBNames.ProjectFieldNameCode}"] ?? DBNull.Value },
			{ $"@{DBNames.ProjectFieldNameName}", queryParameters[$"@{DBNames.ProjectFieldNameName}"] ?? DBNull.Value },
			{ $"@{DBNames.ProjectFieldNameStartDate}", queryParameters[$"@{DBNames.ProjectFieldNameStartDate}"] ?? DBNull.Value },
			{ $"@{DBNames.ProjectFieldNameEndDate}", queryParameters[$"@{DBNames.ProjectFieldNameEndDate}"] ?? DBNull.Value },
			{ $"@{DBNames.ProjectFieldNameExpectedTime}", queryParameters[$"@{DBNames.ProjectFieldNameExpectedTime}"] ?? DBNull.Value },
			{ $"@{DBNames.ProjectFieldNameClosed}", queryParameters[$"@{DBNames.ProjectFieldNameClosed}"] ?? DBNull.Value },
			{ $"@{DBNames.ProjectFieldNameImage}", queryParameters[$"@{DBNames.ProjectFieldNameImage}"] ?? DBNull.Value },
			{ $"@{DBNames.ProjectFieldNameImageRotationAngle}", queryParameters[$"@{DBNames.ProjectFieldNameImageRotationAngle}"] ?? DBNull.Value },
			{ $"@{DBNames.ProjectFieldNameMemo}", queryParameters[$"@{DBNames.ProjectFieldNameMemo}"] ?? DBNull.Value }
		};

		await _dataService.ExecuteScalarAsync<uint>( UpdateProjectQuery, parameters, cancellationToken );
	}

	public Task DeleteProjectAsync( int projectId ) => DeleteProjectAsync( projectId, CancellationToken.None );

	public async Task DeleteProjectAsync( int projectId, CancellationToken cancellationToken )
	{
		var parameters = new Dictionary<string, object>
		{
			{ $"@{DBNames.ProjectFieldNameId}", projectId }
		};

		try
		{
			await _dataService.ExecuteScalarAsync<uint>( DeleteProjectQuery, parameters, cancellationToken );
		}
		catch ( Exception ex ) when ( DatabaseErrors.IsRowStillReferenced( ex ) )
		{
			throw new EntityInUseException( $"{Lang.metadataProjectDeleteError}." );
		}
	}

	public Task<DateOnly?> GetLastWorkDateOnProjectAsync( int projectId ) => GetLastWorkDateOnProjectAsync( projectId, CancellationToken.None );

	public async Task<DateOnly?> GetLastWorkDateOnProjectAsync( int projectId, CancellationToken cancellationToken )
	{
		var parameters = new Dictionary<string, object>
		{
			{ $"@{DBNames.TimeFieldNameProjectId}", projectId }
		};
		var result = await _dataService.ExecuteScalarAsync<DateTime?>(
			LastWorkDateOnProjectQuery,
			parameters, cancellationToken
		);
		return result.HasValue
			? DateOnly.FromDateTime( result.Value )
			: null;
	}

	public Task<bool> NameExistsAsync( string? projectName ) => NameExistsAsync( projectName, CancellationToken.None );

	public async Task<bool> NameExistsAsync( string? projectName, CancellationToken cancellationToken )
	{
		if ( string.IsNullOrWhiteSpace( projectName ) )
			return false;

		var parameters = new Dictionary<string, object>
		{
			{ $"@{DBNames.ProjectFieldNameName}", projectName.Trim() }
		};

		var count = await _dataService.ExecuteScalarAsync<int>( ProjectNameExistsQuery, parameters, cancellationToken );
		return count > 0;
	}

	public Task<ProjectWorkStats?> GetProjectWorkStatsAsync( int projectId ) => GetProjectWorkStatsAsync( projectId, CancellationToken.None );

	public async Task<ProjectWorkStats?> GetProjectWorkStatsAsync( int projectId, CancellationToken cancellationToken )
	{
		return await _dataService.ExecuteSingleAsync<ProjectWorkStats>(
			FirstWorkDateAndHourTotalsOnProjectQueryWithProjectId,
			new Dictionary<string, object>
			{
			{ DBNames.TimeViewFieldNameProjectId, projectId }
			}, cancellationToken );
	}

}