using System;
using System.Collections.Generic;

namespace PlataformaTGA.Models;

public partial class CatProgramaAcademico
{
    public int IdPrograma { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public bool? Activo { get; set; }

    public virtual ICollection<Alumno> Alumno { get; set; } = new List<Alumno>();

    public virtual ICollection<Generacion> Generacion { get; set; } = new List<Generacion>();

    public virtual ICollection<Tesis> Tesis { get; set; } = new List<Tesis>();
}
