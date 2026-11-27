using Modelbouwer.Interfaces;

namespace Modelbouwer.Services;

public class CountryService( GenericDataService dataService ) : ICountryService
{
	private readonly GenericDataService _dataService = dataService;
	public bool CountryUsed { get; set; }

	#region Database queries
	public string CompleteCountryList =
		$"SELECT " +
		$"{DBNames.CountryFieldNameId} AS {DBNames.CountryFieldNameId}, " +
		$"{DBNames.CountryFieldNameCode} AS {DBNames.CountryFieldNameCode}, " +
		$"{DBNames.CountryFieldNameName} AS {DBNames.CountryFieldNameName}, " +
		$"{DBNames.CountryFieldNameCurrencyId} AS {DBNames.CountryFieldNameCurrencyId}, " +
		$"{DBNames.CountryFieldNameCurrencySymbol} AS {DBNames.CountryFieldNameCurrencySymbol} " +
		$"FROM {DBNames.Database}.{DBNames.CountryTable};";

	public string AddNewCountryQuery =
		$"INSERT INTO {DBNames.Database}.{DBNames.CountryTable} " +
		$"({DBNames.CountryFieldNameCode}, {DBNames.CountryFieldNameCurrencyId}, {DBNames.CountryFieldNameCurrencySymbol}, {DBNames.CountryFieldNameName}) " +
		$"VALUES " +
		$"(@{DBNames.CountryFieldNameCode}, @{DBNames.CountryFieldNameCurrencyId}, @{DBNames.CountryFieldNameCurrencySymbol}, @{DBNames.CountryFieldNameName});" +
		$"{DBNames.SqlSelectLastId}";

	public string UpdateCountryQuery =
		$"UPDATE {DBNames.Database}.{DBNames.CountryTable} " +
		$"SET " +
		$"{DBNames.CountryFieldNameCode} = @{DBNames.CountryFieldNameCode}, " +
		$"{DBNames.CountryFieldNameCurrencyId} = @{DBNames.CountryFieldNameCurrencyId}, " +
		$"{DBNames.CountryFieldNameCurrencySymbol} = @{DBNames.CountryFieldNameCurrencySymbol}, " +
		$"{DBNames.CountryFieldNameName} = @{DBNames.CountryFieldNameName} " +
		$"WHERE {DBNames.CountryFieldNameId} = @{DBNames.CountryFieldNameId};";

	public string DeleteCountryQuery =
		$"DELETE FROM {DBNames.Database}.{DBNames.CountryTable} " +
		$"WHERE {DBNames.CountryFieldNameId} = @{DBNames.CountryFieldNameId};";

	public string CountryUsedQuery =
		$"SELECT COUNT({DBNames.SupplierFieldNameCountryId}) " +
		$"FROM {DBNames.Database}.{DBNames.SupplierTable} " +
		$"WHERE {DBNames.SupplierFieldNameCountryId} = @CountryId";

	public string CountryCodeExistsQuery =
		$"SELECT COUNT({DBNames.CountryFieldNameId}) " +
		$"FROM {DBNames.Database}.{DBNames.CountryTable} " +
		$"WHERE LOWER({DBNames.CountryFieldNameCode}) = LOWER(@{DBNames.CountryFieldNameCode})";

	public string CountryNameExistsQuery =
		$"SELECT COUNT({DBNames.CountryFieldNameId}) " +
		$"FROM {DBNames.Database}.{DBNames.CountryTable} " +
		$"WHERE LOWER({DBNames.CountryFieldNameName}) = LOWER(@{DBNames.CountryFieldNameName})";
	#endregion

	public Task<List<CountryModel>> GetAllCountriesAsync() => GetAllCountriesAsync( CancellationToken.None );

	public Task<List<CountryModel>> GetAllCountriesAsync( CancellationToken cancellationToken )
	{
		return _dataService.ExecuteQueryAsync( CompleteCountryList, reader =>
		{
			return new CountryModel
			{
				CountryId = DatabaseValueConverter.GetInt( reader [ $"{DBNames.CountryFieldNameId}" ] ),
				CountryCode = DatabaseValueConverter.GetString( reader [ $"{DBNames.CountryFieldNameCode}" ] ),
				CountryName = DatabaseValueConverter.GetString( reader [ $"{DBNames.CountryFieldNameName}" ] ),
				CountryCurrencyId = DatabaseValueConverter.GetInt( reader [ $"{DBNames.CountryFieldNameCurrencyId}" ] ),
				CountryCurrencySymbol = DatabaseValueConverter.GetString( reader [ $"{DBNames.CountryFieldNameCurrencySymbol}" ] )
			};
		}, null, cancellationToken );
	}

	public Task<int> InsertNewCountryAsync( Dictionary<string, object?> queryParameters ) => InsertNewCountryAsync( queryParameters, CancellationToken.None );

	public async Task<int> InsertNewCountryAsync( Dictionary<string, object?> queryParameters, CancellationToken cancellationToken )
	{
		Dictionary<string, object> parameters = new()
		{
			{ $"@{DBNames.CountryFieldNameCode}", queryParameters[$"@{DBNames.CountryFieldNameCode}"] ?? DBNull.Value },
			{ $"@{DBNames.CountryFieldNameCurrencyId}", queryParameters[$"@{DBNames.CountryFieldNameCurrencyId}"] ?? DBNull.Value },
			{ $"@{DBNames.CountryFieldNameCurrencySymbol}", queryParameters[$"@{DBNames.CountryFieldNameCurrencySymbol}"] ?? DBNull.Value },
			{ $"@{DBNames.CountryFieldNameName}", queryParameters[$"@{DBNames.CountryFieldNameName}"] ?? DBNull.Value }
		};

		uint newId = await _dataService.ExecuteScalarAsync<uint>( AddNewCountryQuery, parameters, cancellationToken );

		return ( int ) newId;
	}

	public Task UpdateCountryAsync( Dictionary<string, object?> queryParameters ) => UpdateCountryAsync( queryParameters, CancellationToken.None );

	public async Task UpdateCountryAsync( Dictionary<string, object?> queryParameters, CancellationToken cancellationToken )
	{
		Dictionary<string, object> parameters = new()
		{
			{ $"@{DBNames.CountryFieldNameCode}", queryParameters[$"@{DBNames.CountryFieldNameCode}"] ?? DBNull.Value },
			{ $"@{DBNames.CountryFieldNameCurrencyId}", queryParameters[$"@{DBNames.CountryFieldNameCurrencyId}"] ?? DBNull.Value },
			{ $"@{DBNames.CountryFieldNameCurrencySymbol}", queryParameters[$"@{DBNames.CountryFieldNameCurrencySymbol}"] ?? DBNull.Value },
			{ $"@{DBNames.CountryFieldNameName}", queryParameters[$"@{DBNames.CountryFieldNameName}"] ?? DBNull.Value },
			{ $"@{DBNames.CountryFieldNameId}", queryParameters[$"@{DBNames.CountryFieldNameId}"] ?? DBNull.Value }
		};

		await _dataService.ExecuteScalarAsync<uint>( UpdateCountryQuery, parameters, cancellationToken );
	}

	public Task DeleteCountryAsync( int countryId ) => DeleteCountryAsync( countryId, CancellationToken.None );

	public async Task DeleteCountryAsync( int countryId, CancellationToken cancellationToken )
	{
		var parameters = new Dictionary<string, object>
		{
			{ $"@{DBNames.CountryFieldNameId}", countryId }
		};

		try
		{
			await _dataService.ExecuteScalarAsync<uint>( DeleteCountryQuery, parameters, cancellationToken );
		}
		catch ( MySqlException ex ) when ( ex.Number == 1451 )
		{
			throw new EntityInUseException(
				$"{Lang.metadataCountryDeleteError}." );
		}
	}

	public Task<bool> IsCountryUsedAsync( int countryId ) => IsCountryUsedAsync( countryId, CancellationToken.None );

	public async Task<bool> IsCountryUsedAsync( int countryId, CancellationToken cancellationToken )
	{
		var parameters = new Dictionary<string, object>
		{
			{ $"@{DBNames.SupplierFieldNameCountryId}", countryId }
		};

		var usedCount = await _dataService.ExecuteScalarAsync<int>(
			CountryUsedQuery,
			parameters, cancellationToken);

		return usedCount > 0;
	}

	public Task<bool> CodeExistsAsync( string? countryCode ) => CodeExistsAsync( countryCode, CancellationToken.None );

	public async Task<bool> CodeExistsAsync( string? countryCode, CancellationToken cancellationToken )
	{
		if ( string.IsNullOrWhiteSpace( countryCode ) )
			return false;

		var parameters = new Dictionary<string, object>
		{
			{ $"@{DBNames.CountryFieldNameCode}", countryCode.Trim() }
		};

		var count = await _dataService.ExecuteScalarAsync<int>( CountryCodeExistsQuery, parameters, cancellationToken );
		return count > 0;
	}

	public Task<bool> NameExistsAsync( string? countryName ) => NameExistsAsync( countryName, CancellationToken.None );

	public async Task<bool> NameExistsAsync( string? countryName, CancellationToken cancellationToken )
	{
		if ( string.IsNullOrWhiteSpace( countryName ) )
			return false;

		var parameters = new Dictionary<string, object>
		{
			{ $"@{DBNames.CountryFieldNameName}", countryName.Trim() }
		};

		var count = await _dataService.ExecuteScalarAsync<int>( CountryNameExistsQuery, parameters, cancellationToken );
		return count > 0;
	}
}