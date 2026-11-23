namespace Modelbouwer.Helpers;

/// <summary>
/// Builds the MySQL connection string from settings on this machine.
/// Credentials are read from environment variables and never stored in the repository.
/// </summary>
public static class DBConnect
{
	public const string PasswordVariable = "MODELBOUWER_DB_PASSWORD";
	public const string UserVariable = "MODELBOUWER_DB_USER";
	public const string ServerVariable = "MODELBOUWER_DB_SERVER";
	public const string PortVariable = "MODELBOUWER_DB_PORT";

	public const string DefaultUser = "root";
	public const uint DefaultPort = 3306;

	private static readonly Lazy<string> _connectionString =
		new( () => Build( ReadVariable, () => IpHelper.GetIP() ) );

	/// <summary>
	/// Connection string for the modelbuilder database. Built once per process.
	/// </summary>
	public static string ConnectionString => _connectionString.Value;

	/// <summary>
	/// True when a database password is available on this machine.
	/// </summary>
	public static bool IsPasswordConfigured => !string.IsNullOrWhiteSpace( ReadVariable( PasswordVariable ) );

	/// <summary>
	/// Builds a connection string. The server comes from <see cref="ServerVariable"/> when set,
	/// otherwise from the configured server (modelbouwer.config).
	/// </summary>
	public static string Build( Func<string, string?> getVariable, Func<string> getConfiguredServer )
	{
		var server = getVariable( ServerVariable );
		var portText = getVariable( PortVariable );
		var user = getVariable( UserVariable );

		var builder = new MySqlConnectionStringBuilder
		{
			Server = string.IsNullOrWhiteSpace( server ) ? getConfiguredServer() : server.Trim(),
			Port = uint.TryParse( portText, out var port ) ? port : DefaultPort,
			Database = DBNames.Database,
			UserID = string.IsNullOrWhiteSpace( user ) ? DefaultUser : user.Trim(),
			Password = getVariable( PasswordVariable ) ?? string.Empty
		};

		return builder.ConnectionString;
	}

	/// <summary>
	/// Reads a variable from the process first and falls back to the Windows user environment,
	/// so a value set after Visual Studio was started is still found.
	/// </summary>
	private static string? ReadVariable( string name )
	{
		var value = Environment.GetEnvironmentVariable( name );
		return string.IsNullOrWhiteSpace( value )
			? Environment.GetEnvironmentVariable( name, EnvironmentVariableTarget.User )
			: value;
	}
}
