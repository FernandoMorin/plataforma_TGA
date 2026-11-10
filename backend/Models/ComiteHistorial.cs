namespace PlataformaTGA.Models;

// Historial exacto de cambios del comité (RF-03.6). No viene del scaffolding: se agregó con la migración ComiteHistorial.
public partial class ComiteHistorial
{
    public int IdHistorial { get; set; }

    public int IdComite { get; set; }

    /// <summary>Fecha del cambio que indicó quien lo hizo.</summary>
    public DateTime Fecha { get; set; }

    /// <summary>Alta, Baja, Reemplazo o Cambio de rol.</summary>
    public string Accion { get; set; } = null!;

    public string? RpeSale { get; set; }

    public string? RpeEntra { get; set; }

    public int? RolAnterior { get; set; }

    public int? RolNuevo { get; set; }

    /// <summary>Quién hizo el cambio.</summary>
    public int? IdUsuario { get; set; }

    public DateTime FechaRegistro { get; set; }

    public virtual Comite IdComiteNavigation { get; set; } = null!;
}
