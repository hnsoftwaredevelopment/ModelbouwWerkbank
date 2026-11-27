namespace Modelbouwer.Services;

/// <summary>The column header text of a CSV or Excel export.</summary>
public static class ExportHeaders
{
	/// <summary>
	/// Order: the header the screen passes for this column, then a translation "ExportHeader_{mapping name}", then the
	/// header the grid shows, then the mapping name. The language provider returns the key itself when there is no
	/// translation; that counts as none, so "ExportHeader_CountryCode" never ends up in a file (KAN-148).
	/// </summary>
	public static string Resolve( string? mappingName, string? gridHeader, Dictionary<string, string>? overrides, ILanguageProvider? languageProvider )
	{
		var name = mappingName ?? string.Empty;

		if ( overrides != null && overrides.TryGetValue( name, out var header ) )
			return header;

		if ( languageProvider != null )
		{
			var key = $"ExportHeader_{name}";
			var translated = languageProvider.GetTranslation( key );
			if ( !string.IsNullOrEmpty( translated ) && !string.Equals( translated, key, StringComparison.Ordinal ) )
				return translated;
		}

		return !string.IsNullOrWhiteSpace( gridHeader ) ? gridHeader : name;
	}
}
