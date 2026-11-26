using System;
using System.Collections.Generic;
using System.Text;

namespace Modelbouwer.Interfaces;

public interface ICategoryService
{
	Task<List<CategoryModel>> GetAllCategorysAsync();
	Task<List<CategoryModel>> GetAllCategorysAsync( CancellationToken cancellationToken );
	Task<int> InsertNewCategoryAsync( Dictionary<string, object?> queryParameters );
	Task<int> InsertNewCategoryAsync( Dictionary<string, object?> queryParameters, CancellationToken cancellationToken );
	Task UpdateCategoryAsync( Dictionary<string, object?> queryParameters );
	Task UpdateCategoryAsync( Dictionary<string, object?> queryParameters, CancellationToken cancellationToken );
	Task DeleteCategoryAsync( int categoryId );
	Task DeleteCategoryAsync( int categoryId, CancellationToken cancellationToken );
	Task<bool> IsCategoryUsedAsync( int categoryId );
	Task<bool> IsCategoryUsedAsync( int categoryId, CancellationToken cancellationToken );
	Task<bool> NameExistsAsync( string? categoryName, int? parentId );
	Task<bool> NameExistsAsync( string? categoryName, int? parentId, CancellationToken cancellationToken );
}