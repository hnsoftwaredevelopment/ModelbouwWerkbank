using System.Windows.Markup;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Modelbouwer.Validators;

namespace Modelbouwer;

public partial class App : Application
{
	private readonly IHost _host;

	/// <summary>Cancelled when the application exits, so start-up work still running stops (KAN-92).</summary>
	private readonly CancellationTokenSource _appLifetime = new();

	/// <summary>How long the host may take to stop its services when the application exits.</summary>
	private static readonly TimeSpan HostStopTimeout = TimeSpan.FromSeconds( 5 );

	public App()
	{
		SetCurrentCulture( "nl-NL" );

		_host = Host.CreateDefaultBuilder()
		   .ConfigureServices( ConfigureServices )
		   .Build();
	}

	private void ConfigureServices( IServiceCollection services )
	{
		// Register Syncfusion license (key is kept outside the repository, see SyncfusionLicenseKeyResolver)
		var syncfusionLicenseKey = SyncfusionLicenseKeyResolver.ResolveFromEnvironmentAndLocalFiles();
		if ( !string.IsNullOrWhiteSpace( syncfusionLicenseKey ) )
			Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense( syncfusionLicenseKey );

		// Register services
		services.AddSingleton<BrandService>();
		services.AddSingleton<CategoryService>();
		services.AddSingleton<ContactService>();
		services.AddSingleton<ContactTypeService>();
		services.AddSingleton<CountryService>();
		services.AddSingleton<CurrencyService>();
		services.AddSingleton<GenericDataService>();
		services.AddSingleton<GitHubReleaseHistoryService>();
		services.AddSingleton<ProductService>();
		services.AddSingleton<ProjectService>();
		services.AddSingleton<SettingsService>();
		services.AddSingleton<StockService>();
		services.AddSingleton<StockOrderService>();
		services.AddSingleton<StorageLocationService>();
		services.AddSingleton<SupplierService>();
		services.AddSingleton<TimeRegistrationService>();
		services.AddSingleton<UnitService>();
		services.AddSingleton<WorkTypeService>();

		// Register ViewModels
		services.AddTransient<BrandPageViewModel>();
		services.AddTransient<AboutPageViewModel>();
		services.AddTransient<CategoryPageViewModel>();
		services.AddTransient<ContactTypePageViewModel>();
		services.AddTransient<CountryPageViewModel>();
		services.AddTransient<CurrencyPageViewModel>();
		services.AddTransient<ProductPageViewModel>();
		services.AddTransient<ProjectPageViewModel>();
		services.AddTransient<ProjectReportsViewModel>();
		services.AddTransient<SettingsPageViewModel>();
		services.AddTransient<StockManagementPageViewModel>();
		services.AddTransient<StockOrderViewModel>();
		services.AddTransient<StockReceiptViewModel>();
		services.AddTransient<StorageLocationPageViewModel>();
		services.AddTransient<SupplierPageViewModel>();
		services.AddTransient<TimeRegistrationViewModel>();
		services.AddTransient<UnitPageViewModel>();
		services.AddTransient<WorkTypePageViewModel>();

		// Register NavigationViewModel
		services.AddSingleton<NavigationViewModel>();

		// Register Views
		services.AddTransient<BrandView>();
		services.AddTransient<AboutView>();
		services.AddTransient<CategoryView>();
		services.AddTransient<ContactTypeView>();
		services.AddTransient<CountryView>();
		services.AddTransient<CurrencyView>();
		services.AddTransient<ProductView>();
		services.AddTransient<ProjectView>();
		services.AddTransient<ProjectReportsView>();
		services.AddTransient<SettingsView>();
		services.AddTransient<StockManagementView>();
		services.AddTransient<StockOrderView>();
		services.AddTransient<StockReceiptView>();
		services.AddTransient<StorageLocationView>();
		services.AddTransient<SupplierView>();
		services.AddTransient<TimeRegistrationView>();
		services.AddTransient<UnitView>();
		services.AddTransient<WorkTypeView>();

		// Register MainWindow
		services.AddSingleton<MainWindow>();

		// Export services
		services.AddSingleton<ILanguageProvider, ResourceLanguageProvider>();
		services.AddSingleton<IExportService, CsvExportService>();
		services.AddSingleton<IExportService, ExcelExportService>();
		services.AddSingleton<CsvExportService>();
		services.AddSingleton<ExcelExportService>();
		services.AddSingleton<IExportService>( provider => provider.GetRequiredService<CsvExportService>() );

		services.AddScoped<IBrandService, BrandService>();
		services.AddScoped<ICategoryService, CategoryService>();
		services.AddScoped<IContactService, ContactService>();
		services.AddScoped<IContactTypeService, ContactTypeService>();
		services.AddScoped<ICountryService, CountryService>();
		services.AddScoped<ICurrencyService, CurrencyService>();
		services.AddSingleton<IGitHubReleaseHistoryService>( provider => provider.GetRequiredService<GitHubReleaseHistoryService>() );
		services.AddScoped<IProductService, ProductService>();
		services.AddScoped<IProjectService, ProjectService>();
		services.AddScoped<IStockService, StockService>();
		services.AddScoped<IStockOrderService, StockOrderService>();
		services.AddScoped<IStorageLocationService, StorageLocationService>();
		services.AddScoped<ISupplierService, SupplierService>();
		services.AddSingleton<ISettingsService>( provider => provider.GetRequiredService<SettingsService>() );
		services.AddScoped<ITimeRegistrationService, TimeRegistrationService>();
		services.AddScoped<IUnitService, UnitService>();
		services.AddScoped<IWorkTypeService, WorkTypeService>();

		services.AddScoped<IEntityValidator<BrandModel>, BrandValidator>();
		services.AddScoped<IEntityValidator<CategoryModel>, CategoryValidator>();
		services.AddScoped<IEntityValidator<ContactTypeModel>, ContactTypeValidator>();
		services.AddScoped<IEntityValidator<CountryModel>, CountryValidator>();
		services.AddScoped<IEntityValidator<CurrencyModel>, CurrencyValidator>();
		services.AddScoped<IEntityValidator<ProductModel>, ProductValidator>();
		services.AddScoped<IEntityValidator<ProjectModel>, ProjectValidator>();
		services.AddScoped<IEntityValidator<StockManagementModel>, StockManagementValidator>();
		services.AddScoped<IEntityValidator<StorageLocationModel>, StorageLocationValidator>();
		services.AddScoped<IEntityValidator<SupplierModel>, SupplierValidator>();
		services.AddScoped<IEntityValidator<SupplierContactModel>, ContactValidator>();
		services.AddScoped<IEntityValidator<UnitModel>, UnitValidator>();
		services.AddScoped<IEntityValidator<WorkTypeModel>, WorkTypeValidator>();
	}

	protected override async void OnStartup( StartupEventArgs e )
	{
		if ( !DBConnect.IsPasswordConfigured )
		{
			MessageBox.Show(
				string.Format( Lang.StartupDatabasePasswordMissingMessage, DBConnect.PasswordVariable ),
				Lang.generalMessageboxWarningTitle, MessageBoxButton.OK, MessageBoxImage.Error );
			Shutdown( -1 );
			return;
		}

		try
		{
			await _host.StartAsync( _appLifetime.Token );

			var settingsService = _host.Services.GetRequiredService<SettingsService>();
			await settingsService.LoadSettingsAsync( _appLifetime.Token );
			ApplyCulture( settingsService.Settings.Culture, settingsService.Settings.Language );

			_ = _host.Services.GetRequiredService<NavigationViewModel>();
			var mainWindow = _host.Services.GetRequiredService<MainWindow>();
			mainWindow.Show();

			base.OnStartup( e );
		}
		catch ( Exception ex )
		{
			MessageBox.Show( ex.Message, Lang.generalMessageboxWarningTitle, MessageBoxButton.OK, MessageBoxImage.Error );
			Shutdown( -1 );
		}
	}

	protected override async void OnExit( ExitEventArgs e )
	{
		try
		{
			_appLifetime.Cancel();
			using var stopTimeout = new CancellationTokenSource( HostStopTimeout );
			await _host.StopAsync( stopTimeout.Token );
		}
		catch ( Exception ex )
		{
			Debug.WriteLine( $"Error while stopping host: {ex}" );
		}
		finally
		{
			_host.Dispose();
			_appLifetime.Dispose();
			base.OnExit( e );
		}
	}

	public static T GetService<T>() where T : class
	{
		if ( ( Current as App )!._host.Services.GetService( typeof( T ) ) is not T service )
		{
			throw new ArgumentException( $"{typeof( T )} needs to be registered in ConfigureServices within App.xaml.cs." );
		}

		return service;
	}

	public static void ApplyCulture( string? cultureName, string? language = null )
	{
		SetCurrentCulture( cultureName, language );
		UseCultureForWpfFormatting( CultureInfo.CurrentCulture );
	}

	#region Number and date format in bindings (KAN-147)
	private static XmlLanguage _wpfLanguage = XmlLanguage.GetLanguage( "nl-NL" );
	private static bool _wpfLanguageHooked;

	/// <summary>
	/// Makes WPF format numbers and dates in bindings (StringFormat) with the culture of the settings. WPF does not
	/// use CurrentCulture for that but the Language of each element, which is en-US unless set, so totals showed
	/// 376.25 instead of 376,25. Every window gets the Language as its own value, so all its elements inherit it,
	/// including the Runs inside a TextBlock. Open windows are updated too, so a culture chosen in the settings
	/// applies at once.
	/// </summary>
	/// <remarks>
	/// Do not also change the default with LanguageProperty.OverrideMetadata: a TextBlock then already has the
	/// language by default, the window's value changes nothing for it, and the Runs inside it keep formatting with
	/// en-US (tried and seen in the app, KAN-147).
	/// </remarks>
	internal static void UseCultureForWpfFormatting( CultureInfo culture )
	{
		_wpfLanguage = XmlLanguage.GetLanguage( culture.IetfLanguageTag );

		if ( !_wpfLanguageHooked )
		{
			_wpfLanguageHooked = true;
			EventManager.RegisterClassHandler( typeof( Window ), FrameworkElement.LoadedEvent, new RoutedEventHandler( ( sender, _ ) =>
			{
				if ( sender is Window window )
					window.Language = _wpfLanguage;
			} ) );
		}

		if ( Current == null )
			return;

		foreach ( Window window in Current.Windows )
			window.Language = _wpfLanguage;
	}
	#endregion

	private static void SetCurrentCulture( string? cultureName, string? language = null )
	{
		CultureInfo culture;
		CultureInfo uiCulture;
		try
		{
			culture = new CultureInfo( string.IsNullOrWhiteSpace( cultureName ) ? "nl-NL" : cultureName );
		}
		catch ( CultureNotFoundException )
		{
			culture = new CultureInfo( "nl-NL" );
		}

		try
		{
			uiCulture = new CultureInfo( GetLanguageCulture( language ) );
		}
		catch ( CultureNotFoundException )
		{
			uiCulture = new CultureInfo( "nl-NL" );
		}

		CultureInfo.DefaultThreadCurrentCulture = culture;
		CultureInfo.DefaultThreadCurrentUICulture = uiCulture;
		Thread.CurrentThread.CurrentCulture = culture;
		Thread.CurrentThread.CurrentUICulture = uiCulture;
		Lang.Culture = uiCulture;
	}

	private static string GetLanguageCulture( string? language ) =>
		language?.ToUpperInvariant() switch
		{
			"EN" => "en-US",
			"DE" => "de",
			_ => "nl-NL"
		};

}