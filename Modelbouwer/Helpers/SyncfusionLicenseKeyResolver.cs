namespace Modelbouwer.Helpers;

/// <summary>
/// Finds the Syncfusion license key for this application without storing it in the repository.
/// Order: environment variable MODELBOUWER_SYNCFUSION_LICENSE_KEY, then SyncfusionLicense.txt next to
/// the executable, then docs\SyncfusionLicense.txt in the application folder or one of its parents.
/// </summary>
/// <remarks>
/// The variable name is specific to this application because it uses an older Syncfusion version
/// than other projects; a general SYNCFUSION_LICENSE_KEY would hold a key for a newer version.
/// </remarks>
public static class SyncfusionLicenseKeyResolver
{
	public const string EnvironmentVariable = "MODELBOUWER_SYNCFUSION_LICENSE_KEY";
	public const string LicenseFileName = "SyncfusionLicense.txt";

	public static string? Resolve( string? environmentKey, IEnumerable<string?> fileKeys )
	{
		if ( !string.IsNullOrWhiteSpace( environmentKey ) )
			return environmentKey.Trim();

		return fileKeys
			.FirstOrDefault( key => !string.IsNullOrWhiteSpace( key ) )
			?.Trim();
	}

	public static string? ResolveFromEnvironmentAndLocalFiles()
	{
		var environmentKey = Environment.GetEnvironmentVariable( EnvironmentVariable )
			?? Environment.GetEnvironmentVariable( EnvironmentVariable, EnvironmentVariableTarget.User );

		var fileKeys = EnumerateCandidateFiles( AppContext.BaseDirectory )
			.Select( ReadLicenseFile );

		return Resolve( environmentKey, fileKeys );
	}

	public static IEnumerable<string> EnumerateCandidateFiles( string startDirectory )
	{
		yield return Path.Combine( startDirectory, LicenseFileName );

		var directory = new DirectoryInfo( startDirectory );
		while ( directory is not null )
		{
			yield return Path.Combine( directory.FullName, "docs", LicenseFileName );
			directory = directory.Parent;
		}
	}

	private static string? ReadLicenseFile( string path )
	{
		try
		{
			return File.Exists( path ) ? File.ReadAllText( path ) : null;
		}
		catch ( IOException )
		{
			return null;
		}
		catch ( UnauthorizedAccessException )
		{
			return null;
		}
	}
}
