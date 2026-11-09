using System;
using System.Collections.Generic;

namespace PlataformaTGA.Models;

public partial class Empresa
{
    public int IdEmpresa { get; set; }

    public string? Nombre { get; set; }

    public string? Sector { get; set; }

    public string? Pais { get; set; }

    public string? Estado { get; set; }

    public string? Ciudad { get; set; }

    public virtual ICollection<Empleo> Empleo { get; set; } = new List<Empleo>();
}
