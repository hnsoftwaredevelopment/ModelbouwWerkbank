using CommunityToolkit.Mvvm.Input;

using Syncfusion.UI.Xaml.TreeGrid;
using Syncfusion.Windows.Shared;

namespace Modelbouwer.ViewModels;

public partial class WorkTypePageViewModel : EntityPageViewModel<WorkTypeModel>
{
	private readonly IWorkTypeService _dataService;

	// Collections
	public ObservableCollection<WorkTypeModel> WorkTypes { get; } = [ ];
	public ObservableCollection<WorkTypeModel> WorkTypeTree { get; } = [ ];

	/// <summary>Called around rebuilding the tree, so the view can keep the open branches (a save reloads the list).</summary>
	public Action? BeforeTreeRebuild { get; set; }
	public Action? AfterTreeRebuild { get; set; }

	// Commands
	public IRelayCommand AddWorkTypeCommand => AddCommand;
	public IAsyncRelayCommand SaveWorkTypeCommand => SaveCommand;
	public IRelayCommand DeleteWorkTypeCommand => DeleteCommand;
	public new IRelayCommand ClearSearchCommand => _clearSearchCommand ??= new RelayCommand( () => SearchText = string.Empty );
	public IRelayCommand AddSubWorkTypeCommand { get; }

	private IRelayCommand? _clearSearchCommand;

	private ICommand? _expandCommand;

	public ICommand? ExpandCommand
	{
		get { return _expandCommand; }
		set { _expandCommand = value; }
	}

	private ICommand? _collapseCommand;

	public ICommand? CollapseCommand
	{
		get { return _collapseCommand; }
		set { _collapseCommand = value; }
	}

	internal delegate void FilterChanged();
	internal FilterChanged? _filterChanged;

	// Constructor
	public WorkTypePageViewModel( IWorkTypeService dataService, IEntityValidator<WorkTypeModel> validator ) : base( validator )
	{
		_dataService = dataService;

		AddSubWorkTypeCommand = new RelayCommand( AddSubWorkType, () => SelectedItem != null );

		ExpandCommand = new DelegateCommand<object>( ExpandExecute );
		CollapseCommand = new DelegateCommand<object>( CollapseExecute );

		// Started last: a load that finishes at once selects an item, which needs the commands above.
		ObserveBackgroundTask( ReloadAsync() );
	}

	protected override void OnPropertyChanged( System.ComponentModel.PropertyChangedEventArgs e )
	{
		base.OnPropertyChanged( e );

		if ( e.PropertyName == nameof( SearchText ) )
		{
			_filterChanged?.Invoke();
		}
	}

	// Override SelectedItem changed om DefaultWorkType te zetten
	protected override void OnSelectedItemChanged( WorkTypeModel? oldValue, WorkTypeModel? newValue )
	{
		base.OnSelectedItemChanged( oldValue, newValue );

		AddSubWorkTypeCommand.NotifyCanExecuteChanged();
	}

	public bool FilterWorkType( object obj )
	{
		if ( obj is not WorkTypeModel worktype )
			return false;

		if ( string.IsNullOrWhiteSpace( base.SearchText ) )
			return true;

		return worktype.WorktypeName?.Contains( base.SearchText, StringComparison.CurrentCultureIgnoreCase ) == true;
	}

	// Abstract overrides voor CRUD
	protected override Task<List<WorkTypeModel>> LoadItemsAsync( CancellationToken cancellationToken ) => _dataService.GetAllWorkTypesAsync( cancellationToken );
	protected override Task<int> InsertAsync( WorkTypeModel item, CancellationToken cancellationToken ) => _dataService.InsertNewWorkTypeAsync( CreateParameters( item ), cancellationToken );
	protected override Task UpdateAsync( WorkTypeModel item, CancellationToken cancellationToken ) => _dataService.UpdateWorkTypeAsync( UpdateParameters( item ), cancellationToken );
	protected override async Task DeleteAsync( WorkTypeModel item, CancellationToken cancellationToken )
	{
		if ( item == null )
			return;

		var result = MessageBox.Show(
			$"{Lang.toolbarButtonActionDeleteMessageQuestionPrefix} '{item.WorktypeName}' {Lang.toolbarButtonActionDeleteMessageQuestionWorkTypeSuffix}",
			$"{Lang.toolbarButtonActionDeleteMessageButtonText}",
			MessageBoxButton.YesNo,
			MessageBoxImage.Warning
		);

		if ( result != MessageBoxResult.Yes )
			return;
		try
		{
			await _dataService.DeleteWorkTypeAsync( item.WorktypeId, cancellationToken );
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

	private void ExpandExecute( object obj )
	{
		var treeGrid = obj as SfTreeGrid;
		treeGrid?.ExpandAllNodes();
	}

	private void CollapseExecute( object obj )
	{
		var treeGrid = obj as SfTreeGrid;
		treeGrid?.CollapseAllNodes();
	}

	public ObservableCollection<WorkTypeModel> BuildTree( IEnumerable<WorkTypeModel> flatList )
	{
		var lookup = flatList.ToDictionary(c => c.WorktypeId);

		// Make sure Children are not doubled
		foreach ( var c in lookup.Values )
			c.Children.Clear();

		foreach ( var worktype in lookup.Values )
		{
			if ( worktype.ParentId != null &&
				lookup.TryGetValue( worktype.ParentId.Value, out var parent ) )
			{
				parent.Children.Add( worktype );
			}
		}

		return new ObservableCollection<WorkTypeModel>( lookup.Values.Where( c => c.ParentId == null || c.ParentId == 0 ) );
	}

	protected override int GetId( WorkTypeModel item ) => item.WorktypeId;
	protected override void SetId( WorkTypeModel item, int id ) => item.WorktypeId = id;

	protected override WorkTypeModel CreateNewItem() => new()
	{
		WorktypeId = 0,
		ParentId = 0,
		WorktypeName = string.Empty
	};

	protected override void OnItemsLoaded()
	{
		base.OnItemsLoaded();

		BeforeTreeRebuild?.Invoke();
		WorkTypeTree.Clear();

		var tree = BuildTree( Items );
		foreach ( var root in tree )
			WorkTypeTree.Add( root );

		OnPropertyChanged( nameof( WorkTypeTree ) );
		AfterTreeRebuild?.Invoke();
	}

	private void AddSubWorkType()
	{
		if ( SelectedItem == null )
			return;

		var newWorkType = new WorkTypeModel
		{
			WorktypeName = string.Empty,
			ParentId = SelectedItem.WorktypeId
		};

		SelectedItem.Children.Add( newWorkType );
		SelectedItem = newWorkType;
	}

	#region Filtering
	public bool FilterRecords( object o )
	{
		if ( o is not WorkTypeModel worktype )
			return false;

		if ( string.IsNullOrWhiteSpace( SearchText ) )
			return true;

		if ( worktype.WorktypeName?
			.Contains( SearchText, StringComparison.OrdinalIgnoreCase ) == true )
			return true;

		return HasMatchingChild( worktype );
	}

	private bool HasMatchingChild( WorkTypeModel parent )
	{
		if ( parent.Children == null || parent.Children.Count == 0 )
			return false;

		foreach ( var child in parent.Children )
		{
			if ( child.WorktypeName?
				.Contains( SearchText, StringComparison.OrdinalIgnoreCase ) == true )
				return true;

			if ( HasMatchingChild( child ) )
				return true;
		}

		return false;
	}
	#endregion

	// Parameter dictionary voor save
	private static Dictionary<string, object?> CreateParameters( WorkTypeModel c ) => new()
	{
		{ $"@{DBNames.WorkTypeFieldNameParentId}", c.ParentId == 0 ? null : c.ParentId },
		{ $"@{DBNames.WorkTypeFieldNameName}", c.WorktypeName?.Trim() }
	};

	private static Dictionary<string, object?> UpdateParameters( WorkTypeModel c ) => new()
	{
		{ $"@{DBNames.WorkTypeFieldNameId}", c.WorktypeId == 0 ? null : c.WorktypeId },
		{ $"@{DBNames.WorkTypeFieldNameParentId}", c.ParentId == 0 ? null : c.ParentId },
		{ $"@{DBNames.WorkTypeFieldNameName}", c.WorktypeName?.Trim() }
	};
}