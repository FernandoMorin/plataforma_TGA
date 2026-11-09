using System;
using System.Collections.Generic;

namespace PlataformaTGA.Models;

public partial class Empleo
{
    public int IdEmpleo { get; set; }

    public int ClaveAlumno { get; set; }

    public int? IdEmpresa { get; set; }

    public string Tipo { get; set; } = null!;

    public string? AreaProfesional { get; set; }

    public virtual Egresado ClaveAlumnoNavigation { get; set; } = null!;

    public virtual ICollection<DetalleEmpleo> DetalleEmpleo { get; set; } = new List<DetalleEmpleo>();

    public virtual Empresa? IdEmpresaNavigation { get; set; }
}
