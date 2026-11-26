namespace Modelbouwer.UnitTests.ViewModels;

/// <summary>
/// KAN-88 (CB-09 to CB-11): the tree screens for categories, storage locations and work types. They share the same
/// behaviour: the flat list becomes a tree, the filter keeps parents of matching children, and a sub item is saved
/// with its parent while a main item is saved without one.
/// </summary>
[TestClass]
public class TreePageViewModelFlowTests
{
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

	private static Mock<IEntityValidator<T>> ValidValidator<T>()
	{
		var validator = new Mock<IEntityValidator<T>>();
		validator.Setup( v => v.ValidateAsync( It.IsAny<T>(), It.IsAny<CancellationToken>() ) ).ReturnsAsync( new ValidationResult() );
		return validator;
	}

	#region Categories (CB-09)
	private static List<CategoryModel> Categories() =>
	[
		new() { CategoryId = 1, ParentId = null, CategoryName = "Hout" },
		new() { CategoryId = 2, ParentId = 1, CategoryName = "Lindehout" },
		new() { CategoryId = 3, ParentId = 0, CategoryName = "Lijm" }
	];

	private static async Task<(CategoryPageViewModel Vm, Mock<ICategoryService> Service)> CreateCategoriesAsync()
	{
		var service = new Mock<ICategoryService>();
		service.Setup( s => s.GetAllCategorysAsync( It.IsAny<CancellationToken>() ) ).ReturnsAsync( Categories );
		var vm = new CategoryPageViewModel( service.Object, ValidValidator<CategoryModel>().Object );
		await WaitUntilAsync( () => vm.CategoryTree.Count > 0 );
		return (vm, service);
	}

	[TestMethod]
	public async Task Categories_BuildTreeWithMainCategoriesAtTheRoot()
	{
		var (vm, _) = await CreateCategoriesAsync();

		CollectionAssert.AreEquivalent( new [ ] { "Hout", "Lijm" }, vm.CategoryTree.Select( c => c.CategoryName ).ToArray(), "ParentId null and 0 both mean main category." );
		Assert.AreEqual( "Lindehout", vm.CategoryTree.Single( c => c.CategoryId == 1 ).Children.Single().CategoryName );
	}

	[TestMethod]
	public async Task Categories_FilterKeepsParentOfMatchingChild()
	{
		var (vm, _) = await CreateCategoriesAsync();
		var wood = vm.CategoryTree.Single( c => c.CategoryId == 1 );
		var glue = vm.CategoryTree.Single( c => c.CategoryId == 3 );

		vm.SearchText = "linde";

		Assert.IsTrue( vm.FilterRecords( wood ), "The parent stays visible so the child can be reached." );
		Assert.IsFalse( vm.FilterRecords( glue ) );
		Assert.IsTrue( vm.FilterCategory( wood.Children [ 0 ] ) );
	}

	[TestMethod]
	public async Task Categories_SubCategoryIsSavedWithItsParent()
	{
		var (vm, service) = await CreateCategoriesAsync();
		service.Setup( s => s.InsertNewCategoryAsync( It.IsAny<Dictionary<string, object?>>(), It.IsAny<CancellationToken>() ) ).ReturnsAsync( 10 );
		vm.SelectedItem = vm.Items.Single( c => c.CategoryId == 1 );

		vm.AddSubCategoryCommand.Execute( null );
		vm.SelectedItem!.CategoryName = " Balsahout ";
		await vm.SaveCategoryCommand.ExecuteAsync( null );

		service.Verify( s => s.InsertNewCategoryAsync( It.Is<Dictionary<string, object?>>( p =>
			Equals( p [ $"@{DBNames.CategoryFieldNameParentId}" ], 1 ) &&
			Equals( p [ $"@{DBNames.CategoryFieldNameName}" ], "Balsahout" ) ), It.IsAny<CancellationToken>() ), Times.Once );
	}

	[TestMethod]
	public async Task Categories_MainCategoryIsSavedWithoutParent()
	{
		var (vm, service) = await CreateCategoriesAsync();
		service.Setup( s => s.InsertNewCategoryAsync( It.IsAny<Dictionary<string, object?>>(), It.IsAny<CancellationToken>() ) ).ReturnsAsync( 11 );

		vm.AddCategoryCommand.Execute( null );
		vm.SelectedItem!.CategoryName = "Verf";
		await vm.SaveCategoryCommand.ExecuteAsync( null );

		service.Verify( s => s.InsertNewCategoryAsync( It.Is<Dictionary<string, object?>>( p => p [ $"@{DBNames.CategoryFieldNameParentId}" ] == null ), It.IsAny<CancellationToken>() ), Times.Once );
	}

	[TestMethod]
	public async Task Categories_AddSubNeedsASelection()
	{
		var (vm, _) = await CreateCategoriesAsync();

		vm.SelectedItem = null;

		Assert.IsFalse( vm.AddSubCategoryCommand.CanExecute( null ) );
	}
	#endregion

	#region Storage locations (CB-10)
	private static async Task<(StorageLocationPageViewModel Vm, Mock<IStorageLocationService> Service)> CreateLocationsAsync()
	{
		var service = new Mock<IStorageLocationService>();
		service.Setup( s => s.GetAllStorageLocationsAsync( It.IsAny<CancellationToken>() ) ).ReturnsAsync( () =>
		[
			new() { StorageId = 1, ParentId = 0, StorageName = "Werkplaats" },
			new() { StorageId = 2, ParentId = 1, StorageName = "Kast A" },
			new() { StorageId = 3, ParentId = 2, StorageName = "Lade 3" }
		] );
		var vm = new StorageLocationPageViewModel( service.Object, ValidValidator<StorageLocationModel>().Object );
		await WaitUntilAsync( () => vm.StorageLocationTree.Count > 0 );
		return (vm, service);
	}

	[TestMethod]
	public async Task Locations_BuildNestedTreeAndFilterThroughLevels()
	{
		var (vm, _) = await CreateLocationsAsync();
		var root = vm.StorageLocationTree.Single();

		Assert.AreEqual( "Lade 3", root.Children.Single().Children.Single().StorageName );

		vm.SearchText = "lade";
		Assert.IsTrue( vm.FilterRecords( root ), "A match two levels down keeps the top level visible." );
		vm.SearchText = "zolder";
		Assert.IsFalse( vm.FilterRecords( root ) );
	}

	[TestMethod]
	public async Task Locations_SubLocationIsSavedWithItsParent()
	{
		var (vm, service) = await CreateLocationsAsync();
		service.Setup( s => s.InsertNewStorageLocationAsync( It.IsAny<Dictionary<string, object?>>(), It.IsAny<CancellationToken>() ) ).ReturnsAsync( 4 );
		vm.SelectedItem = vm.Items.Single( l => l.StorageId == 2 );

		vm.AddSubStorageLocationCommand.Execute( null );
		vm.SelectedItem!.StorageName = "Lade 4";
		await vm.SaveStorageLocationCommand.ExecuteAsync( null );

		service.Verify( s => s.InsertNewStorageLocationAsync( It.Is<Dictionary<string, object?>>( p => Equals( p [ $"@{DBNames.StorageFieldNameParentId}" ], 2 ) ), It.IsAny<CancellationToken>() ), Times.Once );
	}

	[TestMethod]
	public async Task Locations_UpdateSendsIdParentAndName()
	{
		var (vm, service) = await CreateLocationsAsync();
		service.Setup( s => s.UpdateStorageLocationAsync( It.IsAny<Dictionary<string, object?>>(), It.IsAny<CancellationToken>() ) ).Returns( Task.CompletedTask );
		vm.SelectedItem = vm.Items.Single( l => l.StorageId == 3 );

		vm.SelectedItem.StorageName = "Lade 3 (lijm)";
		await vm.SaveStorageLocationCommand.ExecuteAsync( null );

		service.Verify( s => s.UpdateStorageLocationAsync( It.Is<Dictionary<string, object?>>( p =>
			Equals( p [ $"@{DBNames.StorageFieldNameId}" ], 3 ) &&
			Equals( p [ $"@{DBNames.StorageFieldNameParentId}" ], 2 ) &&
			Equals( p [ $"@{DBNames.StorageFieldNameName}" ], "Lade 3 (lijm)" ) ), It.IsAny<CancellationToken>() ), Times.Once );
	}
	#endregion

	#region Work types (CB-11)
	private static async Task<(WorkTypePageViewModel Vm, Mock<IWorkTypeService> Service)> CreateWorkTypesAsync()
	{
		var service = new Mock<IWorkTypeService>();
		service.Setup( s => s.GetAllWorkTypesAsync( It.IsAny<CancellationToken>() ) ).ReturnsAsync( () =>
		[
			new() { WorktypeId = 1, ParentId = null, WorktypeName = "Romp" },
			new() { WorktypeId = 2, ParentId = 1, WorktypeName = "Gangen leggen" },
			new() { WorktypeId = 3, ParentId = null, WorktypeName = "Tuigage" }
		] );
		var vm = new WorkTypePageViewModel( service.Object, ValidValidator<WorkTypeModel>().Object );
		await WaitUntilAsync( () => vm.WorkTypeTree.Count > 0 );
		return (vm, service);
	}

	[TestMethod]
	public async Task WorkTypes_BuildTreeAndFilter()
	{
		var (vm, _) = await CreateWorkTypesAsync();

		Assert.HasCount( 2, vm.WorkTypeTree );
		var hull = vm.WorkTypeTree.Single( w => w.WorktypeId == 1 );
		vm.SearchText = "gangen";
		Assert.IsTrue( vm.FilterRecords( hull ) );
		Assert.IsFalse( vm.FilterRecords( vm.WorkTypeTree.Single( w => w.WorktypeId == 3 ) ) );
	}

	[TestMethod]
	public async Task WorkTypes_SubWorkTypeIsSavedWithItsParent()
	{
		var (vm, service) = await CreateWorkTypesAsync();
		service.Setup( s => s.InsertNewWorkTypeAsync( It.IsAny<Dictionary<string, object?>>(), It.IsAny<CancellationToken>() ) ).ReturnsAsync( 4 );
		vm.SelectedItem = vm.Items.Single( w => w.WorktypeId == 1 );

		vm.AddSubWorkTypeCommand.Execute( null );
		vm.SelectedItem!.WorktypeName = "Kielbalk";
		await vm.SaveWorkTypeCommand.ExecuteAsync( null );

		service.Verify( s => s.InsertNewWorkTypeAsync( It.Is<Dictionary<string, object?>>( p =>
			Equals( p [ $"@{DBNames.WorkTypeFieldNameParentId}" ], 1 ) &&
			Equals( p [ $"@{DBNames.WorkTypeFieldNameName}" ], "Kielbalk" ) ), It.IsAny<CancellationToken>() ), Times.Once );
	}
	#endregion
}
