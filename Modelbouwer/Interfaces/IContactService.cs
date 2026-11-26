namespace Modelbouwer.Interfaces;

public interface IContactService
{
	Task<List<SupplierContactModel>> GetAllContactsAsync();
	Task<List<SupplierContactModel>> GetAllContactsAsync( CancellationToken cancellationToken );
	Task<int> InsertNewContactAsync( Dictionary<string, object?> queryParameters );
	Task<int> InsertNewContactAsync( Dictionary<string, object?> queryParameters, CancellationToken cancellationToken );
	Task UpdateContactAsync( Dictionary<string, object?> queryParameters );
	Task UpdateContactAsync( Dictionary<string, object?> queryParameters, CancellationToken cancellationToken );
	Task DeleteContactAsync( int supplierId );
	Task DeleteContactAsync( int supplierId, CancellationToken cancellationToken );
	Task<bool> NameExistsAsync( string? contactName );
	Task<bool> NameExistsAsync( string? contactName, CancellationToken cancellationToken );


}