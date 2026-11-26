namespace Modelbouwer.Validators;

public class SupplierValidator : IEntityValidator<SupplierModel>
{
	private readonly ISupplierService _dataService;

	public SupplierValidator( ISupplierService dataService ) => _dataService = dataService;

	public Task<ValidationResult> ValidateAsync( SupplierModel supplier ) => ValidateAsync( supplier, CancellationToken.None );

	public async Task<ValidationResult> ValidateAsync( SupplierModel supplier, CancellationToken cancellationToken )
	{
		var result = new ValidationResult();

		// Name
		if ( string.IsNullOrWhiteSpace( supplier.Name ) )
		{
			result.Errors.Add( Lang.ExportValidationMessageNameRequirered );
		}
		else if ( supplier.Name.Length > 100 )
		{
			result.Errors.Add( Lang.ExportValidationMessageNameLength );
		}

		// ❗ Duplicate checks
		if ( supplier.Id == 0 )
		{
			if ( await _dataService.NameExistsAsync( supplier.Name, cancellationToken ) )
				result.Errors.Add( Lang.ExportValidationSupplierNameExists );
		}

		return result;
	}
}