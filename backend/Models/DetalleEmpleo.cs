using System;
using System.Collections.Generic;

namespace PlataformaTGA.Models;

public partial class DetalleEmpleo
{
    public int IdDetalleEmpleo { get; set; }

    public int IdEmpleo { get; set; }

    public string? Puesto { get; set; }

    public DateTime? FechaInicio { get; set; }

    /// <summary>
    /// Nula o futura = empleo vigente
    /// </summary>
    public DateTime? FechaFinal { get; set; }

    public decimal? Sueldo { get; set; }

    public bool? RelacionPosgrado { get; set; }

    public string? Actividades { get; set; }

    public virtual Empleo IdEmpleoNavigation { get; set; } = null!;
}
