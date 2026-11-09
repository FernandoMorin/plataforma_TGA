using System;
using System.Collections.Generic;

namespace PlataformaTGA.Models;

public partial class Generacion
{
    public int IdGeneracion { get; set; }

    public int IdPrograma { get; set; }

    public int? AnioIngreso { get; set; }

    public int? AnioEgresoEstimado { get; set; }

    public string? NombreGeneracion { get; set; }

    public virtual ICollection<Alumno> Alumno { get; set; } = new List<Alumno>();

    public virtual CatProgramaAcademico IdProgramaNavigation { get; set; } = null!;
}
