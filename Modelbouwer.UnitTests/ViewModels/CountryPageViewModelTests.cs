namespace Modelbouwer.UnitTests.ViewModels;

[TestClass]
public class CountryPageViewModelTests
{
	[TestMethod]
	public async Task Constructor_LoadsCountriesAndCurrenciesOnce()
	{
		var countryService = new Mock<ICountryService>();
		var currencyService = new Mock<ICurrencyService>();
		countryService.Setup( service => service.GetAllCountriesAsync( It.IsAny<CancellationToken>() ) ).ReturnsAsync( [ ] );
		currencyService.Setup( service => service.GetAllCurrenciesAsync() ).ReturnsAsync( [ ] );

		_ = CreateViewModel( countryService.Object, currencyService.Object );

		await Task.Delay( 100 );
		countryService.Verify( service => service.GetAllCountriesAsync( It.IsAny<CancellationToken>() ), Times.Once );
		currencyService.Verify( service => service.GetAllCurrenciesAsync(), Times.Once );
	}

	[TestMethod]
	public async Task Constructor_WhenCurrencyLoadFails_StoresAsyncError()
	{
		var expected = new InvalidOperationException( "Unable to load currencies." );
		var currencyService = new Mock<ICurrencyService>();
		currencyService
			.Setup( service => service.GetAllCurrenciesAsync() )
			.Returns( Task.FromException<List<CurrencyModel>>( expected ) );

		var viewModel = CreateViewModel( currencyService: currencyService.Object );

		await WaitUntilAsync( () => ReferenceEquals( expected, viewModel.LastAsyncError ) );
		Assert.AreSame( expected, viewModel.LastAsyncError );
	}

	[TestMethod]
	public async Task Constructor_WhenCountryLoadFails_StoresAsyncError()
	{
		var expected = new InvalidOperationException( "Unable to load countries." );
		var countryService = new Mock<ICountryService>();
		countryService
			.Setup( service => service.GetAllCountriesAsync( It.IsAny<CancellationToken>() ) )
			.Returns( Task.FromException<List<CountryModel>>( expected ) );

		var viewModel = CreateViewModel( countryService.Object );

		await WaitUntilAsync( () => ReferenceEquals( expected, viewModel.LastAsyncError ) );
		Assert.AreSame( expected, viewModel.LastAsyncError );
	}

	[TestMethod]
	public async Task OpeningTheScreen_HasNoUnsavedChanges()
	{
		// KAN-148: selecting the first country linked its currency after the screen started watching it.
		var countryService = new Mock<ICountryService>();
		var currencyService = new Mock<ICurrencyService>();
		countryService.Setup( service => service.GetAllCountriesAsync( It.IsAny<CancellationToken>() ) ).ReturnsAsync(
		[
			new CountryModel { CountryId = 1, CountryCode = "NL", CountryName = "Nederland", CountryCurrencyId = 2, CountryCurrencySymbol = "€" }
		] );
		currencyService.Setup( service => service.GetAllCurrenciesAsync() ).ReturnsAsync(
		[
			new CurrencyModel { CurrencyId = 1, CurrencySymbol = "$" },
			new CurrencyModel { CurrencyId = 2, CurrencySymbol = "€" }
		] );

		var viewModel = CreateViewModel( countryService.Object, currencyService.Object );
		await WaitUntilAsync( () => viewModel.SelectedCountry != null );

		Assert.AreEqual( 2, viewModel.SelectedCountry!.DefaultCurrency?.CurrencyId, "The stored currency is linked." );
		Assert.IsFalse( viewModel.HasUnsavedChanges );
		Assert.IsFalse( viewModel.SaveCountryCommand.CanExecute( null ) );

		viewModel.SelectedCountry.CountryName = "Nederland (NL)";
		Assert.IsTrue( viewModel.HasUnsavedChanges, "A change by the user still counts." );
	}

	[TestMethod]
	public async Task CountryWithoutCurrency_ProposedCurrencyShowsAsUnsavedChange()
	{
		// Found by CodeRabbit in PR #61: a currency chosen for a country without one must not count as stored.
		var countryService = new Mock<ICountryService>();
		var currencyService = new Mock<ICurrencyService>();
		countryService.Setup( service => service.GetAllCountriesAsync( It.IsAny<CancellationToken>() ) ).ReturnsAsync(
		[
			new CountryModel { CountryId = 1, CountryCode = "XX", CountryName = "Zonder valuta", CountryCurrencyId = 0 }
		] );
		currencyService.Setup( service => service.GetAllCurrenciesAsync() ).ReturnsAsync(
		[
			new CurrencyModel { CurrencyId = 1, CurrencySymbol = "$" }
		] );

		var viewModel = CreateViewModel( countryService.Object, currencyService.Object );
		await WaitUntilAsync( () => viewModel.SelectedCountry != null );

		Assert.AreEqual( 1, viewModel.SelectedCountry!.CountryCurrencyId, "The first currency is proposed." );
		Assert.IsTrue( viewModel.HasUnsavedChanges, "The proposal changed the record, so it can be saved or reloaded." );
	}

	private static CountryPageViewModel CreateViewModel( ICountryService? countryService = null, ICurrencyService? currencyService = null )
	{
		var defaultCountryService = new Mock<ICountryService>();
		var defaultCurrencyService = new Mock<ICurrencyService>();
		var validator = new Mock<IEntityValidator<CountryModel>>();

		defaultCountryService.Setup( service => service.GetAllCountriesAsync( It.IsAny<CancellationToken>() ) ).ReturnsAsync( [ ] );
		defaultCurrencyService.Setup( service => service.GetAllCurrenciesAsync() ).ReturnsAsync( [ ] );

		return new CountryPageViewModel(
			countryService ?? defaultCountryService.Object,
			currencyService ?? defaultCurrencyService.Object,
			validator.Object );
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