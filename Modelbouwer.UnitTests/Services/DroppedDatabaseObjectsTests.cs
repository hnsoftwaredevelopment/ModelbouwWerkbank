using System.Text.RegularExpressions;

namespace Modelbouwer.UnitTests.Services;

/// <summary>
/// KAN-99 removed database objects that nothing used. The apps must not start using them again, because a
/// database brought up to date with Tools\Update-Database.ps1 no longer has them.
/// </summary>
[TestClass]
public class DroppedDatabaseObjectsTests
{
	[TestMethod]
	public void NoSourceFileReferencesADroppedDatabaseObject()
	{
		string root = FindRepositoryRoot();
		var dropped = DroppedObjects( root );
		Assert.IsTrue( dropped.Count >= 30, "The drop script lists the removed objects." );

		var offenders = new List<string>();
		foreach ( string project in new [ ] { "Modelbouwer", "Modelbouwer.Mobile" } )
		{
			foreach ( string file in Directory.EnumerateFiles( Path.Combine( root, project ), "*.cs", SearchOption.AllDirectories ) )
			{
				if ( file.Contains( $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}" )
					|| file.Contains( $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}" )
					|| file.EndsWith( "Language.Designer.cs", StringComparison.Ordinal ) )
					continue;

				string text = File.ReadAllText( file );
				offenders.AddRange( dropped.Where( name => References( text, name ) ).Select( name => $"{Path.GetFileName( file )}: {name}" ) );
			}
		}

		Assert.AreEqual( 0, offenders.Count, string.Join( Environment.NewLine, offenders ) );
	}

	[TestMethod]
	[DataRow( "SELECT * FROM view_stock", "view_stock", true, DisplayName = "FROM without schema" )]
	[DataRow( "SELECT * FROM `months` m", "months", true, DisplayName = "FROM with backticks" )]
	[DataRow( "LEFT JOIN modelbuilder.weekdays w ON", "weekdays", true, DisplayName = "JOIN with schema" )]
	[DataRow( "CALL GetProductsUsage(@id);", "GetProductsUsage", true, DisplayName = "CALL" )]
	[DataRow( "public static readonly string X = \"view_timereport\";", "view_timereport", true, DisplayName = "Constant" )]
	[DataRow( "INSERT INTO stock (product_Id) VALUES (1)", "stock", true, DisplayName = "INSERT INTO" )]
	[DataRow( "FROM modelbuilder.stocklog sl", "stock", false, DisplayName = "Longer table name" )]
	[DataRow( "// The stock goes up when the stock is received.", "stock", false, DisplayName = "Domain word" )]
	[DataRow( "var months = 12; // months in stock", "months", false, DisplayName = "Variable name" )]
	public void References_RecognisesSqlUseButNotDomainWords( string text, string name, bool expected )
	{
		Assert.AreEqual( expected, References( text, name ) );
	}

	/// <summary>
	/// A dropped object is referenced when its name is a string literal, follows a schema dot, follows CALL, or
	/// follows FROM, JOIN, INTO, UPDATE, TABLE or VIEW (with or without backticks).
	/// </summary>
	private static bool References( string text, string name )
	{
		string n = Regex.Escape( name );
		return Regex.IsMatch( text,
			$@"(""{n}""|\.`?{n}`?(?!\w)|\bCALL\s+`?{n}`?(?!\w)|\b(FROM|JOIN|INTO|UPDATE|TABLE|VIEW)\s+`?{n}`?(?!\w))",
			RegexOptions.IgnoreCase );
	}

	private static List<string> DroppedObjects( string root )
	{
		string script = File.ReadAllText( Path.Combine( root, "Database", "KAN-99-drop-unused-objects.sql" ) );
		return Regex.Matches( script, @"DROP (?:VIEW|PROCEDURE|TABLE) IF EXISTS (\w+);" )
			.Select( match => match.Groups [ 1 ].Value )
			.ToList();
	}

	private static string FindRepositoryRoot()
	{
		var directory = AppContext.BaseDirectory;
		while ( directory != null && !File.Exists( Path.Combine( directory, "ModelbouwWerkbank.slnx" ) ) )
			directory = Directory.GetParent( directory )?.FullName;

		return directory ?? throw new DirectoryNotFoundException( "Could not locate repository root." );
	}
}
