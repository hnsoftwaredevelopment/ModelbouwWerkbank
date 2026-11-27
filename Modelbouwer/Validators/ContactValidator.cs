namespace Modelbouwer.Validators;

public class ContactValidator : IEntityValidator<SupplierContactModel>
{
	private readonly IContactService _dataService;

	public ContactValidator( IContactService dataService ) => _dataService = dataService;

	public Task<ValidationResult> ValidateAsync( SupplierContactModel contact ) => ValidateAsync( contact, CancellationToken.None );

	public async Task<ValidationResult> ValidateAsync( SupplierContactModel contact, CancellationToken cancellationToken )
	{
		var result = new ValidationResult();

		// Name
		if ( string.IsNullOrWhiteSpace( contact.Name ) )
		{
			result.Errors.Add( Lang.ExportValidationMessageNameRequirered );
		}
		else if ( contact.Name.Length > 100 )
		{
			result.Errors.Add( Lang.ExportValidationMessageNameLength );
		}

		// ❗ Duplicate checks
		if ( contact.ContactId == 0 )
		{
			if ( await _dataService.NameExistsAsync( contact.Name, cancellationToken ) )
				result.Errors.Add( Lang.ExportValidationContactNameExists );
		}

		return result;
	}
}