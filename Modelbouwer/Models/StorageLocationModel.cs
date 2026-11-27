using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Modelbouwer.Models;

public partial class StorageLocationModel : ObservableObject
{
	[ObservableProperty] public int _storageId;

	[ObservableProperty] public int? _parentId;

	public ObservableCollection<StorageLocationModel> Children { get; set; } = [ ];

	public string? StorageName
	{
		get;
		set => SetProperty( ref field, value );
	}

	private StorageLocationModel? _parent;

	public StorageLocationModel? Parent
	{
		get => _parent;
		set => _parent = value;
	}

	public enum RecordState
	{
		Unchanged,
		Added,
		Modified,
		Deleted
	}

	private RecordState _state = RecordState.Unchanged;
	public RecordState State
	{
		get => _state;
		set => SetProperty( ref _state, value );
	}

	public string StatusMarker => State == RecordState.Unchanged ? string.Empty : "*";

	// Mapping dictionary for mapping Database Header to Property name
	public static readonly Dictionary<string, string> HeaderToPropertyMap = new()
	{
		{ DBNames.StorageFieldNameId, "StorageId" },
		{ DBNames.StorageFieldNameParentId, "ParentId" },
		{ DBNames.StorageFieldNameName, "StorageName" },
	};

	public static readonly Dictionary<string, string[]> ColumnMappings = new()
	{
		[nameof(StorageId)] = [ "ID" ],
		[nameof(ParentId)] = [ "Parent" ],

		[nameof(StorageName)] = [
			"Voorraad locatie",
			"Stock location",
			"Lagerort" ]
	};

	/// <summary>
	/// Marks the record as modified when a property changes. Uses the PropertyChanged of ObservableObject: a hiding
	/// "new" event was not seen through INotifyPropertyChanged, so the screen never noticed a change (Save stayed off).
	/// </summary>
	protected override void OnPropertyChanged( PropertyChangedEventArgs e )
	{
		base.OnPropertyChanged( e );

		if ( State == RecordState.Unchanged && e.PropertyName is not ( nameof( State ) or nameof( StatusMarker ) ) )
		{
			_state = RecordState.Modified; // avoid recursion through State
			base.OnPropertyChanged( new PropertyChangedEventArgs( nameof( StatusMarker ) ) );
		}
	}

}