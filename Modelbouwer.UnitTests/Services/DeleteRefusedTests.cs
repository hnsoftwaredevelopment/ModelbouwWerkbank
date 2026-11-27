namespace Modelbouwer.UnitTests.Services;

/// <summary>
/// KAN-145: with the foreign keys back, the database refuses to delete a row that is still in use (MySQL error 1451).
/// Every delete of the metadata screens turns that refusal into an <see cref="EntityInUseException"/> with the message
/// of its screen, which the view model shows. Any other database error is passed on unchanged.
/// </summary>
[TestClass]
public class DeleteRefusedTests
{
	/// <summary>Stands in for MySqlException, which cannot be created outside the connector.</summary>
	private sealed class DatabaseError( int number ) : Exception( $"database error {number}" )
	{
		public int Number { get; } = number;
	}

	public static IEnumerable<object [ ]> Deletes() =>
	[
		[ "Brand", ( Func<GenericDataService, Task> ) ( d => new BrandService( d ).DeleteBrandAsync( 1 ) ), Lang.metadataBrandDeleteError ],
		[ "Category", ( Func<GenericDataService, Task> ) ( d => new CategoryService( d ).DeleteCategoryAsync( 1 ) ), Lang.metadataCategoryDeleteError ],
		[ "Contact", ( Func<GenericDataService, Task> ) ( d => new ContactService( d ).DeleteContactAsync( 1 ) ), Lang.metadataSupplierContactDeleteError ],
		[ "ContactType", ( Func<GenericDataService, Task> ) ( d => new ContactTypeService( d ).DeleteContactTypeAsync( 1 ) ), Lang.metadataContactTypeDeleteError ],
		[ "Country", ( Func<GenericDataService, Task> ) ( d => new CountryService( d ).DeleteCountryAsync( 1 ) ), Lang.metadataCountryDeleteError ],
		[ "Currency", ( Func<GenericDataService, Task> ) ( d => new CurrencyService( d ).DeleteCurrencyAsync( 1 ) ), Lang.metadataCurrencyDeleteError ],
		[ "Product", ( Func<GenericDataService, Task> ) ( d => new ProductService( d ).DeleteProductAsync( 1 ) ), Lang.metadataProductDeleteError ],
		[ "Project", ( Func<GenericDataService, Task> ) ( d => new ProjectService( d ).DeleteProjectAsync( 1 ) ), Lang.metadataProjectDeleteError ],
		[ "StorageLocation", ( Func<GenericDataService, Task> ) ( d => new StorageLocationService( d ).DeleteStorageLocationAsync( 1 ) ), Lang.metadataStorageLocationDeleteError ],
		[ "Supplier", ( Func<GenericDataService, Task> ) ( d => new SupplierService( d ).DeleteSupplierAsync( 1 ) ), Lang.metadataSupplierDeleteError ],
		[ "Unit", ( Func<GenericDataService, Task> ) ( d => new UnitService( d ).DeleteUnitAsync( 1 ) ), Lang.metadataUnitDeleteError ],
		[ "WorkType", ( Func<GenericDataService, Task> ) ( d => new WorkTypeService( d ).DeleteWorkTypeAsync( 1 ) ), Lang.metadataWorkTypeDeleteError ],
	];

	public static string DisplayName( System.Reflection.MethodInfo method, object [ ] data ) => $"{method.Name}({data [ 0 ]})";

	private static Mock<GenericDataService> Failing( int number )
	{
		var data = new Mock<GenericDataService>();
		data.Setup( d => d.ExecuteScalarAsync<uint>( It.IsAny<string>(), It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>() ) )
			.ThrowsAsync( new DatabaseError( number ) );
		return data;
	}

	[TestMethod]
	[DynamicData( nameof( Deletes ), DynamicDataDisplayName = nameof( DisplayName ) )]
	public async Task RowStillInUse_ShowsTheMessageOfTheScreen( string screen, Func<GenericDataService, Task> delete, string message )
	{
		var exception = await Assert.ThrowsExactlyAsync<EntityInUseException>( () => delete( Failing( DatabaseErrors.RowIsReferenced ).Object ) );

		Assert.AreEqual( message + ".", exception.Message, screen );
	}

	[TestMethod]
	[DynamicData( nameof( Deletes ), DynamicDataDisplayName = nameof( DisplayName ) )]
	public async Task OtherDatabaseError_IsPassedOn( string screen, Func<GenericDataService, Task> delete, string message )
	{
		// 1205: lock wait timeout. Not a refused delete, so it must not be shown as "still in use".
		var exception = await Assert.ThrowsExactlyAsync<DatabaseError>( () => delete( Failing( 1205 ).Object ) );

		Assert.AreEqual( 1205, exception.Number, screen );
	}

	[TestMethod]
	[DataRow( 1451, true )]
	[DataRow( 1217, true )]
	[DataRow( 1452, false )]
	[DataRow( 1205, false )]
	public void IsRowStillReferenced_OnlyForRefusedDeletes( int number, bool expected )
	{
		Assert.AreEqual( expected, DatabaseErrors.IsRowStillReferenced( new DatabaseError( number ) ) );
	}

	[TestMethod]
	public void IsRowStillReferenced_IgnoresExceptionsWithoutNumber()
	{
		Assert.IsFalse( DatabaseErrors.IsRowStillReferenced( new InvalidOperationException() ) );
	}
}
