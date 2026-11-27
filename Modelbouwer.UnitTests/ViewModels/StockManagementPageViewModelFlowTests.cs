namespace Modelbouwer.UnitTests.ViewModels;

/// <summary>KAN-88 (CB-08): stock management, where editing a grid cell writes a correction straight away.</summary>
[TestClass]
public class StockManagementPageViewModelFlowTests
{
	private Mock<IStockService> _service = null!;
	private int _loads;

	[TestInitialize]
	public void Setup()
	{
		_loads = 0;
		_service = new Mock<IStockService>();
		_service.Setup( s => s.GetCompleteInventoryAsync( It.IsAny<CancellationToken>() ) ).ReturnsAsync( () =>
		{
			_loads++;
			return
			[
				Item( 1, "Hout", "H-01", "Lindehout 2 mm", "Kast A", 5 ),
				Item( 7, "Lijm", "L-07", "Secondenlijm dun", "Lade 3", 2 )
			];
		} );
		_service.Setup( s => s.InsertCorrectionAsync( It.IsAny<Dictionary<string, object?>>() ) ).ReturnsAsync( 1 );
		_service.Setup( s => s.UpdateProductCorrectionAsync( It.IsAny<Dictionary<string, object?>>() ) ).ReturnsAsync( 1 );
	}

	private static StockManagementModel Item( int id, string category, string code, string name, string location, double inventory ) => new()
	{
		ProductId = id, ProductCategory = category, ProductCode = code, ProductName = name, ProductStorageLocation = location,
		ProductInventory = inventory, ProductOriginalInventory = inventory, ProductMinimalStock = 1, ProductPrice = 2.5
	};

	private async Task<StockManagementPageViewModel> CreateAsync()
	{
		var vm = new StockManagementPageViewModel( _service.Object, new Mock<IEntityValidator<StockManagementModel>>().Object, new SettingsService() );
		await WaitUntilAsync( () => vm.Items.Count == 2 );
		return vm;
	}

	[TestMethod]
	public async Task Filter_SearchesCategoryCodeNameAndLocation()
	{
		var vm = await CreateAsync();
		var wood = vm.Items [ 0 ];
		var glue = vm.Items [ 1 ];

		foreach ( var search in new [ ] { "hout", "H-01", "LINDE", "kast" } )
		{
			vm.SearchText = search;
			Assert.IsTrue( vm.FilterInventory( wood ), search );
			Assert.IsFalse( vm.FilterInventory( glue ), search );
		}

		vm.SearchText = string.Empty;
		Assert.IsTrue( vm.FilterInventory( glue ) );
		Assert.IsFalse( vm.FilterInventory( "no product" ) );
	}

	[TestMethod]
	public async Task EditingInventory_WritesTheDifferenceAsCorrection()
	{
		var vm = await CreateAsync();
		var glue = vm.Items.Single( i => i.ProductId == 7 );

		glue.ProductInventory = 5;
		await WaitUntilAsync( () => _loads == 2 );

		_service.Verify( s => s.InsertCorrectionAsync( It.Is<Dictionary<string, object?>>( p =>
			Equals( p [ $"@{DBNames.StockLogFieldNameProductId}" ], 7 ) &&
			Equals( p [ $"@{DBNames.StockLogFieldNameAmountCorrection}" ], 3d ) ) ), Times.Once );
		_service.Verify( s => s.UpdateProductCorrectionAsync( It.Is<Dictionary<string, object?>>( p => Equals( p [ $"@{DBNames.ProductFieldNameId}" ], 7 ) ) ), Times.Once );
		Assert.AreEqual( 7, vm.SelectedItem?.ProductId, "The edited product stays selected after the reload." );
	}

	[TestMethod]
	public async Task EditingPriceOnly_UpdatesTheProductWithoutCorrection()
	{
		var vm = await CreateAsync();
		var wood = vm.Items.Single( i => i.ProductId == 1 );

		wood.ProductPrice = 3.75;
		await WaitUntilAsync( () => _loads == 2 );

		_service.Verify( s => s.InsertCorrectionAsync( It.IsAny<Dictionary<string, object?>>() ), Times.Never, "The stock did not change." );
		_service.Verify( s => s.UpdateProductCorrectionAsync( It.Is<Dictionary<string, object?>>( p =>
			Equals( p [ $"@{DBNames.ProductFieldNamePrice}" ], 3.75 ) &&
			Equals( p [ $"@{DBNames.ProductFieldNameMinimalStock}" ], 1d ) ) ), Times.Once );
	}

	[TestMethod]
	public async Task EditingOtherColumns_WritesNothing()
	{
		var vm = await CreateAsync();

		vm.Items [ 0 ].ProductName = "Lindehout 3 mm";
		await Task.Delay( 50 );

		_service.Verify( s => s.UpdateProductCorrectionAsync( It.IsAny<Dictionary<string, object?>>() ), Times.Never );
		Assert.AreEqual( 1, _loads );
	}

	[TestMethod]
	public async Task GridLayoutCommands_CallTheViewActions()
	{
		var vm = await CreateAsync();
		int saved = 0, reset = 0;
		vm.SaveGridLayoutAction = () => saved++;
		vm.ResetGridLayoutAction = () => reset++;

		vm.DataGridSaveSettingsCommand.Execute( null );
		vm.DataGridResetSettingsCommand.Execute( null );

		Assert.AreEqual( 1, saved );
		Assert.AreEqual( 1, reset );
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
