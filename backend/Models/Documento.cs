using System;
using System.Collections.Generic;

namespace PlataformaTGA.Models;

public partial class Documento
{
    public int IdDocumento { get; set; }

    public int? ClaveAlumno { get; set; }

    public int? IdTesis { get; set; }

    public int? IdRevision { get; set; }

    public string? CveActa { get; set; }

    public int? IdTipoDocumento { get; set; }

    public string? NombreArchivo { get; set; }

    public string? RutaArchivo { get; set; }

    public DateTime? FechaCarga { get; set; }

    /// <summary>
    /// Quién lo subió
    /// </summary>
    public int? IdUsuario { get; set; }

    /// <summary>
    /// Constancia revisada por el coordinador
    /// </summary>
    public bool Verificado { get; set; }

    public virtual Alumno? ClaveAlumnoNavigation { get; set; }

    public virtual ActaComite? CveActaNavigation { get; set; }

    public virtual Revision? IdRevisionNavigation { get; set; }

    public virtual Tesis? IdTesisNavigation { get; set; }

    public virtual CatTipoDocumento? IdTipoDocumentoNavigation { get; set; }

    public virtual Usuario? IdUsuarioNavigation { get; set; }
}
