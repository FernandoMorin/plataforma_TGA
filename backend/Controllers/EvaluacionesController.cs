using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlataformaTGA.Auth;
using PlataformaTGA.Data;

namespace PlataformaTGA.Controllers;

public record RevisionInput(
    [Range(0, 100)] byte Documento, [Range(0, 100)] byte Exposicion, [Range(0, 100)] byte Dominio, [Range(0, 100)] byte GradoAvance,
    [Range(0, 100)] decimal Avance, [Required, MaxLength(4000)] string Comentarios);

public record ResolucionInput(DateTime Fecha, [Required, MaxLength(6)] string CveActa);
public record CompromisoInput([Required, MaxLength(500)] string Descripcion, DateTime Fecha);
public record ConsolidacionInput(
    [Range(0, 100)] byte? Documento, [Range(0, 100)] byte? Exposicion, [Range(0, 100)] byte? Dominio, [Range(0, 100)] byte? GradoAvance,
    [Range(0, 100)] decimal? Avance, [MaxLength(4000)] string? Comentarios,
    List<int> Cumplidos, [MaxLength(20)] List<CompromisoInput> Nuevos, bool Enviar);

// Evaluaciones del profesor (Figma PT4), su revisión individual (PT5) y la consolidación del director (PT6).
[ApiController, Route("api/profesor/evaluaciones"), Authorize(Roles = $"{Roles.Profesor},{Roles.Coordinador}")]
public class EvaluacionesController(TgaDbContext db) : ControllerBase
{
    static object Error(string mensaje) => new { mensaje };

    async Task<string?> MiRpe()
    {
        var id = User.IdUsuario();
        return await db.ProfesorDetalle.Where(d => d.IdUsuario == id).Select(d => d.Rpe).FirstOrDefaultAsync();
    }

    // Informes de los trabajos donde soy integrante vigente, con su número dentro del trabajo y los conteos del comité.
    async Task<List<Informe>> Informes(string rpe, int? idRevision = null)
    {
        var miembros = Titulacion.MiembrosVigentes(db);
        var filas = await db.Revision.AsNoTracking()
            .Where(r => idRevision == null || r.IdRevision == idRevision)
            .Where(r => miembros.Any(m => m.Rpe == rpe && m.IdComiteNavigation.IdTesis == r.IdTesis))
            .Select(r => new
            {
                r.IdRevision, r.IdTesis, r.Estado, r.IdPeriodoNavigation!.FechaInicio,
                Numero = r.IdTesisNavigation.Revision.Count(x => x.IdRevision <= r.IdRevision),
                Periodo = r.IdPeriodoNavigation!.Nombre,
                r.IdTesisNavigation.ClaveAlumno, r.IdTesisNavigation.Titulo,
                Modalidad = r.IdTesisNavigation.IdModalidadNavigation!.Nombre,
                r.IdTesisNavigation.ClaveAlumnoNavigation.Nombre, r.IdTesisNavigation.ClaveAlumnoNavigation.ApellidoPaterno,
                r.IdTesisNavigation.ClaveAlumnoNavigation.ApellidoMaterno,
                Integrantes = miembros.Where(m => m.IdComiteNavigation.IdTesis == r.IdTesis)
                    .Select(m => new { m.Rpe, Rol = m.IdRolComiteNavigation.Nombre }).ToList(),
                Revisiones = r.RevisionProfesor.Select(p => new { p.Rpe, p.FechaEnvio, p.FechaConformidad }).ToList(),
            })
            .OrderByDescending(r => r.FechaInicio).ThenBy(r => r.Nombre).ToListAsync();

        return filas.Select(r =>
        {
            var rpes = r.Integrantes.Select(i => i.Rpe).ToHashSet();
            var mia = r.Revisiones.FirstOrDefault(p => p.Rpe == rpe);
            return new Informe(r.IdRevision, r.IdTesis, r.Estado, r.Numero, r.Periodo, r.ClaveAlumno, r.Titulo, r.Modalidad,
                Titulacion.NombreCompleto(r.Nombre, r.ApellidoPaterno, r.ApellidoMaterno),
                r.Integrantes.First(i => i.Rpe == rpe).Rol, r.Integrantes.Count,
                r.Revisiones.Count(p => rpes.Contains(p.Rpe) && p.FechaEnvio != null),
                r.Revisiones.Count(p => rpes.Contains(p.Rpe) && p.FechaConformidad != null),
                mia?.FechaEnvio != null, mia?.FechaConformidad != null);
        }).ToList();
    }

    record Informe(int IdRevision, int IdTesis, string Estado, int Numero, string? Periodo, int Clave, string? Titulo, string? Modalidad,
        string Alumno, string MiRol, int Integrantes, int Enviadas, int Firmadas, bool EnvieMiRevision, bool Firme);

    // Lo que ve el profesor en la columna Estado (PT4) y la acción que le toca.
    static (string Texto, string Tono, string? Accion) EstadoParaMi(Informe i) => i.Estado switch
    {
        "Abierta" when !i.EnvieMiRevision => ("Tu revisión pendiente", "rojo", "Revisar"),
        "Abierta" => ($"Revisiones · {i.Enviadas} de {i.Integrantes}", "gris", null),
        "En consolidación" => ($"Por consolidar · {i.Enviadas} de {i.Integrantes}", "ambar", i.MiRol == "Director" ? "Consolidar" : null),
        "En firma" => ($"Por firmar · {i.Firmadas} de {i.Integrantes}", "azul", i.Firme ? null : "Firmar"),
        _ => ("Cerrada", "verde", "Ver acta"),
    };

    // Detalle de un alumno para su comité (sustituye a la lista PT4): su trabajo, el comité y sus informes,
    // cada uno con lo que me toca hacer. Lo ve el director aunque aún no arme el comité.
    [HttpGet("~/api/profesor/alumnos/{idTesis:int}")]
    public async Task<IActionResult> Alumno(int idTesis)
    {
        var rpe = await MiRpe();
        if (rpe is null) return Forbid();
        await Titulacion.AbrirInformes(db);
        var t = await db.Tesis.AsNoTracking()
            .Where(x => x.IdTesis == idTesis && x.EstadoSolicitud == "Aceptada"
                && (x.RpeDirector == rpe || x.Comite.Any(c => c.ComiteProfesor.Any(m => m.Rpe == rpe && m.Activo == true))))
            .Select(x => new
            {
                x.ClaveAlumno, x.Titulo, x.PorcentajeActual, x.FechaRegistro, x.RpeDirector,
                Modalidad = x.IdModalidadNavigation!.Nombre, Linea = x.IdLineaInvestigacionNavigation!.Nombre,
                Estado = x.IdEstadoTesisNavigation!.Nombre, Generacion = x.ClaveAlumnoNavigation.IdGeneracionNavigation!.NombreGeneracion,
                x.ClaveAlumnoNavigation.Nombre, x.ClaveAlumnoNavigation.ApellidoPaterno, x.ClaveAlumnoNavigation.ApellidoMaterno,
                Comite = x.Comite.Select(c => c.Clave).FirstOrDefault(),
            }).SingleOrDefaultAsync();
        if (t is null) return NotFound();

        var integrantes = await Titulacion.MiembrosVigentes(db).AsNoTracking().Where(m => m.IdComiteNavigation.IdTesis == idTesis)
            .OrderBy(m => m.IdRolComite).ThenBy(m => m.IdComiteProfesor)
            .Select(m => new { m.Rpe, m.RpeNavigation.Nombre, m.RpeNavigation.ApellidoPaterno, m.RpeNavigation.ApellidoMaterno, Rol = m.IdRolComiteNavigation.Nombre })
            .ToListAsync();
        var mios = (await Informes(rpe)).Where(i => i.IdTesis == idTesis).ToDictionary(i => i.IdRevision);
        var informes = await Titulacion.InformesDelTrabajo(db, idTesis);
        // Avances que subió el alumno; lo que aún no tiene informe va al siguiente (se le asocia al abrirse).
        int[] tipos = [TipoDocumento.Avance, TipoDocumento.Evidencia, TipoDocumento.TrabajoFinal];
        var numeros = informes.ToDictionary(i => i.Id, i => i.Numero);
        var avances = (await db.Documento.AsNoTracking()
                .Where(d => d.IdTesis == idTesis && d.IdTipoDocumento != null && tipos.Contains(d.IdTipoDocumento.Value))
                .OrderByDescending(d => d.FechaCarga)
                .Select(d => new { d.IdDocumento, d.NombreArchivo, Tipo = d.IdTipoDocumentoNavigation!.Nombre, d.IdRevision, d.FechaCarga }).ToListAsync())
            .Select(d => new
            {
                id = d.IdDocumento, nombre = d.NombreArchivo, tipo = d.Tipo, fecha = d.FechaCarga,
                informe = d.IdRevision is { } r && numeros.TryGetValue(r, out var n) ? n : informes.Count + 1,
            }).ToList();

        return Ok(new
        {
            alumno = Titulacion.NombreCompleto(t.Nombre, t.ApellidoPaterno, t.ApellidoMaterno), clave = t.ClaveAlumno, generacion = t.Generacion,
            modalidad = t.Modalidad, titulo = t.Titulo, linea = t.Linea, estado = t.Estado ?? "Sin iniciar", avance = t.PorcentajeActual ?? 0,
            fechaRegistro = t.FechaRegistro, comite = t.Comite, soyDirector = t.RpeDirector == rpe,
            puedeResolver = t.RpeDirector == rpe && (t.PorcentajeActual ?? 0) >= 100 && t.Estado != "Aceptada" && t.Estado != "Cancelada"
                && informes.All(i => i.Cerrada),
            integrantes = integrantes.Select(m => new { nombre = Titulacion.NombreCompleto(m.Nombre, m.ApellidoPaterno, m.ApellidoMaterno), rol = m.Rol, soyYo = m.Rpe == rpe }),
            avances,
            informes = informes.Select(i =>
            {
                var (texto, tono, accion) = mios.TryGetValue(i.Id, out var mio) ? EstadoParaMi(mio) : (i.Estado, "gris", null);
                return new { informe = i, estadoParaMi = texto, tono, accion };
            }),
        });
    }

    // PT5: el trabajo, las evidencias del informe y mi revisión (si ya la envié, solo lectura).
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Detalle(int id)
    {
        var rpe = await MiRpe();
        if (rpe is null) return Forbid();
        var i = (await Informes(rpe, id)).SingleOrDefault();
        if (i is null) return NotFound();

        var evidencias = await db.Documento.AsNoTracking()
            .Where(d => d.IdRevision == id && (d.IdTipoDocumento == TipoDocumento.Avance || d.IdTipoDocumento == TipoDocumento.Evidencia || d.IdTipoDocumento == TipoDocumento.TrabajoFinal))
            .OrderByDescending(d => d.FechaCarga).Select(d => new { id = d.IdDocumento, nombre = d.NombreArchivo }).ToListAsync();
        var mia = await db.RevisionProfesor.AsNoTracking().Where(p => p.IdRevision == id && p.Rpe == rpe && p.FechaEnvio != null)
            .Select(p => new
            {
                documento = p.CalifDocumento, exposicion = p.CalifExposicion, dominio = p.CalifDominio, gradoAvance = p.CalifGradoAvance,
                avance = p.PorcentajeAvance, comentarios = p.Comentarios, fechaEnvio = p.FechaEnvio,
            }).SingleOrDefaultAsync();

        return Ok(new
        {
            idTesis = i.IdTesis, alumno = i.Alumno, informe = i.Numero, periodo = i.Periodo, modalidad = i.Modalidad, titulo = i.Titulo, miRol = i.MiRol,
            estado = i.Estado, enviadas = i.Enviadas, integrantes = i.Integrantes, evidencias, mia,
        });
    }

    // PT5: enviar mi revisión. Al completarse todas, el informe pasa a En consolidación.
    [HttpPost("{id:int}/revision")]
    public async Task<IActionResult> Enviar(int id, RevisionInput input)
    {
        var rpe = await MiRpe();
        if (rpe is null) return Forbid();
        var comentarios = input.Comentarios.Trim();
        if (comentarios.Length == 0) return BadRequest(Error("Escribe tus comentarios y sugerencias."));

        await using var tx = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT id_revision FROM revision WHERE id_revision = {id} FOR UPDATE");
        var i = (await Informes(rpe, id)).SingleOrDefault();
        if (i is null) return NotFound();
        if (i.Estado != "Abierta") return Conflict(Error("El informe ya no recibe revisiones."));
        if (i.EnvieMiRevision) return Conflict(Error("Ya enviaste tu revisión."));

        var mia = await db.RevisionProfesor.SingleOrDefaultAsync(p => p.IdRevision == id && p.Rpe == rpe);
        if (mia is null) db.RevisionProfesor.Add(mia = new Models.RevisionProfesor { IdRevision = id, Rpe = rpe });
        mia.CalifDocumento = input.Documento;
        mia.CalifExposicion = input.Exposicion;
        mia.CalifDominio = input.Dominio;
        mia.CalifGradoAvance = input.GradoAvance;
        mia.PorcentajeAvance = input.Avance;
        mia.Comentarios = comentarios;
        mia.FechaEnvio = DateTime.UtcNow;
        await db.SaveChangesAsync();
        await Titulacion.PasarAConsolidacion(db, id);
        await tx.CommitAsync();
        return NoContent();
    }

    // Compromisos del informe cerrado anterior: el director marca cuáles se cumplieron.
    Task<int?> InformeAnterior(int idTesis, int idRevision) =>
        db.Revision.Where(r => r.IdTesis == idTesis && r.IdRevision < idRevision && r.Estado == "Cerrada")
            .OrderByDescending(r => r.IdRevision).Select(r => (int?)r.IdRevision).FirstOrDefaultAsync();

    // PT6: revisiones de todos lado a lado, lo que el director lleva guardado y el TOEFL del alumno.
    [HttpGet("{id:int}/consolidacion")]
    public async Task<IActionResult> Consolidacion(int id)
    {
        var rpe = await MiRpe();
        if (rpe is null) return Forbid();
        var i = (await Informes(rpe, id)).SingleOrDefault();
        if (i is null || i.MiRol != "Director") return NotFound();
        if (i.Estado != "En consolidación") return Conflict(Error("El informe no está en consolidación."));

        var miembros = Titulacion.MiembrosVigentes(db).Where(m => m.IdComiteNavigation.IdTesis == i.IdTesis);
        var revisiones = await miembros.OrderBy(m => m.IdRolComite).ThenBy(m => m.IdComiteProfesor)
            .Select(m => new
            {
                m.Rpe, Rol = m.IdRolComiteNavigation.Nombre,
                m.RpeNavigation.Nombre, m.RpeNavigation.ApellidoPaterno, m.RpeNavigation.ApellidoMaterno,
                Revision = db.RevisionProfesor.Where(p => p.IdRevision == id && p.Rpe == m.Rpe)
                    .Select(p => new { p.CalifDocumento, p.CalifExposicion, p.CalifDominio, p.CalifGradoAvance, p.PorcentajeAvance, p.Comentarios })
                    .FirstOrDefault(),
            }).ToListAsync();
        var r = await db.Revision.AsNoTracking().Where(x => x.IdRevision == id)
            .Select(x => new { x.CalifDocumento, x.CalifExposicion, x.CalifDominio, x.CalifGradoAvance, x.PorcentajeAvance, x.Comentarios })
            .SingleAsync();
        var anterior = await InformeAnterior(i.IdTesis, id);
        var anteriores = anterior is null ? [] : await db.AcuerdoEvaluacion.AsNoTracking().Where(a => a.IdRevision == anterior)
            .OrderBy(a => a.IdAcuerdo).Select(a => new { id = a.IdAcuerdo, descripcion = a.Descripcion, fecha = a.FechaCompromiso, cumplido = a.Cumplido == true })
            .ToListAsync();
        var nuevos = await db.AcuerdoEvaluacion.AsNoTracking().Where(a => a.IdRevision == id)
            .OrderBy(a => a.IdAcuerdo).Select(a => new { descripcion = a.Descripcion, fecha = a.FechaCompromiso }).ToListAsync();
        var toefl = await db.RequisitoToefl.AsNoTracking().Where(t => t.ClaveAlumno == i.Clave).OrderByDescending(t => t.IdToefl)
            .Select(t => new { estado = t.IdEstadoToeflNavigation!.Nombre, fecha = t.FechaPresentacion, puntaje = t.Puntaje }).FirstOrDefaultAsync();

        return Ok(new
        {
            idTesis = i.IdTesis, alumno = i.Alumno, informe = i.Numero, periodo = i.Periodo,
            integrantes = revisiones.Select(m => new
            {
                encabezado = $"{m.ApellidoPaterno ?? m.Nombre} ({m.Rol})",
                nombre = Titulacion.NombreCompleto(m.Nombre, m.ApellidoPaterno, m.ApellidoMaterno),
                esMio = m.Rpe == rpe,
                documento = m.Revision?.CalifDocumento, exposicion = m.Revision?.CalifExposicion, dominio = m.Revision?.CalifDominio,
                gradoAvance = m.Revision?.CalifGradoAvance, avance = m.Revision?.PorcentajeAvance, comentarios = m.Revision?.Comentarios,
            }),
            final = new
            {
                documento = r.CalifDocumento, exposicion = r.CalifExposicion, dominio = r.CalifDominio, gradoAvance = r.CalifGradoAvance,
                avance = r.PorcentajeAvance, comentarios = r.Comentarios,
            },
            informeAnterior = anterior is null ? (int?)null : i.Numero - 1,
            anteriores, nuevos,
            toefl = toefl ?? new { estado = "Pendiente", fecha = (DateTime?)null, puntaje = (decimal?)null },
        });
    }

    // PT6: «Guardar» deja el borrador; «Enviar a firma» exige todo completo y pasa el informe a En firma.
    [HttpPut("{id:int}/consolidacion")]
    public async Task<IActionResult> Consolidar(int id, ConsolidacionInput input)
    {
        var rpe = await MiRpe();
        if (rpe is null) return Forbid();
        var comentarios = string.IsNullOrWhiteSpace(input.Comentarios) ? null : input.Comentarios.Trim();
        var nuevos = input.Nuevos.Select(n => (Descripcion: n.Descripcion.Trim(), n.Fecha.Date)).ToList();
        if (nuevos.Any(n => n.Descripcion.Length == 0)) return BadRequest(Error("Escribe la descripción de cada compromiso."));
        if (input.Enviar && (input.Documento is null || input.Exposicion is null || input.Dominio is null || input.GradoAvance is null || input.Avance is null))
            return BadRequest(Error("Captura todas las calificaciones finales."));
        if (input.Enviar && comentarios is null) return BadRequest(Error("Escribe los comentarios y sugerencias."));

        await using var tx = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT id_revision FROM revision WHERE id_revision = {id} FOR UPDATE");
        var i = (await Informes(rpe, id)).SingleOrDefault();
        if (i is null || i.MiRol != "Director") return NotFound();
        if (i.Estado != "En consolidación") return Conflict(Error("El informe no está en consolidación."));

        var r = await db.Revision.SingleAsync(x => x.IdRevision == id);
        r.CalifDocumento = input.Documento;
        r.CalifExposicion = input.Exposicion;
        r.CalifDominio = input.Dominio;
        r.CalifGradoAvance = input.GradoAvance;
        r.PorcentajeAvance = input.Avance;
        r.Comentarios = comentarios;

        var anterior = await InformeAnterior(i.IdTesis, id);
        if (anterior is not null)
            foreach (var a in await db.AcuerdoEvaluacion.Where(a => a.IdRevision == anterior).ToListAsync())
                a.Cumplido = input.Cumplidos.Contains(a.IdAcuerdo);

        // Los compromisos nuevos se reemplazan completos en cada guardado; aún no los ve nadie más.
        db.AcuerdoEvaluacion.RemoveRange(await db.AcuerdoEvaluacion.Where(a => a.IdRevision == id).ToListAsync());
        db.AcuerdoEvaluacion.AddRange(nuevos.Select(n => new Models.AcuerdoEvaluacion
        {
            IdRevision = id, Descripcion = n.Descripcion, FechaCompromiso = n.Date, Cumplido = false,
        }));

        if (input.Enviar)
        {
            r.Estado = "En firma";
            r.Fecha = DateTime.Today; // fecha del acta
        }
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return NoContent();
    }

    // PT7: el acta tal como quedará, las firmas y si me toca firmar.
    [HttpGet("{id:int}/acta")]
    public async Task<IActionResult> VerActa(int id)
    {
        var rpe = await MiRpe();
        if (rpe is null) return Forbid();
        var i = (await Informes(rpe, id)).SingleOrDefault();
        if (i is null) return NotFound();
        if (i.Estado != "En firma" && i.Estado != "Cerrada") return Conflict(Error("El acta aún no está lista."));
        var acta = await Acta.Armar(db, id);
        var documento = await db.Documento.AsNoTracking()
            .Where(d => d.IdRevision == id && d.IdTipoDocumento == TipoDocumento.ActaEvaluacion)
            .Select(d => (int?)d.IdDocumento).FirstOrDefaultAsync();
        return Ok(new
        {
            idTesis = i.IdTesis, alumno = i.Alumno, informe = i.Numero, periodo = i.Periodo, miRol = i.MiRol, estado = i.Estado,
            firmadas = i.Firmadas, integrantes = i.Integrantes, firme = i.Firme, puedoFirmar = i.Estado == "En firma" && !i.Firme && i.EnvieMiRevision,
            encabezado = new[] { Acta.Universidad, Acta.Posgrado, acta.Titulo }, fecha = acta.Fecha, parrafos = acta.Parrafos,
            firmas = acta.Firmas.Select(f => new { nombre = f.Nombre, rol = f.Rol, conformidad = f.Conformidad }),
            documento,
        });
    }

    // PT7: firma de conformidad. Con la última firma se cierra el informe: se genera el acta en PDF y se actualiza el avance.
    [HttpPost("{id:int}/firmar")]
    public async Task<IActionResult> Firmar(int id, [FromServices] IConfiguration config, [FromServices] IWebHostEnvironment env)
    {
        var rpe = await MiRpe();
        if (rpe is null) return Forbid();

        await using var tx = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT id_revision FROM revision WHERE id_revision = {id} FOR UPDATE");
        var i = (await Informes(rpe, id)).SingleOrDefault();
        if (i is null) return NotFound();
        if (i.Estado != "En firma") return Conflict(Error("El acta no está en firma."));
        if (i.Firme) return Conflict(Error("Ya firmaste el acta."));
        var mia = await db.RevisionProfesor.SingleOrDefaultAsync(p => p.IdRevision == id && p.Rpe == rpe && p.FechaEnvio != null);
        if (mia is null) return Conflict(Error("Solo firma quien envió su revisión."));
        mia.FechaConformidad = DateTime.UtcNow;
        await db.SaveChangesAsync();

        string? archivo = null;
        if (i.Firmadas + 1 >= i.Integrantes)
            archivo = await Cerrar(id, i, config, env);
        try
        {
            await tx.CommitAsync();
        }
        catch
        {
            if (archivo is not null) System.IO.File.Delete(archivo);
            throw;
        }
        return NoContent();
    }

    async Task<string> Cerrar(int id, Informe i, IConfiguration config, IWebHostEnvironment env)
    {
        var r = await db.Revision.Include(x => x.IdTesisNavigation).SingleAsync(x => x.IdRevision == id);
        r.Estado = "Cerrada";
        r.IdTesisNavigation.PorcentajeActual = r.PorcentajeAvance;
        r.IdTesisNavigation.IdEstadoTesis = EstadoTesis.EnProgreso;
        r.IdTesisNavigation.UltimaActualizacion = DateTime.UtcNow;

        // Clave del acta (D8): AC + consecutivo de 4 dígitos.
        var ultima = await db.ActaComite.Where(a => a.CveActa.StartsWith("AC")).Select(a => a.CveActa).MaxAsync(c => (string?)c);
        var cve = $"AC{(ultima is null ? 1 : int.Parse(ultima[2..]) + 1):D4}";
        var idComite = await db.Comite.Where(c => c.IdTesis == r.IdTesis).Select(c => c.IdComite).SingleAsync();
        var idToefl = await db.RequisitoToefl.Where(t => t.ClaveAlumno == i.Clave).OrderByDescending(t => t.IdToefl).Select(t => (int?)t.IdToefl).FirstOrDefaultAsync();
        await db.SaveChangesAsync();

        var contenido = await Acta.Armar(db, id);
        var enDisco = $"{Guid.NewGuid():N}.pdf";
        var carpeta = DocumentosController.Carpeta(config, env);
        Directory.CreateDirectory(carpeta);
        var ruta = Path.Combine(carpeta, enDisco);
        await System.IO.File.WriteAllBytesAsync(ruta, Acta.Pdf(contenido));

        var promedio = new[] { r.CalifDocumento, r.CalifExposicion, r.CalifDominio, r.CalifGradoAvance }.Average(c => (decimal?)c);
        db.ActaComite.Add(new Models.ActaComite
        {
            CveActa = cve, ClaveAlumno = i.Clave, IdComite = idComite, IdRevision = id, IdToefl = idToefl, NumEvaluacion = i.Numero,
            FechaReunion = r.Fecha, Observaciones = contenido.Parrafos.Last(), Resultado = $"Promedio {Math.Round(promedio ?? 0)}",
            ArchivoActa = enDisco, Estado = true,
        });
        db.Documento.Add(new Models.Documento
        {
            ClaveAlumno = i.Clave, IdTesis = r.IdTesis, IdRevision = id, CveActa = cve, IdTipoDocumento = TipoDocumento.ActaEvaluacion,
            NombreArchivo = $"Acta_{cve}_Informe{i.Numero}.pdf", RutaArchivo = enDisco, FechaCarga = DateTime.UtcNow, IdUsuario = User.IdUsuario(),
        });
        await db.SaveChangesAsync();
        return ruta;
    }

    // PT9: el trabajo que dirijo llegó a 100 % y no tiene informe en curso.
    async Task<Models.Tesis?> PorResolver(int idTesis, string rpe)
    {
        var t = await db.Tesis.SingleOrDefaultAsync(x => x.IdTesis == idTesis && x.RpeDirector == rpe && x.EstadoSolicitud == "Aceptada"
            && x.IdEstadoTesis != EstadoTesis.Aceptada && x.IdEstadoTesis != EstadoTesis.Cancelada && x.PorcentajeActual >= 100);
        return t is null || await db.Revision.AnyAsync(r => r.IdTesis == idTesis && r.Estado != "Cerrada") ? null : t;
    }

    [HttpGet("~/api/profesor/alumnos/{idTesis:int}/resolucion")]
    public async Task<IActionResult> Resolucion(int idTesis)
    {
        var rpe = await MiRpe();
        if (rpe is null) return Forbid();
        if (await PorResolver(idTesis, rpe) is null) return NotFound();
        var t = await db.Tesis.AsNoTracking().Where(x => x.IdTesis == idTesis).Select(x => new
        {
            x.ClaveAlumno, x.Titulo, x.PorcentajeActual, Modalidad = x.IdModalidadNavigation!.Nombre,
            x.ClaveAlumnoNavigation.Nombre, x.ClaveAlumnoNavigation.ApellidoPaterno, x.ClaveAlumnoNavigation.ApellidoMaterno,
        }).SingleAsync();
        var informes = await Titulacion.InformesDelTrabajo(db, idTesis);
        var actas = await db.ActaComite.AsNoTracking().Where(a => a.IdRevisionNavigation!.IdTesis == idTesis)
            .Select(a => new { a.CveActa, a.IdRevision, a.FechaReunion }).ToListAsync();
        return Ok(new
        {
            alumno = Titulacion.NombreCompleto(t.Nombre, t.ApellidoPaterno, t.ApellidoMaterno), clave = t.ClaveAlumno,
            modalidad = t.Modalidad, titulo = t.Titulo, avance = t.PorcentajeActual, cerradas = informes.Count(i => i.Cerrada),
            // La más reciente primero: es la propuesta.
            actas = actas.Select(a => new { cve = a.CveActa, numero = informes.FirstOrDefault(i => i.Id == a.IdRevision)?.Numero, fecha = a.FechaReunion })
                .OrderByDescending(a => a.numero),
        });
    }

    // Con la resolución el trabajo queda Aceptada; con eso el coordinador ya puede marcar al alumno como Titulado.
    [HttpPost("~/api/profesor/alumnos/{idTesis:int}/resolucion")]
    public async Task<IActionResult> Resolver(int idTesis, ResolucionInput input)
    {
        var rpe = await MiRpe();
        if (rpe is null) return Forbid();
        if (input.Fecha.Date > DateTime.Today) return BadRequest(Error("La fecha de resolución no puede ser futura."));
        await using var tx = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT id_tesis FROM tesis WHERE id_tesis = {idTesis} FOR UPDATE");
        var t = await PorResolver(idTesis, rpe);
        if (t is null) return Conflict(Error("El trabajo no está listo para la resolución final."));
        if (!await db.ActaComite.AnyAsync(a => a.CveActa == input.CveActa && a.IdRevisionNavigation!.IdTesis == idTesis))
            return BadRequest(Error("Elige un acta de este trabajo."));

        t.IdEstadoTesis = EstadoTesis.Aceptada;
        t.FechaCierre = input.Fecha.Date;
        t.CveActaResolucion = input.CveActa;
        t.UltimaActualizacion = DateTime.UtcNow;
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return NoContent();
    }
}
