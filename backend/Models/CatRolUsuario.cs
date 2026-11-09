using System;
using System.Collections.Generic;

namespace PlataformaTGA.Models;

public partial class CatRolUsuario
{
    public int IdRolUsuario { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public bool? Activo { get; set; }

    public virtual ICollection<Usuario> Usuario { get; set; } = new List<Usuario>();
}
