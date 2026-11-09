using System;
using System.Collections.Generic;

namespace PlataformaTGA.Models;

public partial class RetroalimentacionEgresado
{
    public int IdRetroalimentacion { get; set; }

    public int ClaveAlumno { get; set; }

    public DateTime? Fecha { get; set; }

    public string? Comentario { get; set; }

    public string? Sugerencia { get; set; }

    public virtual Egresado ClaveAlumnoNavigation { get; set; } = null!;
}
