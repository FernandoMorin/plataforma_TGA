using System;
using System.Collections.Generic;

namespace PlataformaTGA.Models;

public partial class Revision
{
    public int IdRevision { get; set; }

    public DateTime? Fecha { get; set; }

    public int IdTesis { get; set; }

    public int? IdPeriodo { get; set; }

    public string? Comentarios { get; set; }

    public decimal? PorcentajeAvance { get; set; }

    public string Estado { get; set; } = null!;

    public byte? CalifDocumento { get; set; }

    public byte? CalifExposicion { get; set; }

    public byte? CalifDominio { get; set; }

    public byte? CalifGradoAvance { get; set; }

    public virtual ICollection<ActaComite> ActaComite { get; set; } = new List<ActaComite>();

    public virtual ICollection<AcuerdoEvaluacion> AcuerdoEvaluacion { get; set; } = new List<AcuerdoEvaluacion>();

    public virtual ICollection<Documento> Documento { get; set; } = new List<Documento>();

    public virtual PeriodoAcademico? IdPeriodoNavigation { get; set; }

    public virtual Tesis IdTesisNavigation { get; set; } = null!;

    public virtual ICollection<RevisionProfesor> RevisionProfesor { get; set; } = new List<RevisionProfesor>();
}
