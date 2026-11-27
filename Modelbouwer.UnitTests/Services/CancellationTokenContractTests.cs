using System.Reflection;

namespace Modelbouwer.UnitTests.Services;

/// <summary>KAN-92: every async method of the service and validator contracts has an overload with a CancellationToken.</summary>
[TestClass]
public class CancellationTokenContractTests
{
	public static IEnumerable<object [ ]> Contracts() =>
	[
		[ typeof( IBrandService ) ], [ typeof( ICategoryService ) ], [ typeof( IContactService ) ], [ typeof( IContactTypeService ) ],
		[ typeof( ICountryService ) ], [ typeof( ICurrencyService ) ], [ typeof( IProductService ) ], [ typeof( IProjectService ) ],
		[ typeof( IStockService ) ], [ typeof( IStorageLocationService ) ], [ typeof( ISupplierService ) ], [ typeof( IUnitService ) ],
		[ typeof( IWorkTypeService ) ], [ typeof( IStockOrderService ) ], [ typeof( IEntityValidator<ProductModel> ) ]
	];

	[TestMethod]
	[DynamicData( nameof( Contracts ) )]
	public void AsyncMethods_HaveATokenOverload( Type contract )
	{
		var methods = contract.GetMethods();
		foreach ( var method in methods.Where( m => typeof( Task ).IsAssignableFrom( m.ReturnType ) && !HasToken( m ) ) )
		{
			var parameters = method.GetParameters().Select( p => p.ParameterType ).Append( typeof( CancellationToken ) ).ToArray();
			Assert.IsTrue( methods.Any( m => m.Name == method.Name && m.GetParameters().Select( p => p.ParameterType ).SequenceEqual( parameters ) ),
				$"{contract.Name}.{method.Name} has no overload with a CancellationToken." );
		}
	}

	[TestMethod]
	public void EntityPageViewModel_HooksTakeAToken()
	{
		foreach ( var name in new [ ] { "LoadItemsAsync", "InsertAsync", "UpdateAsync", "DeleteAsync" } )
		{
			var hook = typeof( EntityPageViewModel<ProductModel> ).GetMethod( name, BindingFlags.NonPublic | BindingFlags.Instance );
			Assert.IsNotNull( hook, name );
			Assert.IsTrue( HasToken( hook ), $"{name} takes a CancellationToken." );
		}
	}

	private static bool HasToken( MethodInfo method ) => method.GetParameters().Any( p => p.ParameterType == typeof( CancellationToken ) );
}
