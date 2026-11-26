using System.Collections.Generic;
using System.Text;

using Modelbouwer.Interfaces;

namespace Modelbouwer.Services;

public class CurrencyService : ICurrencyService
{
	private readonly GenericDataService _dataService;
	public bool CurrencyUsed { get; set; }

	public CurrencyService( GenericDataService dataService )
	{
		_dataService = dataService;
	}

	#region Database query's
	public string CompleteCurrencyList = $"" +
		$"SELECT " +
		$"{DBNames.CurrencyFieldNameId} AS {DBNames.CurrencyFieldNameId}, " +
		$"{DBNames.CurrencyFieldNameCode} AS {DBNames.CurrencyFieldNameCode}, " +
		$"{DBNames.CurrencyFieldNameSymbol} AS {DBNames.CurrencyFieldNameSymbol}, " +
		$"{DBNames.CurrencyFieldNameName} AS {DBNames.CurrencyFieldNameName}, " +
		$"{DBNames.CurrencyFieldNameRate} AS {DBNames.CurrencyFieldNameRate}" +
		$" FROM {DBNames.Database}.{DBNames.CurrencyTable};";

	public string AddNewCurrencyQuery =
		$"INSERT INTO {DBNames.Database}.{DBNames.CurrencyTable} " +
		$"({DBNames.CurrencyFieldNameCode}, {DBNames.CurrencyFieldNameSymbol}, {DBNames.CurrencyFieldNameName}, {DBNames.CurrencyFieldNameRate}) " +
		$"VALUES " +
		$"(@{DBNames.CurrencyFieldNameCode}, @{DBNames.CurrencyFieldNameSymbol}, @{DBNames.CurrencyFieldNameName}, @{DBNames.CurrencyFieldNameRate});" +
		$"{DBNames.SqlSelectLastId}";

	public string UpdateCurrencyQuery =
		$"UPDATE {DBNames.Database}.{DBNames.CurrencyTable} " +
		$"SET " +
		$"{DBNames.CurrencyFieldNameCode} = @{DBNames.CurrencyFieldNameCode}, " +
		$"{DBNames.CurrencyFieldNameSymbol} = @{DBNames.CurrencyFieldNameSymbol}, " +
		$"{DBNames.CurrencyFieldNameName} = @{DBNames.CurrencyFieldNameName}, " +
		$"{DBNames.CurrencyFieldNameRate} = @{DBNames.CurrencyFieldNameRate} " +
		$"WHERE {DBNames.CurrencyFieldNameId} = @{DBNames.CurrencyFieldNameId};";

	public string DeleteCurrencyQuery =
		$"DELETE FROM {DBNames.Database}.{DBNames.CurrencyTable} " +
		$"WHERE {DBNames.CurrencyFieldNameId} = @{DBNames.CurrencyFieldNameId};";

	public string CurrencyCodeExistsQuery =
		$"SELECT COUNT({DBNames.CurrencyFieldNameId}) " +
		$"FROM {DBNames.Database}.{DBNames.CurrencyTable} " +
		$"WHERE LOWER({DBNames.CurrencyFieldNameCode}) = LOWER(@{DBNames.CurrencyFieldNameCode})";

	public string CurrencyNameExistsQuery =
		$"SELECT COUNT({DBNames.CurrencyFieldNameId}) " +
		$"FROM {DBNames.Database}.{DBNames.CurrencyTable} " +
		$"WHERE LOWER({DBNames.CurrencyFieldNameName}) = LOWER(@{DBNames.CurrencyFieldNameName})";

	public string CurrencyUsedQuery = $"SELECT COUNT({DBNames.CurrencyFieldNameId}) FROM {DBNames.Database}.{DBNames.CurrencyTable} WHERE {DBNames.CurrencyFieldNameId} = @CurrencyId";
	#endregion

	public Task<List<CurrencyModel>> GetAllCurrenciesAsync() => GetAllCurrenciesAsync( CancellationToken.None );

	public Task<List<CurrencyModel>> GetAllCurrenciesAsync( CancellationToken cancellationToken )
	{
		return _dataService.ExecuteQueryAsync( CompleteCurrencyList, reader =>
		{
			return new CurrencyModel
			{
				CurrencyId = DatabaseValueConverter.GetInt( reader [ $"{DBNames.CurrencyFieldNameId}" ] ),
				CurrencyCode = DatabaseValueConverter.GetString( reader [ $"{DBNames.CurrencyFieldNameCode}" ] ),
				CurrencyName = DatabaseValueConverter.GetString( reader [ $"{DBNames.CurrencyFieldNameName}" ] ),
				CurrencySymbol = DatabaseValueConverter.GetString( reader [ $"{DBNames.CurrencyFieldNameSymbol}" ] ),
				CurrencyConversionRate = DatabaseValueConverter.GetDouble( reader [ $"{DBNames.CurrencyFieldNameRate}" ] )
			};
		}, null, cancellationToken );
	}

	public Task<int> InsertNewCurrencyAsync( Dictionary<string, object?> queryParameters ) => InsertNewCurrencyAsync( queryParameters, CancellationToken.None );

	public async Task<int> InsertNewCurrencyAsync( Dictionary<string, object?> queryParameters, CancellationToken cancellationToken )
	{
		Dictionary<string, object> parameters = new()
		{
			{ $"@{DBNames.CurrencyFieldNameCode}", queryParameters[$"@{DBNames.CurrencyFieldNameCode}"] ?? DBNull.Value },
			{ $"@{DBNames.CurrencyFieldNameName}", queryParameters[$"@{DBNames.CurrencyFieldNameName}"] ?? DBNull.Value },
			{ $"@{DBNames.CurrencyFieldNameSymbol}", queryParameters[$"@{DBNames.CurrencyFieldNameSymbol}"] ?? DBNull.Value },
			{ $"@{DBNames.CurrencyFieldNameRate}", queryParameters[$"@{DBNames.CurrencyFieldNameRate}"] ?? DBNull.Value }
		};

		uint newId = await _dataService.ExecuteScalarAsync<uint>( AddNewCurrencyQuery, parameters, cancellationToken );

		return ( int ) newId;
	}

	public Task UpdateCurrencyAsync( Dictionary<string, object?> queryParameters ) => UpdateCurrencyAsync( queryParameters, CancellationToken.None );

	public async Task UpdateCurrencyAsync( Dictionary<string, object?> queryParameters, CancellationToken cancellationToken )
	{
		Dictionary<string, object> parameters = new()
		{
			{ $"@{DBNames.CurrencyFieldNameId}", queryParameters[$"@{DBNames.CurrencyFieldNameId}"] ?? DBNull.Value },
			{ $"@{DBNames.CurrencyFieldNameCode}", queryParameters[$"@{DBNames.CurrencyFieldNameCode}"] ?? DBNull.Value },
			{ $"@{DBNames.CurrencyFieldNameName}", queryParameters[$"@{DBNames.CurrencyFieldNameName}"] ?? DBNull.Value },
			{ $"@{DBNames.CurrencyFieldNameSymbol}", queryParameters[$"@{DBNames.CurrencyFieldNameSymbol}"] ?? DBNull.Value },
			{ $"@{DBNames.CurrencyFieldNameRate}", queryParameters[$"@{DBNames.CurrencyFieldNameRate}"] ?? DBNull.Value }
		};

		await _dataService.ExecuteScalarAsync<uint>( UpdateCurrencyQuery, parameters, cancellationToken );
	}

	public Task DeleteCurrencyAsync( int currencyId ) => DeleteCurrencyAsync( currencyId, CancellationToken.None );

	public async Task DeleteCurrencyAsync( int currencyId, CancellationToken cancellationToken )
	{
		var parameters = new Dictionary<string, object>
		{
			{ $"@{DBNames.CurrencyFieldNameId}", currencyId }
		};

		try
		{
			await _dataService.ExecuteScalarAsync<uint>( DeleteCurrencyQuery, parameters, cancellationToken );
		}
		catch ( MySqlException ex ) when ( ex.Number == 1451 )
		{
			throw new EntityInUseException(
				$"{Lang.metadataCurrencyDeleteError}." );
		}
	}

	public Task<bool> IsCurrencyUsedAsync( int currencyId ) => IsCurrencyUsedAsync( currencyId, CancellationToken.None );

	public async Task<bool> IsCurrencyUsedAsync( int currencyId, CancellationToken cancellationToken )
	{
		var parameters = new Dictionary<string, object>
		{
			{ $"@{DBNames.SupplierFieldNameCurrencyId}", currencyId }
		};

		var usedCount = await _dataService.ExecuteScalarAsync<int>(
			CurrencyUsedQuery,
			parameters, cancellationToken);

		return usedCount > 0;
	}

	public Task<bool> CodeExistsAsync( string? currencyCode ) => CodeExistsAsync( currencyCode, CancellationToken.None );

	public async Task<bool> CodeExistsAsync( string? currencyCode, CancellationToken cancellationToken )
	{
		if ( string.IsNullOrWhiteSpace( currencyCode ) )
			return false;

		var parameters = new Dictionary<string, object>
		{
			{ $"@{DBNames.CurrencyFieldNameCode}", currencyCode.Trim() }
		};

		var count = await _dataService.ExecuteScalarAsync<int>( CurrencyCodeExistsQuery, parameters, cancellationToken );
		return count > 0;
	}

	public Task<bool> NameExistsAsync( string? currencyName ) => NameExistsAsync( currencyName, CancellationToken.None );

	public async Task<bool> NameExistsAsync( string? currencyName, CancellationToken cancellationToken )
	{
		if ( string.IsNullOrWhiteSpace( currencyName ) )
			return false;

		var parameters = new Dictionary<string, object>
		{
			{ $"@{DBNames.CurrencyFieldNameName}", currencyName.Trim() }
		};

		var count = await _dataService.ExecuteScalarAsync<int>( CurrencyNameExistsQuery, parameters, cancellationToken );
		return count > 0;
	}
}