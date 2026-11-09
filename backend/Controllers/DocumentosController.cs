using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlataformaTGA.Auth;
using PlataformaTGA.Data;

namespace PlataformaTGA.Controllers;

// Descarga de documentos. Los archivos viven fuera de la carpeta pública, en Documentos:Carpeta
// (por defecto App_Data/documentos), con el nombre que guarda documento.ruta_archivo.
[ApiController, Route("api/documentos")]
public class DocumentosController(TgaDbContext db, IConfiguration config, IWebHostEnvironment env) : ControllerBase
{
    // shortcut: por ahora solo coordinador y administrador; abrir al alumno dueño y a su comité cuando existan sus pantallas.
    [HttpGet("{id:int}"), Authorize(Roles = $"{Roles.Coordinador},{Roles.Administrador}")]
    public async Task<IActionResult> Descargar(int id)
    {
        var doc = await db.Documento.AsNoTracking().Where(d => d.IdDocumento == id)
            .Select(d => new { d.RutaArchivo, d.NombreArchivo }).SingleOrDefaultAsync();
        if (doc?.RutaArchivo is null) return NotFound();

        var carpeta = Path.GetFullPath(config["Documentos:Carpeta"] ?? Path.Combine(env.ContentRootPath, "App_Data", "documentos"));
        // GetFileName descarta cualquier ruta que venga en la base: no se puede salir de la carpeta.
        var ruta = Path.Combine(carpeta, Path.GetFileName(doc.RutaArchivo));
        if (!System.IO.File.Exists(ruta)) return NotFound();

        return PhysicalFile(ruta, "application/pdf", doc.NombreArchivo ?? Path.GetFileName(ruta));
    }
}
