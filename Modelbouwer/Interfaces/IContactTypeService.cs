namespace Modelbouwer.Interfaces;

public interface IContactTypeService
{
	Task<List<ContactTypeModel>> GetAllContactTypesAsync();
	Task<List<ContactTypeModel>> GetAllContactTypesAsync( CancellationToken cancellationToken );
	Task<int> InsertNewContactTypeAsync( Dictionary<string, object?> queryParameters );
	Task<int> InsertNewContactTypeAsync( Dictionary<string, object?> queryParameters, CancellationToken cancellationToken );
	Task UpdateContactTypeAsync( Dictionary<string, object?> queryParameters );
	Task UpdateContactTypeAsync( Dictionary<string, object?> queryParameters, CancellationToken cancellationToken );
	Task DeleteContactTypeAsync( int contacttypeId );
	Task DeleteContactTypeAsync( int contacttypeId, CancellationToken cancellationToken );

	Task<bool> NameExistsAsync( string? contacttypeName );
	Task<bool> NameExistsAsync( string? contacttypeName, CancellationToken cancellationToken );
}