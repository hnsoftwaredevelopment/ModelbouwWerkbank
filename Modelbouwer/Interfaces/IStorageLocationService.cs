using System;
using System.Collections.Generic;
using System.Text;

namespace Modelbouwer.Interfaces;

public interface IStorageLocationService
{
	Task<List<StorageLocationModel>> GetAllStorageLocationsAsync();
	Task<List<StorageLocationModel>> GetAllStorageLocationsAsync( CancellationToken cancellationToken );
	Task<int> InsertNewStorageLocationAsync( Dictionary<string, object?> queryParameters );
	Task<int> InsertNewStorageLocationAsync( Dictionary<string, object?> queryParameters, CancellationToken cancellationToken );
	Task UpdateStorageLocationAsync( Dictionary<string, object?> queryParameters );
	Task UpdateStorageLocationAsync( Dictionary<string, object?> queryParameters, CancellationToken cancellationToken );
	Task DeleteStorageLocationAsync( int storagelocationId );
	Task DeleteStorageLocationAsync( int storagelocationId, CancellationToken cancellationToken );
	Task<bool> IsStorageLocationUsedAsync( int storagelocationId );
	Task<bool> IsStorageLocationUsedAsync( int storagelocationId, CancellationToken cancellationToken );
	Task<bool> NameExistsAsync( string? StorageName, int? parentId );
	Task<bool> NameExistsAsync( string? StorageName, int? parentId, CancellationToken cancellationToken );
}