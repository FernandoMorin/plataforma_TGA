using System;
using System.Collections.Generic;

namespace PlataformaTGA.Models;

public partial class ProfesorDetalle
{
    public int IdProfesorDetalle { get; set; }

    public string Rpe { get; set; } = null!;

    public int? IdUsuario { get; set; }

    public string? CorreoInstitucional { get; set; }

    public bool? Activo { get; set; }

    public bool Externo { get; set; }

    /// <summary>
    /// Solo externos
    /// </summary>
    public string? Institucion { get; set; }

    /// <summary>
    /// Solo externos: al vencer se desactiva
    /// </summary>
    public DateTime? VigenteHasta { get; set; }

    public virtual Usuario? IdUsuarioNavigation { get; set; }

    public virtual Profesor RpeNavigation { get; set; } = null!;
}
