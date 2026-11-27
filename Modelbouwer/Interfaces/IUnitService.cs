using System;
using System.Collections.Generic;
using System.Text;

namespace Modelbouwer.Interfaces;

public interface IUnitService
{
	Task<List<UnitModel>> GetAllUnitsAsync();
	Task<List<UnitModel>> GetAllUnitsAsync( CancellationToken cancellationToken );
	Task<int> InsertNewUnitAsync( Dictionary<string, object?> queryParameters );
	Task<int> InsertNewUnitAsync( Dictionary<string, object?> queryParameters, CancellationToken cancellationToken );
	Task UpdateUnitAsync( Dictionary<string, object?> queryParameters );
	Task UpdateUnitAsync( Dictionary<string, object?> queryParameters, CancellationToken cancellationToken );
	Task DeleteUnitAsync( int unitId );
	Task DeleteUnitAsync( int unitId, CancellationToken cancellationToken );

	Task<bool> NameExistsAsync( string? unitName );
	Task<bool> NameExistsAsync( string? unitName, CancellationToken cancellationToken );
}