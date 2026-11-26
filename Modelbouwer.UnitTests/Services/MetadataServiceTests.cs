using static Modelbouwer.UnitTests.Services.ServiceQueryProbe;

namespace Modelbouwer.UnitTests.Services;

/// <summary>
/// KAN-88 (CB-02 to CB-06): Country, Category, StorageLocation, WorkType and Currency services. Each test runs the
/// service against a probe of <see cref="GenericDataService"/>: the mapping is checked with a real data reader, the
/// parameters with the query text they are used in.
/// </summary>
[TestClass]
public class MetadataServiceTests
{
	private static Dictionary<string, object?> P( params (string Key, object? Value) [ ] values ) => values.ToDictionary( v => "@" + v.Key, v => v.Value );

	#region CountryService (CB-02)
	[TestMethod]
	public async Task Country_GetAll_MapsAllColumns()
	{
		var probe = new ServiceQueryProbe();
		probe.SetupQuery<CountryModel>( Table(
			(DBNames.CountryFieldNameId, 3), (DBNames.CountryFieldNameCode, "NL"), (DBNames.CountryFieldNameName, "Nederland"),
			(DBNames.CountryFieldNameCurrencyId, 1), (DBNames.CountryFieldNameCurrencySymbol, "€") ) );

		var country = ( await new CountryService( probe.DataService.Object ).GetAllCountriesAsync() ).Single();

		Assert.AreEqual( 3, country.CountryId );
		Assert.AreEqual( "NL", country.CountryCode );
		Assert.AreEqual( "Nederland", country.CountryName );
		Assert.AreEqual( 1, country.CountryCurrencyId );
		Assert.AreEqual( "€", country.CountryCurrencySymbol );
	}

	[TestMethod]
	public async Task Country_InsertUpdateDelete_SupplyEveryQueryParameter()
	{
		var probe = new ServiceQueryProbe { ScalarUInt = 12 };
		var service = new CountryService( probe.DataService.Object );
		var values = P( (DBNames.CountryFieldNameId, 12), (DBNames.CountryFieldNameCode, "BE"), (DBNames.CountryFieldNameName, "België"),
			(DBNames.CountryFieldNameCurrencyId, 1), (DBNames.CountryFieldNameCurrencySymbol, "€") );

		Assert.AreEqual( 12, await service.InsertNewCountryAsync( values ) );
		await service.UpdateCountryAsync( values );
		await service.DeleteCountryAsync( 12 );

		probe.AssertAllCallsWellFormed();
		Assert.AreEqual( "België", probe.Calls [ 0 ].Parameters [ $"@{DBNames.CountryFieldNameName}" ] );
		Assert.AreEqual( 12, probe.Calls [ 1 ].Parameters [ $"@{DBNames.CountryFieldNameId}" ] );
		Assert.AreEqual( 12, probe.Calls [ 2 ].Parameters [ $"@{DBNames.CountryFieldNameId}" ] );
	}

	[TestMethod]
	public async Task Country_ExistenceChecks_SupplyEveryQueryParameter()
	{
		var probe = new ServiceQueryProbe { ScalarInt = 1 };
		var service = new CountryService( probe.DataService.Object );

		Assert.IsTrue( await service.CodeExistsAsync( " nl " ) );
		Assert.IsTrue( await service.NameExistsAsync( "Nederland" ) );
		Assert.IsFalse( await service.CodeExistsAsync( "  " ), "Empty input never reaches the database." );

		probe.AssertAllCallsWellFormed();
		Assert.HasCount( 2, probe.Calls );
		Assert.AreEqual( "nl", probe.Calls [ 0 ].Parameters [ $"@{DBNames.CountryFieldNameCode}" ], "Input is trimmed." );
	}
	#endregion

	#region CategoryService (CB-03)
	[TestMethod]
	public async Task Category_GetAll_MapsAllColumns()
	{
		var probe = new ServiceQueryProbe();
		probe.SetupQuery<CategoryModel>( Table( (DBNames.CategoryFieldNameId, 5), (DBNames.CategoryFieldNameParentId, 2), (DBNames.CategoryFieldNameName, "Hout") ) );

		var category = ( await new CategoryService( probe.DataService.Object ).GetAllCategorysAsync() ).Single();

		Assert.AreEqual( 5, category.CategoryId );
		Assert.AreEqual( 2, category.ParentId );
		Assert.AreEqual( "Hout", category.CategoryName );
	}

	[TestMethod]
	public async Task Category_CrudAndChecks_SupplyEveryQueryParameter()
	{
		var probe = new ServiceQueryProbe { ScalarUInt = 5 };
		var service = new CategoryService( probe.DataService.Object );
		var values = P( (DBNames.CategoryFieldNameId, 5), (DBNames.CategoryFieldNameParentId, 2), (DBNames.CategoryFieldNameName, "Hout") );

		Assert.AreEqual( 5, await service.InsertNewCategoryAsync( values ) );
		await service.UpdateCategoryAsync( values );
		await service.DeleteCategoryAsync( 5 );
		await service.NameExistsAsync( "Hout", 2 );
		await service.NameExistsAsync( "Hout", null );

		probe.AssertAllCallsWellFormed();
		Assert.AreEqual( DBNull.Value, probe.Calls [ 4 ].Parameters [ $"@{DBNames.CategoryFieldNameParentId}" ], "A main category has no parent." );
	}
	#endregion

	#region StorageLocationService (CB-04)
	[TestMethod]
	public async Task StorageLocation_GetAll_MapsAllColumns()
	{
		var probe = new ServiceQueryProbe();
		probe.SetupQuery<StorageLocationModel>( Table( (DBNames.StorageFieldNameId, 8), (DBNames.StorageFieldNameParentId, 1), (DBNames.StorageFieldNameName, "Kast A") ) );

		var location = ( await new StorageLocationService( probe.DataService.Object ).GetAllStorageLocationsAsync() ).Single();

		Assert.AreEqual( 8, location.StorageId );
		Assert.AreEqual( 1, location.ParentId );
		Assert.AreEqual( "Kast A", location.StorageName );
	}

	[TestMethod]
	public async Task StorageLocation_CrudAndChecks_SupplyEveryQueryParameter()
	{
		var probe = new ServiceQueryProbe { ScalarUInt = 8 };
		var service = new StorageLocationService( probe.DataService.Object );
		var values = P( (DBNames.StorageFieldNameId, 8), (DBNames.StorageFieldNameParentId, 1), (DBNames.StorageFieldNameName, "Kast A") );

		Assert.AreEqual( 8, await service.InsertNewStorageLocationAsync( values ) );
		await service.UpdateStorageLocationAsync( values );
		await service.DeleteStorageLocationAsync( 8 );
		await service.NameExistsAsync( "Kast A", 1 );

		probe.AssertAllCallsWellFormed();
	}

	[TestMethod]
	public async Task StorageLocation_InsertWithoutName_SendsDbNull()
	{
		var probe = new ServiceQueryProbe();
		var service = new StorageLocationService( probe.DataService.Object );

		await service.InsertNewStorageLocationAsync( P( (DBNames.StorageFieldNameParentId, null), (DBNames.StorageFieldNameName, null) ) );

		Assert.AreEqual( DBNull.Value, probe.Single().Parameters [ $"@{DBNames.StorageFieldNameName}" ] );
	}
	#endregion

	#region WorkTypeService (CB-05)
	[TestMethod]
	public async Task WorkType_GetAll_MapsAllColumns()
	{
		var probe = new ServiceQueryProbe();
		probe.SetupQuery<WorkTypeModel>( Table( (DBNames.WorkTypeFieldNameId, 4), (DBNames.WorkTypeFieldNameParentId, 0), (DBNames.WorkTypeFieldNameName, "Zagen") ) );

		var workType = ( await new WorkTypeService( probe.DataService.Object ).GetAllWorkTypesAsync() ).Single();

		Assert.AreEqual( 4, workType.WorktypeId );
		Assert.AreEqual( 0, workType.ParentId );
		Assert.AreEqual( "Zagen", workType.WorktypeName );
	}

	[TestMethod]
	public async Task WorkType_CrudAndChecks_SupplyEveryQueryParameter()
	{
		var probe = new ServiceQueryProbe { ScalarUInt = 4 };
		var service = new WorkTypeService( probe.DataService.Object );
		var values = P( (DBNames.WorkTypeFieldNameId, 4), (DBNames.WorkTypeFieldNameParentId, 0), (DBNames.WorkTypeFieldNameName, "Zagen") );

		Assert.AreEqual( 4, await service.InsertNewWorkTypeAsync( values ) );
		await service.UpdateWorkTypeAsync( values );
		await service.DeleteWorkTypeAsync( 4 );
		await service.NameExistsAsync( "Zagen", 0 );

		probe.AssertAllCallsWellFormed();
	}
	#endregion

	#region CurrencyService (CB-06)
	[TestMethod]
	public async Task Currency_GetAll_MapsAllColumns()
	{
		var probe = new ServiceQueryProbe();
		probe.SetupQuery<CurrencyModel>( Table(
			(DBNames.CurrencyFieldNameId, 2), (DBNames.CurrencyFieldNameCode, "GBP"), (DBNames.CurrencyFieldNameName, "Pond"),
			(DBNames.CurrencyFieldNameSymbol, "£"), (DBNames.CurrencyFieldNameRate, 1.17d) ) );

		var currency = ( await new CurrencyService( probe.DataService.Object ).GetAllCurrenciesAsync() ).Single();

		Assert.AreEqual( 2, currency.CurrencyId );
		Assert.AreEqual( "GBP", currency.CurrencyCode );
		Assert.AreEqual( "Pond", currency.CurrencyName );
		Assert.AreEqual( "£", currency.CurrencySymbol );
		Assert.AreEqual( 1.17d, currency.CurrencyConversionRate, 1e-9 );
	}

	[TestMethod]
	public async Task Currency_CrudAndChecks_SupplyEveryQueryParameter()
	{
		var probe = new ServiceQueryProbe { ScalarUInt = 2 };
		var service = new CurrencyService( probe.DataService.Object );
		var values = P( (DBNames.CurrencyFieldNameId, 2), (DBNames.CurrencyFieldNameCode, "GBP"), (DBNames.CurrencyFieldNameName, "Pond"),
			(DBNames.CurrencyFieldNameSymbol, "£"), (DBNames.CurrencyFieldNameRate, 1.17d) );

		Assert.AreEqual( 2, await service.InsertNewCurrencyAsync( values ) );
		await service.UpdateCurrencyAsync( values );
		await service.DeleteCurrencyAsync( 2 );
		await service.CodeExistsAsync( "GBP" );
		await service.NameExistsAsync( "Pond" );

		probe.AssertAllCallsWellFormed();
		Assert.AreEqual( 2, probe.Calls [ 1 ].Parameters [ $"@{DBNames.CurrencyFieldNameId}" ], "The update changes the selected currency." );
	}
	#endregion

	#region All services
	[TestMethod]
	public void AllServiceQueries_HaveSpacesBetweenKeywords()
	{
		var dataService = new ServiceQueryProbe().DataService.Object;
		var checkedQueries = 0;
		foreach ( var type in typeof( GenericDataService ).Assembly.GetTypes().Where( t => t.Namespace == "Modelbouwer.Services" && t.IsClass && !t.IsAbstract && t.Name.EndsWith( "Service" ) ) )
		{
			var constructor = type.GetConstructor( [ typeof( GenericDataService ) ] );
			object? instance = constructor?.Invoke( [ dataService ] );
			foreach ( var field in type.GetFields( System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Instance ).Where( f => f.FieldType == typeof( string ) ) )
			{
				if ( !field.IsStatic && instance == null )
					continue;
				if ( field.GetValue( field.IsStatic ? null : instance ) is not string query || !query.Contains( "SELECT" ) && !query.Contains( "UPDATE" ) && !query.Contains( "INSERT" ) && !query.Contains( "DELETE" ) )
					continue;

				AssertKeywordsSeparated( query );
				checkedQueries++;
			}
		}

		Assert.IsGreaterThan( 50, checkedQueries, "The scan found the service queries." );
	}
	#endregion

	#region Suppliers (found in the KAN-88 test round: saving a supplier failed with KeyNotFoundException)
	[TestMethod]
	public async Task Supplier_InsertAndUpdate_SupplyEveryQueryParameter()
	{
		var probe = new ServiceQueryProbe();
		var service = new SupplierService( probe.DataService.Object );
		var values = P( (DBNames.SupplierFieldNameId, 4), (DBNames.SupplierFieldNameCode, "CW"), (DBNames.SupplierFieldNameName, "Cornwall"),
			(DBNames.SupplierFieldNameAddress1, "Straat 1"), (DBNames.SupplierFieldNameAddress2, "Unit 2"), (DBNames.SupplierFieldNameZip, "TR1"),
			(DBNames.SupplierFieldNameCity, "Truro"), (DBNames.SupplierFieldNameUrl, "https://example.com"), (DBNames.SupplierFieldNameShippingCosts, 7.5),
			(DBNames.SupplierFieldNameMinShippingCosts, 0d), (DBNames.SupplierFieldNameOrderCosts, 1d), (DBNames.SupplierFieldNameMinOrderCosts, 0d),
			(DBNames.SupplierFieldNameCurrencyId, 2), (DBNames.SupplierFieldNameCountryId, 3), (DBNames.SupplierFieldNameGeneralEmail, "info@example.com"),
			(DBNames.SupplierFieldNameGeneralPhone, "0123"), (DBNames.SupplierFieldNameMemo, null) );

		await service.InsertNewSupplierAsync( values );
		await service.UpdateSupplierAsync( values );

		probe.AssertAllCallsWellFormed();
		Assert.AreEqual( "Unit 2", probe.Calls [ 1 ].Parameters [ $"@{DBNames.SupplierFieldNameAddress2}" ], "Address 2 is not a copy of address 1." );
	}

	[TestMethod]
	public async Task SupplierScreen_SavesEveryFieldOfTheSupplier()
	{
		var service = new Mock<ISupplierService>();
		Dictionary<string, object?>? saved = null;
		service.Setup( s => s.UpdateSupplierAsync( It.IsAny<Dictionary<string, object?>>(), It.IsAny<CancellationToken>() ) )
			.Callback<Dictionary<string, object?>, CancellationToken>( ( p, _ ) => saved = p ).Returns( Task.CompletedTask );
		var vm = new Modelbouwer.ViewModels.SupplierPageViewModel( service.Object, new Mock<ICountryService>().Object, new Mock<ICurrencyService>().Object, new Mock<IContactService>().Object, new Mock<IContactTypeService>().Object, new Mock<IStockOrderService>().Object, new Mock<IEntityValidator<SupplierModel>>().Object );
		var supplier = new SupplierModel { Id = 4, Name = "Cornwall", Address2 = "Unit 2", Mail = "info@example.com", CurrencyId = 2 };
		vm.SelectedItem = supplier;

		var update = typeof( Modelbouwer.ViewModels.SupplierPageViewModel ).GetMethod( "UpdateAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance )!;
		await ( Task ) update.Invoke( vm, [ supplier, CancellationToken.None ] )!;

		var probe = new ServiceQueryProbe();
		await new SupplierService( probe.DataService.Object ).UpdateSupplierAsync( saved! );
		probe.AssertAllCallsWellFormed();
		Assert.AreEqual( "info@example.com", saved! [ $"@{DBNames.SupplierFieldNameGeneralEmail}" ] );
	}
	#endregion

	#region Same checks for the other metadata services
	[TestMethod]
	public async Task Contact_CrudAndChecks_SupplyEveryQueryParameter()
	{
		var probe = new ServiceQueryProbe();
		var service = new ContactService( probe.DataService.Object );
		var values = P( (DBNames.SupplierContactFieldNameId, 3), (DBNames.SupplierContactFieldNameSupplierId, 9), (DBNames.SupplierContactFieldNameTypeId, 1),
			(DBNames.SupplierContactFieldNameName, "Jan"), (DBNames.SupplierContactFieldNameMail, "jan@example.com"), (DBNames.SupplierContactFieldNamePhone, "0123") );

		await service.InsertNewContactAsync( values );
		await service.UpdateContactAsync( values );
		await service.DeleteContactAsync( 3 );
		await service.NameExistsAsync( "Jan" );

		probe.AssertAllCallsWellFormed();
		Assert.AreEqual( 9, probe.Calls [ 0 ].Parameters [ $"@{DBNames.SupplierContactFieldNameSupplierId}" ], "A new contact belongs to its supplier." );
		Assert.AreEqual( 3, probe.Calls [ 1 ].Parameters [ $"@{DBNames.SupplierContactFieldNameId}" ], "The update changes that contact only." );
	}

	[TestMethod]
	public async Task Brand_CrudAndChecks_SupplyEveryQueryParameter()
	{
		var probe = new ServiceQueryProbe();
		var service = new BrandService( probe.DataService.Object );
		var values = P( (DBNames.BrandFieldNameId, 1), (DBNames.BrandFieldNameName, "Tamiya") );

		await service.InsertNewBrandAsync( values );
		await service.UpdateBrandAsync( values );
		await service.DeleteBrandAsync( 1 );
		await service.NameExistsAsync( "Tamiya" );

		probe.AssertAllCallsWellFormed();
	}

	[TestMethod]
	public async Task Unit_CrudAndChecks_SupplyEveryQueryParameter()
	{
		var probe = new ServiceQueryProbe();
		var service = new UnitService( probe.DataService.Object );
		var values = P( (DBNames.UnitFieldNameUnitId, 1), (DBNames.UnitFieldNameUnitName, "Stuks") );

		await service.InsertNewUnitAsync( values );
		await service.UpdateUnitAsync( values );
		await service.DeleteUnitAsync( 1 );
		await service.NameExistsAsync( "Stuks" );

		probe.AssertAllCallsWellFormed();
	}

	[TestMethod]
	public async Task ContactType_CrudAndChecks_SupplyEveryQueryParameter()
	{
		var probe = new ServiceQueryProbe();
		var service = new ContactTypeService( probe.DataService.Object );
		var values = P( (DBNames.ContactTypeFieldNameId, 1), (DBNames.ContactTypeFieldNameName, "Verkoop") );

		await service.InsertNewContactTypeAsync( values );
		await service.UpdateContactTypeAsync( values );
		await service.DeleteContactTypeAsync( 1 );
		await service.NameExistsAsync( "Verkoop" );

		probe.AssertAllCallsWellFormed();
	}
	#endregion
}
