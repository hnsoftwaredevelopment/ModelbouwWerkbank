using System.ComponentModel;

using CommunityToolkit.Mvvm.Input;

using Microsoft.Win32;

namespace Modelbouwer.ViewModels;

public partial class ProjectPageViewModel : EntityPageViewModel<ProjectModel>
{
	private readonly IProjectService _dataService;

	private int? _lastSelectedProjectId;
	private int _workStatsLoadVersion;

	public ProjectModel? SelectedProject
	{
		get => SelectedItem;
		set => SelectedItem = value;
	}

	public bool IsProjectClosed => SelectedProject?.ProjectClosed ?? false;

	public Visibility EndDatePickerVisibility => IsProjectClosed ? Visibility.Visible : Visibility.Collapsed;

	public Visibility ExpectedEndDateVisibility => IsProjectClosed ? Visibility.Collapsed : Visibility.Visible;

	// Commands
	public IRelayCommand AddProjectCommand => AddCommand;
	public IAsyncRelayCommand SaveProjectCommand => SaveCommand;
	public IRelayCommand DeleteProjectCommand => DeleteCommand;
	public new IRelayCommand ClearSearchCommand => _clearSearchCommand ??= new RelayCommand( () => SearchText = string.Empty );
	public IRelayCommand RotateCommand => _rotateCommand ??= new RelayCommand( RotateImage );
	public IRelayCommand AddImageCommand => _addImageCommand ??= new RelayCommand( AddImage );
	public IRelayCommand DeleteImageCommand => _deleteImageCommand ??= new RelayCommand( DeleteImage );

	private ProjectModel? _previousProject;

	private IRelayCommand? _rotateCommand;
	private IRelayCommand? _addImageCommand;
	private IRelayCommand? _deleteImageCommand;

	private IRelayCommand? _clearSearchCommand;

	// Constructor
	public ProjectPageViewModel( IProjectService dataService, IEntityValidator<ProjectModel> validator, ITimeRegistrationService timeRegistrationService ) : base( validator )
	{
		_dataService = dataService;
		_timeRegistrationService = timeRegistrationService;

		ObserveBackgroundTask( ReloadAsync() );
	}

	#region Hours and materials tabs (KAN-147)
	private readonly ITimeRegistrationService _timeRegistrationService;
	private int _projectDetailsVersion;

	/// <summary>
	/// Time entries of the selected project, newest first. Read only: hours are booked in time registration. The view
	/// groups them per work type, with the worked time of each group in its header (<see cref="WorkTimeAggregate"/>).
	/// </summary>
	public ObservableCollection<TimeEntryModel> ProjectTimeEntries { get; } = [ ];

	/// <summary>Material used on the selected project, newest first. Costs use the current product price.</summary>
	public ObservableCollection<MaterialUsageModel> ProjectMaterialUsages { get; } = [ ];

	[ObservableProperty] private bool _hasProjectTimeEntries;
	[ObservableProperty] private bool _hasProjectMaterialUsages;
	[ObservableProperty] private int _projectTimeEntryCount;
	[ObservableProperty] private double _projectTotalMinutes;
	[ObservableProperty] private int _projectMaterialUsageCount;
	[ObservableProperty] private double _projectMaterialCosts;
	[ObservableProperty] private double _projectHourRate;

	/// <summary>Culture of the settings, for the amounts in the cost overview.</summary>
	private CultureInfo _costCulture = CultureInfo.CurrentCulture;

	/// <summary>Total worked time in decimal hours, for example 37,75 for 37 hours and 45 minutes.</summary>
	public string ProjectTotalTime => ProjectTime.Format( ProjectTotalMinutes );

	#region Cost overview on the general tab
	/// <summary>Worked hours times the hour rate of the settings.</summary>
	public double ProjectHoursCosts => Math.Round( ProjectTotalMinutes / 60d * ProjectHourRate, 2, MidpointRounding.AwayFromZero );
	public double ProjectGrandTotal => ProjectMaterialCosts + ProjectHoursCosts;

	public string ProjectHoursCostsLabel => string.Format( CultureInfo.CurrentCulture, Lang.ProjectCostsHours, ProjectHourRate.ToString( "C", _costCulture ) );
	public string ProjectMaterialCostsText => ProjectMaterialCosts.ToString( "C", _costCulture );
	public string ProjectHoursCostsText => ProjectHoursCosts.ToString( "C", _costCulture );
	public string ProjectGrandTotalText => ProjectGrandTotal.ToString( "C", _costCulture );

	partial void OnProjectTotalMinutesChanged( double value )
	{
		OnPropertyChanged( nameof( ProjectTotalTime ) );
		RaiseCostOverview();
	}

	partial void OnProjectMaterialCostsChanged( double value ) => RaiseCostOverview();
	partial void OnProjectHourRateChanged( double value ) => RaiseCostOverview();

	private void RaiseCostOverview()
	{
		OnPropertyChanged( nameof( ProjectHoursCosts ) );
		OnPropertyChanged( nameof( ProjectGrandTotal ) );
		OnPropertyChanged( nameof( ProjectHoursCostsLabel ) );
		OnPropertyChanged( nameof( ProjectMaterialCostsText ) );
		OnPropertyChanged( nameof( ProjectHoursCostsText ) );
		OnPropertyChanged( nameof( ProjectGrandTotalText ) );
	}
	#endregion

	/// <summary>
	/// Loads the hours and material of <paramref name="project"/>. The lists are emptied first, so the previous
	/// project's rows are never shown as this project's; a load that is overtaken by a newer selection is dropped.
	/// </summary>
	internal async Task LoadProjectDetailsAsync( ProjectModel? project )
	{
		int version = ++_projectDetailsVersion;

		ProjectTimeEntries.Clear();
		ProjectMaterialUsages.Clear();
		HasProjectTimeEntries = false;
		HasProjectMaterialUsages = false;
		ProjectTimeEntryCount = 0;
		ProjectTotalMinutes = 0d;
		ProjectMaterialUsageCount = 0;
		ProjectMaterialCosts = 0d;

		if ( project == null || project.ProjectId <= 0 )
			return;

		var entriesTask = _timeRegistrationService.GetTimeEntriesByProjectAsync( project.ProjectId );
		var usagesTask = _timeRegistrationService.GetMaterialUsageByProjectAsync( project.ProjectId );
		var hourRateTask = _timeRegistrationService.GetHourRateAsync();
		var cultureTask = _timeRegistrationService.GetCultureAsync();
		await Task.WhenAll( entriesTask, usagesTask, hourRateTask, cultureTask );

		if ( version != _projectDetailsVersion )
			return;

		_costCulture = cultureTask.Result ?? CultureInfo.CurrentCulture;
		ProjectHourRate = hourRateTask.Result;

		var entries = ( entriesTask.Result ?? [ ] )
			.OrderByDescending( entry => entry.WorkDate )
			.ThenBy( entry => entry.StartTime, StringComparer.Ordinal )
			.ToList();
		foreach ( var entry in entries )
			ProjectTimeEntries.Add( entry );

		foreach ( var usage in ( usagesTask.Result ?? [ ] ).OrderByDescending( usage => usage.UsageDate ) )
			ProjectMaterialUsages.Add( usage );

		ProjectTimeEntryCount = ProjectTimeEntries.Count;
		HasProjectTimeEntries = ProjectTimeEntryCount > 0;
		ProjectTotalMinutes = entries.Sum( entry => entry.WorkedMinutes );

		ProjectMaterialUsageCount = ProjectMaterialUsages.Count;
		HasProjectMaterialUsages = ProjectMaterialUsageCount > 0;
		ProjectMaterialCosts = Math.Round( ProjectMaterialUsages.Sum( usage => usage.Costs ), 2, MidpointRounding.AwayFromZero );

		// The culture may have changed without any amount changing.
		RaiseCostOverview();
	}
	#endregion

	// Override SelectedItem changed om DefaultProject te zetten
	protected override void OnSelectedItemChanged( ProjectModel? oldValue, ProjectModel? newValue )
	{
		base.OnSelectedItemChanged( oldValue, newValue );

		// Unhook oude handlers
		if ( _previousProject != null )
			_previousProject.PropertyChanged -= SelectedProject_PropertyChanged;

		_previousProject = newValue;

		if ( newValue != null )
			newValue.PropertyChanged += SelectedProject_PropertyChanged;

		// Refresh UI properties die afhankelijk zijn van de geselecteerde project
		RaiseProjectStateProperties();

		// Reset workstats en calculated fields
		_currentWorkStats = null;
		ProjectExpectedEndDate = null;

		// Laad nieuwe workstats en recalc expected end date
		if ( newValue != null )
		{
			var loadVersion = ++_workStatsLoadVersion;
			ObserveBackgroundTask( LoadWorkStatsAsync( newValue, loadVersion ) );
		}

		// Hours and material tabs (KAN-147)
		ObserveBackgroundTask( LoadProjectDetailsAsync( newValue ) );
	}

	private void SelectedProject_PropertyChanged( object? sender, PropertyChangedEventArgs e )
	{
		switch ( e.PropertyName )
		{
			case nameof( ProjectModel.ProjectExpectedTime ):
			case nameof( ProjectModel.ProjectStartDate ):
				RecalculateExpectedEndDate();
				break;

			case nameof( ProjectModel.ProjectClosed ):
				ObserveBackgroundTask( HandleProjectClosedChangedAsync() );
				if ( SelectedProject != null )
				{
					var loadVersion = ++_workStatsLoadVersion;
					ObserveBackgroundTask( LoadWorkStatsAsync( SelectedProject, loadVersion ) );
				}
				RaiseProjectStateProperties();
				break;
		}
	}

	private void RaiseProjectStateProperties()
	{
		OnPropertyChanged( nameof( IsProjectClosed ) );
		OnPropertyChanged( nameof( EndDatePickerVisibility ) );
		OnPropertyChanged( nameof( ExpectedEndDateVisibility ) );
	}

	// Properties voor UI binding
	public ObservableCollection<ProjectModel> Projects => Items;
	public int TotalProjectCount => TotalItemCount;
	public int VisibleProjectCount
	{
		get => base.VisibleItemCount;
		set => base.VisibleItemCount = value;
	}

	// Filtering
	public bool FilterProject( object obj )
	{
		if ( obj is not ProjectModel project )
			return false;

		if ( string.IsNullOrWhiteSpace( SearchText ) )
			return true;

		return project.ProjectName?.Contains( SearchText, StringComparison.CurrentCultureIgnoreCase ) == true;
	}

	private void RotateImage()
	{
		if ( SelectedProject == null )
			return;

		SelectedProject.ProjectImageRotationAngle = ( SelectedProject.ProjectImageRotationAngle + 90 ) % 360;
		Debug.WriteLine( SelectedProject.ProjectImageRotationAngle );
	}

	private void AddImage()
	{

		if ( SelectedProject == null )
			return;

		var dialog = new OpenFileDialog
		{
			Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp",
			Title = "Select image"
		};

		if ( dialog.ShowDialog() != true )
			return;

		SelectedProject.ProjectImage = File.ReadAllBytes( dialog.FileName );
		SelectedProject.ProjectImageRotationAngle = 0;
	}

	private void DeleteImage()
	{
		if ( SelectedProject == null )
			return;

		SelectedProject.ProjectImage = null;
		SelectedProject.ProjectImageRotationAngle = 0;
	}

	private async Task HandleProjectClosedChangedAsync()
	{
		var project = SelectedProject;
		if ( project == null )
			return;

		if ( project.ProjectClosed )
		{
			var lastWorkDate =
			await _dataService.GetLastWorkDateOnProjectAsync(project.ProjectId);

			project.ProjectEndDate =
				lastWorkDate ?? DateOnly.FromDateTime( DateTime.Today );
		}
		else
		{
			project.ProjectEndDate = null;
		}

		// Notify the UI th the EndDate has changed
		OnPropertyChanged( nameof( SelectedProject ) );
	}

	// Abstract overrides voor CRUD
	protected override Task<List<ProjectModel>> LoadItemsAsync( CancellationToken cancellationToken ) => _dataService.GetAllProjectsAsync( cancellationToken );
	protected override Task<int> InsertAsync( ProjectModel item, CancellationToken cancellationToken ) => _dataService.InsertNewProjectAsync( CreateParameters( item ), cancellationToken );
	protected override Task UpdateAsync( ProjectModel item, CancellationToken cancellationToken )
	{
		if ( SelectedItem == null )
			return Task.CompletedTask;

		_lastSelectedProjectId = SelectedItem.ProjectId;

		return _dataService.UpdateProjectAsync( CreateParameters( SelectedItem ), cancellationToken );
	}
	protected override async Task DeleteAsync( ProjectModel item, CancellationToken cancellationToken )
	{
		if ( item == null )
			return;

		var result = MessageBox.Show(
			$"{Lang.toolbarButtonActionDeleteMessageQuestionPrefix} '{item.ProjectName}' {Lang.toolbarButtonActionDeleteMessageQuestionSuffix}",
			$"{Lang.toolbarButtonActionDeleteMessageButtonText}",
			MessageBoxButton.YesNo,
			MessageBoxImage.Warning
		);

		if ( result != MessageBoxResult.Yes )
			return;
		try
		{
			await _dataService.DeleteProjectAsync( item.ProjectId, cancellationToken );
		}
		catch ( EntityInUseException ex )
		{
			MessageBox.Show(
				ex.Message,
				Lang.generalMessageboxWarningTitle,
				MessageBoxButton.OK,
				MessageBoxImage.Information
			);
		}
	}

	protected override int GetId( ProjectModel item ) => item.ProjectId;
	protected override void SetId( ProjectModel item, int id ) => item.ProjectId = id;

	protected override ProjectModel CreateNewItem() => new()
	{
		ProjectId = 0,
		ProjectName = string.Empty
	};

	protected override void OnItemsLoaded()
	{
		base.OnItemsLoaded();

		OnPropertyChanged( nameof( TotalProjectCount ) );

		if ( _lastSelectedProjectId.HasValue )
		{
			var match = Projects.FirstOrDefault( p => p.ProjectId == _lastSelectedProjectId.Value );

			if ( match != null )
			{
				SelectedItem = match;
				return;
			}

			_lastSelectedProjectId = null;
		}

		// Default project selection (Highest Id)
		SelectProjectWithHighestId();
	}

	private void SelectProjectWithHighestId()
	{
		if ( Projects.Count == 0 )
		{
			SelectedItem = null;
			return;
		}

		SelectedItem = Projects
			.OrderByDescending( p => p.ProjectId )
			.First();
	}


	#region Expected end date of the project
	private double _projectExpectedTime;
	public double ProjectExpectedTime
	{
		get => _projectExpectedTime;
		set
		{
			if ( SetProperty( ref _projectExpectedTime, value ) )
			{
				RecalculateExpectedEndDate();
			}
		}
	}

	private DateTime? _projectExpectedEndDate;
	public DateTime? ProjectExpectedEndDate
	{
		get => _projectExpectedEndDate;
		private set => SetProperty( ref _projectExpectedEndDate, value );
	}

	private ProjectWorkStats? _currentWorkStats;

	private async Task LoadWorkStatsAsync( ProjectModel project, int loadVersion )
	{
		var workStats = await _dataService.GetProjectWorkStatsAsync( project.ProjectId );

		if ( loadVersion != _workStatsLoadVersion || !ReferenceEquals( SelectedProject, project ) )
			return;

		_currentWorkStats = workStats;

		RecalculateExpectedEndDate();
	}

	private void RecalculateExpectedEndDate()
	{
		if ( _currentWorkStats == null
			|| SelectedProject == null
			|| !SelectedProject.ProjectStartDate.HasValue
			|| !SelectedProject.ProjectExpectedTime.HasValue
			|| SelectedProject.ProjectExpectedTime.Value <= 0
			|| SelectedItem == null )
		{
			ProjectExpectedEndDate = null;
			return;
		}

		var totalWorkedHours = _currentWorkStats.TotalHours;
		var startDate = SelectedProject.ProjectStartDate.Value.ToDateTime( TimeOnly.MinValue );

		var hoursToDo = SelectedProject.ProjectExpectedTime.Value - totalWorkedHours;
		if ( hoursToDo <= 0 )
		{
			ProjectExpectedEndDate = DateTime.Now;
			return;
		}

		var elapsedDays = (DateTime.Now - startDate).TotalDays;
		if ( elapsedDays <= 0 )
		{
			ProjectExpectedEndDate = null;
			return;
		}

		var workedHoursPerDay = totalWorkedHours / elapsedDays;
		if ( workedHoursPerDay <= 0 )
		{
			ProjectExpectedEndDate = null;
			return;
		}

		var daysToGo = hoursToDo / workedHoursPerDay;
		ProjectExpectedEndDate = DateTime.Now.AddDays( ( double ) daysToGo );
	}
	#endregion

	// Parameter dictionary voor save
	private static Dictionary<string, object?> CreateParameters( ProjectModel c ) => new()
	{
		{ $"@{DBNames.ProjectFieldNameId}", c.ProjectId },
		{ $"@{DBNames.ProjectFieldNameCode}", c.ProjectCode },
		{ $"@{DBNames.ProjectFieldNameName}", c.ProjectName?.Trim() },
		{ $"@{DBNames.ProjectFieldNameStartDate}",
			c.ProjectStartDate.HasValue
				? c.ProjectStartDate.Value.ToDateTime(TimeOnly.MinValue)
				: DBNull.Value },
		{ $"@{DBNames.ProjectFieldNameEndDate}",
			c.ProjectEndDate.HasValue
				? c.ProjectEndDate.Value.ToDateTime(TimeOnly.MinValue)
				: DBNull.Value },
		{ $"@{DBNames.ProjectFieldNameExpectedTime}", c.ProjectExpectedTime },
		{ $"@{DBNames.ProjectFieldNameImage}", c.ProjectImage },
		{ $"@{DBNames.ProjectFieldNameImageRotationAngle}", c.ProjectImageRotationAngle },
		{ $"@{DBNames.ProjectFieldNameClosed}", c.ProjectClosed },
		{ $"@{DBNames.ProjectFieldNameMemo}", c.ProjectMemo }
	};
}