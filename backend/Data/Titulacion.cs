using Microsoft.EntityFrameworkCore;
using PlataformaTGA.Models;

namespace PlataformaTGA.Data;

public record CompromisoVista(string? Descripcion, DateTime? Fecha, bool Cumplido);
// Un informe tal como lo ven el alumno (AT5) y su comité: el detalle solo existe cuando está cerrado.
public record InformeVista(int Id, int Numero, string? Periodo, string Estado, bool Cerrada, DateTime? Fecha, decimal? Avance, decimal? Promedio,
    byte? Documento, byte? Exposicion, byte? Dominio, byte? GradoAvance, string? Comentarios, List<CompromisoVista>? Compromisos, int? Acta);

// Reglas de titulación que comparten las pantallas del alumno, del profesor y del coordinador.
public static class Titulacion
{
    public static string NombreCompleto(string? nombre, string? paterno, string? materno) =>
        string.Join(" ", new[] { nombre, paterno, materno }.Where(p => !string.IsNullOrWhiteSpace(p)));

    // Un integrante deja libre su lugar cuando su cuenta se desactiva o, si es externo, se le vence la vigencia.
    public static IQueryable<Profesor> Vigentes(TgaDbContext db)
    {
        var hoy = DateTime.Today;
        return db.Profesor.Where(p => p.ProfesorDetalle.Any(d =>
            d.Activo != false && d.IdUsuarioNavigation!.Activo == true && (!d.Externo || d.VigenteHasta >= hoy)));
    }

    // Integrantes activos del comité cuyo profesor sigue vigente.
    public static IQueryable<ComiteProfesor> MiembrosVigentes(TgaDbContext db)
    {
        var vigentes = Vigentes(db);
        return db.ComiteProfesor.Where(m => m.Activo == true && vigentes.Any(p => p.Rpe == m.Rpe));
    }

    // Comité completo: un director y dos sinodales vigentes (el codirector es opcional).
    public static async Task<HashSet<int>> TesisConComiteCompleto(TgaDbContext db, IEnumerable<int> idsTesis)
    {
        var ids = idsTesis.ToList();
        var conteos = await MiembrosVigentes(db).Where(m => ids.Contains(m.IdComiteNavigation.IdTesis))
            .GroupBy(m => m.IdComiteNavigation.IdTesis)
            .Select(g => new
            {
                IdTesis = g.Key,
                Directores = g.Count(m => m.IdRolComite == RolComite.Director),
                Sinodales = g.Count(m => m.IdRolComite == RolComite.Sinodal),
            }).ToListAsync();
        return conteos.Where(c => c.Directores == 1 && c.Sinodales == 2).Select(c => c.IdTesis).ToHashSet();
    }

    // Informes del trabajo, del más reciente al más antiguo. Lo de un informe sin cerrar (borradores) no se expone.
    public static async Task<List<InformeVista>> InformesDelTrabajo(TgaDbContext db, int idTesis)
    {
        var informes = await db.Revision.AsNoTracking().Where(r => r.IdTesis == idTesis).OrderBy(r => r.IdRevision)
            .Select(r => new
            {
                r.IdRevision, r.Estado, Periodo = r.IdPeriodoNavigation!.Nombre, r.Fecha, r.PorcentajeAvance, r.Comentarios,
                r.CalifDocumento, r.CalifExposicion, r.CalifDominio, r.CalifGradoAvance,
                Compromisos = r.AcuerdoEvaluacion.OrderBy(a => a.IdAcuerdo).Select(a => new CompromisoVista(a.Descripcion, a.FechaCompromiso, a.Cumplido == true)).ToList(),
                Acta = r.Documento.Where(d => d.IdTipoDocumento == TipoDocumento.ActaEvaluacion).Select(d => (int?)d.IdDocumento).FirstOrDefault(),
            }).ToListAsync();

        return informes.Select((r, i) =>
        {
            if (r.Estado != "Cerrada")
                return new InformeVista(r.IdRevision, i + 1, r.Periodo, r.Estado, false, null, null, null, null, null, null, null, null, null, null);
            var califs = new decimal?[] { r.CalifDocumento, r.CalifExposicion, r.CalifDominio, r.CalifGradoAvance };
            var promedio = califs.All(c => c is not null) ? Math.Round(califs.Average()!.Value) : (decimal?)null;
            return new InformeVista(r.IdRevision, i + 1, r.Periodo, r.Estado, true, r.Fecha, r.PorcentajeAvance, promedio,
                r.CalifDocumento, r.CalifExposicion, r.CalifDominio, r.CalifGradoAvance, r.Comentarios, r.Compromisos, r.Acta);
        }).Reverse().ToList();
    }

    // Historial exacto del comité: empareja a quien sale con quien entra (mismo profesor = cambio de rol; mismo rol =
    // reemplazo) y deja el resto como bajas y altas. Se guarda con SaveChanges de quien llama.
    public static void RegistrarHistorial(TgaDbContext db, Comite comite, List<(string Rpe, int Rol)> salen, List<(string Rpe, int Rol)> entran, DateTime fecha, int idUsuario)
    {
        var ahora = DateTime.UtcNow;
        void Agregar(string accion, (string Rpe, int Rol)? sale, (string Rpe, int Rol)? entra) => db.ComiteHistorial.Add(new ComiteHistorial
        {
            IdComiteNavigation = comite, Fecha = fecha, Accion = accion, IdUsuario = idUsuario, FechaRegistro = ahora,
            RpeSale = sale?.Rpe, RolAnterior = sale?.Rol, RpeEntra = entra?.Rpe, RolNuevo = entra?.Rol,
        });
        var pendientes = new List<(string Rpe, int Rol)>(entran);
        foreach (var s in salen)
        {
            var mismo = pendientes.FindIndex(e => e.Rpe == s.Rpe);
            var rol = mismo >= 0 ? -1 : pendientes.FindIndex(e => e.Rol == s.Rol);
            if (mismo >= 0) { Agregar("Cambio de rol", s, pendientes[mismo]); pendientes.RemoveAt(mismo); }
            else if (rol >= 0) { Agregar("Reemplazo", s, pendientes[rol]); pendientes.RemoveAt(rol); }
            else Agregar("Baja", s, null);
        }
        foreach (var e in pendientes) Agregar("Alta", null, e);
    }

    // Tras un cambio de comité con un informe en curso: la revisión de quien sale se descarta. Si falta la revisión de
    // alguien (el que entra) o el comité quedó incompleto, el informe vuelve a Abierta; si estaba en firma, las firmas se
    // reinician porque el acta cambia. Lo consolidado por el director se conserva como borrador.
    public static async Task AjustarInformeTrasCambio(TgaDbContext db, int idTesis, IReadOnlyCollection<string> salen)
    {
        var r = await db.Revision.SingleOrDefaultAsync(x => x.IdTesis == idTesis && x.Estado != "Cerrada");
        if (r is null) return;
        var revisiones = await db.RevisionProfesor.Where(p => p.IdRevision == r.IdRevision).ToListAsync();
        db.RevisionProfesor.RemoveRange(revisiones.Where(p => salen.Contains(p.Rpe)));

        if (r.Estado != "Abierta")
        {
            var integrantes = await MiembrosVigentes(db).Where(m => m.IdComiteNavigation.IdTesis == idTesis).Select(m => m.Rpe).ToListAsync();
            var completo = (await TesisConComiteCompleto(db, [idTesis])).Contains(idTesis);
            var faltaRevision = integrantes.Any(rpe => !revisiones.Any(p => p.Rpe == rpe && p.FechaEnvio != null));
            if (!completo || faltaRevision || r.Estado == "En firma")
                foreach (var p in revisiones) p.FechaConformidad = null;
            if (!completo || faltaRevision) r.Estado = "Abierta";
        }
        await db.SaveChangesAsync();
        if (r.Estado == "Abierta") await PasarAConsolidacion(db, r.IdRevision);
    }

    // Un informe abierto pasa a En consolidación cuando todos los integrantes vigentes enviaron su revisión.
    public static async Task PasarAConsolidacion(TgaDbContext db, int idRevision)
    {
        var r = await db.Revision.SingleAsync(x => x.IdRevision == idRevision);
        if (r.Estado != "Abierta" || !(await TesisConComiteCompleto(db, [r.IdTesis])).Contains(r.IdTesis)) return;
        var integrantes = await MiembrosVigentes(db).Where(m => m.IdComiteNavigation.IdTesis == r.IdTesis).Select(m => m.Rpe).ToListAsync();
        var enviaron = await db.RevisionProfesor.Where(p => p.IdRevision == idRevision && p.FechaEnvio != null && integrantes.Contains(p.Rpe)).CountAsync();
        if (enviaron < integrantes.Count) return;
        r.Estado = "En consolidación";
        await db.SaveChangesAsync();
    }

    // Apertura automática de informes (D10): en el periodo en curso, cada trabajo aceptado y sin cerrar cuyo
    // alumno está activo, cursa 2.º semestre o más y tiene comité completo recibe su informe del periodo.
    // Se llama al leer las pantallas de titulación; no hay tarea programada.
    public static async Task AbrirInformes(TgaDbContext db)
    {
        var hoy = DateTime.Today;
        var periodo = await db.PeriodoAcademico
            .Where(p => p.FechaInicio <= hoy && p.FechaFin >= hoy)
            .OrderByDescending(p => p.FechaInicio).Select(p => (int?)p.IdPeriodo).FirstOrDefaultAsync();
        if (periodo is null) return;

        var candidatas = await db.Tesis
            .Where(t => t.EstadoSolicitud == "Aceptada"
                && t.IdEstadoTesis != EstadoTesis.Aceptada && t.IdEstadoTesis != EstadoTesis.Cancelada
                && t.ClaveAlumnoNavigation.IdEstadoAcademico == EstadoAcademico.Activo
                && t.ClaveAlumnoNavigation.AlumnoDetalle.Any(d => d.IdPeriodo == periodo && d.Semestre >= 2)
                && !t.Revision.Any(r => r.IdPeriodo == periodo))
            .ToListAsync();
        if (candidatas.Count == 0) return;
        var completas = await TesisConComiteCompleto(db, candidatas.Select(t => t.IdTesis));

        var abrir = candidatas.Where(t => completas.Contains(t.IdTesis)).ToList();
        var ids = abrir.Select(t => t.IdTesis).ToList();
        // Lo que el alumno subió sin informe abierto se asocia al que se abre ahora.
        int[] tipos = [TipoDocumento.Avance, TipoDocumento.Evidencia, TipoDocumento.TrabajoFinal];
        var sueltas = await db.Documento.Where(d => d.IdTesis != null && ids.Contains(d.IdTesis.Value) && d.IdRevision == null
            && d.IdTipoDocumento != null && tipos.Contains(d.IdTipoDocumento.Value)).ToListAsync();
        foreach (var t in abrir)
        {
            var nueva = new Revision { IdTesis = t.IdTesis, IdPeriodo = periodo, Estado = "Abierta" };
            db.Revision.Add(nueva);
            foreach (var d in sueltas.Where(d => d.IdTesis == t.IdTesis)) d.IdRevisionNavigation = nueva;
            t.IdEstadoTesis = EstadoTesis.EnEvaluacion;
            t.UltimaActualizacion = DateTime.UtcNow;
        }
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Otra petición abrió el mismo informe al mismo tiempo: lo frena UNIQUE (id_tesis, id_periodo).
            db.ChangeTracker.Clear();
        }
    }
}
