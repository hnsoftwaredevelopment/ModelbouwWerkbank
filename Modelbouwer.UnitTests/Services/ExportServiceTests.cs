using Syncfusion.XlsIO;

namespace Modelbouwer.UnitTests.Services;

/// <summary>
/// KAN-88 (CB-16, CB-17): CSV and Excel export. The grid-independent parts are tested directly: the CSV text and the
/// workbook built from the columns and rows the grid would deliver.
/// </summary>
[TestClass]
public class ExportServiceTests
{
	public sealed class Row
	{
		public string? Name { get; set; }
		public double Price { get; set; }
		public string? Memo { get; set; }
		public List<Row> Children { get; set; } = [ ];
	}

	private static ExportData<Row> Data( params Row [ ] rows )
	{
		var data = new ExportData<Row>();
		foreach ( var (mapping, header) in new [ ] { ("Name", "Naam"), ("Price", "Prijs"), ("Memo", "Opmerking") } )
		{
			data.ColumnInfos.Add( new ColumnInfo { MappingName = mapping, HeaderText = header, Column = new object() } );
			data.Headers.Add( header );
		}
		data.Items.AddRange( rows );
		return data;
	}

	#region CSV (CB-16)
	[TestMethod]
	public void Csv_WritesBomHeadersAndRowsWithSeparator()
	{
		var service = new CsvExportService();

		var csv = service.GenerateCsvContent<Row, object>( Data( new Row { Name = "Lindehout", Price = 2.5, Memo = null } ), null );

		Assert.IsTrue( csv.StartsWith( '﻿' ), "Excel recognises UTF-8 by the BOM." );
		var lines = csv.TrimStart( '﻿' ).Split( Environment.NewLine, StringSplitOptions.RemoveEmptyEntries );
		Assert.AreEqual( "Naam;Prijs;Opmerking", lines [ 0 ] );
		Assert.AreEqual( $"Lindehout;{2.5};", lines [ 1 ], "An empty value stays an empty field." );
	}

	[TestMethod]
	public void Csv_QuotesValuesWithSeparatorQuoteOrLineBreak()
	{
		var service = new CsvExportService();

		Assert.AreEqual( "\"a;b\"", service.FormatValueForCsv( "a;b" ) );
		Assert.AreEqual( "\"12 \"\"inch\"\"\"", service.FormatValueForCsv( "12 \"inch\"" ) );
		Assert.AreEqual( "\"regel 1\nregel 2\"", service.FormatValueForCsv( "regel 1\nregel 2" ) );
		Assert.AreEqual( "gewoon", service.FormatValueForCsv( "gewoon" ) );
		Assert.AreEqual( string.Empty, service.FormatValueForCsv( string.Empty ) );
	}

	[TestMethod]
	public void Csv_EmptyDataSetGivesHeaderOnly()
	{
		var service = new CsvExportService { IncludeBom = false };

		var csv = service.GenerateCsvContent<Row, object>( Data(), null );

		Assert.AreEqual( "Naam;Prijs;Opmerking" + Environment.NewLine, csv );
	}

	[TestMethod]
	public void Csv_SettingsForSeparatorHeadersAndBomAreApplied()
	{
		var service = new CsvExportService { Separator = ",", IncludeHeaders = false, IncludeBom = false };

		var csv = service.GenerateCsvContent<Row, object>( Data( new Row { Name = "Lijm, dun", Price = 4 } ), null );

		Assert.AreEqual( "\"Lijm, dun\",4," + Environment.NewLine, csv, "The value with the separator is quoted." );
	}

	[TestMethod]
	public void Csv_CustomFormatterIsUsedPerColumn()
	{
		var service = new CsvExportService { IncludeBom = false, IncludeHeaders = false };

		var csv = service.GenerateCsvContent<Row, object>( Data( new Row { Name = "Hout", Price = 1 } ), ( row, _ ) => "x" );

		Assert.AreEqual( "x;x;x" + Environment.NewLine, csv );
	}

	[TestMethod]
	public void TreeExport_IncludesCollapsedChildren()
	{
		var root = new Row { Name = "Hout", Children = [ new Row { Name = "Lindehout", Children = [ new Row { Name = "Lindehout 2 mm" } ] } ] };
		var data = new ExportData<Row>();

		new CsvExportService().AddItemAndChildren( root, data, nameof( Row.Children ) );

		CollectionAssert.AreEqual( new [ ] { "Hout", "Lindehout", "Lindehout 2 mm" }, data.Items.Select( r => r.Name ).ToArray() );
	}
	#endregion

	#region Excel (CB-17)
	/// <summary>Without a license Syncfusion adds a notice to the workbook, as in the application the key is registered first.</summary>
	private static bool _licensed;

	[ClassInitialize]
	public static void RegisterSyncfusionLicense( TestContext _ )
	{
		var key = SyncfusionLicenseKeyResolver.ResolveFromEnvironmentAndLocalFiles();
		_licensed = !string.IsNullOrWhiteSpace( key );
		if ( _licensed )
			Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense( key );
	}

	private static string CellText( IWorksheet sheet, int row, int column ) => sheet.Range [ row, column ].Text ?? string.Empty;

	[TestMethod]
	public void Excel_WritesHeadersAndValues()
	{
		var path = Path.Combine( Path.GetTempPath(), $"kan88-{Guid.NewGuid():N}.xlsx" );
		try
		{
			new ExcelExportService().WriteExcelFile<Row, object>( Data( new Row { Name = "Lindehout", Price = 2.5, Memo = null }, new Row { Name = "Lijm", Price = 4 } ), path, null );

			using var engine = new ExcelEngine();
			var sheet = engine.Excel.Workbooks.Open( path ).Worksheets [ 0 ];
			Assert.AreEqual( "Naam", CellText( sheet, 1, 1 ) );
			Assert.AreEqual( "Opmerking", CellText( sheet, 1, 3 ) );
			Assert.AreEqual( "Lindehout", CellText( sheet, 2, 1 ) );
			Assert.AreEqual( 2.5.ToString(), CellText( sheet, 2, 2 ) );
			Assert.AreEqual( string.Empty, CellText( sheet, 2, 3 ), "An empty value gives an empty cell." );
			Assert.AreEqual( "Lijm", CellText( sheet, 3, 1 ) );
		}
		finally
		{
			File.Delete( path );
		}
	}

	[TestMethod]
	public void Excel_EmptyDataSetWritesHeaderRowOnly()
	{
		var path = Path.Combine( Path.GetTempPath(), $"kan88-{Guid.NewGuid():N}.xlsx" );
		try
		{
			new ExcelExportService().WriteExcelFile<Row, object>( Data(), path, null );

			using var engine = new ExcelEngine();
			var sheet = engine.Excel.Workbooks.Open( path ).Worksheets [ 0 ];
			Assert.AreEqual( "Prijs", CellText( sheet, 1, 2 ) );
			if ( !_licensed )
				Assert.Inconclusive( "No Syncfusion license on this machine: the trial notice fills the second row." );
			Assert.AreEqual( string.Empty, CellText( sheet, 2, 1 ) );
		}
		finally
		{
			File.Delete( path );
		}
	}
	#endregion
}
