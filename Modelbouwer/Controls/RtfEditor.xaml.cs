namespace Modelbouwer.Controls;

public partial class RtfEditor : UserControl
{
	/// <summary>True while the document is filled from <see cref="RtfText"/>: that is a load, not an edit.</summary>
	private bool _loadingDocument;

	/// <summary>True while <see cref="RtfText"/> is set from the document, so the document is not reloaded (caret stays).</summary>
	private bool _writingFromEditor;

	public RtfEditor()
	{
		InitializeComponent();

		// Every edit goes to the bound memo at once (KAN-147). Before, the memo was only written when the focus left
		// the editor; the toolbar buttons take no focus, so Save stayed off until the user switched tab.
		RtbEditor.TextChanged += ( _, _ ) =>
		{
			if ( !_loadingDocument )
				WriteToSource();
		};
	}

	/// <summary>Writes the document to <see cref="RtfText"/> and pushes it to the bound property.</summary>
	internal void WriteToSource()
	{
		_writingFromEditor = true;
		try
		{
			UpdateRtfTextFromEditor();
			BindingOperations.GetBindingExpression( this, RtfTextProperty )?.UpdateSource();
		}
		finally
		{
			_writingFromEditor = false;
		}
	}

	public static readonly DependencyProperty RtfTextProperty =
		DependencyProperty.Register(
			nameof(RtfText),
			typeof(string),
			typeof(RtfEditor),
			new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnRtfTextChanged));

	public string RtfText
	{
		get => ( string ) GetValue( RtfTextProperty );
		set => SetValue( RtfTextProperty, value );
	}

	private static void OnRtfTextChanged( DependencyObject d, DependencyPropertyChangedEventArgs e )
	{
		if ( d is not RtfEditor editor || editor._writingFromEditor )
			return;

		var rtb = editor.RtbEditor;
		var rtf = e.NewValue as string;

		editor._loadingDocument = true;
		try
		{
			rtb.Document.Blocks.Clear();
			if ( !string.IsNullOrEmpty( rtf ) )
			{
				using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rtf));
				var range = new TextRange(rtb.Document.ContentStart, rtb.Document.ContentEnd);
				range.Load( stream, DataFormats.Rtf );
			}
		}
		finally
		{
			editor._loadingDocument = false;
		}
	}

	public void UpdateRtfTextFromEditor()
	{
		var range = new TextRange(RtbEditor.Document.ContentStart, RtbEditor.Document.ContentEnd);
		using var stream = new MemoryStream();
		range.Save( stream, DataFormats.Rtf );
		RtfText = Encoding.UTF8.GetString( stream.ToArray() );
	}

	// StrikeThrough button
	private void StrikeThrough_Click( object sender, RoutedEventArgs e )
	{
		var selection = RtbEditor.Selection;
		if ( selection != null )
		{
			var current = selection.GetPropertyValue(Inline.TextDecorationsProperty);
			if ( current == DependencyProperty.UnsetValue || current != TextDecorations.Strikethrough )
				selection.ApplyPropertyValue( Inline.TextDecorationsProperty, TextDecorations.Strikethrough );
			else
				selection.ApplyPropertyValue( Inline.TextDecorationsProperty, null );
		}
	}

	public void UpdateBinding()
	{
		var bindingExpression = BindingOperations.GetBindingExpression(this, RtfTextProperty);
		bindingExpression?.UpdateSource();
	}
}