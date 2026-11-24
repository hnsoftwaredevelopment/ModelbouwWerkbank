namespace Modelbouwer.ViewModels;

/// <summary>
/// Edits the supplier specific data of a product: article number, description and unit price.
/// Shared by the receipt screen (KAN-102) and the order screen (KAN-103).
/// </summary>
public class SupplierProductDialogViewModel
{
	public StockOrderProductDialogModel Model { get; }

	public SupplierProductDialogViewModel( StockOrderProductDialogModel model )
	{
		Model = model;
	}

	public bool TryConfirm( out string? errorMessage )
	{
		if ( string.IsNullOrWhiteSpace( Model.SupplierProductName ) )
		{
			errorMessage = Lang.StockOrderProductDialogSupplierProductNameRequired;
			return false;
		}

		if ( Model.UnitPrice <= 0 )
		{
			errorMessage = Lang.StockOrderProductDialogPriceRequired;
			return false;
		}

		errorMessage = null;
		return true;
	}
}
