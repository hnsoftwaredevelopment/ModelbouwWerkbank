using Syncfusion.UI.Xaml.TreeGrid;

namespace Modelbouwer.Helpers;

/// <summary>
/// Keeps the open and closed branches of a tree grid when its items are rebuilt, for example after a save reloads
/// the list. Nodes are matched by a key of their item (the id), because the reload creates new item objects.
/// </summary>
public static class TreeGridExpansionState
{
	/// <summary>The keys of the expanded nodes; null when the grid shows nothing yet (first load keeps its default).</summary>
	public static HashSet<object>? Capture( SfTreeGrid grid, Func<object, object?> keyOf )
	{
		if ( grid.View == null || grid.View.Nodes.Count == 0 )
			return null;

		var expanded = new HashSet<object>();

		void Walk( IEnumerable<TreeNode> nodes )
		{
			foreach ( var node in nodes )
			{
				if ( !node.IsExpanded || node.Item == null || keyOf( node.Item ) is not { } key )
					continue;

				expanded.Add( key );
				Walk( node.ChildNodes );
			}
		}

		Walk( grid.View.Nodes );
		return expanded;
	}

	public static void Restore( SfTreeGrid grid, HashSet<object> expanded, Func<object, object?> keyOf )
	{
		if ( grid.View == null )
			return;

		void Walk( IEnumerable<TreeNode> nodes )
		{
			// Copied with foreach: TreeNodeCollection does not implement CopyTo, which ToList() uses.
			var snapshot = new List<TreeNode>();
			foreach ( var item in nodes )
				snapshot.Add( item );

			foreach ( var node in snapshot )
			{
				var key = node.Item == null ? null : keyOf( node.Item );
				if ( key != null && expanded.Contains( key ) )
				{
					if ( !node.IsExpanded )
						grid.ExpandNode( node );
					Walk( node.ChildNodes );
				}
				else if ( node.IsExpanded )
				{
					grid.CollapseNode( node );
				}
			}
		}

		Walk( grid.View.Nodes );
	}

	/// <summary>Wires a view's tree grid to the rebuild hooks of its view model.</summary>
	public static void KeepExpansion( SfTreeGrid grid, Func<object, object?> keyOf, Action<Action, Action> setHooks )
	{
		HashSet<object>? expanded = null;
		setHooks(
			() => expanded = Capture( grid, keyOf ),
			() =>
			{
				var keep = expanded;
				if ( keep == null )
					return;
				// The grid builds its nodes after the collection changed, so restore once it is done.
				_ = grid.Dispatcher.BeginInvoke( new Action( () => Restore( grid, keep, keyOf ) ), System.Windows.Threading.DispatcherPriority.Loaded );
			} );
	}
}
