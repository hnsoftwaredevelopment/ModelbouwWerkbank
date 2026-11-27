using System.ComponentModel;
using System.Text.RegularExpressions;

namespace Modelbouwer.UnitTests.Views;

/// <summary>
/// Found in the KAN-88 test round: the Brand, Unit and Currency screens were copied from the Country screen and their
/// toolbar still bound to AddCountryCommand and SaveCountryCommand, so the buttons did nothing. A wrong binding is not
/// a build error, so this test checks every command binding of a view against its view model.
/// </summary>
[TestClass]
public class ViewCommandBindingTests
{
	public static IEnumerable<object [ ]> PageViews() =>
	[
		[ "BrandView", typeof( BrandPageViewModel ) ], [ "UnitView", typeof( UnitPageViewModel ) ], [ "CurrencyView", typeof( CurrencyPageViewModel ) ],
		[ "CountryView", typeof( CountryPageViewModel ) ], [ "ContactTypeView", typeof( ContactTypePageViewModel ) ], [ "CategoryView", typeof( CategoryPageViewModel ) ],
		[ "StorageLocationView", typeof( StorageLocationPageViewModel ) ], [ "WorkTypeView", typeof( WorkTypePageViewModel ) ], [ "ProductView", typeof( ProductPageViewModel ) ],
		[ "SupplierView", typeof( SupplierPageViewModel ) ], [ "ProjectView", typeof( ProjectPageViewModel ) ], [ "StockManagementView", typeof( StockManagementPageViewModel ) ]
	];

	[TestMethod]
	[DynamicData( nameof( PageViews ) )]
	public void CommandBindings_ExistOnTheViewModel( string view, Type viewModel )
	{
		var xaml = LoadView( view );
		var commands = Regex.Matches( xaml, @"Command=""\{Binding\s+(?:Path=)?(\w+Command)\b" ).Select( m => m.Groups [ 1 ].Value ).Distinct();

		foreach ( var command in commands )
			Assert.IsNotNull( viewModel.GetProperty( command ), $"{view} binds {command}, which {viewModel.Name} does not have." );
	}

	[TestMethod]
	public void CurrencyGrid_ShowsTheLoadedCurrencies()
	{
		var xaml = LoadView( "CurrencyView" );
		StringAssert.Contains( xaml, "ItemsSource=\"{Binding Currencies}\"" );
		Assert.IsFalse( xaml.Contains( "{Binding CurrencyRate}" ), "The rate is CurrencyConversionRate." );
	}

	[TestMethod]
	public void StorageLocationName_RaisesPropertyChangedThroughTheInterface()
	{
		var location = new StorageLocationModel { StorageName = "Kast A" };
		var changed = new List<string?>();
		( ( INotifyPropertyChanged ) location ).PropertyChanged += ( _, e ) => changed.Add( e.PropertyName );

		location.StorageName = "Kast B";

		CollectionAssert.Contains( changed, nameof( StorageLocationModel.StorageName ), "The screen only sees changes through INotifyPropertyChanged." );
		Assert.AreEqual( StorageLocationModel.RecordState.Modified, location.State );
		Assert.AreEqual( "*", location.StatusMarker );
	}

	[TestMethod]
	public void ProjectReportsToggles_ExistOnTheViewModel()
	{
		// The Worktype -> WorkType rename changed the binding but not the backing field, so the toggle did nothing.
		var xaml = LoadView( "ProjectReportsView" );
		var toggles = Regex.Matches( xaml, @"\{Binding\s+(?:Path=)?(\w+ShowsTable)\b" ).Select( m => m.Groups [ 1 ].Value ).Distinct().ToList();

		Assert.IsTrue( toggles.Count > 0 );
		foreach ( var toggle in toggles )
			Assert.IsNotNull( typeof( ProjectReportsViewModel ).GetProperty( toggle ), $"ProjectReportsView binds {toggle}, which ProjectReportsViewModel does not have." );
	}

	[TestMethod]
	public void StorageLocation_LoadedWithStateLast_IsUnchanged()
	{
		// The same initializer as StorageLocationService: the setters mark the record modified, State resets it.
		var location = new StorageLocationModel { StorageId = 1, StorageName = "Kast A", State = StorageLocationModel.RecordState.Unchanged };

		Assert.AreEqual( StorageLocationModel.RecordState.Unchanged, location.State );
		Assert.AreEqual( string.Empty, location.StatusMarker );
	}

	[TestMethod]
	public void ExcelAndCsvExport_UseTheSameRowType()
	{
		// KAN-148: the country screen exported CurrencyModel to Excel, so no country matched and the file stayed empty.
		var views = Path.Combine( RepoRoot(), "Modelbouwer", "Views" );
		foreach ( var file in Directory.GetFiles( views, "*View.xaml.cs" ) )
		{
			var code = File.ReadAllText( file );
			var csv = Regex.Matches( code, @"ExportToCsvAsync<(\w+)>" ).Select( m => m.Groups [ 1 ].Value ).Distinct().ToList();
			var excel = Regex.Matches( code, @"ExportToExcelAsync<(\w+)>" ).Select( m => m.Groups [ 1 ].Value ).Distinct().ToList();
			if ( csv.Count == 0 || excel.Count == 0 )
				continue;

			CollectionAssert.AreEquivalent( csv, excel, $"{Path.GetFileName( file )} exports other rows to Excel than to CSV." );
		}
	}

	private static string RepoRoot()
	{
		var directory = AppContext.BaseDirectory;
		while ( directory != null && !File.Exists( Path.Combine( directory, "ModelbouwWerkbank.slnx" ) ) )
			directory = Directory.GetParent( directory )?.FullName;
		return directory ?? throw new DirectoryNotFoundException();
	}

	private static string LoadView( string name ) => File.ReadAllText( Path.Combine( RepoRoot(), "Modelbouwer", "Views", name + ".xaml" ) );
}
