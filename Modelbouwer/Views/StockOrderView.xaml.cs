using Syncfusion.UI.Xaml.Grid;
using Syncfusion.UI.Xaml.Grid.Helpers;

namespace Modelbouwer.Views;

public partial class StockOrderView : UserControl
{
	public StockOrderView( StockOrderViewModel viewModel )
	{
		InitializeComponent();
		ApplyCurrentCultureFormatting();
		DataContext = viewModel;
	}

	private void ApplyCurrentCultureFormatting()
	{
		var culture = CultureInfo.CurrentCulture;
		Language = System.Windows.Markup.XmlLanguage.GetLanguage( culture.IetfLanguageTag );

		foreach ( var column in OrderLinesGrid.Columns.OfType<GridNumericColumn>()
			.Concat( ProductsGrid.Columns.OfType<GridNumericColumn>() ) )
		{
			column.NumberDecimalSeparator = culture.NumberFormat.NumberDecimalSeparator;
			column.NumberGroupSeparator = culture.NumberFormat.NumberGroupSeparator;
			column.NumberGroupSizes = new System.Windows.Media.Int32Collection( culture.NumberFormat.NumberGroupSizes );
		}
	}

	/// <summary>Fully received lines can not be changed anymore.</summary>
	private void OrderLinesGrid_CurrentCellBeginEdit( object? sender, CurrentCellBeginEditEventArgs e )
	{
		var record = OrderLinesGrid.GetRecordAtRowIndex( e.RowColumnIndex.RowIndex );
		// Closed lines stay read only, and nothing is edited while the order is being saved or deleted.
		if ( record is StockOrderLineModel { Closed: true } || DataContext is StockOrderViewModel { CanEditOrder: false } )
			e.Cancel = true;
	}

	/// <summary>A right click selects the row under the mouse first, so the context menu acts on that row.</summary>
	private void ProductsGrid_GridContextMenuOpening( object? sender, GridContextMenuEventArgs e )
	{
		if ( e.ContextMenuInfo is GridRecordContextMenuInfo { Record: StockOrderProductRowModel row } && DataContext is StockOrderViewModel vm )
			vm.SelectedProductRow = row;
	}

	private void OrderLinesGrid_GridContextMenuOpening( object? sender, GridContextMenuEventArgs e )
	{
		if ( e.ContextMenuInfo is GridRecordContextMenuInfo { Record: StockOrderLineModel line } && DataContext is StockOrderViewModel vm )
			vm.SelectedOrderLine = line;
	}
}
