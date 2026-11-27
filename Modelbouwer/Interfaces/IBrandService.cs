using System;
using System.Collections.Generic;
using System.Text;

namespace Modelbouwer.Interfaces;

public interface IBrandService
{
	Task<List<BrandModel>> GetAllBrandsAsync();
	Task<List<BrandModel>> GetAllBrandsAsync( CancellationToken cancellationToken );
	Task<int> InsertNewBrandAsync( Dictionary<string, object?> queryParameters );
	Task<int> InsertNewBrandAsync( Dictionary<string, object?> queryParameters, CancellationToken cancellationToken );
	Task UpdateBrandAsync( Dictionary<string, object?> queryParameters );
	Task UpdateBrandAsync( Dictionary<string, object?> queryParameters, CancellationToken cancellationToken );
	Task DeleteBrandAsync( int brandId );
	Task DeleteBrandAsync( int brandId, CancellationToken cancellationToken );
	Task<bool> IsBrandUsedAsync( int brandId );
	Task<bool> IsBrandUsedAsync( int brandId, CancellationToken cancellationToken );
	Task<bool> NameExistsAsync( string? brandName );
	Task<bool> NameExistsAsync( string? brandName, CancellationToken cancellationToken );
}