using MySql.Data.MySqlClient;

namespace Modelbouwer.Mobile.Services;

/// <summary>
/// Connection settings for the tablet app. Credentials are not in the repository: the build writes
/// them from the developer's environment variables into the app package (modelbouwer.secrets, see the csproj).
/// </summary>
public sealed class MobileDbConnectionSettings
{
	public const string SecretsFileName = "modelbouwer.secrets";
	private const string DefaultEncodedIp = "3444, 4920, 5328, 3696";
	private string? connectionString;

	public string Database { get; init; } = "modelbuilder";
	public uint Port { get; init; } = 3306;

	public async Task<string> GetConnectionStringAsync()
	{
		if ( connectionString is not null )
			return connectionString;

		var secrets = ParseSecrets( await ReadPackageFileAsync( SecretsFileName ) );
		if ( !secrets.TryGetValue( "Password", out var password ) || string.IsNullOrWhiteSpace( password ) )
			throw new InvalidOperationException(
				"Er is geen databasewachtwoord meegebouwd. Zet MODELBOUWER_DB_PASSWORD op de ontwikkellaptop en bouw de app opnieuw." );

		var encodedIp = await ReadPackageFileAsync( "modelbouwer.config" );
		var server = secrets.TryGetValue( "Server", out var serverOverride ) && !string.IsNullOrWhiteSpace( serverOverride )
			? serverOverride
			: DecodeServer( string.IsNullOrWhiteSpace( encodedIp ) ? DefaultEncodedIp : encodedIp.Trim() );

		var builder = new MySqlConnectionStringBuilder
		{
			Server = server,
			Port = Port,
			Database = Database,
			UserID = secrets.TryGetValue( "User", out var user ) && !string.IsNullOrWhiteSpace( user ) ? user : "root",
			Password = password,
			SslMode = MySqlSslMode.Disabled,
			ConnectionTimeout = 8,
			DefaultCommandTimeout = 20
		};

		connectionString = builder.ConnectionString;
		return connectionString;
	}

	/// <summary>
	/// Parses "Key=Value" lines. Only the first '=' separates key and value, so a password may contain '='.
	/// </summary>
	public static Dictionary<string, string> ParseSecrets( string? content )
	{
		var result = new Dictionary<string, string>( StringComparer.OrdinalIgnoreCase );
		if ( string.IsNullOrEmpty( content ) )
			return result;

		foreach ( var line in content.Split( [ "\r\n", "\n" ], StringSplitOptions.RemoveEmptyEntries ) )
		{
			var separator = line.IndexOf( '=' );
			if ( separator <= 0 )
				continue;

			result [ line [ ..separator ].Trim() ] = line [ ( separator + 1 ).. ];
		}

		return result;
	}

	public static string DecodeServer( string encoded )
	{
		var parts = encoded
			.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
			.Select(value => ((int.Parse(value) / 2) - 1200) / 6)
			.ToArray();

		if ( parts.Length != 4 )
			throw new InvalidOperationException( "De databaseconfiguratie bevat geen geldig IP-adres." );

		return string.Join( ".", parts );
	}

	private static async Task<string?> ReadPackageFileAsync( string fileName )
	{
		try
		{
			await using var stream = await FileSystem.OpenAppPackageFileAsync( fileName );
			using var reader = new StreamReader( stream );
			return await reader.ReadToEndAsync();
		}
		catch ( Exception )
		{
			// Missing asset: Android and Windows throw different exception types.
			return null;
		}
	}
}
