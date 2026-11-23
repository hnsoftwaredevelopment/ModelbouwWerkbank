using System.Text.RegularExpressions;

namespace Modelbouwer.UnitTests.Helpers;

/// <summary>
/// Guards against credentials or license keys returning to the (public) source code. See KAN-78.
/// </summary>
[TestClass]
public class NoSecretsInSourceTests
{
	private static readonly Regex [ ] SecretPatterns =
	[
		new( @"password\s*=\s*""[^""]+""", RegexOptions.IgnoreCase ),
		new( @"Password\s*\{\s*get;\s*init;\s*\}\s*=\s*""[^""]+""" ),
		new( @"PASSWORD\s*=\s*[^{;""\s][^;""]*;", RegexOptions.IgnoreCase ),
		new( @"RegisterLicense\(\s*""" )
	];

	[TestMethod]
	public void SourceFiles_ContainNoHardcodedPasswordsOrLicenseKeys()
	{
		var root = FindRepositoryRoot();
		var offenders = new[] { "Modelbouwer", "Modelbouwer.Mobile" }
			.SelectMany( project => Directory.EnumerateFiles( Path.Combine( root, project ), "*.cs", SearchOption.AllDirectories ) )
			.Where( path => !path.Contains( $"{Path.DirectorySeparatorChar}Resources{Path.DirectorySeparatorChar}Languages{Path.DirectorySeparatorChar}" ) )
			.SelectMany( path => File.ReadLines( path )
				.Select( ( line, index ) => (path, line, index) )
				.Where( item => SecretPatterns.Any( pattern => pattern.IsMatch( item.line ) ) )
				.Select( item => $"{Path.GetRelativePath( root, item.path )}:{item.index + 1}" ) )
			.ToList();

		Assert.AreEqual( 0, offenders.Count, "Hardcoded secret found in: " + string.Join( ", ", offenders ) );
	}

	private static string FindRepositoryRoot()
	{
		var directory = AppContext.BaseDirectory;
		while ( directory != null && !File.Exists( Path.Combine( directory, "ModelbouwWerkbank.slnx" ) ) )
			directory = Directory.GetParent( directory )?.FullName;

		return directory ?? throw new DirectoryNotFoundException( "Could not locate repository root." );
	}
}
