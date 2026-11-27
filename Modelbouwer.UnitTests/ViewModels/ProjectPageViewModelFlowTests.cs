using System.Windows;

namespace Modelbouwer.UnitTests.ViewModels;

/// <summary>KAN-88 (CB-07): user flows of the project screen.</summary>
[TestClass]
public class ProjectPageViewModelFlowTests
{
	private Mock<IProjectService> _service = null!;
	private Mock<IEntityValidator<ProjectModel>> _validator = null!;
	private List<ProjectModel> _stored = null!;

	[TestInitialize]
	public void Setup()
	{
		_stored =
		[
			new() { ProjectId = 3, ProjectName = "Bismarck" },
			new() { ProjectId = 9, ProjectName = "Victory" },
			new() { ProjectId = 5, ProjectName = "Cutty Sark" }
		];
		_service = new Mock<IProjectService>();
		_service.Setup( s => s.GetAllProjectsAsync( It.IsAny<CancellationToken>() ) ).ReturnsAsync( () => _stored.ToList() );
		_validator = new Mock<IEntityValidator<ProjectModel>>();
		_validator.Setup( v => v.ValidateAsync( It.IsAny<ProjectModel>(), It.IsAny<CancellationToken>() ) ).ReturnsAsync( new ValidationResult() );
	}

	private async Task<ProjectPageViewModel> CreateAsync()
	{
		var vm = new ProjectPageViewModel( _service.Object, _validator.Object, new Mock<ITimeRegistrationService>().Object );
		await WaitUntilAsync( () => vm.Projects.Count == _stored.Count );
		return vm;
	}

	[TestMethod]
	public async Task Load_SelectsTheNewestProject()
	{
		var vm = await CreateAsync();

		Assert.AreEqual( 9, vm.SelectedProject?.ProjectId, "The project with the highest id is the newest." );
		Assert.AreEqual( 3, vm.TotalProjectCount );
	}

	[TestMethod]
	public async Task Filter_MatchesNamePartIgnoringCase()
	{
		var vm = await CreateAsync();

		vm.SearchText = "sark";

		Assert.IsTrue( vm.FilterProject( _stored [ 2 ] ) );
		Assert.IsFalse( vm.FilterProject( _stored [ 0 ] ) );
		Assert.IsFalse( vm.FilterProject( "not a project" ) );

		vm.ClearSearchCommand.Execute( null );
		Assert.IsTrue( vm.FilterProject( _stored [ 0 ] ), "Without search text every project is shown." );
	}

	[TestMethod]
	public async Task SaveNewProject_InsertsAndSelectsIt()
	{
		var vm = await CreateAsync();
		_service.Setup( s => s.InsertNewProjectAsync( It.IsAny<Dictionary<string, object?>>(), It.IsAny<CancellationToken>() ) )
			.ReturnsAsync( 12 )
			.Callback( () => _stored.Add( new ProjectModel { ProjectId = 12, ProjectName = "Endeavour" } ) );

		vm.AddProjectCommand.Execute( null );
		Assert.AreEqual( 0, vm.SelectedProject?.ProjectId, "A new project starts without id." );
		vm.SelectedProject!.ProjectName = "  Endeavour  ";
		Assert.IsTrue( vm.SaveProjectCommand.CanExecute( null ) );
		await vm.SaveProjectCommand.ExecuteAsync( null );

		_service.Verify( s => s.InsertNewProjectAsync( It.Is<Dictionary<string, object?>>( p => Equals( p [ $"@{DBNames.ProjectFieldNameName}" ], "Endeavour" ) ), It.IsAny<CancellationToken>() ), Times.Once );
		Assert.AreEqual( 12, vm.SelectedProject?.ProjectId );
		Assert.IsFalse( vm.HasUnsavedChanges );
	}

	[TestMethod]
	public async Task SaveExistingProject_UpdatesAndKeepsItSelected()
	{
		var vm = await CreateAsync();
		_service.Setup( s => s.UpdateProjectAsync( It.IsAny<Dictionary<string, object?>>(), It.IsAny<CancellationToken>() ) ).Returns( Task.CompletedTask );

		vm.SelectedProject = vm.Projects.Single( p => p.ProjectId == 3 );
		vm.SelectedProject.ProjectMemo = "Rompdelen gelijmd";
		await vm.SaveProjectCommand.ExecuteAsync( null );

		_service.Verify( s => s.UpdateProjectAsync( It.Is<Dictionary<string, object?>>( p => Equals( p [ $"@{DBNames.ProjectFieldNameId}" ], 3 ) ), It.IsAny<CancellationToken>() ), Times.Once );
		Assert.AreEqual( 3, vm.SelectedProject?.ProjectId, "The saved project stays selected after the reload, not the newest one." );
	}

	[TestMethod]
	public async Task SaveCommand_IsOnlyAvailableWithChanges()
	{
		var vm = await CreateAsync();

		Assert.IsFalse( vm.SaveProjectCommand.CanExecute( null ) );

		vm.SelectedProject!.ProjectName = "Victory 1765";

		Assert.IsTrue( vm.SaveProjectCommand.CanExecute( null ) );
	}

	[TestMethod]
	public async Task ExpectedEndDate_FollowsWorkedHoursPerDay()
	{
		var project = new ProjectModel { ProjectId = 20, ProjectName = "Mayflower", ProjectStartDate = DateOnly.FromDateTime( DateTime.Today.AddDays( -10 ) ), ProjectExpectedTime = 30 };
		_stored = [ project ];
		_service.Setup( s => s.GetProjectWorkStatsAsync( 20 ) ).ReturnsAsync( new ProjectWorkStats { TotalHours = 10 } );

		var vm = await CreateAsync();
		await WaitUntilAsync( () => vm.ProjectExpectedEndDate != null );

		// 10 hours in about 10 days is 1 hour a day; 20 hours to go is about 20 days.
		var daysToGo = ( vm.ProjectExpectedEndDate!.Value - DateTime.Now ).TotalDays;
		Assert.IsTrue( daysToGo is > 17 and < 23, $"Expected about 20 days, got {daysToGo:0.0}." );

		project.ProjectExpectedTime = 5;
		Assert.AreEqual( DateTime.Today, vm.ProjectExpectedEndDate!.Value.Date, "With more hours worked than expected the project is due now." );
	}

	[TestMethod]
	public async Task ClosingProject_SetsEndDateToLastWorkDay()
	{
		var lastWorkDay = new DateOnly( 2026, 11, 20 );
		_service.Setup( s => s.GetLastWorkDateOnProjectAsync( 9 ) ).ReturnsAsync( lastWorkDay );
		var vm = await CreateAsync();

		vm.SelectedProject!.ProjectClosed = true;
		await WaitUntilAsync( () => vm.SelectedProject.ProjectEndDate != null );

		Assert.AreEqual( lastWorkDay, vm.SelectedProject.ProjectEndDate );
		Assert.IsTrue( vm.IsProjectClosed );
		Assert.AreEqual( Visibility.Visible, vm.EndDatePickerVisibility );
		Assert.AreEqual( Visibility.Collapsed, vm.ExpectedEndDateVisibility );

		vm.SelectedProject.ProjectClosed = false;
		await WaitUntilAsync( () => vm.SelectedProject.ProjectEndDate == null );
		Assert.AreEqual( Visibility.Visible, vm.ExpectedEndDateVisibility );
	}

	[TestMethod]
	public async Task ImageCommands_RotateAndRemoveTheImage()
	{
		var vm = await CreateAsync();
		vm.SelectedProject!.ProjectImage = [ 1, 2, 3 ];
		vm.SelectedProject.ProjectImageRotationAngle = 270;

		vm.RotateCommand.Execute( null );
		Assert.AreEqual( 0, vm.SelectedProject.ProjectImageRotationAngle, "A full turn starts at 0 again." );

		vm.RotateCommand.Execute( null );
		vm.DeleteImageCommand.Execute( null );
		Assert.IsNull( vm.SelectedProject.ProjectImage );
		Assert.AreEqual( 0, vm.SelectedProject.ProjectImageRotationAngle );
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
