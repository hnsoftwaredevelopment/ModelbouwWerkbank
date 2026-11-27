using System.Collections;
using System.ComponentModel;

using Syncfusion.Data;

namespace Modelbouwer.Helpers;

/// <summary>
/// Group subtotal for time entries (KAN-147): the worked time of the rows in a group in decimal hours, for the
/// header of each work type group on the hours tab of the project screen.
/// </summary>
public class WorkTimeAggregate : ISummaryAggregate
{
	/// <summary>Worked time of the group, for example 5,50. Shown with Format="'{Time}'".</summary>
	public string Time { get; set; } = ProjectTime.Format( 0 );

	public Action<IEnumerable, string, PropertyDescriptor> CalculateAggregateFunc() => ( items, _, property ) =>
	{
		if ( property.Name == nameof( Time ) )
			Time = ProjectTime.Format( items.OfType<TimeEntryModel>().Sum( entry => entry.WorkedMinutes ) );
	};
}
