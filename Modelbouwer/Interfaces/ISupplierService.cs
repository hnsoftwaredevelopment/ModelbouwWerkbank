using Modelbouwer.Model;

namespace Modelbouwer.Interfaces;

public interface ISupplierService
{
	Task<List<SupplierModel>> GetAllSuppliersAsync();
	Task<List<SupplierModel>> GetAllSuppliersAsync( CancellationToken cancellationToken );
	Task<List<ProductSupplierModel>> GetAllProductSuppliersAsync();
	Task<List<ProductSupplierModel>> GetAllProductSuppliersAsync( CancellationToken cancellationToken );
	Task<ProductSupplierModel?> GetProductSupplierAsync( int supplierId, int productId );
	Task<ProductSupplierModel?> GetProductSupplierAsync( int supplierId, int productId, CancellationToken cancellationToken );
	Task<int> UpsertProductSupplierAsync( ProductSupplierModel productSupplier );
	Task<int> UpsertProductSupplierAsync( ProductSupplierModel productSupplier, CancellationToken cancellationToken );
	Task<List<CountryModel>> GetAllCountriesAsync();
	Task<List<CountryModel>> GetAllCountriesAsync( CancellationToken cancellationToken );
	Task<List<CurrencyModel>> GetAllCurrenciesAsync();
	Task<List<CurrencyModel>> GetAllCurrenciesAsync( CancellationToken cancellationToken );
	Task<int> InsertNewSupplierAsync( Dictionary<string, object?> queryParameters );
	Task<int> InsertNewSupplierAsync( Dictionary<string, object?> queryParameters, CancellationToken cancellationToken );
	Task UpdateSupplierAsync( Dictionary<string, object?> queryParameters );
	Task UpdateSupplierAsync( Dictionary<string, object?> queryParameters, CancellationToken cancellationToken );
	Task DeleteSupplierAsync( int supplierId );
	Task DeleteSupplierAsync( int supplierId, CancellationToken cancellationToken );

	Task<bool> IsSupplierUsedAsync( int supplierId );
	Task<bool> IsSupplierUsedAsync( int supplierId, CancellationToken cancellationToken );
	Task<bool> NameExistsAsync( string? supplierName );
	Task<bool> NameExistsAsync( string? supplierName, CancellationToken cancellationToken );
}