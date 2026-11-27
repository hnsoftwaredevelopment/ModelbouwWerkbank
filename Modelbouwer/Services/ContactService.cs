namespace Modelbouwer.Services;

public class ContactService : IContactService
{
	private readonly GenericDataService _dataService;

	public ContactService( GenericDataService dataService )
	{
		_dataService = dataService;
	}

	public string CompleteContactList = $"" +
		$"SELECT " +
		$"{ DBNames.SupplierContactFieldNameId}, " +
		$"{ DBNames.SupplierContactFieldNameSupplierId}, " +
		$"{ DBNames.SupplierContactFieldNameTypeId}, " +
		$"{ DBNames.SupplierContactFieldNameName}, " +
		$"{ DBNames.SupplierContactFieldNameMail}, " +
		$"{ DBNames.SupplierContactFieldNamePhone}" +
		$" FROM {DBNames.Database}.{DBNames.SupplierContactTable};";

	public string AddNewContactQuery =
		$"INSERT INTO {DBNames.Database}.{DBNames.SupplierContactTable} ( " +
		$"{DBNames.SupplierContactFieldNameSupplierId}, " +
		$"{DBNames.SupplierContactFieldNameTypeId}, " +
		$"{DBNames.SupplierContactFieldNameName}, " +
		$"{DBNames.SupplierContactFieldNameMail}, " +
		$"{DBNames.SupplierContactFieldNamePhone} ) " +
		$"VALUES ( " +
		$"@{DBNames.SupplierContactFieldNameSupplierId}, " +
		$"@{DBNames.SupplierContactFieldNameTypeId}, " +
		$"@{DBNames.SupplierContactFieldNameName}, " +
		$"@{DBNames.SupplierContactFieldNameMail}, " +
		$"@{DBNames.SupplierContactFieldNamePhone} );" +
		$"{DBNames.SqlSelectLastId}";

	public string UpdateContactQuery =
		$"UPDATE {DBNames.Database}.{DBNames.SupplierContactTable} " +
		$"SET " +
		$"{DBNames.SupplierContactFieldNameTypeId} = @{DBNames.SupplierContactFieldNameTypeId}, " +
		$"{DBNames.SupplierContactFieldNameName} = @{DBNames.SupplierContactFieldNameName}, " +
		$"{DBNames.SupplierContactFieldNameMail} = @{DBNames.SupplierContactFieldNameMail}, " +
		$"{DBNames.SupplierContactFieldNamePhone} = @{DBNames.SupplierContactFieldNamePhone} " +
		$"WHERE {DBNames.SupplierContactFieldNameId} = @{DBNames.SupplierContactFieldNameId};";

	public string DeleteContactQuery =
		$"DELETE FROM {DBNames.Database}.{DBNames.SupplierContactTable} " +
		$"WHERE {DBNames.SupplierContactFieldNameId} = @{DBNames.SupplierContactFieldNameId};";

	public string ContactNameExistsQuery =
		$"SELECT COUNT({DBNames.SupplierContactFieldNameId}) " +
		$"FROM {DBNames.Database}.{DBNames.SupplierContactTable} " +
		$"WHERE LOWER({DBNames.SupplierContactFieldNameName}) = LOWER(@{DBNames.SupplierContactFieldNameName})";

	public Task<List<SupplierContactModel>> GetAllContactsAsync() => GetAllContactsAsync( CancellationToken.None );

	public Task<List<SupplierContactModel>> GetAllContactsAsync( CancellationToken cancellationToken )
	{
		return _dataService.ExecuteQueryAsync( CompleteContactList, reader =>
		{
			return new SupplierContactModel
			{
				SupplierContactId = DatabaseValueConverter.GetInt( reader [ $"{DBNames.SupplierContactFieldNameId}" ] ),
				SupplierId = DatabaseValueConverter.GetInt( reader [ $"{DBNames.SupplierContactFieldNameSupplierId}" ] ),
				ContactTypeId = DatabaseValueConverter.GetInt( reader [ $"{DBNames.SupplierContactFieldNameTypeId}" ] ),
				Name = DatabaseValueConverter.GetString( reader [ $"{DBNames.SupplierContactFieldNameName}" ] ),
				Mail = DatabaseValueConverter.GetString( reader [ $"{DBNames.SupplierContactFieldNameMail}" ] ),
				Phone = DatabaseValueConverter.GetString( reader [ $"{DBNames.SupplierContactFieldNamePhone}" ] ),
			};
		}, null, cancellationToken );
	}

	public Task<int> InsertNewContactAsync( Dictionary<string, object?> queryParameters ) => InsertNewContactAsync( queryParameters, CancellationToken.None );

	public async Task<int> InsertNewContactAsync( Dictionary<string, object?> queryParameters, CancellationToken cancellationToken )
	{
		Dictionary<string, object> parameters = new()
		{
			{ $"@{DBNames.SupplierContactFieldNameSupplierId}", queryParameters[$"@{DBNames.SupplierContactFieldNameSupplierId}"] ?? DBNull.Value },
			{ $"@{DBNames.SupplierContactFieldNameTypeId}", queryParameters[$"@{DBNames.SupplierContactFieldNameTypeId}"] ?? DBNull.Value },
			{ $"@{DBNames.SupplierContactFieldNameName}", queryParameters[$"@{DBNames.SupplierContactFieldNameName}"] ?? DBNull.Value },
			{ $"@{DBNames.SupplierContactFieldNameMail}", queryParameters[$"@{DBNames.SupplierContactFieldNameMail}"] ?? DBNull.Value },
			{ $"@{DBNames.SupplierContactFieldNamePhone}", queryParameters[$"@{DBNames.SupplierContactFieldNamePhone}"] ?? DBNull.Value }
		};

		uint newId = await _dataService.ExecuteScalarAsync<uint>( AddNewContactQuery, parameters, cancellationToken );

		return ( int ) newId;
	}

	public Task UpdateContactAsync( Dictionary<string, object?> queryParameters ) => UpdateContactAsync( queryParameters, CancellationToken.None );

	public async Task UpdateContactAsync( Dictionary<string, object?> queryParameters, CancellationToken cancellationToken )
	{
		Dictionary<string, object> parameters = new()
		{
			{ $"@{DBNames.SupplierContactFieldNameId}", queryParameters[$"@{DBNames.SupplierContactFieldNameId}"] ?? DBNull.Value },
			{ $"@{DBNames.SupplierContactFieldNameTypeId}", queryParameters[$"@{DBNames.SupplierContactFieldNameTypeId}"] ?? DBNull.Value },
			{ $"@{DBNames.SupplierContactFieldNameName}", queryParameters[$"@{DBNames.SupplierContactFieldNameName}"] ?? DBNull.Value },
			{ $"@{DBNames.SupplierContactFieldNameMail}", queryParameters[$"@{DBNames.SupplierContactFieldNameMail}"] ?? DBNull.Value },
			{ $"@{DBNames.SupplierContactFieldNamePhone}", queryParameters[$"@{DBNames.SupplierContactFieldNamePhone}"] ?? DBNull.Value }
		};

		await _dataService.ExecuteScalarAsync<uint>( UpdateContactQuery, parameters, cancellationToken );
	}

	public Task DeleteContactAsync( int contactId ) => DeleteContactAsync( contactId, CancellationToken.None );

	public async Task DeleteContactAsync( int contactId, CancellationToken cancellationToken )
	{
		var parameters = new Dictionary<string, object>
		{
			{ $"@{DBNames.SupplierContactFieldNameId}", contactId }
		};

		try
		{
			await _dataService.ExecuteScalarAsync<uint>( DeleteContactQuery, parameters, cancellationToken );
		}
		catch ( Exception ex ) when ( DatabaseErrors.IsRowStillReferenced( ex ) )
		{
			throw new EntityInUseException(
				$"{Lang.metadataSupplierContactDeleteError}." );
		}
	}

	public Task<bool> NameExistsAsync( string? contactName ) => NameExistsAsync( contactName, CancellationToken.None );

	public async Task<bool> NameExistsAsync( string? contactName, CancellationToken cancellationToken )
	{
		if ( string.IsNullOrWhiteSpace( contactName ) )
			return false;

		var parameters = new Dictionary<string, object>
		{
			{ $"@{DBNames.SupplierContactFieldNameName}", contactName.Trim() }
		};

		var count = await _dataService.ExecuteScalarAsync<int>( ContactNameExistsQuery, parameters, cancellationToken );
		return count > 0;
	}

}