namespace Modelbouwer.Services;

/// <summary>Counts of a CSV import (<see cref="CsvImportService"/>).</summary>
public class CsvImportResult
{
	public int TotalRows { get; set; }
	public int Imported { get; set; }
	public int Skipped { get; set; }
	public int Updated { get; set; }
}
