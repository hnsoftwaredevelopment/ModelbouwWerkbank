using System;
using System.Collections.Generic;
using System.Text;

namespace Modelbouwer.Interfaces;

public interface IWorkTypeService
{
	Task<List<WorkTypeModel>> GetAllWorkTypesAsync();
	Task<List<WorkTypeModel>> GetAllWorkTypesAsync( CancellationToken cancellationToken );
	Task<int> InsertNewWorkTypeAsync( Dictionary<string, object?> queryParameters );
	Task<int> InsertNewWorkTypeAsync( Dictionary<string, object?> queryParameters, CancellationToken cancellationToken );
	Task UpdateWorkTypeAsync( Dictionary<string, object?> queryParameters );
	Task UpdateWorkTypeAsync( Dictionary<string, object?> queryParameters, CancellationToken cancellationToken );
	Task DeleteWorkTypeAsync( int worktypeId );
	Task DeleteWorkTypeAsync( int worktypeId, CancellationToken cancellationToken );

	Task<bool> NameExistsAsync( string? worktypeName, int? parentId );
	Task<bool> NameExistsAsync( string? worktypeName, int? parentId, CancellationToken cancellationToken );
}