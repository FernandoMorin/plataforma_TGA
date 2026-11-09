using System;
using System.Collections.Generic;

namespace PlataformaTGA.Models;

public partial class Comite
{
    public int IdComite { get; set; }

    public int IdTesis { get; set; }

    /// <summary>
    /// CT-AAAA-NNNN, la genera el servidor
    /// </summary>
    public string Clave { get; set; } = null!;

    public virtual ICollection<ActaComite> ActaComite { get; set; } = new List<ActaComite>();

    public virtual ICollection<ComiteProfesor> ComiteProfesor { get; set; } = new List<ComiteProfesor>();

    public virtual Tesis IdTesisNavigation { get; set; } = null!;
}
