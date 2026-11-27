using System.Globalization;

namespace Modelbouwer.UnitTests.ViewModels;

/// <summary>KAN-147: the hours and materials tabs of the project screen.</summary>
[TestClass]
public class ProjectDetailsTabsTests
{
	private Mock<IProjectService> _projects = null!;
	private Mock<ITimeRegistrationService> _time = null!;

	[TestInitialize]
	public void Setup()
	{
		_projects = new Mock<IProjectService>();
		_projects.Setup( s => s.GetAllProjectsAsync( It.IsAny<CancellationToken>() ) ).ReturnsAsync( [ ] );
		_time = new Mock<ITimeRegistrationService>();
		_time.Setup( s => s.GetTimeEntriesByProjectAsync( It.IsAny<int>() ) ).ReturnsAsync( [ ] );
		_time.Setup( s => s.GetMaterialUsageByProjectAsync( It.IsAny<int>() ) ).ReturnsAsync( [ ] );
	}

	private ProjectPageViewModel Create() =>
		new( _projects.Object, new Mock<IEntityValidator<ProjectModel>>().Object, _time.Object );

	private static TimeEntryModel Entry( string date, string workType, string start, string end ) => new()
	{
		WorkDate = DateTime.Parse( date, CultureInfo.InvariantCulture ),
		WorktypeName = workType,
		StartTime = start,
		EndTime = end
	};

	private static MaterialUsageModel Usage( string date, string name, double amount, double price ) => new()
	{
		UsageDate = DateTime.Parse( date, CultureInfo.InvariantCulture ),
		ProductName = name,
		Amount = amount,
		Price = price,
		Costs = amount * price
	};

	[TestMethod]
	public void WorkTypeGroupHeader_ShowsTheWorkedTimeOfTheGroup()
	{
		// The hours grid groups per work type; the group header uses this aggregate for its subtotal.
		var aggregate = new WorkTimeAggregate();
		var property = System.ComponentModel.TypeDescriptor.GetProperties( aggregate ) [ nameof( WorkTimeAggregate.Time ) ]!;

		aggregate.CalculateAggregateFunc()( new [ ] { Entry( "2026-11-02", "Romp", "09:00", "12:30" ), Entry( "2026-11-04", "Romp", "20:00", "22:00" ) }, "WorkedMinutes", property );

		Assert.AreEqual( Hours( 5.5 ), aggregate.Time );
	}

	[TestMethod]
	public async Task CostOverview_AddsMaterialAndHoursAtTheHourRate()
	{
		_time.Setup( s => s.GetTimeEntriesByProjectAsync( 7 ) ).ReturnsAsync( [ Entry( "2026-11-02", "Romp", "09:00", "10:30" ) ] );
		_time.Setup( s => s.GetMaterialUsageByProjectAsync( 7 ) ).ReturnsAsync( [ Usage( "2026-11-01", "Lijm", 2, 4.5 ) ] );
		_time.Setup( s => s.GetHourRateAsync() ).ReturnsAsync( 20d );
		_time.Setup( s => s.GetCultureAsync() ).ReturnsAsync( new CultureInfo( "nl-NL" ) );
		var vm = Create();

		await vm.LoadProjectDetailsAsync( new ProjectModel { ProjectId = 7 } );

		Assert.AreEqual( 9d, vm.ProjectMaterialCosts );
		Assert.AreEqual( 30d, vm.ProjectHoursCosts, "1:30 hours at 20 per hour." );
		Assert.AreEqual( 39d, vm.ProjectGrandTotal );
		Assert.AreEqual( new CultureInfo( "nl-NL" ).NumberFormat.CurrencySymbol, vm.ProjectGrandTotalText.Trim().Substring( 0, 1 ), "Amounts use the currency of the settings." );
		StringAssert.Contains( vm.ProjectHoursCostsLabel, 20d.ToString( "C", new CultureInfo( "nl-NL" ) ), "The label shows the hour rate." );
	}

	[TestMethod]
	public async Task Load_ShowsHoursNewestFirstWithTotal()
	{
		_time.Setup( s => s.GetTimeEntriesByProjectAsync( 7 ) ).ReturnsAsync(
		[
			Entry( "2026-11-02", "Romp", "09:00", "12:30" ),
			Entry( "2026-11-03", "Tuigage", "13:00", "14:15" ),
			Entry( "2026-11-04", "Romp", "20:00", "22:00" )
		] );
		var vm = Create();

		await vm.LoadProjectDetailsAsync( new ProjectModel { ProjectId = 7 } );

		Assert.AreEqual( 3, vm.ProjectTimeEntryCount );
		Assert.IsTrue( vm.HasProjectTimeEntries );
		Assert.AreEqual( 405d, vm.ProjectTotalMinutes, "3:30 + 1:15 + 2:00." );
		Assert.AreEqual( Hours( 6.75 ), vm.ProjectTotalTime );
		CollectionAssert.AreEqual( new [ ] { "2026-11-04", "2026-11-03", "2026-11-02" },
			vm.ProjectTimeEntries.Select( e => e.WorkDate.ToString( "yyyy-MM-dd", CultureInfo.InvariantCulture ) ).ToArray(), "Newest first." );
	}

	[TestMethod]
	public void HoursGrid_IsGroupedPerWorkTypeWithSubtotalInTheHeader()
	{
		var xaml = File.ReadAllText( Path.Combine( RepoRoot(), "Modelbouwer", "Views", "ProjectView.xaml" ) );

		StringAssert.Contains( xaml, "<syncfusion:GroupColumnDescription ColumnName=\"WorktypeName\" />" );
		StringAssert.Contains( xaml, "CustomAggregate=\"{StaticResource WorkTimeAggregate}\" Format=\"'{Time}'\"" );
		StringAssert.Contains( Lang.ProjectHoursGroupCaption, "{Time}", "The group header shows the subtotal." );
		StringAssert.Contains( Lang.ProjectHoursGroupCaption, "{Key}", "The group header shows the work type." );
	}

	private static string RepoRoot()
	{
		var directory = AppContext.BaseDirectory;
		while ( directory != null && !File.Exists( Path.Combine( directory, "ModelbouwWerkbank.slnx" ) ) )
			directory = Directory.GetParent( directory )?.FullName;
		return directory ?? throw new DirectoryNotFoundException();
	}

	[TestMethod]
	public async Task Load_ShowsMaterialWithTotalCosts()
	{
		_time.Setup( s => s.GetMaterialUsageByProjectAsync( 7 ) ).ReturnsAsync(
		[
			Usage( "2026-10-01", "Lindehout 2 mm", 3, 1.25 ),
			Usage( "2026-11-01", "Lijm", 1, 4.99 )
		] );
		var vm = Create();

		await vm.LoadProjectDetailsAsync( new ProjectModel { ProjectId = 7 } );

		Assert.AreEqual( 2, vm.ProjectMaterialUsageCount );
		Assert.IsTrue( vm.HasProjectMaterialUsages );
		Assert.AreEqual( 8.74, vm.ProjectMaterialCosts, 0.0001, "3 x 1.25 + 1 x 4.99." );
		Assert.AreEqual( "Lijm", vm.ProjectMaterialUsages [ 0 ].ProductName, "Newest first." );
	}

	[TestMethod]
	public async Task EmptyProject_ShowsNoRowsAndZeroTotals()
	{
		var vm = Create();

		await vm.LoadProjectDetailsAsync( new ProjectModel { ProjectId = 7 } );

		Assert.IsFalse( vm.HasProjectTimeEntries, "The view shows the 'no hours yet' text instead of the grid." );
		Assert.IsFalse( vm.HasProjectMaterialUsages );
		Assert.AreEqual( Hours( 0 ), vm.ProjectTotalTime );
		Assert.AreEqual( 0d, vm.ProjectMaterialCosts );
	}

	[TestMethod]
	public async Task SwitchingProject_NeverShowsTheOldProjectsRows()
	{
		var slow = new TaskCompletionSource<List<TimeEntryModel>>();
		_time.Setup( s => s.GetTimeEntriesByProjectAsync( 1 ) ).Returns( slow.Task );
		_time.Setup( s => s.GetTimeEntriesByProjectAsync( 2 ) ).ReturnsAsync( [ Entry( "2026-11-05", "Verf", "10:00", "11:00" ) ] );
		var vm = Create();

		var first = vm.LoadProjectDetailsAsync( new ProjectModel { ProjectId = 1 } );
		await vm.LoadProjectDetailsAsync( new ProjectModel { ProjectId = 2 } );
		slow.SetResult( [ Entry( "2026-01-01", "Romp", "09:00", "17:00" ), Entry( "2026-01-02", "Romp", "09:00", "17:00" ) ] );
		await first;

		Assert.AreEqual( 1, vm.ProjectTimeEntryCount, "The slower load of the first project is dropped." );
		Assert.AreEqual( "Verf", vm.ProjectTimeEntries.Single().WorktypeName );
		Assert.AreEqual( Hours( 1 ), vm.ProjectTotalTime );
	}

	[TestMethod]
	public async Task NewProject_IsNotLoaded()
	{
		_time.Setup( s => s.GetTimeEntriesByProjectAsync( 7 ) ).ReturnsAsync( [ Entry( "2026-11-05", "Verf", "10:00", "11:00" ) ] );
		var vm = Create();
		await vm.LoadProjectDetailsAsync( new ProjectModel { ProjectId = 7 } );

		await vm.LoadProjectDetailsAsync( new ProjectModel { ProjectId = 0 } );

		Assert.AreEqual( 0, vm.ProjectTimeEntryCount, "A project that is not saved yet has no hours; the old rows are cleared." );
		_time.Verify( s => s.GetTimeEntriesByProjectAsync( 0 ), Times.Never );
	}

	[TestMethod]
	public async Task SelectingAProject_LoadsItsDetails()
	{
		var project = new ProjectModel { ProjectId = 4, ProjectName = "Victory" };
		_projects.Setup( s => s.GetAllProjectsAsync( It.IsAny<CancellationToken>() ) ).ReturnsAsync( [ project ] );
		_time.Setup( s => s.GetMaterialUsageByProjectAsync( 4 ) ).ReturnsAsync( [ Usage( "2026-11-01", "Lijm", 2, 3 ) ] );

		var vm = Create();
		await WaitUntilAsync( () => vm.ProjectMaterialUsageCount == 1 );

		Assert.AreEqual( 6d, vm.ProjectMaterialCosts );
	}

	/// <summary>Decimal hours as the screen shows them in the current culture.</summary>
	private static string Hours( double hours ) => hours.ToString( "N2", CultureInfo.CurrentCulture );

	[TestMethod]
	[DataRow( 0d, 0d )]
	[DataRow( 15d, 0.25 )]
	[DataRow( 22575d, 376.25 )]
	public void ProjectTime_ShowsDecimalHours( double minutes, double hours )
	{
		// Herbert, KAN-147: 376,25 reads easier than 376:15 for most users.
		Assert.AreEqual( Hours( hours ), ProjectTime.Format( minutes ) );
	}

	[TestMethod]
	public void ProjectTime_UsesTheDecimalSeparatorOfTheCulture()
	{
		var previous = CultureInfo.CurrentCulture;
		try
		{
			CultureInfo.CurrentCulture = new CultureInfo( "nl-NL" );
			Assert.AreEqual( "376,25", ProjectTime.Format( 22575 ) );
		}
		finally
		{
			CultureInfo.CurrentCulture = previous;
		}
	}

	[TestMethod]
	public void TimeEntry_WorkedHoursIsDecimal()
	{
		Assert.AreEqual( 1.25, Entry( "2026-11-02", "Romp", "09:00", "10:15" ).WorkedHours );
	}

	private static async Task WaitUntilAsync( Func<bool> condition )
	{
		using var timeout = new CancellationTokenSource( TimeSpan.FromSeconds( 2 ) );
		while ( !condition() )
		{
			if ( timeout.IsCancellationRequested )
				Assert.Fail( "Condition was not met before timeout." );
			await Task.Delay( 10 );
		}
	}
}
