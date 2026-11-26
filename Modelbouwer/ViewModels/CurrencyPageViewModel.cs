using CommunityToolkit.Mvvm.Input;

namespace Modelbouwer.ViewModels;

public partial class CurrencyPageViewModel : EntityPageViewModel<CurrencyModel>
{
	private readonly ICurrencyService _dataService;

	// Collections

	// SelectedCurrency als type-safe alias
	public CurrencyModel? SelectedCurrency
	{
		get => SelectedItem;
		set => SelectedItem = value;
	}

	// Commands
	public IRelayCommand AddCurrencyCommand => AddCommand;
	public IAsyncRelayCommand SaveCurrencyCommand => SaveCommand;
	public IRelayCommand DeleteCurrencyCommand => DeleteCommand;
	public new IRelayCommand ClearSearchCommand => _clearSearchCommand ??= new RelayCommand( () => SearchText = string.Empty );

	private IRelayCommand? _clearSearchCommand;

	// Constructor
	public CurrencyPageViewModel(
		ICurrencyService dataService,
		IEntityValidator<CurrencyModel> validator
	) : base( validator )
	{
		_dataService = dataService;

		ObserveBackgroundTask( ReloadAsync() );
	}

	// Override SelectedItem changed om DefaultCurrency te zetten
	protected override void OnSelectedItemChanged( CurrencyModel? oldValue, CurrencyModel? newValue )
	{
		base.OnSelectedItemChanged( oldValue, newValue );

		if ( newValue == null )
			return;

		OnPropertyChanged( nameof( SelectedCurrency ) );
		OnPropertyChanged( nameof( SelectedCurrency.CurrencyCode ) );
		OnPropertyChanged( nameof( SelectedCurrency.CurrencyName ) );
		OnPropertyChanged( nameof( SelectedCurrency.CurrencyId ) );
		OnPropertyChanged( nameof( SelectedCurrency.CurrencySymbol ) );
		OnPropertyChanged( nameof( SelectedCurrency.CurrencyConversionRate ) );
	}

	// Properties voor UI binding
	public ObservableCollection<CurrencyModel> Currencies => Items;
	public int TotalCurrencyCount => TotalItemCount;
	public int VisibleCurrencyCount
	{
		get => base.VisibleItemCount;
		set => base.VisibleItemCount = value;
	}

	// Filtering
	public bool FilterCurrency( object obj )
	{
		if ( obj is not CurrencyModel currency )
			return false;

		if ( string.IsNullOrWhiteSpace( SearchText ) )
			return true;

		return currency.CurrencyCode?.Contains( SearchText, StringComparison.CurrentCultureIgnoreCase ) == true
			|| currency.CurrencyName?.Contains( SearchText, StringComparison.CurrentCultureIgnoreCase ) == true
			|| currency.CurrencySymbol?.Contains( SearchText, StringComparison.CurrentCultureIgnoreCase ) == true;
	}

	// Abstract overrides voor CRUD
	protected override Task<List<CurrencyModel>> LoadItemsAsync( CancellationToken cancellationToken ) => _dataService.GetAllCurrenciesAsync( cancellationToken );
	protected override Task<int> InsertAsync( CurrencyModel item, CancellationToken cancellationToken ) => _dataService.InsertNewCurrencyAsync( CreateParameters( item ), cancellationToken );
	protected override Task UpdateAsync( CurrencyModel item, CancellationToken cancellationToken ) => _dataService.UpdateCurrencyAsync( CreateParameters( item ), cancellationToken );
	protected override async Task DeleteAsync( CurrencyModel item, CancellationToken cancellationToken )
	{
		if ( item == null )
			return;

		var result = MessageBox.Show(
			$"{Lang.toolbarButtonActionDeleteMessageQuestionPrefix} '{item.CurrencyName}' {Lang.toolbarButtonActionDeleteMessageQuestionSuffix}",
			$"{Lang.toolbarButtonActionDeleteMessageButtonText}",
			MessageBoxButton.YesNo,
			MessageBoxImage.Warning
		);

		if ( result != MessageBoxResult.Yes )
			return;
		try
		{
			await _dataService.DeleteCurrencyAsync( item.CurrencyId, cancellationToken );
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

	protected override int GetId( CurrencyModel item ) => item.CurrencyId;
	protected override void SetId( CurrencyModel item, int id ) => item.CurrencyId = id;

	protected override CurrencyModel CreateNewItem() => new()
	{
		CurrencyId = 0,
		CurrencyCode = string.Empty,
		CurrencyName = string.Empty
	};

	protected override void OnItemsLoaded()
	{
		base.OnItemsLoaded();
		OnPropertyChanged( nameof( TotalCurrencyCount ) );
	}

	// Parameter dictionary voor save
	private static Dictionary<string, object?> CreateParameters( CurrencyModel c ) => new()
	{
		{ $"@{DBNames.CurrencyFieldNameId}", c.CurrencyId },
		{ $"@{DBNames.CurrencyFieldNameCode}", c.CurrencyCode?.Trim().ToUpper() },
		{ $"@{DBNames.CurrencyFieldNameName}", c.CurrencyName?.Trim() },
		{ $"@{DBNames.CurrencyFieldNameRate}", c.CurrencyConversionRate },
		{ $"@{DBNames.CurrencyFieldNameSymbol}", c.CurrencySymbol }
	};
}