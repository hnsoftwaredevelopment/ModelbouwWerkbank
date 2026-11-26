using System;
using System.Collections.Generic;
using System.Text;

namespace Modelbouwer.Interfaces;

public interface IEntityValidator<T>
{
	Task<ValidationResult> ValidateAsync( T entity );
	Task<ValidationResult> ValidateAsync( T entity, CancellationToken cancellationToken );
}