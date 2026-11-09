using System;
using System.Collections.Generic;

namespace PlataformaTGA.Models;

public partial class CatTipoReporte
{
    public int IdTipoReporte { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public virtual ICollection<Reporte> Reporte { get; set; } = new List<Reporte>();
}
