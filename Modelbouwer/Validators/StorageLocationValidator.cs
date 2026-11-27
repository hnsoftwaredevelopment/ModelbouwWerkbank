namespace Modelbouwer.Validators;

public class StorageLocationValidator : IEntityValidator<StorageLocationModel>
{
	private readonly IStorageLocationService _dataService;

	public StorageLocationValidator( IStorageLocationService dataService ) => _dataService = dataService;

	public Task<ValidationResult> ValidateAsync( StorageLocationModel storagelocation ) => ValidateAsync( storagelocation, CancellationToken.None );

	public async Task<ValidationResult> ValidateAsync( StorageLocationModel storagelocation, CancellationToken cancellationToken )
	{
		var result = new ValidationResult();

		// Name
		if ( string.IsNullOrWhiteSpace( storagelocation.StorageName ) )
		{
			result.Errors.Add( Lang.ExportValidationMessageNameRequirered );
		}
		else if ( storagelocation.StorageName.Length > 400 )
		{
			result.Errors.Add( Lang.ExportValidationMessageNameLength );
		}

		if ( await _dataService.NameExistsAsync( storagelocation.StorageName, storagelocation.ParentId, cancellationToken ) )
		{
			result.Errors.Add( Lang.ExportValidationStorageNameExists );
		}

		return result;
	}
}