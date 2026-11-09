using System;
using System.Collections.Generic;

namespace PlataformaTGA.Models;

public partial class PeriodoAcademico
{
    public int IdPeriodo { get; set; }

    public string? Nombre { get; set; }

    public DateTime? FechaInicio { get; set; }

    public DateTime? FechaFin { get; set; }

    public string? TipoPeriodo { get; set; }

    public virtual ICollection<AlumnoDetalle> AlumnoDetalle { get; set; } = new List<AlumnoDetalle>();

    public virtual ICollection<Revision> Revision { get; set; } = new List<Revision>();
}
