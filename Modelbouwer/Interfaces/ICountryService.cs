namespace Modelbouwer.Interfaces;

public interface ICountryService
{
	Task<bool> CodeExistsAsync( string? countryCode );
	Task<bool> CodeExistsAsync( string? countryCode, CancellationToken cancellationToken );
	Task<bool> NameExistsAsync( string? countryName );
	Task<bool> NameExistsAsync( string? countryName, CancellationToken cancellationToken );

	Task<List<CountryModel>> GetAllCountriesAsync();
	Task<List<CountryModel>> GetAllCountriesAsync( CancellationToken cancellationToken );

	Task<int> InsertNewCountryAsync( Dictionary<string, object?> parameters );
	Task<int> InsertNewCountryAsync( Dictionary<string, object?> parameters, CancellationToken cancellationToken );
	Task DeleteCountryAsync( int countryId );
	Task DeleteCountryAsync( int countryId, CancellationToken cancellationToken );
	Task UpdateCountryAsync( Dictionary<string, object?> parameters );
	Task UpdateCountryAsync( Dictionary<string, object?> parameters, CancellationToken cancellationToken );
}