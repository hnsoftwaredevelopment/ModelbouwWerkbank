using System;
using System.Collections.Generic;
using System.Text;

namespace Modelbouwer.Validators
{
	public class ContactTypeValidator : IEntityValidator<ContactTypeModel>
	{
		private readonly IContactTypeService _dataService;

		public ContactTypeValidator( IContactTypeService dataService ) => _dataService = dataService;

		public Task<ValidationResult> ValidateAsync( ContactTypeModel contacttype ) => ValidateAsync( contacttype, CancellationToken.None );

		public async Task<ValidationResult> ValidateAsync( ContactTypeModel contacttype, CancellationToken cancellationToken )
		{
			var result = new ValidationResult();

			// Name
			if ( string.IsNullOrWhiteSpace( contacttype.ContactTypeName ) )
			{
				result.Errors.Add( Lang.ExportValidationMessageNameRequirered );
			}
			else if ( contacttype.ContactTypeName.Length > 100 )
			{
				result.Errors.Add( Lang.ExportValidationMessageNameLength );
			}

			// ❗ Duplicate checks
			if ( contacttype.ContactTypeId == 0 )
			{
				if ( await _dataService.NameExistsAsync( contacttype.ContactTypeName, cancellationToken ) )
					result.Errors.Add( Lang.ExportValidationContactTypeNameExists );
			}

			return result;
		}
	}
}