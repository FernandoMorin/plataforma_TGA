using System;
using System.Collections.Generic;

namespace PlataformaTGA.Models;

public partial class Reporte
{
    public int IdReporte { get; set; }

    public int? IdTipoReporte { get; set; }

    public int? IdFormatoReporte { get; set; }

    public int? IdUsuarioGenero { get; set; }

    public DateTime? FechaGeneracion { get; set; }

    public string? FiltrosUsados { get; set; }

    public string? RutaArchivo { get; set; }

    public virtual CatFormatoReporte? IdFormatoReporteNavigation { get; set; }

    public virtual CatTipoReporte? IdTipoReporteNavigation { get; set; }

    public virtual Usuario? IdUsuarioGeneroNavigation { get; set; }
}
