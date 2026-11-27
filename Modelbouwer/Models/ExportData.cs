using System.Collections.Generic;

namespace Modelbouwer.Services;

/// <summary>Columns, headers and rows of a grid export.</summary>
public class ExportData<T>
{
	public List<ColumnInfo> ColumnInfos { get; set; } = new();
	public List<string> Headers { get; set; } = new();
	public List<T> Items { get; set; } = new();
}
