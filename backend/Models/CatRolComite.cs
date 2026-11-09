using System;
using System.Collections.Generic;

namespace PlataformaTGA.Models;

public partial class CatRolComite
{
    public int IdRolComite { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public virtual ICollection<ComiteProfesor> ComiteProfesor { get; set; } = new List<ComiteProfesor>();
}
