using System;
using System.Collections.Generic;

namespace PlataformaTGA.Models;

public partial class Alumno
{
    /// <summary>
    /// La asigna la universidad
    /// </summary>
    public int ClaveAlumno { get; set; }

    public string? Nombre { get; set; }

    public string? ApellidoPaterno { get; set; }

    public string? ApellidoMaterno { get; set; }

    public DateTime? FechaIngreso { get; set; }

    public int? IdUsuario { get; set; }

    public int? IdPrograma { get; set; }

    public int? IdGeneracion { get; set; }

    public string? CorreoInstitucional { get; set; }

    public string? CorreoPersonal { get; set; }

    public string? Telefono { get; set; }

    /// <summary>
    /// Nulo = Sin asignar
    /// </summary>
    public int? IdModalidad { get; set; }

    public int? IdEstadoAcademico { get; set; }

    /// <summary>
    /// Alumno temporal: aún no está en el registro de la universidad
    /// </summary>
    public bool PendienteConciliar { get; set; }

    public virtual ICollection<ActaComite> ActaComite { get; set; } = new List<ActaComite>();

    public virtual ICollection<AlumnoDetalle> AlumnoDetalle { get; set; } = new List<AlumnoDetalle>();

    public virtual ICollection<Documento> Documento { get; set; } = new List<Documento>();

    public virtual Egresado? Egresado { get; set; }

    public virtual CatEstadoAcademico? IdEstadoAcademicoNavigation { get; set; }

    public virtual Generacion? IdGeneracionNavigation { get; set; }

    public virtual CatModalidadTitulacion? IdModalidadNavigation { get; set; }

    public virtual CatProgramaAcademico? IdProgramaNavigation { get; set; }

    public virtual Usuario? IdUsuarioNavigation { get; set; }

    public virtual ICollection<RequisitoToefl> RequisitoToefl { get; set; } = new List<RequisitoToefl>();

    public virtual ICollection<Tesis> Tesis { get; set; } = new List<Tesis>();
}
