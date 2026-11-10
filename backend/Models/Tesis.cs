using System;
using System.Collections.Generic;

namespace PlataformaTGA.Models;

public partial class Tesis
{
    public int IdTesis { get; set; }

    public string? Titulo { get; set; }

    public int ClaveAlumno { get; set; }

    public int? IdPrograma { get; set; }

    public decimal? PorcentajeActual { get; set; }

    public int? IdEstadoTesis { get; set; }

    public DateTime? FechaRegistro { get; set; }

    public int? IdLineaInvestigacion { get; set; }

    public virtual CatLineaInvestigacion? IdLineaInvestigacionNavigation { get; set; }

    /// <summary>
    /// Queda fijada al aceptarse la solicitud
    /// </summary>
    public int? IdModalidad { get; set; }

    public string EstadoSolicitud { get; set; } = null!;

    public string? RpeDirector { get; set; }

    public DateTime? FechaSolicitud { get; set; }

    /// <summary>
    /// Cuándo aceptó o rechazó el director
    /// </summary>
    public DateTime? FechaResolucion { get; set; }

    public string? MotivoRechazo { get; set; }

    public DateTime? UltimaActualizacion { get; set; }

    /// <summary>
    /// Resolución final, la registra el director
    /// </summary>
    public DateTime? FechaCierre { get; set; }

    public string? CveActaResolucion { get; set; }

    public virtual Alumno ClaveAlumnoNavigation { get; set; } = null!;

    public virtual ICollection<Comite> Comite { get; set; } = new List<Comite>();

    public virtual ActaComite? CveActaResolucionNavigation { get; set; }

    public virtual ICollection<Documento> Documento { get; set; } = new List<Documento>();

    public virtual CatEstadoTesis? IdEstadoTesisNavigation { get; set; }

    public virtual CatModalidadTitulacion? IdModalidadNavigation { get; set; }

    public virtual CatProgramaAcademico? IdProgramaNavigation { get; set; }

    public virtual ICollection<Revision> Revision { get; set; } = new List<Revision>();

    public virtual Profesor? RpeDirectorNavigation { get; set; }
}
