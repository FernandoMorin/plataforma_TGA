using System;
using System.Collections.Generic;

namespace PlataformaTGA.Models;

public partial class ComiteProfesor
{
    public int IdComiteProfesor { get; set; }

    public int IdComite { get; set; }

    public string Rpe { get; set; } = null!;

    public int IdRolComite { get; set; }

    public DateTime? FechaAsignacion { get; set; }

    public bool? Activo { get; set; }

    public DateTime? FechaBaja { get; set; }

    public virtual Comite IdComiteNavigation { get; set; } = null!;

    public virtual CatRolComite IdRolComiteNavigation { get; set; } = null!;

    public virtual Profesor RpeNavigation { get; set; } = null!;
}
