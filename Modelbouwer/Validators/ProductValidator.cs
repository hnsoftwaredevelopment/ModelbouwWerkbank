namespace Modelbouwer.Validators;

public class ProductValidator : IEntityValidator<ProductModel>
{
	private readonly IProductService _dataService;

	public ProductValidator( IProductService dataService ) => _dataService = dataService;

	public Task<ValidationResult> ValidateAsync( ProductModel product ) => ValidateAsync( product, CancellationToken.None );

	public async Task<ValidationResult> ValidateAsync( ProductModel product, CancellationToken cancellationToken )
	{
		var result = new ValidationResult();

		// Name
		if ( string.IsNullOrWhiteSpace( product.Name ) )
		{
			result.Errors.Add( Lang.ExportValidationMessageNameRequirered );
		}
		else if ( product.Name.Length > 100 )
		{
			result.Errors.Add( Lang.ExportValidationMessageNameLength );
		}

		// ❗ Duplicate checks
		if ( product.ProductId == 0 )
		{
			if ( await _dataService.NameExistsAsync( product.Name, cancellationToken ) )
				result.Errors.Add( Lang.ExportValidationProductNameExists );
		}

		return result;
	}
}