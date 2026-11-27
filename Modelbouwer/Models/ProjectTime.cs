namespace Modelbouwer.Models;

/// <summary>
/// Shows worked time on the project screen as decimal hours, for example 376,25 for 376 hours and 15 minutes
/// (KAN-147). Easier to read than 376:15 for most users, and the same unit the hour rate is multiplied with.
/// </summary>
public static class ProjectTime
{
	public static string Format( double minutes ) => ( minutes / 60d ).ToString( "N2", CultureInfo.CurrentCulture );
}
