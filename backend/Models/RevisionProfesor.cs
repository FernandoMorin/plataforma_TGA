using System;
using System.Collections.Generic;

namespace PlataformaTGA.Models;

public partial class RevisionProfesor
{
    public int IdRevisionProfesor { get; set; }

    public string Rpe { get; set; } = null!;

    public int IdRevision { get; set; }

    public string? Comentarios { get; set; }

    public byte? CalifDocumento { get; set; }

    public byte? CalifExposicion { get; set; }

    public byte? CalifDominio { get; set; }

    public byte? CalifGradoAvance { get; set; }

    public decimal? PorcentajeAvance { get; set; }

    /// <summary>
    /// Nulo mientras no envía su revisión
    /// </summary>
    public DateTime? FechaEnvio { get; set; }

    /// <summary>
    /// Su firma del acta
    /// </summary>
    public DateTime? FechaConformidad { get; set; }

    public virtual Revision IdRevisionNavigation { get; set; } = null!;

    public virtual Profesor RpeNavigation { get; set; } = null!;
}
