namespace Modelbouwer.Interfaces;

public interface IProductService
{
	Task<List<ProductModel>> GetAllProductsAsync();
	Task<List<ProductModel>> GetAllProductsAsync( CancellationToken cancellationToken );
	Task<List<BrandModel>> GetAllBrandsAsync();
	Task<List<BrandModel>> GetAllBrandsAsync( CancellationToken cancellationToken );
	Task<List<UnitModel>> GetAllUnitsAsync();
	Task<List<UnitModel>> GetAllUnitsAsync( CancellationToken cancellationToken );
	Task<List<CategoryModel>> GetAllCategoriesAsync();
	Task<List<CategoryModel>> GetAllCategoriesAsync( CancellationToken cancellationToken );
	Task<int> InsertNewProductAsync( Dictionary<string, object?> queryParameters );
	Task<int> InsertNewProductAsync( Dictionary<string, object?> queryParameters, CancellationToken cancellationToken );
	Task UpdateProductAsync( Dictionary<string, object?> queryParameters );
	Task UpdateProductAsync( Dictionary<string, object?> queryParameters, CancellationToken cancellationToken );
	Task DeleteProductAsync( int productId );
	Task DeleteProductAsync( int productId, CancellationToken cancellationToken );

	Task<bool> IsProductUsedAsync( int productId );
	Task<bool> IsProductUsedAsync( int productId, CancellationToken cancellationToken );
	Task<bool> NameExistsAsync( string? productName );
	Task<bool> NameExistsAsync( string? productName, CancellationToken cancellationToken );
}