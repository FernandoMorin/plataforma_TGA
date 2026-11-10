namespace PlataformaTGA.Models;

// Catálogo de líneas de investigación (D9). No viene del scaffolding: se agregó con la migración LineaInvestigacion.
public partial class CatLineaInvestigacion
{
    public int IdLineaInvestigacion { get; set; }

    public string Nombre { get; set; } = null!;

    public bool Activo { get; set; } = true;

    public virtual ICollection<Tesis> Tesis { get; set; } = new List<Tesis>();
}
