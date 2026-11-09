using System;
using System.Collections.Generic;

namespace PlataformaTGA.Models;

public partial class Profesor
{
    /// <summary>
    /// RPE de la universidad o EXT-NNN para externos
    /// </summary>
    public string Rpe { get; set; } = null!;

    public string? Nombre { get; set; }

    public string? ApellidoPaterno { get; set; }

    public string? ApellidoMaterno { get; set; }

    public virtual ICollection<ComiteProfesor> ComiteProfesor { get; set; } = new List<ComiteProfesor>();

    public virtual ICollection<ProfesorDetalle> ProfesorDetalle { get; set; } = new List<ProfesorDetalle>();

    public virtual ICollection<RevisionProfesor> RevisionProfesor { get; set; } = new List<RevisionProfesor>();

    public virtual ICollection<Tesis> Tesis { get; set; } = new List<Tesis>();
}
