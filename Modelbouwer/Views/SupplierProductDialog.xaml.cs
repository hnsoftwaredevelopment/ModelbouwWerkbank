namespace Modelbouwer.Views;

public partial class SupplierProductDialog : Window
{
	public SupplierProductDialog( SupplierProductDialogViewModel viewModel )
	{
		InitializeComponent();
		DataContext = viewModel;
	}

	private void ConfirmClick( object sender, RoutedEventArgs e )
	{
		if ( DataContext is not SupplierProductDialogViewModel vm )
			return;

		if ( !vm.TryConfirm( out var errorMessage ) )
		{
			MessageBox.Show( errorMessage, Lang.generalMessageboxWarningTitle, MessageBoxButton.OK, MessageBoxImage.Warning );
			return;
		}

		DialogResult = true;
	}
}
