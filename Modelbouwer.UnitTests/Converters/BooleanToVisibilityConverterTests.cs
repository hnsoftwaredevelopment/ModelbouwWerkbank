using System.Globalization;
using System.Windows;

using Modelbouwer.Converters;

namespace Modelbouwer.UnitTests.Converters;

/// <summary>Moved from the old Modelbouwer.Tests folder (xUnit, not in the solution) in KAN-79.</summary>
[TestClass]
public sealed class BooleanToVisibilityConverterTests
{
	[TestMethod]
	public void Convert_BoolTrue_ReturnsVisible()
	{
		var result = new BooleanToVisibilityConverter().Convert( true, typeof( Visibility ), null, CultureInfo.InvariantCulture );
		Assert.AreEqual( Visibility.Visible, result );
	}

	[TestMethod]
	public void Convert_BoolFalse_ReturnsCollapsed()
	{
		var result = new BooleanToVisibilityConverter().Convert( false, typeof( Visibility ), null, CultureInfo.InvariantCulture );
		Assert.AreEqual( Visibility.Collapsed, result );
	}

	[TestMethod]
	public void Convert_StringTrue_ReturnsVisible()
	{
		var result = new BooleanToVisibilityConverter().Convert( "true", typeof( Visibility ), null, CultureInfo.InvariantCulture );
		Assert.AreEqual( Visibility.Visible, result );
	}

	[TestMethod]
	public void Convert_IntOne_ReturnsVisible()
	{
		var result = new BooleanToVisibilityConverter().Convert( 1, typeof( Visibility ), null, CultureInfo.InvariantCulture );
		Assert.AreEqual( Visibility.Visible, result );
	}

	[TestMethod]
	public void Convert_InvertedParameter_ReturnsOpposite()
	{
		var result = new BooleanToVisibilityConverter().Convert( true, typeof( Visibility ), "Invert", CultureInfo.InvariantCulture );
		Assert.AreEqual( Visibility.Collapsed, result );
	}

	[TestMethod]
	public void ConvertBack_FromVisible_ReturnsTrue()
	{
		var result = new BooleanToVisibilityConverter().ConvertBack( Visibility.Visible, typeof( bool ), null, CultureInfo.InvariantCulture );
		Assert.IsTrue( ( bool ) result );
	}

	[TestMethod]
	public void ConvertBack_InvertedParameter_ReturnsFalse()
	{
		var result = new BooleanToVisibilityConverter { IsInverted = false }.ConvertBack( Visibility.Visible, typeof( bool ), "Invert", CultureInfo.InvariantCulture );
		Assert.IsFalse( ( bool ) result );
	}
}
