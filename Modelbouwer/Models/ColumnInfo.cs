namespace Modelbouwer.Services;

/// <summary>One exported grid column.</summary>
public class ColumnInfo
{
	public string MappingName { get; set; } = string.Empty;
	public string HeaderText { get; set; } = string.Empty;
	public dynamic? Column { get; set; } // SfDataGrid or TreeGridColumn
}
