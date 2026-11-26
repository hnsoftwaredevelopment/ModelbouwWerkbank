using System;
using System.Collections.Generic;
using System.Text;

namespace Modelbouwer.Validators;

public class WorkTypeValidator : IEntityValidator<WorkTypeModel>
{
	private readonly IWorkTypeService _dataService;

	public WorkTypeValidator( IWorkTypeService dataService ) => _dataService = dataService;

	public Task<ValidationResult> ValidateAsync( WorkTypeModel worktype ) => ValidateAsync( worktype, CancellationToken.None );

	public async Task<ValidationResult> ValidateAsync( WorkTypeModel worktype, CancellationToken cancellationToken )
	{
		var result = new ValidationResult();

		// Name
		if ( string.IsNullOrWhiteSpace( worktype.WorktypeName ) )
		{
			result.Errors.Add( Lang.ExportValidationMessageNameRequirered );
		}
		else if ( worktype.WorktypeName.Length > 100 )
		{
			result.Errors.Add( Lang.ExportValidationMessageNameLength );
		}

		if ( await _dataService.NameExistsAsync( worktype.WorktypeName, worktype.ParentId, cancellationToken ) )
		{
			result.Errors.Add( Lang.ExportValidationWorkTypeNameExists );
		}

		return result;
	}
}