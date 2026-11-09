using System;
using System.Collections.Generic;

namespace PlataformaTGA.Models;

public partial class RequisitoToefl
{
    public int IdToefl { get; set; }

    public int ClaveAlumno { get; set; }

    public int? IdEstadoToefl { get; set; }

    /// <summary>
    /// La registra el alumno
    /// </summary>
    public DateTime? FechaPresentacion { get; set; }

    /// <summary>
    /// Lo captura el coordinador al aprobar
    /// </summary>
    public decimal? Puntaje { get; set; }

    public string? Observaciones { get; set; }

    /// <summary>
    /// Próxima fecha para presentar; la registra el alumno
    /// </summary>
    public DateTime? FechaProxima { get; set; }

    public virtual ICollection<ActaComite> ActaComite { get; set; } = new List<ActaComite>();

    public virtual Alumno ClaveAlumnoNavigation { get; set; } = null!;

    public virtual CatEstadoToefl? IdEstadoToeflNavigation { get; set; }
}
