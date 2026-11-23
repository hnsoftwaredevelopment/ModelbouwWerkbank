using Syncfusion.UI.Xaml.Grid;

namespace Modelbouwer.Views;

public partial class StockReceiptView : UserControl
{
	public StockReceiptView( StockReceiptViewModel viewModel )
	{
		InitializeComponent();
		DataContext = viewModel;
	}

	/// <summary>
	/// A right click selects the line under the mouse first, so the context menu acts on that line.
	/// </summary>
	private void OrderLinesGrid_GridContextMenuOpening( object? sender, GridContextMenuEventArgs e )
	{
		if ( e.ContextMenuInfo is GridRecordContextMenuInfo { Record: StockOrderLineModel line }
			&& DataContext is StockReceiptViewModel vm )
		{
			vm.SelectedOrderLine = line;
		}
	}
}
