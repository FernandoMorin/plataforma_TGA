using System;
using System.Collections.Generic;

namespace PlataformaTGA.Models;

public partial class Usuario
{
    public int IdUsuario { get; set; }

    public string Correo { get; set; } = null!;

    public string ContrasenaHash { get; set; } = null!;

    public int? IdRolUsuario { get; set; }

    public bool? Activo { get; set; }

    public int? IntentosFallidos { get; set; }

    public DateTime? BloqueadoHasta { get; set; }

    public bool? RequiereCambio { get; set; }

    public DateTime? FechaCreacion { get; set; }

    public DateTime? UltimoAcceso { get; set; }

    /// <summary>
    /// RPE, clave de alumno, EXT-… o identificador de egresado
    /// </summary>
    public string? ClaveInstitucional { get; set; }

    public string? Nombre { get; set; }

    public virtual ICollection<Alumno> Alumno { get; set; } = new List<Alumno>();

    public virtual ICollection<Documento> Documento { get; set; } = new List<Documento>();

    public virtual CatRolUsuario? IdRolUsuarioNavigation { get; set; }

    public virtual ICollection<LogAcceso> LogAcceso { get; set; } = new List<LogAcceso>();

    public virtual ICollection<ProfesorDetalle> ProfesorDetalle { get; set; } = new List<ProfesorDetalle>();

    public virtual ICollection<Reporte> Reporte { get; set; } = new List<Reporte>();

    public virtual ICollection<SesionUsuario> SesionUsuario { get; set; } = new List<SesionUsuario>();

    public virtual ICollection<TokenRecuperacion> TokenRecuperacion { get; set; } = new List<TokenRecuperacion>();
}
