using MySql.Data.MySqlClient;

namespace Modelbouwer.UnitTests.Helpers;

[TestClass]
public class DBConnectTests
{
	private static Func<string, string?> Variables( Dictionary<string, string?> values ) =>
		name => values.TryGetValue( name, out var value ) ? value : null;

	[TestMethod]
	public void Build_UsesEnvironmentValues()
	{
		var result = DBConnect.Build( Variables( new()
		{
			[ DBConnect.PasswordVariable ] = "geheim",
			[ DBConnect.UserVariable ] = "modelbouwer",
			[ DBConnect.ServerVariable ] = "nas.local",
			[ DBConnect.PortVariable ] = "3307"
		} ), () => "127.0.0.1" );

		var builder = new MySqlConnectionStringBuilder( result );
		Assert.AreEqual( "nas.local", builder.Server );
		Assert.AreEqual( 3307u, builder.Port );
		Assert.AreEqual( "modelbouwer", builder.UserID );
		Assert.AreEqual( "geheim", builder.Password );
		Assert.AreEqual( DBNames.Database, builder.Database );
	}

	[TestMethod]
	public void Build_FallsBackToConfiguredServerDefaultUserAndPort()
	{
		var result = DBConnect.Build( Variables( new() { [ DBConnect.PasswordVariable ] = "geheim" } ), () => "127.0.0.1" );

		var builder = new MySqlConnectionStringBuilder( result );
		Assert.AreEqual( "127.0.0.1", builder.Server );
		Assert.AreEqual( DBConnect.DefaultPort, builder.Port );
		Assert.AreEqual( DBConnect.DefaultUser, builder.UserID );
	}

	[TestMethod]
	public void Build_KeepsSpecialCharactersInPassword()
	{
		const string password = "a;b=c'd\"e";
		var result = DBConnect.Build( Variables( new() { [ DBConnect.PasswordVariable ] = password } ), () => "localhost" );

		Assert.AreEqual( password, new MySqlConnectionStringBuilder( result ).Password );
	}

	[TestMethod]
	public void Build_WithoutPassword_DoesNotThrow()
	{
		var result = DBConnect.Build( Variables( new() ), () => "localhost" );

		Assert.AreEqual( string.Empty, new MySqlConnectionStringBuilder( result ).Password );
	}
}
