using System.Collections;

using Syncfusion.UI.Xaml.Grid;
using Syncfusion.UI.Xaml.TreeGrid;
using Syncfusion.XlsIO;

namespace Modelbouwer.Services;

public class ExcelExportService : IExportService
{
	private readonly ILanguageProvider? _languageProvider;

	public ExcelExportService( ILanguageProvider? languageProvider = null )
	{
		_languageProvider = languageProvider;
	}

	#region CSV Methods (noop)

	public Task ExportToCsvAsync<T>( SfDataGrid dataGrid, string filePath, Dictionary<string, string>? columnHeaderOverrides = null, Func<T, GridColumn, string>? customValueFormatter = null )
	{
		// Not supported for Excel service
		return Task.CompletedTask;
	}

	public Task ExportToCsvAsync<T>( SfTreeGrid treeGrid, string filePath, Dictionary<string, string>? columnHeaderOverrides = null, Func<T, TreeGridColumn, string>? customValueFormatter = null ) =>
		// Not supported for Excel service
		Task.CompletedTask;

	#endregion

	#region Excel Methods

	public async Task ExportToExcelAsync<T>( SfDataGrid dataGrid, string filePath, Dictionary<string, string>? columnHeaderOverrides = null, Func<T, GridColumn, string>? customValueFormatter = null )
	{
		if ( dataGrid == null )
			return;

		int exported = 0;
		await dataGrid.Dispatcher.InvokeAsync( () =>
		{
			var exportData = PrepareDataGridExportData<T>(dataGrid, columnHeaderOverrides);
			exported = WriteExcelFile( exportData, filePath, customValueFormatter );
		} );

		// Shown after the dispatcher call: waiting for a message box on the dispatcher from inside it blocked the app.
		await ShowSuccessMessageAsync( filePath, exported );
	}

	public async Task ExportToExcelAsync<T>( SfTreeGrid treeGrid, string filePath, Dictionary<string, string>? columnHeaderOverrides = null, Func<T, TreeGridColumn, string>? customValueFormatter = null )
	{
		if ( treeGrid == null )
			return;

		int exported = 0;
		await treeGrid.Dispatcher.InvokeAsync( () =>
		{
			var exportData = PrepareTreeGridExportData<T>(treeGrid, columnHeaderOverrides);
			exported = WriteExcelFile( exportData, filePath, customValueFormatter );
		} );

		// Shown after the dispatcher call: waiting for a message box on the dispatcher from inside it blocked the app.
		await ShowSuccessMessageAsync( filePath, exported );
	}


	private object? GetPropertyValue<T>( T item, string propertyName ) => item?.GetType().GetProperty( propertyName )?.GetValue( item );

	#endregion

	#region Internal Helpers

	private ExportData<T> PrepareDataGridExportData<T>( SfDataGrid grid, Dictionary<string, string>? columnHeaderOverrides )
	{
		var exportData = new ExportData<T>();

		foreach ( var column in grid.Columns )
		{
			if ( !column.IsHidden && column.MappingName != null )
			{
				exportData.ColumnInfos.Add( new ColumnInfo
				{
					MappingName = column.MappingName,
					HeaderText = GetColumnHeader( column, columnHeaderOverrides ),
					Column = column
				} );

				exportData.Headers.Add( GetColumnHeader( column, columnHeaderOverrides ) );
			}
		}

		foreach ( var record in grid.View.Records )
		{
			if ( record.Data is T item )
				exportData.Items.Add( item );
		}

		return exportData;
	}

	private ExportData<T> PrepareTreeGridExportData<T>( SfTreeGrid treeGrid, Dictionary<string, string>? columnHeaderOverrides )
	{
		var exportData = new ExportData<T>();

		foreach ( var column in treeGrid.Columns )
		{
			if ( !column.IsHidden && column.MappingName != null )
			{
				exportData.ColumnInfos.Add( new ColumnInfo
				{
					MappingName = column.MappingName,
					HeaderText = GetColumnHeader( column, columnHeaderOverrides ),
					Column = column
				} );

				exportData.Headers.Add( GetColumnHeader( column, columnHeaderOverrides ) );
			}
		}

		exportData.HierarchyColumn = !string.IsNullOrEmpty( treeGrid.ExpanderColumn )
			? treeGrid.ExpanderColumn
			: exportData.ColumnInfos.FirstOrDefault()?.MappingName;

		// ✅ Recursively get ALL nodes, including collapsed children
		if ( treeGrid.ItemsSource is IEnumerable rootItems )
		{
			var childPropertyName = treeGrid.ChildPropertyName ?? "Children";

			foreach ( var rootItem in rootItems )
			{
				if ( rootItem is T item )
				{
					AddItemAndChildren( item, exportData, childPropertyName );
				}
			}
		}

		return exportData;
	}

	internal void AddItemAndChildren<T>( T item, ExportData<T> exportData, string childPropertyName, int level = 0 )
	{
		// Add current item, with its depth for the indent and the outline groups in Excel
		exportData.Items.Add( item );
		exportData.Levels.Add( level );

		// Get the children collection using reflection
		var childProperty = typeof(T).GetProperty(childPropertyName);
		if ( childProperty != null )
		{
			var childrenValue = childProperty.GetValue(item);

			if ( childrenValue is IEnumerable children )
			{
				foreach ( var child in children )
				{
					if ( child is T childItem )
					{
						AddItemAndChildren( childItem, exportData, childPropertyName, level + 1 );
					}
				}
			}
		}
	}

	internal int WriteExcelFile<T, TColumn>( ExportData<T> exportData, string filePath, Func<T, TColumn, string>? customValueFormatter )
	{
		using ( ExcelEngine excelEngine = new ExcelEngine() )
		{
			IApplication application = excelEngine.Excel;
			application.DefaultVersion = ExcelVersion.Xlsx;

			IWorkbook workbook = application.Workbooks.Create(1);
			IWorksheet worksheet = workbook.Worksheets[0];

			// Write headers
			for ( int col = 0; col < exportData.Headers.Count; col++ )
			{
				worksheet.Range [ 1, col + 1 ].Text = exportData.Headers [ col ];
			}

			// Write data
			int row = 2;
			foreach ( var item in exportData.Items )
			{
				int col = 1;
				foreach ( var colInfo in exportData.ColumnInfos )
				{
					if ( colInfo.Column != null )
					{
						var value = customValueFormatter != null
						? customValueFormatter(item, (TColumn)colInfo.Column)
						: GetPropertyValue(item, colInfo.MappingName);

						worksheet.Range [ row, col ].Text = value?.ToString() ?? "";
					}
					col++;
				}
				row++;
			}

			ApplyHierarchy( worksheet, exportData );

			// Save the file
			workbook.SaveAs( filePath );
		}

		return exportData.Items.Count;
	}

	/// <summary>Excel shows at most 7 nested row groups.</summary>
	private const int MaxOutlineDepth = 7;

	/// <summary>
	/// Shows a tree in Excel (KAN-148): the tree column indents one step per level, and every branch is a row group,
	/// so it can be collapsed with the buttons 1, 2, 3 on the left. The group button sits at the parent row above its
	/// children. A flat grid (no levels) is left as it is.
	/// </summary>
	internal static void ApplyHierarchy<T>( IWorksheet worksheet, ExportData<T> exportData )
	{
		var levels = exportData.Levels;
		if ( levels.Count == 0 || levels.Count != exportData.Items.Count )
			return;

		const int firstDataRow = 2;
		int column = exportData.ColumnInfos.FindIndex( c => c.MappingName == exportData.HierarchyColumn ) + 1;
		if ( column < 1 )
			column = 1;

		for ( int i = 0; i < levels.Count; i++ )
		{
			if ( levels [ i ] > 0 )
				worksheet.Range [ firstDataRow + i, column ].CellStyle.IndentLevel = Math.Min( levels [ i ], 15 );
		}

		worksheet.PageSetup.IsSummaryRowBelow = false;

		// Each pass groups the runs of rows that lie at least this deep; nested passes give nested groups.
		int deepest = Math.Min( levels.Max(), MaxOutlineDepth );
		for ( int depth = 1; depth <= deepest; depth++ )
		{
			int start = -1;
			for ( int i = 0; i <= levels.Count; i++ )
			{
				bool inside = i < levels.Count && levels [ i ] >= depth;
				if ( inside && start < 0 )
				{
					start = i;
				}
				else if ( !inside && start >= 0 )
				{
					worksheet.Range [ firstDataRow + start, 1, firstDataRow + i - 1, 1 ].Group( ExcelGroupBy.ByRows );
					start = -1;
				}
			}
		}
	}

	private string GetColumnHeader( GridColumn column, Dictionary<string, string>? columnHeaderOverrides ) =>
		ExportHeaders.Resolve( column.MappingName, column.HeaderText, columnHeaderOverrides, _languageProvider );

	private string GetColumnHeader( TreeGridColumn column, Dictionary<string, string>? columnHeaderOverrides ) =>
		ExportHeaders.Resolve( column.MappingName, column.HeaderText, columnHeaderOverrides, _languageProvider );

	private async Task ShowSuccessMessageAsync( string filePath, int recordCount )
	{
		try
		{
			var message = _languageProvider?.GetTranslation("ExportGeneralSuccess")
						  ?.Replace("{count}", recordCount.ToString(), StringComparison.Ordinal)
						  ?.Replace("{file}", Path.GetFileName(filePath), StringComparison.Ordinal)
						  ?? $"Exported {recordCount} records to {Path.GetFileName(filePath)}";

			if ( Application.Current != null )
			{
				await Application.Current.Dispatcher.InvokeAsync( () =>
				{
					MessageBox.Show( message, "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information );
				} );
			}
		}
		catch { }
	}

	#endregion
}