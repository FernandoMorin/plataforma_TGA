using System;
using System.Collections.Generic;

namespace PlataformaTGA.Models;

public partial class CatEstadoToefl
{
    public int IdEstadoToefl { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public virtual ICollection<RequisitoToefl> RequisitoToefl { get; set; } = new List<RequisitoToefl>();
}
