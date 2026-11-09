using System;
using System.Collections.Generic;

namespace PlataformaTGA.Models;

public partial class CatSituacionLaboral
{
    public int IdSituacionLaboral { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }
}
