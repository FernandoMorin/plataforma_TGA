using System;
using System.Collections.Generic;

namespace PlataformaTGA.Models;

public partial class CatEstadoEgresado
{
    public int IdEstadoEgresado { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public virtual ICollection<Egresado> Egresado { get; set; } = new List<Egresado>();
}
