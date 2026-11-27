using System.Collections;

using Syncfusion.UI.Xaml.Grid;
using Syncfusion.UI.Xaml.TreeGrid;

using sfGridColumn = Syncfusion.UI.Xaml.Grid.GridColumn;

namespace Modelbouwer.Services;

public class CsvExportService : IExportService
{
	public string Separator { get; set; } = ";";
	public Encoding Encoding { get; set; } = Encoding.UTF8;
	public bool IncludeBom { get; set; } = true;
	public bool IncludeHeaders { get; set; } = true;

	private readonly ILanguageProvider? _languageProvider;

	public CsvExportService( ILanguageProvider? languageProvider = null )
	{
		_languageProvider = languageProvider;
	}

	#region Public Export Methods

	public Task ExportToCsvAsync<T>(
		SfDataGrid dataGrid,
		string filePath,
		Dictionary<string, string>? columnHeaderOverrides = null,
		Func<T, sfGridColumn, string>? customValueFormatter = null )
	{
		return ExportSfDataGridAsync( dataGrid, filePath, columnHeaderOverrides, customValueFormatter );
	}

	public Task ExportToCsvAsync<T>(
		SfTreeGrid treeGrid,
		string filePath,
		Dictionary<string, string>? columnHeaderOverrides = null,
		Func<T, TreeGridColumn, string>? customValueFormatter = null ) => ExportSfTreeGridAsync( treeGrid, filePath, columnHeaderOverrides, customValueFormatter );

	#endregion

	#region Internal Export Implementations

	private async Task ExportSfDataGridAsync<T>(
		SfDataGrid dataGrid,
		string filePath,
		Dictionary<string, string>? columnHeaderOverrides,
		Func<T, sfGridColumn, string>? customValueFormatter )
	{
		ExportData<T>? exportData = null;

		await dataGrid.Dispatcher.InvokeAsync( () =>
		{
			exportData = PrepareDataGridExportData<T>( dataGrid, columnHeaderOverrides );
		} );

		if ( exportData == null )
			return;

		await Task.Run( () =>
		{
			var csv = GenerateCsvContent<T, sfGridColumn>(exportData, customValueFormatter);
			File.WriteAllText( filePath, csv, Encoding );
		} );

		await ShowSuccessMessageAsync( filePath, exportData.Items.Count );
	}

	private async Task ExportSfTreeGridAsync<T>(
		SfTreeGrid treeGrid,
		string filePath,
		Dictionary<string, string>? columnHeaderOverrides,
		Func<T, TreeGridColumn, string>? customValueFormatter )
	{
		ExportData<T>? exportData = null;

		await treeGrid.Dispatcher.InvokeAsync( () =>
		{
			exportData = PrepareTreeGridExportData<T>( treeGrid, columnHeaderOverrides );
		} );

		if ( exportData == null )
			return;

		await Task.Run( () =>
		{
			var csv = GenerateCsvContent<T, TreeGridColumn>(exportData, customValueFormatter);
			File.WriteAllText( filePath, csv, Encoding );
		} );

		await ShowSuccessMessageAsync( filePath, exportData.Items.Count );
	}

	#endregion

	#region Prepare Export Data
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

	private ExportData<T> PrepareTreeGridExportData<T>( SfTreeGrid grid, Dictionary<string, string>? columnHeaderOverrides )
	{
		var exportData = new ExportData<T>();

		foreach ( var column in grid.Columns )
		{
			if ( column.MappingName != null )
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

		// ✅ Get data directly from ItemsSource, not from View.Nodes
		if ( grid.ItemsSource is IEnumerable rootItems )
		{
			var childPropertyName = grid.ChildPropertyName ?? "Children";

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

	internal void AddItemAndChildren<T>( T item, ExportData<T> exportData, string childPropertyName )
	{
		// Add current item
		exportData.Items.Add( item );

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
						AddItemAndChildren( childItem, exportData, childPropertyName );
					}
				}
			}
		}
	}
	#endregion

	#region CSV Generation

	internal string GenerateCsvContent<T, TColumn>(
		ExportData<T> exportData,
		Func<T, TColumn, string>? customValueFormatter )
		where TColumn : class
	{
		var csvBuilder = new StringBuilder();

		if ( IncludeBom )
			csvBuilder.Append( '\uFEFF' );

		if ( IncludeHeaders )
			csvBuilder.AppendLine( string.Join( Separator, exportData.Headers.Select( FormatValueForCsv ) ) );

		foreach ( var item in exportData.Items )
		{
			var rowValues = new List<string>();

			foreach ( var columnInfo in exportData.ColumnInfos )
			{
				string value;
				try
				{
					if ( customValueFormatter != null && columnInfo.Column is TColumn col )
						value = customValueFormatter( item, col );
					else
						value = GetCellValueSafely( item, columnInfo.MappingName );
				}
				catch
				{
					value = GetCellValueSafely( item, columnInfo.MappingName );
				}

				rowValues.Add( FormatValueForCsv( value ) );
			}

			csvBuilder.AppendLine( string.Join( Separator, rowValues ) );
		}

		return csvBuilder.ToString();
	}

	private string GetCellValueSafely<T>( T item, string mappingName )
	{
		try
		{
			var prop = typeof(T).GetProperty(mappingName);
			if ( prop != null )
			{
				var val = prop.GetValue(item);
				return val?.ToString() ?? string.Empty;
			}
		}
		catch { }

		return string.Empty;
	}

	#endregion

	#region Column Headers

	private string GetColumnHeader( sfGridColumn column, Dictionary<string, string>? columnHeaderOverrides ) =>
		ExportHeaders.Resolve( column.MappingName, column.HeaderText, columnHeaderOverrides, _languageProvider );

	private string GetColumnHeader( TreeGridColumn column, Dictionary<string, string>? columnHeaderOverrides ) =>
		ExportHeaders.Resolve( column.MappingName, column.HeaderText, columnHeaderOverrides, _languageProvider );

	#endregion

	#region Helpers

	internal string FormatValueForCsv( string value )
	{
		if ( string.IsNullOrEmpty( value ) )
			return string.Empty;

		if ( value.Contains( Separator, StringComparison.Ordinal ) || value.Contains( '"', StringComparison.Ordinal ) || value.Contains( '\n', StringComparison.Ordinal ) || value.Contains( '\r', StringComparison.Ordinal ) )
			return $"\"{value.Replace( "\"", "\"\"", StringComparison.Ordinal )}\"";

		return value;
	}

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