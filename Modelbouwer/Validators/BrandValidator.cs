using System;
using System.Collections.Generic;
using System.Text;

namespace Modelbouwer.Validators;

public class BrandValidator : IEntityValidator<BrandModel>
{
	private readonly IBrandService _dataService;

	public BrandValidator( IBrandService dataService ) => _dataService = dataService;

	public Task<ValidationResult> ValidateAsync( BrandModel brand ) => ValidateAsync( brand, CancellationToken.None );

	public async Task<ValidationResult> ValidateAsync( BrandModel brand, CancellationToken cancellationToken )
	{
		var result = new ValidationResult();

		// Name
		if ( string.IsNullOrWhiteSpace( brand.BrandName ) )
		{
			result.Errors.Add( Lang.ExportValidationMessageNameRequirered );
		}
		else if ( brand.BrandName.Length > 100 )
		{
			result.Errors.Add( Lang.ExportValidationMessageNameLength );
		}

		// ❗ Duplicate checks
		if ( brand.BrandId == 0 )
		{
			if ( await _dataService.NameExistsAsync( brand.BrandName, cancellationToken ) )
				result.Errors.Add( Lang.ExportValidationBrandNameExists );
		}

		return result;
	}
}