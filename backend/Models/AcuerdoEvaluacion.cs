using System;
using System.Collections.Generic;

namespace PlataformaTGA.Models;

public partial class AcuerdoEvaluacion
{
    public int IdAcuerdo { get; set; }

    public int IdRevision { get; set; }

    public string? Descripcion { get; set; }

    public DateTime? FechaCompromiso { get; set; }

    public bool? Cumplido { get; set; }

    public virtual Revision IdRevisionNavigation { get; set; } = null!;
}
