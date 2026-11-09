using System;
using System.Collections.Generic;

namespace PlataformaTGA.Models;

public partial class CatTipoDocumento
{
    public int IdTipoDocumento { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public virtual ICollection<Documento> Documento { get; set; } = new List<Documento>();
}
