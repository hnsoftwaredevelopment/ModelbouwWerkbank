using System;
using System.Collections.Generic;
using System.Text;

using Modelbouwer.Interfaces;

namespace Modelbouwer.Validators;

public class CurrencyValidator : IEntityValidator<CurrencyModel>
{
	private readonly ICurrencyService _dataService;

	public CurrencyValidator( ICurrencyService dataService ) => _dataService = dataService;

	public Task<ValidationResult> ValidateAsync( CurrencyModel currency ) => ValidateAsync( currency, CancellationToken.None );

	public async Task<ValidationResult> ValidateAsync( CurrencyModel currency, CancellationToken cancellationToken )
	{
		var result = new ValidationResult();

		// Code
		if ( string.IsNullOrWhiteSpace( currency.CurrencyCode ) )
		{
			result.Errors.Add( Lang.ExportValidationMessageCodeRequirered );
		}
		else if ( currency.CurrencyCode.Length > 10 )
		{
			result.Errors.Add( Lang.ExportValidationMessageCodeLength );
		}

		// Name
		if ( string.IsNullOrWhiteSpace( currency.CurrencyName ) )
		{
			result.Errors.Add( Lang.ExportValidationMessageNameRequirered );
		}
		else if ( currency.CurrencyName.Length > 100 )
		{
			result.Errors.Add( Lang.ExportValidationMessageNameLength );
		}

		// Symbol (the old check looked at CurrencyId, copied from the country validator, so a new currency never passed)
		if ( string.IsNullOrWhiteSpace( currency.CurrencySymbol ) )
			result.Errors.Add( Lang.ExportValidationMessageCurrencyRequired );

		// ❗ Duplicate checks
		if ( currency.CurrencyId == 0 )
		{
			if ( await _dataService.CodeExistsAsync( currency.CurrencyCode, cancellationToken ) )
				result.Errors.Add( Lang.ExportValidationCurrencyCodeExists );

			if ( await _dataService.NameExistsAsync( currency.CurrencyName, cancellationToken ) )
				result.Errors.Add( Lang.ExportValidationCurrencyNameExists );
		}

		return result;
	}
}