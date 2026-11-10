using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlataformaTGA.Auth;
using PlataformaTGA.Data;

namespace PlataformaTGA.Controllers;

public record ExportarInput(FiltrosReporte Filtros, string Formato, int? Grafica);

// Reportes del coordinador (Figma R1-R3 generar, R4 historial).
[ApiController, Route("api/reportes"), Authorize(Roles = $"{Roles.Coordinador},{Roles.Administrador}")]
public class ReportesController(TgaDbContext db, IConfiguration config, IWebHostEnvironment env) : ControllerBase
{
    const int Pdf = 1, Excel = 2, Imagen = 3;
    static readonly Dictionary<int, (string Nombre, string Extension, string Contenido)> Formatos = new()
    {
        [Pdf] = ("PDF", "pdf", "application/pdf"),
        [Excel] = ("Excel", "xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"),
        [Imagen] = ("Imagen", "png", "image/png"),
    };

    [HttpGet("generaciones")]
    public async Task<IActionResult> Generaciones() =>
        Ok(await db.Generacion.AsNoTracking().OrderBy(g => g.AnioIngreso).Select(g => new { id = g.IdGeneracion, nombre = g.NombreGeneracion }).ToListAsync());

    [HttpPost("vista")]
    public async Task<IActionResult> Vista(FiltrosReporte f) =>
        Reportes.Tipos.ContainsKey(f.Tipo) ? Ok(await Reportes.Generar(db, f)) : BadRequest(new { mensaje = "Tipo de reporte desconocido." });

    [HttpPost("exportar")]
    public async Task<IActionResult> Exportar(ExportarInput i)
    {
        if (!Reportes.Tipos.TryGetValue(i.Filtros.Tipo, out var tipo)) return BadRequest(new { mensaje = "Tipo de reporte desconocido." });
        var formato = i.Formato switch { "pdf" => Pdf, "excel" => Excel, "imagen" => Imagen, _ => 0 };
        if (formato == 0) return BadRequest(new { mensaje = "Formato desconocido." });

        var datos = await Reportes.Generar(db, i.Filtros);
        var grafica = formato == Imagen && i.Grafica is int n && n >= 0 && n < datos.Graficas.Count ? datos.Graficas[n] : null;
        if (formato == Imagen && grafica is null) return BadRequest(new { mensaje = "Gráfica desconocida." });
        var bytes = formato switch
        {
            Pdf => Reportes.Pdf(datos, DateTime.Now),
            Excel => Reportes.Excel(datos, DateTime.Now),
            _ => Reportes.Imagen(grafica!),
        };
        var (_, extension, contenido) = Formatos[formato];
        var enDisco = $"{Guid.NewGuid():N}.{extension}";
        var carpeta = DocumentosController.Carpeta(config, env);
        Directory.CreateDirectory(carpeta);
        await System.IO.File.WriteAllBytesAsync(Path.Combine(carpeta, enDisco), bytes);

        db.Reporte.Add(new Models.Reporte
        {
            IdTipoReporte = tipo.Id, IdFormatoReporte = formato, IdUsuarioGenero = User.IdUsuario(),
            FechaGeneracion = DateTime.UtcNow, FiltrosUsados = grafica is null ? datos.Filtros : $"Gráfica: {grafica.Titulo} · {datos.Filtros}", RutaArchivo = enDisco,
        });
        await db.SaveChangesAsync();
        return File(bytes, contenido, Nombre(tipo.Nombre, DateTime.Now, extension));
    }

    // R4
    [HttpGet("historial")]
    public async Task<IActionResult> Historial()
    {
        var reportes = await db.Reporte.AsNoTracking().OrderByDescending(r => r.FechaGeneracion).ThenByDescending(r => r.IdReporte).Select(r => new
        {
            r.IdReporte, r.FechaGeneracion, r.IdTipoReporte, Catalogo = r.IdTipoReporteNavigation!.Nombre, r.FiltrosUsados,
            Genero = r.IdUsuarioGeneroNavigation!.Nombre, r.IdFormatoReporte,
        }).ToListAsync();
        // Nombre corto, como en las tarjetas de Generar reporte (Figma R4).
        var cortos = Reportes.Tipos.Values.ToDictionary(t => t.Id, t => t.Nombre);
        return Ok(reportes.Select(r => new
        {
            id = r.IdReporte, fecha = r.FechaGeneracion, tipo = r.IdTipoReporte is int t && cortos.TryGetValue(t, out var n) ? n : r.Catalogo,
            filtros = r.FiltrosUsados, genero = r.Genero, formato = Formatos.TryGetValue(r.IdFormatoReporte ?? 0, out var f) ? f.Nombre : "",
        }));
    }

    [HttpGet("{id:int}/archivo")]
    public async Task<IActionResult> Archivo(int id)
    {
        var r = await db.Reporte.AsNoTracking().Where(x => x.IdReporte == id)
            .Select(x => new { x.RutaArchivo, x.IdFormatoReporte, x.FechaGeneracion, x.IdTipoReporte }).SingleOrDefaultAsync();
        if (r?.RutaArchivo is null) return NotFound();
        // GetFileName descarta cualquier ruta que venga en la base: no se puede salir de la carpeta.
        var ruta = Path.Combine(DocumentosController.Carpeta(config, env), Path.GetFileName(r.RutaArchivo));
        if (!System.IO.File.Exists(ruta) || !Formatos.TryGetValue(r.IdFormatoReporte ?? 0, out var formato)) return NotFound();
        var fecha = r.FechaGeneracion is { } f ? DateTime.SpecifyKind(f, DateTimeKind.Utc).ToLocalTime() : DateTime.Now;
        return PhysicalFile(ruta, formato.Contenido, Nombre(Reportes.Tipos.Values.FirstOrDefault(t => t.Id == r.IdTipoReporte).Nombre ?? "Reporte", fecha, formato.Extension));
    }

    static string Nombre(string tipo, DateTime fecha, string extension) =>
        $"Reporte_{string.Concat(tipo.Normalize(System.Text.NormalizationForm.FormD).Where(char.IsAsciiLetterOrDigit))}_{fecha:yyyyMMdd_HHmm}.{extension}";
}
