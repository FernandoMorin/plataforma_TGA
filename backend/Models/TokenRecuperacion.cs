using System;
using System.Collections.Generic;

namespace PlataformaTGA.Models;

public partial class TokenRecuperacion
{
    public int IdToken { get; set; }

    public int IdUsuario { get; set; }

    /// <summary>
    /// Hash del token, nunca el token
    /// </summary>
    public string Token { get; set; } = null!;

    public DateTime FechaExpiracion { get; set; }

    public bool? Usado { get; set; }

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;
}
