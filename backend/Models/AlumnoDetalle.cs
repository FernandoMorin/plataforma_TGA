using System;
using System.Collections.Generic;

namespace PlataformaTGA.Models;

public partial class AlumnoDetalle
{
    public int IdAlumnoDetalle { get; set; }

    public int ClaveAlumno { get; set; }

    public int IdPeriodo { get; set; }

    public int? Semestre { get; set; }

    public DateTime? FechaRegistro { get; set; }

    public virtual Alumno ClaveAlumnoNavigation { get; set; } = null!;

    public virtual PeriodoAcademico IdPeriodoNavigation { get; set; } = null!;
}
