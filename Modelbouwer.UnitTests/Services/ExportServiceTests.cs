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
	public void Csv_HeaderWithSeparatorOrQuoteIsQuoted()
	{
		// Found by CodeRabbit in PR #61: a grid header such as "Code;omschrijving" split into two header fields.
		var data = new ExportData<Row>();
		data.ColumnInfos.Add( new ColumnInfo { MappingName = "Name", Column = new object() } );
		data.Headers.Add( "Code;omschrijving" );
		data.ColumnInfos.Add( new ColumnInfo { MappingName = "Memo", Column = new object() } );
		data.Headers.Add( "12 \"inch\"" );

		var csv = new CsvExportService { IncludeBom = false }.GenerateCsvContent<Row, object>( data, null );

		Assert.AreEqual( "\"Code;omschrijving\";\"12 \"\"inch\"\"\"" + Environment.NewLine, csv );
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

	#region Column headers (KAN-148)
	[TestMethod]
	public void Header_MissingTranslation_UsesTheGridHeader()
	{
		// ResourceLanguageProvider returns the key itself when there is no translation.
		var provider = new ResourceLanguageProvider();

		Assert.AreEqual( "Code", ExportHeaders.Resolve( "CountryCode", "Code", null, provider ), "Not \"ExportHeader_CountryCode\"." );
		Assert.AreEqual( "CountryCode", ExportHeaders.Resolve( "CountryCode", "  ", null, provider ), "Without a grid header the mapping name is left." );
	}

	[TestMethod]
	public void Header_OverrideComesFirstThenTranslation()
	{
		var provider = new Mock<ILanguageProvider>();
		provider.Setup( p => p.GetTranslation( "ExportHeader_Name" ) ).Returns( "Vertaalde naam" );

		Assert.AreEqual( "Eigen kop", ExportHeaders.Resolve( "Name", "Naam", new Dictionary<string, string> { [ "Name" ] = "Eigen kop" }, provider.Object ) );
		Assert.AreEqual( "Vertaalde naam", ExportHeaders.Resolve( "Name", "Naam", null, provider.Object ) );
		Assert.AreEqual( "Naam", ExportHeaders.Resolve( "Name", "Naam", null, null ) );
	}
	#endregion

	#region Excel hierarchy (KAN-148)
	/// <summary>Hout > (Lindehout > Lindehout 2 mm, Balsa), Lijm: levels 0, 1, 2, 1, 0.</summary>
	private static ExportData<Row> Tree( ExcelExportService service )
	{
		var data = Data();
		data.HierarchyColumn = nameof( Row.Name );
		var roots = new [ ]
		{
			new Row { Name = "Hout", Children = [ new Row { Name = "Lindehout", Children = [ new Row { Name = "Lindehout 2 mm" } ] }, new Row { Name = "Balsa" } ] },
			new Row { Name = "Lijm" }
		};
		foreach ( var root in roots )
			service.AddItemAndChildren( root, data, nameof( Row.Children ) );
		return data;
	}

	private static IWorksheet WriteAndOpen( ExportData<Row> data, ExcelEngine engine, string path )
	{
		new ExcelExportService().WriteExcelFile<Row, object>( data, path, null );
		return engine.Excel.Workbooks.Open( path ).Worksheets [ 0 ];
	}

	[TestMethod]
	public void TreeExport_RecordsTheLevelOfEveryRowInTreeOrder()
	{
		var data = Tree( new ExcelExportService() );

		CollectionAssert.AreEqual( new [ ] { "Hout", "Lindehout", "Lindehout 2 mm", "Balsa", "Lijm" }, data.Items.Select( r => r.Name ).ToArray(), "Parent directly followed by its children." );
		CollectionAssert.AreEqual( new [ ] { 0, 1, 2, 1, 0 }, data.Levels );
	}

	[TestMethod]
	public void Excel_Tree_IndentsTheNamePerLevel()
	{
		var path = Path.Combine( Path.GetTempPath(), $"kan148-{Guid.NewGuid():N}.xlsx" );
		try
		{
			using var engine = new ExcelEngine();
			var sheet = WriteAndOpen( Tree( new ExcelExportService() ), engine, path );

			CollectionAssert.AreEqual( new [ ] { 0, 1, 2, 1, 0 }, Enumerable.Range( 2, 5 ).Select( r => sheet.Range [ r, 1 ].CellStyle.IndentLevel ).ToArray() );
			Assert.AreEqual( "Lindehout 2 mm", CellText( sheet, 4, 1 ), "The text itself gets no leading spaces." );
			Assert.AreEqual( 0, sheet.Range [ 3, 2 ].CellStyle.IndentLevel, "Only the tree column is indented." );
		}
		finally
		{
			File.Delete( path );
		}
	}

	[TestMethod]
	public void Excel_Tree_GroupsEveryBranchWithTheButtonAtTheParent()
	{
		var path = Path.Combine( Path.GetTempPath(), $"kan148-{Guid.NewGuid():N}.xlsx" );
		try
		{
			using var engine = new ExcelEngine();
			var sheet = WriteAndOpen( Tree( new ExcelExportService() ), engine, path );

			CollectionAssert.AreEqual( new [ ] { 0, 1, 2, 1, 0 }, Enumerable.Range( 2, 5 ).Select( r => sheet.Range [ r, 1 ].RowGroupLevel ).ToArray(),
				"Each row is grouped as deep as it lies in the tree." );
			Assert.IsFalse( sheet.PageSetup.IsSummaryRowBelow, "The collapse button belongs at the parent row above the children." );
		}
		finally
		{
			File.Delete( path );
		}
	}

	[TestMethod]
	public void Excel_FlatGrid_HasNoIndentOrGroups()
	{
		var path = Path.Combine( Path.GetTempPath(), $"kan148-{Guid.NewGuid():N}.xlsx" );
		try
		{
			using var engine = new ExcelEngine();
			var sheet = WriteAndOpen( Data( new Row { Name = "Lindehout" }, new Row { Name = "Lijm" } ), engine, path );

			Assert.AreEqual( 0, sheet.Range [ 3, 1 ].CellStyle.IndentLevel );
			Assert.AreEqual( 0, sheet.Range [ 3, 1 ].RowGroupLevel );
		}
		finally
		{
			File.Delete( path );
		}
	}

	[TestMethod]
	public void Excel_TreeDeeperThanExcelAllows_GroupsUpToSevenLevels()
	{
		var data = Data();
		var deep = new Row { Name = "Niveau 9" };
		for ( int level = 8; level >= 0; level-- )
			deep = new Row { Name = $"Niveau {level}", Children = [ deep ] };
		new ExcelExportService().AddItemAndChildren( deep, data, nameof( Row.Children ) );

		var path = Path.Combine( Path.GetTempPath(), $"kan148-{Guid.NewGuid():N}.xlsx" );
		try
		{
			using var engine = new ExcelEngine();
			var sheet = WriteAndOpen( data, engine, path );

			Assert.AreEqual( 9, sheet.Range [ 11, 1 ].CellStyle.IndentLevel, "The indent still follows the tree." );
			Assert.AreEqual( 7, sheet.Range [ 11, 1 ].RowGroupLevel, "Excel nests row groups at most 7 deep." );
		}
		finally
		{
			File.Delete( path );
		}
	}

	[TestMethod]
	public void Csv_TreeExport_IsUnchanged()
	{
		var data = Tree( new ExcelExportService() );
		var csvData = Data();
		foreach ( var item in data.Items.Where( r => r.Name is "Hout" or "Lijm" ) )
			new CsvExportService().AddItemAndChildren( item, csvData, nameof( Row.Children ) );

		var csv = new CsvExportService { IncludeBom = false, IncludeHeaders = false }.GenerateCsvContent<Row, object>( csvData, null );

		StringAssert.StartsWith( csv, "Hout;0;" + Environment.NewLine + "Lindehout;0;", "No indent or level in the CSV." );
	}
	#endregion

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
