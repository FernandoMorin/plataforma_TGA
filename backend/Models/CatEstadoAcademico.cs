using System;
using System.Collections.Generic;

namespace PlataformaTGA.Models;

public partial class CatEstadoAcademico
{
    public int IdEstadoAcademico { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public virtual ICollection<Alumno> Alumno { get; set; } = new List<Alumno>();
}
