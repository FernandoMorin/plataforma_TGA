using System.Globalization;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PlataformaTGA.Data;

public record FirmaActa(string Nombre, string Rol, DateTime? Conformidad);
public record ContenidoActa(string Titulo, DateTime? Fecha, List<string> Parrafos, List<FirmaActa> Firmas);

// Acta de evaluación de avance (anexo del AMyD). El mismo contenido se muestra al firmar (PT7) y se imprime en el PDF.
public static class Acta
{
    public const string Universidad = "UNIVERSIDAD AUTÓNOMA DE SAN LUIS POTOSÍ · FACULTAD DE INGENIERÍA";
    public const string Posgrado = "Posgrado en Tecnología y Gestión del Agua";

    static string F(DateTime? d) => d?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) ?? "—";
    static string N(decimal? v) => v is null ? "—" : Math.Round(v.Value, 2).ToString("0.##", CultureInfo.InvariantCulture);

    public static async Task<ContenidoActa> Armar(TgaDbContext db, int idRevision)
    {
        var r = await db.Revision.AsNoTracking().Where(x => x.IdRevision == idRevision).Select(x => new
        {
            x.IdTesis, x.Fecha, x.PorcentajeAvance, x.Comentarios, x.CalifDocumento, x.CalifExposicion, x.CalifDominio, x.CalifGradoAvance,
            Numero = x.IdTesisNavigation.Revision.Count(o => o.IdRevision <= x.IdRevision),
            x.IdTesisNavigation.Titulo, x.IdTesisNavigation.IdModalidad, x.IdTesisNavigation.ClaveAlumno,
            Programa = x.IdTesisNavigation.ClaveAlumnoNavigation.IdProgramaNavigation!.Nombre,
            x.IdTesisNavigation.ClaveAlumnoNavigation.Nombre, x.IdTesisNavigation.ClaveAlumnoNavigation.ApellidoPaterno,
            x.IdTesisNavigation.ClaveAlumnoNavigation.ApellidoMaterno,
            Compromisos = x.AcuerdoEvaluacion.OrderBy(a => a.IdAcuerdo).Select(a => new { a.Descripcion, a.FechaCompromiso }).ToList(),
        }).SingleAsync();
        var toefl = await db.RequisitoToefl.AsNoTracking().Where(t => t.ClaveAlumno == r.ClaveAlumno).OrderByDescending(t => t.IdToefl)
            .Select(t => new { t.IdEstadoToefl, t.FechaPresentacion, t.Puntaje }).FirstOrDefaultAsync();
        var firmas = await Titulacion.MiembrosVigentes(db).AsNoTracking().Where(m => m.IdComiteNavigation.IdTesis == r.IdTesis)
            .OrderBy(m => m.IdRolComite).ThenBy(m => m.IdComiteProfesor)
            .Select(m => new
            {
                m.RpeNavigation.Nombre, m.RpeNavigation.ApellidoPaterno, m.RpeNavigation.ApellidoMaterno, Rol = m.IdRolComiteNavigation.Nombre,
                Conformidad = db.RevisionProfesor.Where(p => p.IdRevision == idRevision && p.Rpe == m.Rpe).Select(p => p.FechaConformidad).FirstOrDefault(),
            }).ToListAsync();

        // Para el artículo científico solo cambia la redacción.
        var articulo = r.IdModalidad == 2;
        var trabajo = articulo ? "del artículo científico titulado" : "de la tesis titulada";
        var desarrollo = articulo ? "del artículo" : "de la tesis";
        var califs = new decimal?[] { r.CalifDocumento, r.CalifExposicion, r.CalifDominio, r.CalifGradoAvance };
        var promedio = califs.All(c => c is not null) ? califs.Average() : null;
        var alumno = Titulacion.NombreCompleto(r.Nombre, r.ApellidoPaterno, r.ApellidoMaterno);
        var compromisos = r.Compromisos.Count == 0 ? "sin compromisos."
            : string.Join("; ", r.Compromisos.Select(c => $"{c.Descripcion} (para el {F(c.FechaCompromiso)})")) + ".";
        var toeflTexto = toefl?.FechaPresentacion is null ? "Fecha de presentación examen TOEFL: pendiente"
            : $"Fecha de presentación examen TOEFL: {F(toefl.FechaPresentacion)} · Puntaje: {N(toefl.Puntaje)}";

        var parrafos = new List<string>
        {
            // Sin artículo («la alumna», «el alumno»): el sistema no guarda el sexo del alumno.
            $"{alumno}, estudiante del programa de {r.Programa ?? "Maestría en Tecnología y Gestión del Agua"}, presentó su informe de avance No. {r.Numero} {trabajo}: «{r.Titulo}».",
            $"Como resultado de esta evaluación se observó un avance del {N(r.PorcentajeAvance)} % en el desarrollo {desarrollo}, y se generaron los siguientes comentarios y sugerencias: {r.Comentarios}",
            $"Documento de avance: {r.CalifDocumento} · Exposición: {r.CalifExposicion} · Dominio del tema: {r.CalifDominio} · Grado de avance en el periodo: {r.CalifGradoAvance}",
            $"Promedio de la evaluación: {N(promedio is null ? null : Math.Round(promedio.Value))}",
            toeflTexto,
            $"Observaciones y compromisos: {compromisos}",
        };
        var titulo = $"ACTA DE EVALUACIÓN DE AVANCE {(articulo ? "DEL ARTÍCULO CIENTÍFICO" : "DE TESIS")} POR PARTE DEL COMITÉ TUTORIAL";
        return new ContenidoActa(titulo, r.Fecha, parrafos,
            // La conformidad se guarda en UTC; el acta la muestra en hora local.
            firmas.Select(f => new FirmaActa(Titulacion.NombreCompleto(f.Nombre, f.ApellidoPaterno, f.ApellidoMaterno), f.Rol,
                f.Conformidad is null ? null : DateTime.SpecifyKind(f.Conformidad.Value, DateTimeKind.Utc).ToLocalTime())).ToList());
    }

    public static byte[] Pdf(ContenidoActa c) =>
        Document.Create(doc => doc.Page(page =>
        {
            page.Size(PageSizes.Letter);
            page.Margin(2.2f, Unit.Centimetre);
            page.DefaultTextStyle(t => t.FontSize(10.5f));
            page.Content().Column(col =>
            {
                col.Spacing(10);
                col.Item().AlignCenter().Text(Universidad).Bold();
                col.Item().AlignCenter().Text(Posgrado).Bold();
                col.Item().AlignCenter().Text(c.Titulo).Bold();
                col.Item().AlignRight().Text(F(c.Fecha));
                foreach (var p in c.Parrafos) col.Item().Text(p).Justify();
                col.Item().PaddingTop(10).AlignCenter().Text("El Comité Tutorial").Bold();
                foreach (var f in c.Firmas)
                    col.Item().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(4).Row(row =>
                    {
                        row.RelativeItem().Text(f.Nombre);
                        row.ConstantItem(90).Text(f.Rol);
                        row.ConstantItem(140).AlignRight().Text(f.Conformidad is null ? "Pendiente" : $"Conforme · {F(f.Conformidad)}");
                    });
            });
        })).GeneratePdf();
}
