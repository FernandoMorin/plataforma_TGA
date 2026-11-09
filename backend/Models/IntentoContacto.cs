using System;
using System.Collections.Generic;

namespace PlataformaTGA.Models;

public partial class IntentoContacto
{
    public int IdIntento { get; set; }

    public int ClaveAlumno { get; set; }

    public DateTime? FechaIntento { get; set; }

    public string? Observaciones { get; set; }

    public virtual Egresado ClaveAlumnoNavigation { get; set; } = null!;
}
