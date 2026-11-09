using System;
using System.Collections.Generic;

namespace PlataformaTGA.Models;

public partial class CatEstadoTesis
{
    public int IdEstadoTesis { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public virtual ICollection<Tesis> Tesis { get; set; } = new List<Tesis>();
}
