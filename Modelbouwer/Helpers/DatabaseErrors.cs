namespace Modelbouwer.Helpers;

/// <summary>MySQL errors the application reacts to.</summary>
public static class DatabaseErrors
{
	/// <summary>ER_ROW_IS_REFERENCED_2: a foreign key refuses the delete, another row still points to this one.</summary>
	public const int RowIsReferenced = 1451;

	/// <summary>ER_ROW_IS_REFERENCED: the older form of the same refusal.</summary>
	public const int RowIsReferencedLegacy = 1217;

	/// <summary>
	/// True when <paramref name="exception"/> is a delete the database refused because the row is still in use
	/// (KAN-145). Other exceptions read the error number from a Number property, so tests can throw a stand-in:
	/// MySqlException cannot be created outside the connector.
	/// </summary>
	public static bool IsRowStillReferenced( Exception exception )
	{
		int? number = exception is MySqlException mySql
			? mySql.Number
			: exception.GetType().GetProperty( "Number" )?.GetValue( exception ) as int?;

		return number is RowIsReferenced or RowIsReferencedLegacy;
	}
}
