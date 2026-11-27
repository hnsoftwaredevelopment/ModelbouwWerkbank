using System;
using System.Collections.Generic;
using System.Text;

namespace Modelbouwer.Services;

public class ContactTypeService : IContactTypeService
{
	private readonly GenericDataService _dataService;

	public ContactTypeService( GenericDataService dataService )
	{
		_dataService = dataService;
	}

	#region Database query's
	public string CompleteContactTypeList = $"" +
		$"SELECT " +
		$"{DBNames.ContactTypeFieldNameId} AS {DBNames.ContactTypeFieldNameId}, " +
		$"{DBNames.ContactTypeFieldNameName} AS {DBNames.ContactTypeFieldNameName}" +
		$" FROM {DBNames.Database}.{DBNames.ContactTypeTable};";

	public string AddNewContactTypeQuery =
		$"INSERT INTO {DBNames.Database}.{DBNames.ContactTypeTable} " +
		$"({DBNames.ContactTypeFieldNameName}) " +
		$"VALUES " +
		$"(@{DBNames.ContactTypeFieldNameName});" +
		$"{DBNames.SqlSelectLastId}";

	public string UpdateContactTypeQuery =
		$"UPDATE {DBNames.Database}.{DBNames.ContactTypeTable} " +
		$"SET " +
		$"{DBNames.ContactTypeFieldNameName} = @{DBNames.ContactTypeFieldNameName}" +
		$" WHERE {DBNames.ContactTypeFieldNameId} = @{DBNames.ContactTypeFieldNameId};";

	public string DeleteContactTypeQuery =
		$"DELETE FROM {DBNames.Database}.{DBNames.ContactTypeTable} " +
		$"WHERE {DBNames.ContactTypeFieldNameId} = @{DBNames.ContactTypeFieldNameId};";

	public string ContactTypeNameExistsQuery =
		$"SELECT COUNT({DBNames.ContactTypeFieldNameId}) " +
		$"FROM {DBNames.Database}.{DBNames.ContactTypeTable} " +
		$"WHERE LOWER({DBNames.ContactTypeFieldNameName}) = LOWER(@{DBNames.ContactTypeFieldNameName})";

	#endregion

	public Task<List<ContactTypeModel>> GetAllContactTypesAsync() => GetAllContactTypesAsync( CancellationToken.None );

	public Task<List<ContactTypeModel>> GetAllContactTypesAsync( CancellationToken cancellationToken )
	{
		return _dataService.ExecuteQueryAsync( CompleteContactTypeList, reader =>
		{
			return new ContactTypeModel
			{
				ContactTypeId = DatabaseValueConverter.GetInt( reader [ $"{DBNames.ContactTypeFieldNameId}" ] ),
				ContactTypeName = DatabaseValueConverter.GetString( reader [ $"{DBNames.ContactTypeFieldNameName}" ] )
			};
		}, null, cancellationToken );
	}

	public Task<int> InsertNewContactTypeAsync( Dictionary<string, object?> queryParameters ) => InsertNewContactTypeAsync( queryParameters, CancellationToken.None );

	public async Task<int> InsertNewContactTypeAsync( Dictionary<string, object?> queryParameters, CancellationToken cancellationToken )
	{
		Dictionary<string, object> parameters = new()
		{
			{ $"@{DBNames.ContactTypeFieldNameName}", queryParameters[$"@{DBNames.ContactTypeFieldNameName}"] ?? DBNull.Value }
		};

		uint newId = await _dataService.ExecuteScalarAsync<uint>( AddNewContactTypeQuery, parameters, cancellationToken );

		return ( int ) newId;
	}

	public Task UpdateContactTypeAsync( Dictionary<string, object?> queryParameters ) => UpdateContactTypeAsync( queryParameters, CancellationToken.None );

	public async Task UpdateContactTypeAsync( Dictionary<string, object?> queryParameters, CancellationToken cancellationToken )
	{
		Dictionary<string, object> parameters = new()
		{
			{ $"@{DBNames.ContactTypeFieldNameId}", queryParameters[$"@{DBNames.ContactTypeFieldNameId}"] ?? DBNull.Value },
			{ $"@{DBNames.ContactTypeFieldNameName}", queryParameters[$"@{DBNames.ContactTypeFieldNameName}"] ?? DBNull.Value }
		};

		await _dataService.ExecuteScalarAsync<uint>( UpdateContactTypeQuery, parameters, cancellationToken );
	}

	public Task DeleteContactTypeAsync( int contacttypeId ) => DeleteContactTypeAsync( contacttypeId, CancellationToken.None );

	public async Task DeleteContactTypeAsync( int contacttypeId, CancellationToken cancellationToken )
	{
		var parameters = new Dictionary<string, object>
		{
			{ $"@{DBNames.ContactTypeFieldNameId}", contacttypeId }
		};

		try
		{
			await _dataService.ExecuteScalarAsync<uint>( DeleteContactTypeQuery, parameters, cancellationToken );
		}
		catch ( Exception ex ) when ( DatabaseErrors.IsRowStillReferenced( ex ) )
		{
			throw new EntityInUseException(
				$"{Lang.metadataContactTypeDeleteError}." );
		}
	}

	public Task<bool> NameExistsAsync( string? contacttypeName ) => NameExistsAsync( contacttypeName, CancellationToken.None );

	public async Task<bool> NameExistsAsync( string? contacttypeName, CancellationToken cancellationToken )
	{
		if ( string.IsNullOrWhiteSpace( contacttypeName ) )
			return false;

		var parameters = new Dictionary<string, object>
		{
			{ $"@{DBNames.ContactTypeFieldNameName}", contacttypeName.Trim() }
		};

		var count = await _dataService.ExecuteScalarAsync<int>( ContactTypeNameExistsQuery, parameters, cancellationToken );
		return count > 0;
	}
}