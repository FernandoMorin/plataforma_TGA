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
    public static string Carpeta(IConfiguration config, IWebHostEnvironment env) =>
        Path.GetFullPath(config["Documentos:Carpeta"] ?? Path.Combine(env.ContentRootPath, "App_Data", "documentos"));

    public const long MaximoBytes = 10 * 1024 * 1024;
    public const long LimitePeticion = MaximoBytes + 64 * 1024;

    // Lo que suben los usuarios: PDF de 10 MB como máximo. Devuelve el error o null si es válido.
    public static async Task<string?> ValidarPdf(IFormFile? archivo, string queFalta)
    {
        if (archivo is null || archivo.Length == 0) return queFalta;
        if (archivo.Length > MaximoBytes) return "El archivo supera los 10 MB.";
        var cabecera = new byte[5];
        await using var s = archivo.OpenReadStream();
        var esPdf = await s.ReadAtLeastAsync(cabecera, 5, throwOnEndOfStream: false) == 5 && cabecera.AsSpan().SequenceEqual("%PDF-"u8);
        return esPdf ? null : "El archivo debe ser un PDF.";
    }

    // El nombre en disco lo pone el servidor; el del usuario solo se guarda para mostrarlo y descargarlo.
    public static async Task<(string EnDisco, string Ruta, string Nombre)> GuardarPdf(IFormFile archivo, IConfiguration config, IWebHostEnvironment env)
    {
        var enDisco = $"{Guid.NewGuid():N}.pdf";
        var carpeta = Carpeta(config, env);
        Directory.CreateDirectory(carpeta);
        var ruta = Path.Combine(carpeta, enDisco);
        await using (var destino = System.IO.File.Create(ruta)) await archivo.CopyToAsync(destino);
        var nombre = Path.GetFileName(archivo.FileName);
        return (enDisco, ruta, nombre.Length > 255 ? nombre[^255..] : nombre);
    }

    // Coordinador y administrador descargan todo; el alumno y el egresado, los suyos; el profesor, los de alumnos en cuyo comité está.
    [HttpGet("{id:int}"), Authorize(Roles = $"{Roles.Coordinador},{Roles.Administrador},{Roles.Alumno},{Roles.Profesor},{Roles.Egresado}")]
    public async Task<IActionResult> Descargar(int id)
    {
        var doc = await db.Documento.AsNoTracking().Where(d => d.IdDocumento == id)
            .Select(d => new { d.RutaArchivo, d.NombreArchivo, d.ClaveAlumno, Dueno = d.ClaveAlumnoNavigation!.IdUsuario }).SingleOrDefaultAsync();
        if (doc?.RutaArchivo is null) return NotFound();
        if (User.IsInRole(Roles.Alumno) && doc.Dueno != User.IdUsuario()) return NotFound();
        if (User.IsInRole(Roles.Egresado))
        {
            var idUsuario = User.IdUsuario();
            var identificador = await db.Usuario.Where(u => u.IdUsuario == idUsuario).Select(u => u.ClaveInstitucional).SingleAsync();
            if (!await db.Egresado.AnyAsync(e => e.IdentificadorEgresado == identificador && e.ClaveAlumno == doc.ClaveAlumno)) return NotFound();
        }
        if (User.IsInRole(Roles.Profesor))
        {
            var idUsuario = User.IdUsuario();
            var rpe = await db.ProfesorDetalle.Where(d => d.IdUsuario == idUsuario).Select(d => d.Rpe).FirstOrDefaultAsync();
            var integrante = rpe is not null && await db.ComiteProfesor.AnyAsync(m =>
                m.Rpe == rpe && m.Activo == true && m.IdComiteNavigation.IdTesisNavigation.ClaveAlumno == doc.ClaveAlumno);
            if (!integrante) return NotFound();
        }

        // GetFileName descarta cualquier ruta que venga en la base: no se puede salir de la carpeta.
        var ruta = Path.Combine(Carpeta(config, env), Path.GetFileName(doc.RutaArchivo));
        if (!System.IO.File.Exists(ruta)) return NotFound();

        return PhysicalFile(ruta, "application/pdf", doc.NombreArchivo ?? Path.GetFileName(ruta));
    }
}
