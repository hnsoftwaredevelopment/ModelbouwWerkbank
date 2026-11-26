namespace Modelbouwer.Controls;

/// <summary>
/// Order selection as a dropdown (KAN-104). Uses the DataContext of the screen it is placed on.
/// </summary>
public partial class OrderSelector : UserControl
{
	/// <summary>Optional content shown directly to the right of the dropdown, such as an action for the selected order.</summary>
	public static readonly DependencyProperty SelectedOrderActionsProperty = DependencyProperty.Register(
		nameof( SelectedOrderActions ),
		typeof( object ),
		typeof( OrderSelector ),
		new PropertyMetadata( null ) );

	public object? SelectedOrderActions
	{
		get => GetValue( SelectedOrderActionsProperty );
		set => SetValue( SelectedOrderActionsProperty, value );
	}

	public OrderSelector()
	{
		InitializeComponent();
	}
}
