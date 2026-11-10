using System;
using System.Collections.Generic;

namespace PlataformaTGA.Models;

public partial class Egresado
{
    public int ClaveAlumno { get; set; }

    /// <summary>
    /// Generación + iniciales + consecutivo, p. ej. 2022RSC01
    /// </summary>
    public string? IdentificadorEgresado { get; set; }

    public DateTime? FechaEgreso { get; set; }

    public int? IdEstadoEgresado { get; set; }

    /// <summary>
    /// Correo personal = su usuario
    /// </summary>
    public string? Correo { get; set; }

    public string? Telefono { get; set; }

    public string? Ciudad { get; set; }

    public string? OtrosContactos { get; set; }

    public DateTime? UltimaActualizacion { get; set; }

    /// <summary>
    /// Baja lógica (RF-02.6): false = dado de baja, sin borrar su información
    /// </summary>
    public bool Activo { get; set; } = true;

    public virtual Alumno ClaveAlumnoNavigation { get; set; } = null!;

    public virtual ICollection<Empleo> Empleo { get; set; } = new List<Empleo>();

    public virtual CatEstadoEgresado? IdEstadoEgresadoNavigation { get; set; }

    public virtual ICollection<IntentoContacto> IntentoContacto { get; set; } = new List<IntentoContacto>();

    public virtual ICollection<RetroalimentacionEgresado> RetroalimentacionEgresado { get; set; } = new List<RetroalimentacionEgresado>();
}
