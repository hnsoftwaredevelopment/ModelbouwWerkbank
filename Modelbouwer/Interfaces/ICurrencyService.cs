namespace Modelbouwer.Interfaces;

public interface ICurrencyService
{
	Task<List<CurrencyModel>> GetAllCurrenciesAsync();
	Task<List<CurrencyModel>> GetAllCurrenciesAsync( CancellationToken cancellationToken );
	Task<int> InsertNewCurrencyAsync( Dictionary<string, object?> queryParameters );
	Task<int> InsertNewCurrencyAsync( Dictionary<string, object?> queryParameters, CancellationToken cancellationToken );
	Task UpdateCurrencyAsync( Dictionary<string, object?> queryParameters );
	Task UpdateCurrencyAsync( Dictionary<string, object?> queryParameters, CancellationToken cancellationToken );
	Task DeleteCurrencyAsync( int currencyId );
	Task DeleteCurrencyAsync( int currencyId, CancellationToken cancellationToken );

	Task<bool> CodeExistsAsync( string? currencyCode );
	Task<bool> CodeExistsAsync( string? currencyCode, CancellationToken cancellationToken );
	Task<bool> NameExistsAsync( string? currencyName );
	Task<bool> NameExistsAsync( string? currencyName, CancellationToken cancellationToken );
}