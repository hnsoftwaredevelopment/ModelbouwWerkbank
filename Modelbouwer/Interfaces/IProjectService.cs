namespace Modelbouwer.Interfaces;

public interface IProjectService
{
	Task<List<ProjectModel>> GetAllProjectsAsync();
	Task<List<ProjectModel>> GetAllProjectsAsync( CancellationToken cancellationToken );
	Task<int> InsertNewProjectAsync( Dictionary<string, object?> queryParameters );
	Task<int> InsertNewProjectAsync( Dictionary<string, object?> queryParameters, CancellationToken cancellationToken );
	Task UpdateProjectAsync( Dictionary<string, object?> queryParameters );
	Task UpdateProjectAsync( Dictionary<string, object?> queryParameters, CancellationToken cancellationToken );
	Task DeleteProjectAsync( int projectId );
	Task DeleteProjectAsync( int projectId, CancellationToken cancellationToken );
	Task<bool> IsProjectUsedAsync( int projectId );
	Task<bool> IsProjectUsedAsync( int projectId, CancellationToken cancellationToken );
	Task<bool> NameExistsAsync( string? projectName );
	Task<bool> NameExistsAsync( string? projectName, CancellationToken cancellationToken );
	//Task<DateOnly?> GetExpectedEndDateAsync( int projectId );
	Task<DateOnly?> GetLastWorkDateOnProjectAsync( int projectId );
	Task<DateOnly?> GetLastWorkDateOnProjectAsync( int projectId, CancellationToken cancellationToken );
	Task<ProjectWorkStats?> GetProjectWorkStatsAsync( int projectId );
	Task<ProjectWorkStats?> GetProjectWorkStatsAsync( int projectId, CancellationToken cancellationToken );
}