using System;
using System.Collections.Generic;
using System.Text;

namespace Modelbouwer.Services;

public class UnitService : IUnitService
{
	private readonly GenericDataService _dataService;

	public UnitService( GenericDataService dataService )
	{
		_dataService = dataService;
	}

	#region Database query's
	public string CompleteUnitList = $"" +
		$"SELECT " +
		$"{DBNames.UnitFieldNameUnitId} AS {DBNames.UnitFieldNameUnitId}, " +
		$"{DBNames.UnitFieldNameUnitName} AS {DBNames.UnitFieldNameUnitName}" +
		$" FROM {DBNames.Database}.{DBNames.UnitTable};";

	public string AddNewUnitQuery =
		$"INSERT INTO {DBNames.Database}.{DBNames.UnitTable} " +
		$"({DBNames.UnitFieldNameUnitName}) " +
		$"VALUES " +
		$"(@{DBNames.UnitFieldNameUnitName});" +
		$"{DBNames.SqlSelectLastId}";

	public string UpdateUnitQuery =
		$"UPDATE {DBNames.Database}.{DBNames.UnitTable} " +
		$"SET " +
		$"{DBNames.UnitFieldNameUnitName} = @{DBNames.UnitFieldNameUnitName}" +
		$" WHERE {DBNames.UnitFieldNameUnitId} = @{DBNames.UnitFieldNameUnitId};";

	public string DeleteUnitQuery =
		$"DELETE FROM {DBNames.Database}.{DBNames.UnitTable} " +
		$"WHERE {DBNames.UnitFieldNameUnitId} = @{DBNames.UnitFieldNameUnitId};";

	public string UnitNameExistsQuery =
		$"SELECT COUNT({DBNames.UnitFieldNameUnitId}) " +
		$"FROM {DBNames.Database}.{DBNames.UnitTable} " +
		$"WHERE LOWER({DBNames.UnitFieldNameUnitName}) = LOWER(@{DBNames.UnitFieldNameUnitName})";

	#endregion

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

	public Task<int> InsertNewUnitAsync( Dictionary<string, object?> queryParameters ) => InsertNewUnitAsync( queryParameters, CancellationToken.None );

	public async Task<int> InsertNewUnitAsync( Dictionary<string, object?> queryParameters, CancellationToken cancellationToken )
	{
		Dictionary<string, object> parameters = new()
		{
			{ $"@{DBNames.UnitFieldNameUnitName}", queryParameters[$"@{DBNames.UnitFieldNameUnitName}"] ?? DBNull.Value }
		};

		uint newId = await _dataService.ExecuteScalarAsync<uint>( AddNewUnitQuery, parameters, cancellationToken );

		return ( int ) newId;
	}

	public Task UpdateUnitAsync( Dictionary<string, object?> queryParameters ) => UpdateUnitAsync( queryParameters, CancellationToken.None );

	public async Task UpdateUnitAsync( Dictionary<string, object?> queryParameters, CancellationToken cancellationToken )
	{
		Dictionary<string, object> parameters = new()
		{
			{ $"@{DBNames.UnitFieldNameUnitId}", queryParameters[$"@{DBNames.UnitFieldNameUnitId}"] ?? DBNull.Value },
			{ $"@{DBNames.UnitFieldNameUnitName}", queryParameters[$"@{DBNames.UnitFieldNameUnitName}"] ?? DBNull.Value }
		};

		await _dataService.ExecuteScalarAsync<uint>( UpdateUnitQuery, parameters, cancellationToken );
	}

	public Task DeleteUnitAsync( int unitId ) => DeleteUnitAsync( unitId, CancellationToken.None );

	public async Task DeleteUnitAsync( int unitId, CancellationToken cancellationToken )
	{
		var parameters = new Dictionary<string, object>
		{
			{ $"@{DBNames.UnitFieldNameUnitId}", unitId }
		};

		try
		{
			await _dataService.ExecuteScalarAsync<uint>( DeleteUnitQuery, parameters, cancellationToken );
		}
		catch ( Exception ex ) when ( DatabaseErrors.IsRowStillReferenced( ex ) )
		{
			throw new EntityInUseException( $"{Lang.metadataUnitDeleteError}." );
		}
	}

	public Task<bool> NameExistsAsync( string? unitName ) => NameExistsAsync( unitName, CancellationToken.None );

	public async Task<bool> NameExistsAsync( string? unitName, CancellationToken cancellationToken )
	{
		if ( string.IsNullOrWhiteSpace( unitName ) )
			return false;

		var parameters = new Dictionary<string, object>
		{
			{ $"@{DBNames.UnitFieldNameUnitName}", unitName.Trim() }
		};

		var count = await _dataService.ExecuteScalarAsync<int>( UnitNameExistsQuery, parameters, cancellationToken );
		return count > 0;
	}
}