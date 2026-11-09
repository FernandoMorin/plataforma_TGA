using System;
using System.Collections.Generic;

namespace PlataformaTGA.Models;

public partial class CatFormatoReporte
{
    public int IdFormatoReporte { get; set; }

    public string Nombre { get; set; } = null!;

    public virtual ICollection<Reporte> Reporte { get; set; } = new List<Reporte>();
}
