namespace Modelbouwer.Validators;

public class StockManagementValidator : IEntityValidator<StockManagementModel>
{
	// No validation is required for stock management yet; this is where future rules go.
	public Task<ValidationResult> ValidateAsync( StockManagementModel stockmanagement ) => ValidateAsync( stockmanagement, CancellationToken.None );

	public Task<ValidationResult> ValidateAsync( StockManagementModel stockmanagement, CancellationToken cancellationToken ) => Task.FromResult( new ValidationResult() );
}
