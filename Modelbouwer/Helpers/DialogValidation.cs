namespace Modelbouwer.Helpers;

/// <summary>
/// Input checks for dialogs. A TextBox with text that cannot be converted keeps the last valid value in the
/// model, so a dialog must refuse to confirm while any binding reports a validation error.
/// </summary>
public static class DialogValidation
{
	public static bool HasValidationErrors( DependencyObject root )
	{
		if ( System.Windows.Controls.Validation.GetHasError( root ) )
			return true;

		int count = VisualTreeHelper.GetChildrenCount( root );
		for ( int i = 0; i < count; i++ )
		{
			if ( HasValidationErrors( VisualTreeHelper.GetChild( root, i ) ) )
				return true;
		}

		return false;
	}

	/// <summary>Shows a warning and returns true when the dialog contains input that is not a valid number.</summary>
	public static bool BlockOnInvalidInput( Window dialog )
	{
		if ( !HasValidationErrors( dialog ) )
			return false;

		MessageBox.Show( Lang.DialogInvalidNumber, Lang.generalMessageboxWarningTitle, MessageBoxButton.OK, MessageBoxImage.Warning );
		return true;
	}
}
