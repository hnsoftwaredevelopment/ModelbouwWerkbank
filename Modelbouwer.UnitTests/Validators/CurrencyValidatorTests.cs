using Modelbouwer.Validators;

namespace Modelbouwer.UnitTests.Validators;

/// <summary>Found in the KAN-88 test round: a new currency was always refused because the check looked at its id.</summary>
[TestClass]
public class CurrencyValidatorTests
{
	private static CurrencyValidator Create( bool exists = false )
	{
		var service = new Mock<ICurrencyService>();
		service.Setup( s => s.CodeExistsAsync( It.IsAny<string?>(), It.IsAny<CancellationToken>() ) ).ReturnsAsync( exists );
		service.Setup( s => s.NameExistsAsync( It.IsAny<string?>(), It.IsAny<CancellationToken>() ) ).ReturnsAsync( exists );
		return new CurrencyValidator( service.Object );
	}

	[TestMethod]
	public async Task NewCurrencyWithCodeNameAndSymbol_IsValid()
	{
		var result = await Create().ValidateAsync( new CurrencyModel { CurrencyId = 0, CurrencyCode = "TST", CurrencyName = "Test", CurrencySymbol = "T", CurrencyConversionRate = 2 } );

		Assert.IsTrue( result.IsValid, string.Join( ", ", result.Errors ) );
	}

	[TestMethod]
	public async Task MissingSymbol_IsRefused()
	{
		var result = await Create().ValidateAsync( new CurrencyModel { CurrencyCode = "TST", CurrencyName = "Test", CurrencySymbol = " " } );

		CollectionAssert.Contains( result.Errors, Lang.ExportValidationMessageCurrencyRequired );
	}

	[TestMethod]
	public async Task NewCurrencyWithExistingCode_IsRefused()
	{
		var result = await Create( exists: true ).ValidateAsync( new CurrencyModel { CurrencyCode = "EUR", CurrencyName = "Euro", CurrencySymbol = "€" } );

		CollectionAssert.Contains( result.Errors, Lang.ExportValidationCurrencyCodeExists );
	}
}
