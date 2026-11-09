using System;
using System.Collections.Generic;

namespace PlataformaTGA.Models;

public partial class ActaComite
{
    public string CveActa { get; set; } = null!;

    public int ClaveAlumno { get; set; }

    public int IdComite { get; set; }

    public int? IdRevision { get; set; }

    public int? IdToefl { get; set; }

    public int? NumEvaluacion { get; set; }

    public DateTime? FechaReunion { get; set; }

    public string? Observaciones { get; set; }

    public string? Resultado { get; set; }

    public string? ArchivoActa { get; set; }

    public bool? Estado { get; set; }

    public virtual Alumno ClaveAlumnoNavigation { get; set; } = null!;

    public virtual ICollection<Documento> Documento { get; set; } = new List<Documento>();

    public virtual Comite IdComiteNavigation { get; set; } = null!;

    public virtual Revision? IdRevisionNavigation { get; set; }

    public virtual RequisitoToefl? IdToeflNavigation { get; set; }

    public virtual ICollection<Tesis> Tesis { get; set; } = new List<Tesis>();
}
