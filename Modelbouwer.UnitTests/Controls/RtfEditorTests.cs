using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Documents;

using Modelbouwer.Controls;

namespace Modelbouwer.UnitTests.Controls;

/// <summary>
/// KAN-147: a memo edit must reach the bound model at once, so the screen's Save button turns on while the focus is
/// still in the editor. Loading a memo into the editor is not an edit.
/// </summary>
[TestClass]
public class RtfEditorTests
{
	private sealed class Memo : INotifyPropertyChanged
	{
		private string? _text;
		public int Writes { get; private set; }

		public string? Text
		{
			get => _text;
			set
			{
				_text = value;
				Writes++;
				PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( nameof( Text ) ) );
			}
		}

		public event PropertyChangedEventHandler? PropertyChanged;
	}

	private const string StoredRtf = @"{\rtf1\ansi\deff0 {\fonttbl {\f0 Segoe UI;}} \f0 Lindehout bestellen\par}";

	private static void RunOnSta( Action action )
	{
		Exception? error = null;
		var thread = new Thread( () =>
		{
			try
			{
				// Lets the control load its XAML from the application assembly without a running application.
				_ = System.IO.Packaging.PackUriHelper.UriSchemePack;
				action();
			}
			catch ( Exception ex )
			{
				error = ex;
			}
		} );
		thread.SetApartmentState( ApartmentState.STA );
		thread.Start();
		thread.Join();
		if ( error != null )
			throw new AssertFailedException( error.ToString(), error );
	}

	private static void Bind( RtfEditor editor, Memo memo ) =>
		editor.SetBinding( RtfEditor.RtfTextProperty, new Binding( nameof( Memo.Text ) ) { Source = memo, Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.Explicit } );

	private static (RtfEditor Editor, Memo Memo) Bound()
	{
		var memo = new Memo();
		memo.Text = StoredRtf;
		var editor = new RtfEditor();
		Bind( editor, memo );
		return (editor, memo);
	}

	private static string DocumentText( RtfEditor editor ) =>
		new TextRange( editor.RtbEditor.Document.ContentStart, editor.RtbEditor.Document.ContentEnd ).Text;

	[TestMethod]
	public void LoadingAMemo_IsNotAnEdit() => RunOnSta( () =>
	{
		var (editor, memo) = Bound();

		StringAssert.Contains( DocumentText( editor ), "Lindehout bestellen" );
		Assert.AreEqual( 1, memo.Writes, "Only the test wrote the memo; showing it wrote nothing back." );
		Assert.AreEqual( StoredRtf, memo.Text );
	} );

	[TestMethod]
	public void TypingInTheEditor_ReachesTheModelAtOnce() => RunOnSta( () =>
	{
		var (editor, memo) = Bound();

		editor.RtbEditor.Document.ContentEnd.InsertTextInRun( " en lijm" );

		Assert.IsTrue( memo.Writes > 1, "The edit was written without the focus leaving the editor." );
		StringAssert.Contains( memo.Text, "en lijm" );
		StringAssert.Contains( DocumentText( editor ), "Lindehout bestellen", "The document is not reloaded while typing." );
	} );

	[TestMethod]
	public void SwitchingToAnotherMemo_ShowsItWithoutWritingBack() => RunOnSta( () =>
	{
		var (editor, memo) = Bound();
		var other = new Memo();
		other.Text = @"{\rtf1\ansi Tweede project\par}";

		Bind( editor, other );

		StringAssert.Contains( DocumentText( editor ), "Tweede project" );
		Assert.AreEqual( 1, other.Writes );
		Assert.AreEqual( 1, memo.Writes, "The first memo is not touched either." );
	} );
}
