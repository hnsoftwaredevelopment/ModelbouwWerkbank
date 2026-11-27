using System.Collections.Generic;

namespace Modelbouwer.Services;

/// <summary>Columns, headers and rows of a grid export.</summary>
public class ExportData<T>
{
	public List<ColumnInfo> ColumnInfos { get; set; } = new();
	public List<string> Headers { get; set; } = new();
	public List<T> Items { get; set; } = new();

	/// <summary>Tree depth of each item in <see cref="Items"/> (0 = top level). Empty for a flat grid (KAN-148).</summary>
	public List<int> Levels { get; set; } = new();

	/// <summary>Mapping name of the column that shows the tree, indented per level in Excel (KAN-148).</summary>
	public string? HierarchyColumn { get; set; }
}
