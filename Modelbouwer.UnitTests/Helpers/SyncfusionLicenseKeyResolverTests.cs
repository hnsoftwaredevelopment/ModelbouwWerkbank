namespace Modelbouwer.UnitTests.Helpers;

[TestClass]
public class SyncfusionLicenseKeyResolverTests
{
	[TestMethod]
	public void Resolve_PrefersEnvironmentKey()
	{
		Assert.AreEqual( "from-env", SyncfusionLicenseKeyResolver.Resolve( " from-env ", [ "from-file" ] ) );
	}

	[TestMethod]
	public void Resolve_UsesFirstNonEmptyFileKey()
	{
		Assert.AreEqual( "from-file", SyncfusionLicenseKeyResolver.Resolve( null, [ null, "", "  from-file  ", "other" ] ) );
	}

	[TestMethod]
	public void Resolve_ReturnsNullWhenNothingFound()
	{
		Assert.IsNull( SyncfusionLicenseKeyResolver.Resolve( "", [ null, " " ] ) );
	}

	[TestMethod]
	public void EnumerateCandidateFiles_ChecksExeFolderFirstThenDocsUpwards()
	{
		var start = Path.Combine( Path.GetTempPath(), "a", "b" );

		var candidates = SyncfusionLicenseKeyResolver.EnumerateCandidateFiles( start ).ToList();

		Assert.AreEqual( Path.Combine( start, "SyncfusionLicense.txt" ), candidates [ 0 ] );
		Assert.AreEqual( Path.Combine( start, "docs", "SyncfusionLicense.txt" ), candidates [ 1 ] );
		CollectionAssert.Contains( candidates, Path.Combine( Path.GetTempPath(), "a", "docs", "SyncfusionLicense.txt" ) );
	}
}
