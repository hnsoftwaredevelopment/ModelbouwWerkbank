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
		string script = File.ReadAllText( Path.Combine( root, "Database", "KAN-99-drop-unused-objects.sql" ) );
		var dropped = Regex.Matches( script, @"DROP (?:VIEW|PROCEDURE|TABLE) IF EXISTS (\w+);" )
			.Select( match => match.Groups [ 1 ].Value )
			.ToList();
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
				foreach ( string name in dropped )
				{
					// As a quoted name ("view_stock") or as SQL text (modelbuilder.months, CALL GetProductsUsage).
					if ( Regex.IsMatch( text, $@"(""{name}""|\.{name}\b|CALL\s+{name}\b)", RegexOptions.IgnoreCase ) )
						offenders.Add( $"{Path.GetFileName( file )}: {name}" );
				}
			}
		}

		Assert.AreEqual( 0, offenders.Count, string.Join( Environment.NewLine, offenders ) );
	}

	private static string FindRepositoryRoot()
	{
		var directory = AppContext.BaseDirectory;
		while ( directory != null && !File.Exists( Path.Combine( directory, "ModelbouwWerkbank.slnx" ) ) )
			directory = Directory.GetParent( directory )?.FullName;

		return directory ?? throw new DirectoryNotFoundException( "Could not locate repository root." );
	}
}
