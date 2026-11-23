using System.Windows.Controls;

namespace Modelbouwer.Views;

public partial class StockReceiptDialog : Window
{
	public StockReceiptDialog( StockReceiptDialogViewModel viewModel )
	{
		InitializeComponent();
		DataContext = viewModel;
	}

	private void ReceiveAmountBox_GotKeyboardFocus( object sender, KeyboardFocusChangedEventArgs e )
	{
		if ( sender is TextBox textBox )
			textBox.SelectAll();
	}

	private void SaveClick( object sender, RoutedEventArgs e )
	{
		if ( DataContext is not StockReceiptDialogViewModel vm )
			return;

		var validationMessage = vm.Validate();
		if ( validationMessage != null )
		{
			MessageBox.Show( validationMessage, Lang.generalMessageboxWarningTitle, MessageBoxButton.OK, MessageBoxImage.Warning );
			return;
		}

		DialogResult = true;
	}
}
