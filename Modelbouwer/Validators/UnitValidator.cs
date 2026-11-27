using System;
using System.Collections.Generic;
using System.Text;

namespace Modelbouwer.Validators;

public class UnitValidator : IEntityValidator<UnitModel>
{
	private readonly IUnitService _dataService;

	public UnitValidator( IUnitService dataService ) => _dataService = dataService;

	public Task<ValidationResult> ValidateAsync( UnitModel unit ) => ValidateAsync( unit, CancellationToken.None );

	public async Task<ValidationResult> ValidateAsync( UnitModel unit, CancellationToken cancellationToken )
	{
		var result = new ValidationResult();

		// Name
		if ( string.IsNullOrWhiteSpace( unit.UnitName ) )
		{
			result.Errors.Add( Lang.ExportValidationMessageNameRequirered );
		}
		else if ( unit.UnitName.Length > 100 )
		{
			result.Errors.Add( Lang.ExportValidationMessageNameLength );
		}

		// ❗ Duplicate checks
		if ( unit.UnitId == 0 )
		{
			if ( await _dataService.NameExistsAsync( unit.UnitName, cancellationToken ) )
				result.Errors.Add( Lang.ExportValidationUnitNameExists );
		}

		return result;
	}
}