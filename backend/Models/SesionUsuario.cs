using System;
using System.Collections.Generic;

namespace PlataformaTGA.Models;

public partial class SesionUsuario
{
    public string Id { get; set; } = null!;

    public int UsuarioId { get; set; }

    public DateTime UltimaActividad { get; set; }

    public DateTime Creada { get; set; }

    public virtual Usuario Usuario { get; set; } = null!;
}
