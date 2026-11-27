using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Markup;

namespace Modelbouwer.UnitTests.Views;

/// <summary>
/// KAN-147: totals with StringFormat showed 376.25 instead of 376,25. WPF formats bindings with the Language of the
/// element (en-US unless set), not with CurrentCulture. The app sets that Language from the settings at start-up.
/// </summary>
[TestClass]
public class WpfCultureTests
{
	[TestMethod]
	public void Culture_FromTheSettingsAlsoSetsTheWpfLanguage()
	{
		var directory = AppContext.BaseDirectory;
		while ( directory != null && !File.Exists( Path.Combine( directory, "ModelbouwWerkbank.slnx" ) ) )
			directory = Directory.GetParent( directory )?.FullName;
		var source = File.ReadAllText( Path.Combine( directory ?? throw new DirectoryNotFoundException(), "Modelbouwer", "App.xaml.cs" ) );

		var applyCulture = source.Substring( source.IndexOf( "public static void ApplyCulture(", StringComparison.Ordinal ) );
		applyCulture = applyCulture.Substring( 0, applyCulture.IndexOf( "\n\t}", StringComparison.Ordinal ) );
		StringAssert.Contains( applyCulture, "UseCultureForWpfFormatting( CultureInfo.CurrentCulture );", "Start-up and the settings screen both go through ApplyCulture." );
		StringAssert.Contains( source, "EventManager.RegisterClassHandler( typeof( Window ), FrameworkElement.LoadedEvent", "Every window, dialogs too, gets the language." );
		Assert.IsFalse( source.Contains( ".OverrideMetadata(", StringComparison.Ordinal ), "Overriding the default stops the Runs in a TextBlock from following the window's language." );
	}

	[TestMethod]
	public void ViewShownInAWindowWithLanguage_FormatsItsTotalsInThatLanguage()
	{
		// As the app: the window has the language, the view with a total line is put in it afterwards.
		string? runText = null, blockText = null;
		var thread = new Thread( () =>
		{
			var window = new Window { Language = XmlLanguage.GetLanguage( "nl-NL" ), ShowActivated = false, WindowStyle = WindowStyle.None, Width = 10, Height = 10, Left = -2000 };
			window.Show();
			var (view, run) = TotalLine();
			var block = new TextBlock();
			block.SetBinding( TextBlock.TextProperty, new Binding { Source = 3186.2, StringFormat = "{0:N2}", Mode = BindingMode.OneWay } );
			( ( StackPanel ) view ).Children.Add( block );
			window.Content = view;
			window.UpdateLayout();
			runText = run.Text;
			blockText = block.Text;
			window.Close();
		} );
		thread.SetApartmentState( ApartmentState.STA );
		thread.Start();
		thread.Join();

		Assert.AreEqual( "376,25", runText, "The hours total in time registration is a Run." );
		Assert.AreEqual( "3.186,20", blockText, "Thousands separator too, as the costs in the reports." );
	}

	/// <summary>Runs <paramref name="build"/> on an STA thread and returns the text of the bound Run it builds.</summary>
	private static string? RunText( Func<(FrameworkElement Root, Run Run)> build, XmlLanguage? setAfterwards )
	{
		string? text = null;
		var thread = new Thread( () =>
		{
			var (root, run) = build();
			if ( setAfterwards != null )
				root.Language = setAfterwards;
			text = run.Text;
		} );
		thread.SetApartmentState( ApartmentState.STA );
		thread.Start();
		thread.Join();
		return text;
	}

	private static (FrameworkElement Root, Run Run) TotalLine()
	{
		// As the totals of time registration: a Run with StringFormat inside a TextBlock inside a panel.
		var panel = new StackPanel();
		var block = new TextBlock();
		var run = new Run();
		block.Inlines.Add( run );
		panel.Children.Add( block );
		run.SetBinding( Run.TextProperty, new Binding { Source = 376.25, StringFormat = "{0:N2}", Mode = BindingMode.OneWay } );
		return (panel, run);
	}

	[TestMethod]
	public void RunInATotalLine_FollowsTheLanguageOfItsWindow()
	{
		Assert.AreEqual( "376,25", RunText( TotalLine, XmlLanguage.GetLanguage( "nl-NL" ) ), "Also when the window gets its language after the binding." );
		Assert.AreEqual( "376.25", RunText( TotalLine, XmlLanguage.GetLanguage( "en-US" ) ) );
	}
}
