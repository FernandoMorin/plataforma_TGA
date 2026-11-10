using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using PlataformaTGA.Controllers;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PlataformaTGA.Data;

public record FiltrosReporte(string Tipo, List<int>? Generaciones, bool IncluirNoLocalizables, List<string>? Modalidades, List<string>? Estados);
public record Indicador(string Etiqueta, string Valor, string? Nota = null);
public record TablaReporte(string[] Columnas, List<string[]> Filas, string[]? Total = null);
// Lo mismo alimenta la vista previa (con «Grafica»), el PDF y el Excel (indicadores y tabla).
public record DatosReporte(string Tipo, string Titulo, string Filtros, List<Indicador> Indicadores, TablaReporte Tabla, object Grafica, string? Nota = null)
{
    // Las mismas gráficas de la pantalla, en el mismo orden, para el PDF y la descarga como imagen.
    [System.Text.Json.Serialization.JsonIgnore] public List<GraficaPdf> Graficas { get; init; } = [];
}
public record GraficaPdf(string Titulo, string? Subtitulo, Action<IContainer> Dibujar);

// Reportes (RF-06, Figma R1-R3). Los indicadores no se guardan: se calculan al generar (BASE_DE_DATOS 3.12).
public static class Reportes
{
    public static readonly Dictionary<string, (int Id, string Nombre)> Tipos = new()
    {
        ["generacion"] = (4, "Por generación"),
        ["laboral"] = (3, "Situación laboral"),
        ["egresados"] = (1, "Egresados"),
        ["avance"] = (2, "Avance de titulación"),
    };

    static string Pct(int parte, int total) => total == 0 ? "—" : $"{Math.Round(parte * 100m / total)} %";

    public static async Task<DatosReporte> Generar(TgaDbContext db, FiltrosReporte f)
    {
        var generaciones = await db.Generacion.AsNoTracking().OrderBy(g => g.AnioIngreso)
            .Select(g => new { g.IdGeneracion, g.NombreGeneracion, g.AnioEgresoEstimado }).ToListAsync();
        var elegidas = f.Generaciones is { Count: > 0 } ? generaciones.Where(g => f.Generaciones.Contains(g.IdGeneracion)).ToList() : generaciones;
        var ids = elegidas.Select(g => g.IdGeneracion).ToList();
        var nombresGen = elegidas.Count == generaciones.Count ? "Todas" : string.Join(", ", elegidas.Select(g => g.NombreGeneracion));

        switch (f.Tipo)
        {
            case "generacion":
            {
                var alumnos = await db.Alumno.AsNoTracking().Where(a => a.IdGeneracion != null && ids.Contains(a.IdGeneracion.Value)).Select(a => new
                {
                    IdGeneracion = a.IdGeneracion!.Value, a.IdEstadoAcademico,
                    Egreso = a.Egresado != null ? a.Egresado.FechaEgreso : a.Tesis.Where(t => t.FechaCierre != null).Max(t => t.FechaCierre),
                }).ToListAsync();
                var hoy = DateTime.Today;
                var filas = elegidas.Select(g =>
                {
                    var suyos = alumnos.Where(a => a.IdGeneracion == g.IdGeneracion).ToList();
                    var egresados = suyos.Where(a => a.IdEstadoAcademico == EstadoAcademico.Titulado).ToList();
                    // En tiempo = egresó a más tardar el 31 de diciembre del año de egreso estimado de su generación.
                    var enTiempo = egresados.Count(a => a.Egreso is { } e && g.AnioEgresoEstimado is { } fin && e.Year <= fin);
                    var enCurso = egresados.Count == 0 && (g.AnioEgresoEstimado ?? 0) >= hoy.Year;
                    return new
                    {
                        generacion = g.NombreGeneracion ?? "", ingresaron = suyos.Count,
                        activos = suyos.Count(a => a.IdEstadoAcademico == EstadoAcademico.Activo),
                        inactivos = suyos.Count(a => a.IdEstadoAcademico == EstadoAcademico.BajaTemporal),
                        egresados = egresados.Count, baja = suyos.Count(a => a.IdEstadoAcademico == EstadoAcademico.BajaDefinitiva),
                        enTiempo, enCurso,
                    };
                }).ToList();
                var totalEgr = filas.Sum(x => x.egresados);
                var totalTiempo = filas.Sum(x => x.enTiempo);
                string Tiempo(int en, int egr, bool curso) => curso ? "En curso" : egr == 0 ? "—" : $"{Pct(en, egr)} ({en} de {egr})";
                return new DatosReporte(f.Tipo, "Reporte por generación", nombresGen,
                    [
                        new("Alumnos", filas.Sum(x => x.ingresaron).ToString(), $"{filas.Count} {(filas.Count == 1 ? "generación" : "generaciones")}"),
                        new("Activos", filas.Sum(x => x.activos).ToString()),
                        new("Egresados", totalEgr.ToString()),
                        new("Titulación en tiempo", Pct(totalTiempo, totalEgr), totalEgr == 0 ? null : $"{totalTiempo} de {totalEgr} egresados"),
                    ],
                    new TablaReporte(["Generación", "Ingresaron", "Activos", "Inactivos", "Egresados", "Baja", "Titulación en tiempo"],
                        filas.Select(x => new[] { x.generacion, $"{x.ingresaron}", $"{x.activos}", $"{x.inactivos}", $"{x.egresados}", $"{x.baja}", Tiempo(x.enTiempo, x.egresados, x.enCurso) }).ToList(),
                        ["Total", $"{filas.Sum(x => x.ingresaron)}", $"{filas.Sum(x => x.activos)}", $"{filas.Sum(x => x.inactivos)}", $"{totalEgr}", $"{filas.Sum(x => x.baja)}", Tiempo(totalTiempo, totalEgr, false)]),
                    new { porGeneracion = filas })
                {
                    Graficas =
                    [
                        new("Estado de los alumnos por generación", null, c => Agrupadas(c, filas.Select(x => (x.generacion, new[] { x.activos, x.inactivos, x.egresados, x.baja })).ToList())),
                        new("Titulación en tiempo", "De los egresados de cada generación", c => Progreso(c, filas.Select(x => (x.generacion,
                            x.enCurso ? "En curso" : x.egresados == 0 ? "Sin egresados" : $"{Pct(x.enTiempo, x.egresados)} · {x.enTiempo} de {x.egresados}",
                            x.egresados == 0 ? 0f : x.enTiempo * 100f / x.egresados)).ToList())),
                    ],
                };
            }

            case "laboral":
            case "egresados":
            {
                var egresados = await db.Egresado.AsNoTracking()
                    .Where(e => e.Activo && e.ClaveAlumnoNavigation.IdGeneracion != null && ids.Contains(e.ClaveAlumnoNavigation.IdGeneracion.Value)
                        && (f.IncluirNoLocalizables || e.IdEstadoEgresado != Egresados.NoLocalizable))
                    .Select(e => new
                    {
                        e.ClaveAlumno, e.IdentificadorEgresado, e.FechaEgreso, e.IdEstadoEgresado, e.UltimaActualizacion,
                        e.ClaveAlumnoNavigation.Nombre, e.ClaveAlumnoNavigation.ApellidoPaterno, e.ClaveAlumnoNavigation.ApellidoMaterno,
                        Generacion = e.ClaveAlumnoNavigation.IdGeneracionNavigation!.NombreGeneracion,
                    }).ToListAsync();
                var claves = egresados.Select(e => e.ClaveAlumno).ToList();
                var empleos = (await db.Empleo.AsNoTracking().Where(x => claves.Contains(x.ClaveAlumno)).Select(x => new
                {
                    x.ClaveAlumno, Vista = new EmpleoVista(x.IdEmpleo, x.Tipo, x.DetalleEmpleo.Select(d => d.Puesto).FirstOrDefault(), x.IdEmpresaNavigation!.Nombre,
                        x.AreaProfesional, x.DetalleEmpleo.Select(d => d.FechaInicio).FirstOrDefault(), x.DetalleEmpleo.Select(d => d.FechaFinal).FirstOrDefault(),
                        x.DetalleEmpleo.Select(d => d.RelacionPosgrado).FirstOrDefault(), null),
                }).ToListAsync()).ToLookup(x => x.ClaveAlumno, x => x.Vista);
                var datos = egresados.Select(e =>
                {
                    var suyos = empleos[e.ClaveAlumno].ToList();
                    var actual = Egresados.Actual(suyos);
                    var (meses, rango) = Egresados.TiempoParaEmplearse(e.FechaEgreso, suyos);
                    return new
                    {
                        nombre = Titulacion.NombreCompleto(e.Nombre, e.ApellidoPaterno, e.ApellidoMaterno), e.IdentificadorEgresado, generacion = e.Generacion ?? "",
                        situacion = Egresados.Situacion(suyos), actual, meses, rango,
                        noLocalizable = e.IdEstadoEgresado == Egresados.NoLocalizable, sinActualizar = Egresados.SinActualizar(e.UltimaActualizacion),
                    };
                }).OrderBy(x => x.nombre).ToList();
                var filtros = $"{nombresGen} · {(f.IncluirNoLocalizables ? "con no localizables" : "solo localizables")}";
                var noLoc = datos.Count(x => x.noLocalizable);

                if (f.Tipo == "laboral")
                {
                    var conTrabajo = datos.Where(x => x.situacion == "Con trabajo").ToList();
                    var relacionados = conTrabajo.Count(x => x.actual?.Relacion == true);
                    var conTiempo = datos.Where(x => x.meses is not null).ToList();
                    string[] rangos = ["0 a 6 meses", "6 a 12 meses", "12 a 24 meses", "Más de 24 meses"];
                    var barras = rangos.Select(r => new { etiqueta = r, cantidad = datos.Count(x => x.rango == r) })
                        .Append(new { etiqueta = "Sin empleo", cantidad = datos.Count(x => x.meses is null) }).ToList();
                    return new DatosReporte(f.Tipo, "Reporte de situación laboral", filtros,
                        [
                            new("Con trabajo", Pct(conTrabajo.Count, datos.Count), $"{conTrabajo.Count} de {datos.Count} egresados"),
                            new("Empleo relacionado con la maestría", Pct(relacionados, conTrabajo.Count), "De los que trabajan"),
                            new("Tiempo promedio para emplearse", conTiempo.Count == 0 ? "—" : $"{Math.Round(conTiempo.Average(x => x.meses!.Value))} meses"),
                        ],
                        new TablaReporte(["Egresado", "Generación", "Situación laboral", "Tiempo para emplearse", "Relación con la maestría"],
                            datos.Select(x => new[] { x.nombre, x.generacion, x.situacion, x.meses is null ? "Sin empleo" : $"{x.meses} meses", x.actual?.Relacion is null ? "—" : x.actual.Relacion.Value ? "Sí" : "No" }).ToList()),
                        new { rangos = barras },
                        f.IncluirNoLocalizables && noLoc > 0 ? $"Incluye {noLoc} {(noLoc == 1 ? "egresado no localizable" : "egresados no localizables")} con su último dato registrado." : null)
                    {
                        Graficas = [new("Tiempo para emplearse después de egresar", null, c => Horizontales(c, barras.Select(b => (b.etiqueta, b.cantidad)).ToList(), "Sin empleo"))],
                    };
                }

                string[] situaciones = ["Con trabajo", "Becario", "Otro", "Sin trabajo"];
                return new DatosReporte(f.Tipo, "Reporte de egresados", filtros,
                    [
                        new("Egresados", datos.Count.ToString()),
                        new("Localizables", datos.Count(x => !x.noLocalizable).ToString()),
                        new("No localizables", noLoc.ToString()),
                        new("Sin actualizar", datos.Count(x => x.sinActualizar).ToString(), "Más de un año"),
                    ],
                    new TablaReporte(["Egresado", "Identificador", "Generación", "Situación laboral", "Empleo actual", "Estado"],
                        datos.Select(x => new[] { x.nombre, x.IdentificadorEgresado ?? "", x.generacion, x.situacion,
                            x.actual is null ? "—" : string.Join(" · ", new[] { x.actual.Puesto, x.actual.Empresa }.Where(s => !string.IsNullOrWhiteSpace(s))),
                            x.noLocalizable ? "No localizable" : "Localizable" }).ToList()),
                    new { situaciones = situaciones.Select(s => new { etiqueta = s, cantidad = datos.Count(x => x.situacion == s) }) })
                {
                    Graficas = [new("Egresados por situación laboral", null, c => Horizontales(c, situaciones.Select(s => (s, datos.Count(x => x.situacion == s))).ToList(), "Sin trabajo"))],
                };
            }

            case "avance":
            {
                var alumnos = await db.Alumno.AsNoTracking().Where(a => a.IdGeneracion != null && ids.Contains(a.IdGeneracion.Value)).Select(a => new
                {
                    a.Nombre, a.ApellidoPaterno, a.ApellidoMaterno, Estado = a.IdEstadoAcademicoNavigation!.Nombre,
                    Modalidad = a.IdModalidadNavigation!.Nombre,
                    Semestre = a.AlumnoDetalle.OrderByDescending(d => d.IdPeriodoNavigation.FechaInicio).Select(d => d.Semestre).FirstOrDefault(),
                    Avance = a.Tesis.Where(t => t.EstadoSolicitud == "Aceptada").OrderByDescending(t => t.IdTesis).Select(t => t.PorcentajeActual).FirstOrDefault(),
                    Evaluaciones = a.Tesis.Where(t => t.EstadoSolicitud == "Aceptada").SelectMany(t => t.Revision).Count(r => r.Estado == "Cerrada"),
                }).ToListAsync();
                var filas = alumnos
                    .Where(a => (f.Modalidades is not { Count: > 0 } || f.Modalidades.Contains(a.Modalidad ?? "Sin asignar"))
                        && (f.Estados is not { Count: > 0 } || f.Estados.Contains(a.Estado ?? "")))
                    .Select(a =>
                    {
                        var avance = a.Avance ?? 0;
                        var rezago = AlumnosController.RezagoPorAvance(a.Estado, a.Semestre, avance);
                        // Rezago por tiempo: semestres de más sobre los 4 del programa (+1 amarillo, +2 naranja, +3 rojo).
                        var extra = a.Estado == "Activo" && a.Semestre is int s ? Math.Max(0, s - 4) : 0;
                        return new
                        {
                            alumno = Titulacion.NombreCompleto(a.Nombre, a.ApellidoPaterno, a.ApellidoMaterno), modalidad = a.Modalidad ?? "Sin asignar",
                            avance, esperado = a.Semestre is int sem ? AlumnosController.AvanceEsperado(sem) : (decimal?)null,
                            evaluaciones = a.Evaluaciones, rezago, extra,
                            tiempo = extra == 0 ? "En tiempo" : $"+{Math.Min(extra, 3)} semestre{(extra > 1 ? "s" : "")}",
                        };
                    }).OrderBy(x => x.alumno).ToList();
                var aplican = filas.Where(x => x.rezago != "No aplica").ToList();
                var fuera = filas.Where(x => x.extra > 0).ToList();
                var esperados = filas.Where(x => x.esperado is not null).Select(x => x.esperado!.Value).ToList();
                var filtros = string.Join(" · ", new[] { nombresGen,
                    f.Modalidades is { Count: > 0 } ? string.Join(", ", f.Modalidades) : null,
                    f.Estados is { Count: > 0 } ? string.Join(", ", f.Estados) : null }.Where(s => s is not null));
                return new DatosReporte(f.Tipo, "Reporte de avance de titulación", filtros,
                    [
                        new("Avance promedio", filas.Count == 0 ? "—" : $"{Math.Round(filas.Average(x => x.avance))} %", esperados.Count == 0 ? null : $"Esperado: {Math.Round(esperados.Average())} %"),
                        new("En rezago por avance", $"{aplican.Count(x => x.rezago == "En rezago")} de {aplican.Count}"),
                        new("Fuera de tiempo", fuera.Count.ToString(), fuera.Count == 0 ? null : string.Join(" · ", fuera.GroupBy(x => x.tiempo).OrderBy(g => g.Key).Select(g => $"{g.Key}: {g.Count()}"))),
                        new("Evaluaciones cerradas", filas.Sum(x => x.evaluaciones).ToString()),
                    ],
                    new TablaReporte(["Alumno", "Modalidad", "Avance", "Esperado", "Evaluaciones", "Rezago por avance", "Tiempo"],
                        filas.Select(x => new[] { x.alumno, x.modalidad, $"{Math.Round(x.avance)} %", x.esperado is null ? "—" : $"{Math.Round(x.esperado.Value)} %",
                            $"{x.evaluaciones}", x.rezago, x.tiempo }).ToList()),
                    new
                    {
                        alumnos = filas,
                        // Opciones de los filtros con su conteo antes de filtrar (como FiltroMultiple en las consultas).
                        modalidades = alumnos.GroupBy(a => a.Modalidad ?? "Sin asignar").OrderBy(g => g.Key).Select(g => new { valor = g.Key, conteo = g.Count() }),
                        estados = alumnos.GroupBy(a => a.Estado ?? "").OrderBy(g => g.Key).Select(g => new { valor = g.Key, conteo = g.Count() }),
                    });
            }

            default:
                throw new ArgumentException("Tipo de reporte desconocido.");
        }
    }

    // El archivo lleva indicadores y tabla; la gráfica solo se ve en pantalla.
    public static byte[] Pdf(DatosReporte d, DateTime generado) =>
        Document.Create(doc => doc.Page(page =>
        {
            page.Size(PageSizes.Letter.Landscape());
            page.Margin(1.5f, Unit.Centimetre);
            page.DefaultTextStyle(t => t.FontSize(9));
            page.Content().Column(col =>
            {
                col.Spacing(10);
                col.Item().Text("Maestría en Tecnología y Gestión del Agua").FontSize(10).FontColor(Colors.Grey.Darken1);
                col.Item().Text(d.Titulo).FontSize(16).Bold();
                col.Item().Text($"{d.Filtros} · Generado el {generado:dd/MM/yyyy HH:mm}").FontColor(Colors.Grey.Darken1);
                col.Item().Row(row =>
                {
                    foreach (var i in d.Indicadores)
                        row.RelativeItem().Border(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(8).Column(c =>
                        {
                            c.Item().Text(i.Etiqueta).FontColor(Colors.Grey.Darken1);
                            c.Item().Text(i.Valor).FontSize(14).Bold();
                            if (i.Nota is not null) c.Item().Text(i.Nota).FontSize(8).FontColor(Colors.Grey.Darken1);
                        });
                });
                if (d.Graficas.Count > 0)
                    col.Item().ShowEntire().Row(row =>
                    {
                        row.Spacing(12);
                        foreach (var g in d.Graficas) row.RelativeItem().Element(c => Marco(c, g));
                    });
                col.Item().Table(t =>
                {
                    t.ColumnsDefinition(c => { foreach (var _ in d.Tabla.Columnas) c.RelativeColumn(); });
                    foreach (var h in d.Tabla.Columnas)
                        t.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text(h).Bold();
                    foreach (var fila in d.Tabla.Filas)
                        foreach (var v in fila)
                            t.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(v);
                    if (d.Tabla.Total is { } total)
                        foreach (var v in total)
                            t.Cell().Background(Colors.Grey.Lighten4).Padding(4).Text(v).Bold();
                });
                if (d.Nota is not null) col.Item().Text(d.Nota).FontSize(8).FontColor(Colors.Grey.Darken1);
            });
            page.Footer().AlignRight().Text(t => { t.CurrentPageNumber(); t.Span(" / "); t.TotalPages(); });
        })).GeneratePdf();

    public static byte[] Excel(DatosReporte d, DateTime generado)
    {
        using var libro = new XLWorkbook();
        var hoja = libro.Worksheets.Add("Reporte");
        hoja.Cell(1, 1).Value = d.Titulo;
        hoja.Cell(1, 1).Style.Font.SetBold().Font.SetFontSize(14);
        hoja.Cell(2, 1).Value = $"{d.Filtros} · Generado el {generado:dd/MM/yyyy HH:mm}";
        var r = 4;
        foreach (var i in d.Indicadores)
        {
            hoja.Cell(r, 1).Value = i.Etiqueta;
            hoja.Cell(r, 2).Value = i.Valor;
            hoja.Cell(r, 3).Value = i.Nota ?? "";
            r++;
        }
        r++;
        var inicio = r;
        for (var c = 0; c < d.Tabla.Columnas.Length; c++) hoja.Cell(r, c + 1).Value = d.Tabla.Columnas[c];
        foreach (var fila in d.Tabla.Filas.Append(d.Tabla.Total).OfType<string[]>())
        {
            r++;
            for (var c = 0; c < fila.Length; c++) hoja.Cell(r, c + 1).Value = fila[c];
        }
        hoja.Range(inicio, 1, inicio, d.Tabla.Columnas.Length).Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.LightGray);
        if (d.Tabla.Total is not null) hoja.Row(r).Style.Font.SetBold();
        if (d.Nota is not null) hoja.Cell(r + 2, 1).Value = d.Nota;
        hoja.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        libro.SaveAs(ms);
        return ms.ToArray();
    }

    // Gráficas (paleta de Figma R1, en orden fijo).
    static readonly (string Nombre, string Color)[] Series = [("Activos", "#2a78d6"), ("Inactivos", "#eb6834"), ("Egresados", "#1baf7a"), ("Baja", "#eda100")];
    const string Azul = "#2a78d6", GrisBarra = "#8e93ae", Neutro = "#eef0f4", Borde = "#e6e9f0", Eje = "#d3d8e3", Texto = "#101a5a", Subtexto = "#555c8e";

    public static byte[] Imagen(GraficaPdf g) =>
        Document.Create(doc => doc.Page(page =>
        {
            page.ContinuousSize(480);
            page.Margin(12);
            page.PageColor(Colors.White);
            page.DefaultTextStyle(t => t.FontSize(9));
            page.Content().Element(c => Marco(c, g));
        })).GenerateImages(new ImageGenerationSettings { ImageFormat = ImageFormat.Png, RasterDpi = 216 }).First();

    static void Marco(IContainer c, GraficaPdf g) =>
        c.Border(0.75f).BorderColor(Borde).Padding(12).Column(col =>
        {
            col.Spacing(8);
            col.Item().Text(g.Titulo).FontSize(11).SemiBold();
            if (g.Subtitulo is not null) col.Item().Text(g.Subtitulo).FontSize(8).FontColor(Subtexto);
            col.Item().Element(g.Dibujar);
        });

    static void Agrupadas(IContainer c, List<(string Etiqueta, int[] Valores)> grupos)
    {
        var maximo = Math.Max(1, grupos.SelectMany(g => g.Valores).DefaultIfEmpty(0).Max());
        c.Column(col =>
        {
            col.Spacing(10);
            col.Item().Row(r =>
            {
                r.Spacing(12);
                foreach (var s in Series)
                    r.AutoItem().Row(x =>
                    {
                        x.AutoItem().AlignMiddle().Width(8).Height(8).Background(s.Color);
                        x.AutoItem().PaddingLeft(4).Text(s.Nombre).FontSize(8).FontColor(Texto);
                    });
            });
            col.Item().Row(r =>
            {
                r.Spacing(14);
                foreach (var g in grupos)
                    r.RelativeItem().Column(gc =>
                    {
                        gc.Item().Height(130).AlignBottom().AlignCenter().Row(barras =>
                        {
                            barras.Spacing(2);
                            for (var i = 0; i < Series.Length; i++)
                            {
                                var v = g.Valores[i];
                                var color = Series[i].Color;
                                barras.ConstantItem(14).AlignBottom().Column(b =>
                                {
                                    b.Item().AlignCenter().Text($"{v}").FontSize(7).FontColor(Subtexto);
                                    b.Item().Height(v == 0 ? 1.5f : v * 110f / maximo).Background(v == 0 ? Borde : color);
                                });
                            }
                        });
                        gc.Item().Height(0.75f).Background(Eje);
                        gc.Item().PaddingTop(4).AlignCenter().Text(g.Etiqueta).FontSize(8).FontColor(Texto);
                    });
            });
        });
    }

    static void Progreso(IContainer c, List<(string Etiqueta, string Valor, float Pct)> filas) =>
        c.Column(col =>
        {
            col.Spacing(10);
            foreach (var f in filas)
                col.Item().Column(x =>
                {
                    x.Spacing(3);
                    x.Item().Row(r =>
                    {
                        r.RelativeItem().Text(f.Etiqueta).FontColor(Texto);
                        var valor = r.AutoItem().Text(f.Valor).FontSize(8);
                        if (f.Pct > 0) valor.SemiBold(); else valor.FontColor(Subtexto);
                    });
                    x.Item().Height(6).Row(r =>
                    {
                        if (f.Pct > 0) r.RelativeItem(f.Pct).Background(Azul);
                        if (f.Pct < 100) r.RelativeItem(100 - f.Pct).Background(Neutro);
                    });
                });
        });

    static void Horizontales(IContainer c, List<(string Etiqueta, int Cantidad)> barras, string gris)
    {
        var maximo = Math.Max(1, barras.Max(b => b.Cantidad));
        c.Column(col =>
        {
            col.Spacing(6);
            foreach (var b in barras)
                col.Item().Row(r =>
                {
                    r.ConstantItem(90).AlignMiddle().Text(b.Etiqueta).FontColor(Texto);
                    r.RelativeItem().Row(fila =>
                    {
                        if (b.Cantidad > 0) fila.RelativeItem(b.Cantidad).Height(12).Background(b.Etiqueta == gris ? GrisBarra : Azul);
                        // El resto del ancho deja lugar a la etiqueta: la barra más larga ocupa 3/4.
                        fila.RelativeItem(maximo - b.Cantidad + maximo / 3f).AlignMiddle().PaddingLeft(6)
                            .Text($"{b.Cantidad} {(b.Cantidad == 1 ? "egresado" : "egresados")}").FontSize(8).FontColor(Texto);
                    });
                });
        });
    }
}
