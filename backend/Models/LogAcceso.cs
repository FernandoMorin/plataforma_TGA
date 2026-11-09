using System;
using System.Collections.Generic;

namespace PlataformaTGA.Models;

public partial class LogAcceso
{
    public int IdLog { get; set; }

    public int? IdUsuario { get; set; }

    public DateTime? Fecha { get; set; }

    /// <summary>
    /// Correcto | Contraseña incorrecta | Bloqueado | Primer acceso
    /// </summary>
    public string? Resultado { get; set; }

    public string? Ip { get; set; }

    public virtual Usuario? IdUsuarioNavigation { get; set; }
}
