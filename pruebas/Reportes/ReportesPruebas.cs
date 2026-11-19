using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using PlataformaTGA.Data;
using PlataformaTGA.Pruebas.Evaluaciones;
using PlataformaTGA.Pruebas.Infraestructura;

namespace PlataformaTGA.Pruebas.Reportes;

// RF-5 Generación de reportes. Cada caso usa su propia generación para que los totales solo cuenten sus datos.
[Collection(ColeccionPlataforma.Nombre)]
public class ReportesPruebas(Aplicacion app)
{
    async Task<HttpClient> CoordinadorAsync() => await app.SesionAsync((await Datos.ProfesorAsync(app, coordinador: true)).Correo);

    static object Filtros(string tipo, int generacion, bool noLocalizables = false) =>
        new { tipo, generaciones = new[] { generacion }, incluirNoLocalizables = noLocalizables };

    static Dictionary<string, string> Indicadores(JsonElement vista) =>
        vista.GetProperty("indicadores").EnumerateArray().ToDictionary(i => i.Texto("etiqueta"), i => i.Texto("valor"));

    static List<Dictionary<string, string>> Filas(JsonElement vista)
    {
        var tabla = vista.GetProperty("tabla");
        return Enumerable.Range(0, tabla.GetProperty("filas").GetArrayLength()).Select(i => EvaluacionesPruebas.Fila(tabla, i)).ToList();
    }

    [Fact(DisplayName = "CP-RF-5.1-01 Reporte filtrado")]
    public async Task CP_RF_5_1_01()
    {
        var generacion = await Datos.GeneracionAsync(app);
        var otra = await Datos.GeneracionAsync(app);
        for (var i = 0; i < 3; i++) await Datos.EgresadoAsync(app, generacion);
        await Datos.EgresadoAsync(app, otra);
        var coordinador = await CoordinadorAsync();

        var vista = await coordinador.PostAsJsonAsync("/api/reportes/vista", Filtros("egresados", generacion)).JsonAsync();

        var enBase = await app.ConBaseAsync(db => db.Egresado
            .Where(e => e.Activo && e.ClaveAlumnoNavigation.IdGeneracion == generacion && e.IdEstadoEgresado != Egresados.NoLocalizable)
            .Select(e => e.IdentificadorEgresado!).ToListAsync());
        Assert.Equal(enBase.Count.ToString(), Indicadores(vista)["Egresados"]);
        Assert.Equal(enBase.Order(), Filas(vista).Select(f => f["Identificador"]).Order());
        var nombre = await app.ConBaseAsync(db => db.Generacion.Where(g => g.IdGeneracion == generacion).Select(g => g.NombreGeneracion!).SingleAsync());
        Assert.All(Filas(vista), f => Assert.Equal(nombre, f["Generación"]));
    }

    [Fact(DisplayName = "CP-RF-5.2-01 Reporte por generación")]
    public async Task CP_RF_5_2_01()
    {
        var generacion = await Datos.GeneracionAsync(app);
        var t = await Datos.TrabajoAsync(app, semestre: 3, idGeneracion: generacion);
        var periodos = await Datos.PeriodosPasadosAsync(app, 1);
        await Datos.InformeCerradoAsync(app, t, periodos[0], 50);
        var sinTrabajo = await Datos.AlumnoAsync(app, generacion, semestre: 2);
        var coordinador = await CoordinadorAsync();

        var vista = await coordinador.PostAsJsonAsync("/api/reportes/vista", Filtros("avance", generacion)).JsonAsync();

        var filas = Filas(vista);
        Assert.Equal(2, filas.Count);
        var conTrabajo = filas.Single(f => f["Alumno"].Contains(t.Alumno.Clave.ToString()));
        Assert.Equal("En progreso", conTrabajo["Estado de titulación"]);
        Assert.Equal("50 %", conTrabajo["Avance"]);
        Assert.Equal("1", conTrabajo["Evaluaciones"]);
        var comite = conTrabajo["Comité tutorial"].Split("; ");
        Assert.Equal(3, comite.Length);
        Assert.StartsWith("Director: Profesor Prueba", comite[0]);
        Assert.All(comite[1..], m => Assert.StartsWith("Sinodal: Profesor Prueba", m));

        var otro = filas.Single(f => f["Alumno"].Contains(sinTrabajo.Clave.ToString()));
        Assert.Equal("Sin trabajo registrado", otro["Estado de titulación"]);
        Assert.Equal("Sin comité", otro["Comité tutorial"]);
    }

    [Fact(DisplayName = "CP-RF-5.3-01 Exportación PDF y Excel")]
    public async Task CP_RF_5_3_01()
    {
        var generacion = await Datos.GeneracionAsync(app);
        for (var i = 0; i < 3; i++) await Datos.TrabajoAsync(app, semestre: 2, idGeneracion: generacion);
        var coordinador = await CoordinadorAsync();
        var filtros = Filtros("avance", generacion);
        var vista = Filas(await coordinador.PostAsJsonAsync("/api/reportes/vista", filtros).JsonAsync());

        var reloj = Stopwatch.StartNew();
        var pdf = await coordinador.PostAsJsonAsync("/api/reportes/exportar", new { filtros, formato = "pdf" }).EsperarAsync(HttpStatusCode.OK);
        var bytesPdf = await pdf.Content.ReadAsByteArrayAsync();
        reloj.Stop();
        Assert.Equal("application/pdf", pdf.Content.Headers.ContentType?.MediaType);
        Assert.Equal("%PDF-"u8.ToArray(), bytesPdf[..5]);
        Assert.True(reloj.Elapsed.TotalSeconds <= 7, $"El PDF tardó {reloj.Elapsed.TotalSeconds:0.0} s (RD-03 pide ≤ 7 s).");

        var excel = await coordinador.PostAsJsonAsync("/api/reportes/exportar", new { filtros, formato = "excel" }).EsperarAsync(HttpStatusCode.OK);
        using var libro = new XLWorkbook(await excel.Content.ReadAsStreamAsync());
        var hoja = libro.Worksheet(1);
        var encabezado = hoja.RowsUsed().First(r => r.Cell(1).GetString() == "Alumno");
        var columnas = encabezado.CellsUsed().Select(c => c.GetString()).ToArray();
        var filasExcel = hoja.RowsUsed().Where(r => r.RowNumber() > encabezado.RowNumber())
            .Select(r => columnas.Select((c, i) => (c, r.Cell(i + 1).GetString())).ToDictionary(x => x.c, x => x.Item2)).ToList();
        Assert.Equal(vista.Count, filasExcel.Count);
        for (var i = 0; i < vista.Count; i++) Assert.Equal(vista[i], filasExcel[i]);
    }

    [Fact(DisplayName = "CP-RF-5.5-01 Registro en historial")]
    public async Task CP_RF_5_5_01()
    {
        var generacion = await Datos.GeneracionAsync(app);
        await Datos.TrabajoAsync(app, semestre: 2, idGeneracion: generacion);
        var coordinador = await Datos.ProfesorAsync(app, coordinador: true);
        var cliente = await app.SesionAsync(coordinador.Correo);
        var nombreGeneracion = await app.ConBaseAsync(db => db.Generacion.Where(g => g.IdGeneracion == generacion).Select(g => g.NombreGeneracion!).SingleAsync());

        var exportado = await (await cliente.PostAsJsonAsync("/api/reportes/exportar", new { filtros = Filtros("avance", generacion), formato = "excel" })
            .EsperarAsync(HttpStatusCode.OK)).Content.ReadAsByteArrayAsync();

        var historial = await cliente.GetAsync("/api/reportes/historial").JsonAsync();
        var registro = historial.EnumerateArray().First();
        Assert.Equal("Avance de titulación", registro.Texto("tipo"));
        Assert.Equal("Excel", registro.Texto("formato"));
        Assert.Contains(nombreGeneracion, registro.Texto("filtros"));
        Assert.Equal(await app.ConBaseAsync(db => db.Usuario.Where(u => u.Correo == coordinador.Correo).Select(u => u.Nombre!).SingleAsync()), registro.Texto("genero"));
        Assert.Equal(JsonValueKind.String, registro.GetProperty("fecha").ValueKind);

        var archivo = await cliente.GetAsync($"/api/reportes/{registro.GetProperty("id").GetInt32()}/archivo").EsperarAsync(HttpStatusCode.OK);
        Assert.Equal(exportado, await archivo.Content.ReadAsByteArrayAsync());
    }

    [Fact(DisplayName = "CP-RF-5.6-01 Conteo con no localizables")]
    public async Task CP_RF_5_6_01()
    {
        var generacion = await Datos.GeneracionAsync(app);
        for (var i = 0; i < 8; i++) await Datos.EgresadoAsync(app, generacion);
        for (var i = 0; i < 2; i++) await Datos.EgresadoAsync(app, generacion, noLocalizable: true);
        var coordinador = await CoordinadorAsync();

        var sin = await coordinador.PostAsJsonAsync("/api/reportes/vista", Filtros("egresados", generacion)).JsonAsync();
        Assert.Equal("8", Indicadores(sin)["Egresados"]);
        Assert.Equal(8, Filas(sin).Count);

        var con = await coordinador.PostAsJsonAsync("/api/reportes/vista", Filtros("egresados", generacion, noLocalizables: true)).JsonAsync();
        Assert.Equal("10", Indicadores(con)["Egresados"]);
        Assert.Equal("2", Indicadores(con)["No localizables"]);
        Assert.Equal(2, Filas(con).Count(f => f["Estado"] == "No localizable"));
    }
}
