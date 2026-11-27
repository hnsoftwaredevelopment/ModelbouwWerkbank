namespace Modelbouwer.Converters;

public class MappingNameToAlignmentConverter : IValueConverter
{
	public object Convert( object value, Type targetType, object parameter, CultureInfo culture )
	{
		if ( value is string mappingName )
		{
			if ( mappingName.Contains( "Value", StringComparison.Ordinal ) ||
				mappingName.Contains( "Price", StringComparison.Ordinal ) ||
				mappingName.Contains( "Inventory", StringComparison.Ordinal ) ||
				mappingName.Contains( "Order", StringComparison.Ordinal ) )
			{
				return HorizontalAlignment.Right;
			}
		}

		return HorizontalAlignment.Left;
	}
	public object ConvertBack( object value, Type targetType, object parameter, System.Globalization.CultureInfo culture ) => throw new NotImplementedException();
}