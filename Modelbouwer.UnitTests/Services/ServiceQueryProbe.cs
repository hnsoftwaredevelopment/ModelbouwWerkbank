using System.Data;
using System.Data.Common;
using System.Text.RegularExpressions;

using Moq;

namespace Modelbouwer.UnitTests.Services;

/// <summary>
/// Records the queries and parameters a service sends to <see cref="GenericDataService"/> (KAN-88), so tests can
/// check that every parameter in a query is supplied and that mapping code reads the right columns.
/// </summary>
internal sealed class ServiceQueryProbe
{
	public Mock<GenericDataService> DataService { get; } = new();
	public List<(string Query, Dictionary<string, object> Parameters)> Calls { get; } = [ ];
	public object? Mapper { get; private set; }

	public uint ScalarUInt { get; set; } = 1;
	public int ScalarInt { get; set; }
	public Exception? ThrowOnScalar { get; set; }

	public ServiceQueryProbe()
	{
		DataService
			.Setup( s => s.ExecuteScalarAsync<uint>( It.IsAny<string>(), It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>() ) )
			.Returns<string, Dictionary<string, object>, CancellationToken>( ( q, p, _ ) =>
			{
				Calls.Add( (q, p) );
				return ThrowOnScalar != null ? Task.FromException<uint>( ThrowOnScalar ) : Task.FromResult( ScalarUInt );
			} );
		DataService
			.Setup( s => s.ExecuteScalarAsync<int>( It.IsAny<string>(), It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>() ) )
			.Returns<string, Dictionary<string, object>, CancellationToken>( ( q, p, _ ) =>
			{
				Calls.Add( (q, p) );
				return Task.FromResult( ScalarInt );
			} );
	}

	/// <summary>Makes GetAll return the rows that the service's own mapper builds from <paramref name="table"/>.</summary>
	public void SetupQuery<T>( DataTable table )
	{
		DataService
			.Setup( s => s.ExecuteQueryAsync( It.IsAny<string>(), It.IsAny<Func<DbDataReader, T>>(), It.IsAny<Dictionary<string, object>?>(), It.IsAny<CancellationToken>() ) )
			.Returns<string, Func<DbDataReader, T>, Dictionary<string, object>?, CancellationToken>( ( q, map, p, _ ) =>
			{
				Calls.Add( (q, p ?? [ ]) );
				Mapper = map;
				var result = new List<T>();
				using var reader = table.CreateDataReader();
				while ( reader.Read() )
					result.Add( map( reader ) );
				return Task.FromResult( result );
			} );
	}

	public (string Query, Dictionary<string, object> Parameters) Single()
	{
		Assert.HasCount( 1, Calls );
		return Calls [ 0 ];
	}

	public static DataTable Table( params (string Column, object? Value) [ ] cells )
	{
		var table = new DataTable();
		foreach ( var (column, value) in cells )
			table.Columns.Add( column, value?.GetType() ?? typeof( string ) );
		table.Rows.Add( cells.Select( c => c.Value ?? DBNull.Value ).ToArray() );
		return table;
	}

	/// <summary>Every @parameter in the query has a value; user variables (@@) are skipped.</summary>
	public static void AssertParametersSupplied( string query, Dictionary<string, object> parameters )
	{
		foreach ( System.Text.RegularExpressions.Match m in Regex.Matches( query, @"(?<![@\w])@(\w+)" ) )
			Assert.IsTrue( parameters.ContainsKey( "@" + m.Groups [ 1 ].Value ), $"Parameter @{m.Groups [ 1 ].Value} is used in the query but not supplied.{Environment.NewLine}{query}" );
	}

	/// <summary>Catches string concatenation without a space, such as "@NameWHERE".</summary>
	public static void AssertKeywordsSeparated( string query )
	{
		var glued = Regex.Match( query, @"[A-Za-z0-9_}](WHERE|FROM|VALUES|ORDER BY|GROUP BY)\b" );
		Assert.IsFalse( glued.Success, $"'{glued.Value}' misses a space in:{Environment.NewLine}{query}" );
	}

	public void AssertAllCallsWellFormed()
	{
		Assert.IsNotEmpty( Calls );
		foreach ( var (query, parameters) in Calls )
		{
			AssertKeywordsSeparated( query );
			AssertParametersSupplied( query, parameters );
		}
	}
}
