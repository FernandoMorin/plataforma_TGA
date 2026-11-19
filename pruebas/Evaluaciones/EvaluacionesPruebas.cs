using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PlataformaTGA.Data;
using PlataformaTGA.Pruebas.Infraestructura;

namespace PlataformaTGA.Pruebas.Evaluaciones;

// RF-4 Evaluaciones de tesis. Cada prueba es un caso de la matriz de trazabilidad (columna «ID»).
[Collection(ColeccionPlataforma.Nombre)]
public class EvaluacionesPruebas(Aplicacion app)
{
    readonly FlujoInforme flujo = new(app);

    static IEnumerable<JsonElement> Arreglo(JsonElement e) => e.EnumerateArray();

    Task<JsonElement> MisEvaluacionesAsync(AlumnoPrueba a) =>
        app.SesionAsync(a.Correo).ContinueWith(c => c.Result.GetAsync("/api/mi-titulacion/evaluaciones")).Unwrap().JsonAsync();

    [Fact(DisplayName = "CP-RF-4-01 Flujo de evaluación")]
    public async Task CP_RF_4_01()
    {
        var t = await Datos.TrabajoAsync(app, semestre: 2);

        var id = await flujo.AbrirAsync(t);
        Assert.Equal("Abierta", await flujo.EstadoAsync(t.Director, id));
        await flujo.TodasLasRevisionesAsync(t, id, avance: 40);
        Assert.Equal("En consolidación", await flujo.EstadoAsync(t.Director, id));
        await flujo.ConsolidarAsync(t, id, enviar: true, avance: 40, nuevos: [new("Entregar el capítulo 2.", DateTime.Today.AddMonths(2))])
            .EsperarAsync(HttpStatusCode.NoContent);
        Assert.Equal("En firma", await flujo.EstadoAsync(t.Director, id));
        await flujo.FirmasAsync(t, id);
        Assert.Equal("Cerrada", await flujo.EstadoAsync(t.Director, id));

        var informe = Arreglo(await MisEvaluacionesAsync(t.Alumno)).Single(i => i.GetProperty("id").GetInt32() == id);
        Assert.True(informe.GetProperty("cerrada").GetBoolean());
        Assert.Equal(40, informe.Numero("avance"));
        Assert.Equal(85, informe.Numero("promedio"));
        Assert.Equal("Comentarios del comité.", informe.Texto("comentarios"));
        Assert.Equal("Entregar el capítulo 2.", Arreglo(informe.GetProperty("compromisos")).Single().Texto("descripcion"));
        Assert.Equal(JsonValueKind.Number, informe.GetProperty("acta").ValueKind);
    }

    [Fact(DisplayName = "CP-RF-4.1-01 Registro de revisión")]
    public async Task CP_RF_4_1_01()
    {
        var t = await Datos.TrabajoAsync(app, semestre: 2);
        var sinodal = t.Sinodales[0];

        var detalle = await (await app.SesionAsync(sinodal.Correo)).GetAsync($"/api/profesor/alumnos/{t.IdTesis}").JsonAsync();
        var fila = Arreglo(detalle.GetProperty("informes")).Single();
        Assert.Equal("Abierta", fila.GetProperty("informe").Texto("estado"));
        Assert.Equal("Revisar", fila.Texto("accion"));
        var id = fila.GetProperty("informe").GetProperty("id").GetInt32();
        var periodo = await app.ConBaseAsync(db => db.Revision.Where(r => r.IdRevision == id).Select(r => r.IdPeriodo).SingleAsync());
        Assert.Equal(app.PeriodoActual, periodo);

        await flujo.RevisionAsync(sinodal, id, avance: 35).EsperarAsync(HttpStatusCode.NoContent);

        var envio = await app.ConBaseAsync(db => db.RevisionProfesor.Where(p => p.IdRevision == id && p.Rpe == sinodal.Rpe)
            .Select(p => new { p.FechaEnvio, p.PorcentajeAvance }).SingleAsync());
        Assert.NotNull(envio.FechaEnvio);
        Assert.Equal(35, envio.PorcentajeAvance);
        detalle = await (await app.SesionAsync(sinodal.Correo)).GetAsync($"/api/profesor/alumnos/{t.IdTesis}").JsonAsync();
        Assert.Equal("Revisiones · 1 de 3", Arreglo(detalle.GetProperty("informes")).Single().Texto("estadoParaMi"));
    }

    [Fact(DisplayName = "CP-RF-4.1-02 Alumno de primer semestre")]
    public async Task CP_RF_4_1_02()
    {
        var t = await Datos.TrabajoAsync(app, semestre: 1);

        var detalle = await (await app.SesionAsync(t.Director.Correo)).GetAsync($"/api/profesor/alumnos/{t.IdTesis}").JsonAsync();

        Assert.Empty(Arreglo(detalle.GetProperty("informes")));
        Assert.Equal(0, await app.ConBaseAsync(db => db.Revision.CountAsync(r => r.IdTesis == t.IdTesis)));
    }

    [Fact(DisplayName = "CP-RF-4.1-03 Un solo informe por periodo")]
    public async Task CP_RF_4_1_03()
    {
        var t = await Datos.TrabajoAsync(app, semestre: 2);
        var coordinador = await Datos.ProfesorAsync(app, coordinador: true);

        for (var i = 0; i < 2; i++)
        {
            await (await app.SesionAsync(t.Director.Correo)).GetAsync($"/api/profesor/alumnos/{t.IdTesis}").JsonAsync();
            await (await app.SesionAsync(t.Sinodales[1].Correo)).GetAsync($"/api/profesor/alumnos/{t.IdTesis}").JsonAsync();
            await (await app.SesionAsync(coordinador.Correo)).GetAsync("/api/titulacion").JsonAsync();
        }

        Assert.Equal(1, await app.ConBaseAsync(db => db.Revision.CountAsync(r => r.IdTesis == t.IdTesis && r.IdPeriodo == app.PeriodoActual)));
    }

    [Fact(DisplayName = "CP-RF-4.2-01 Historial de evaluaciones")]
    public async Task CP_RF_4_2_01()
    {
        var t = await Datos.TrabajoAsync(app, semestre: 4);
        var periodos = await Datos.PeriodosPasadosAsync(app, 3);
        await Datos.InformeCerradoAsync(app, t, periodos[0], 20);
        await Datos.InformeCerradoAsync(app, t, periodos[1], 35);
        await Datos.InformeCerradoAsync(app, t, periodos[2], 50);
        var coordinador = await Datos.ProfesorAsync(app, coordinador: true);
        decimal[] esperado = [50, 35, 20];

        // Alumno
        var delAlumno = Arreglo(await MisEvaluacionesAsync(t.Alumno)).Where(i => i.GetProperty("cerrada").GetBoolean()).ToList();
        Assert.Equal(esperado, delAlumno.Select(i => i.Numero("avance")));
        Assert.All(delAlumno, i =>
        {
            Assert.Equal(JsonValueKind.String, i.GetProperty("fecha").ValueKind);
            Assert.Equal(80, i.Numero("promedio"));
            Assert.False(string.IsNullOrEmpty(i.Texto("comentarios")));
            Assert.Equal(JsonValueKind.Array, i.GetProperty("compromisos").ValueKind);
        });

        // Profesor del comité
        var detalle = await (await app.SesionAsync(t.Sinodales[0].Correo)).GetAsync($"/api/profesor/alumnos/{t.IdTesis}").JsonAsync();
        var delProfesor = Arreglo(detalle.GetProperty("informes")).Select(i => i.GetProperty("informe")).Where(i => i.GetProperty("cerrada").GetBoolean());
        Assert.Equal(esperado, delProfesor.Select(i => i.Numero("avance")));

        // Coordinador
        var delCoordinador = Arreglo(await (await app.SesionAsync(coordinador.Correo)).GetAsync($"/api/alumnos/{t.Alumno.Clave}/evaluaciones").JsonAsync())
            .Where(i => i.Texto("estado") == "Cerrada");
        Assert.Equal(esperado, delCoordinador.Select(i => i.Numero("avance")));
    }

    [Fact(DisplayName = "CP-RF-4.3-01 Edición durante la consolidación")]
    public async Task CP_RF_4_3_01()
    {
        var t = await Datos.TrabajoAsync(app, semestre: 2);
        var id = await flujo.AbrirAsync(t);
        await flujo.TodasLasRevisionesAsync(t, id);

        await flujo.ConsolidarAsync(t, id, enviar: false, comentarios: "Primera versión.").EsperarAsync(HttpStatusCode.NoContent);
        await flujo.ConsolidarAsync(t, id, enviar: false, comentarios: "Segunda versión.").EsperarAsync(HttpStatusCode.NoContent);

        var consolidacion = await (await app.SesionAsync(t.Director.Correo)).GetAsync($"/api/profesor/evaluaciones/{id}/consolidacion").JsonAsync();
        Assert.Equal("Segunda versión.", consolidacion.GetProperty("final").Texto("comentarios"));
        Assert.Equal("En consolidación", await flujo.EstadoAsync(t.Director, id));
    }

    [Fact(DisplayName = "CP-RF-4.3-02 Edición bloqueada tras envío o cierre")]
    public async Task CP_RF_4_3_02()
    {
        var t = await Datos.TrabajoAsync(app, semestre: 3);
        var periodos = await Datos.PeriodosPasadosAsync(app, 1);
        var cerrado = await Datos.InformeCerradoAsync(app, t, periodos[0], 30);
        var id = await flujo.AbrirAsync(t);

        await flujo.RevisionAsync(t.Sinodales[0], id, comentarios: "Original.").EsperarAsync(HttpStatusCode.NoContent);
        var otraVez = await flujo.RevisionAsync(t.Sinodales[0], id, comentarios: "Cambiada.").EsperarAsync(HttpStatusCode.Conflict);
        Assert.Equal("Ya enviaste tu revisión.", await otraVez.MensajeAsync());
        Assert.Equal("Original.", await app.ConBaseAsync(db =>
            db.RevisionProfesor.Where(p => p.IdRevision == id && p.Rpe == t.Sinodales[0].Rpe).Select(p => p.Comentarios).SingleAsync()));

        var sobreCerrado = await flujo.ConsolidarAsync(t, cerrado, enviar: false, comentarios: "Cambio.").EsperarAsync(HttpStatusCode.Conflict);
        Assert.Equal("El informe no está en consolidación.", await sobreCerrado.MensajeAsync());
        Assert.Equal("Informe con 30 %", await app.ConBaseAsync(db => db.Revision.Where(r => r.IdRevision == cerrado).Select(r => r.Comentarios).SingleAsync()));
    }

    [Fact(DisplayName = "CP-RF-4.4-01 Observaciones por profesor")]
    public async Task CP_RF_4_4_01()
    {
        var t = await Datos.TrabajoAsync(app, semestre: 2);
        var id = await flujo.AbrirAsync(t);

        foreach (var p in t.Integrantes)
            await flujo.RevisionAsync(p, id, comentarios: $"Observaciones de {p.Rpe}.").EsperarAsync(HttpStatusCode.NoContent);

        var guardadas = await app.ConBaseAsync(db => db.RevisionProfesor.Where(p => p.IdRevision == id).Select(p => new { p.Rpe, p.Comentarios }).ToListAsync());
        Assert.Equal(t.Integrantes.Select(p => p.Rpe).Order(), guardadas.Select(g => g.Rpe).Order());
        Assert.All(guardadas, g => Assert.Equal($"Observaciones de {g.Rpe}.", g.Comentarios));

        var consolidacion = await (await app.SesionAsync(t.Director.Correo)).GetAsync($"/api/profesor/evaluaciones/{id}/consolidacion").JsonAsync();
        var lado = Arreglo(consolidacion.GetProperty("integrantes")).Select(i => i.Texto("comentarios")).Order();
        Assert.Equal(t.Integrantes.Select(p => $"Observaciones de {p.Rpe}.").Order(), lado);
    }

    [Fact(DisplayName = "CP-RF-4.4-02 Registro y cumplimiento de compromisos")]
    public async Task CP_RF_4_4_02()
    {
        var t = await Datos.TrabajoAsync(app, semestre: 3);
        var periodos = await Datos.PeriodosPasadosAsync(app, 1);
        var anterior = await Datos.InformeCerradoAsync(app, t, periodos[0], 30, "Entregar el marco teórico.");
        var id = await flujo.AbrirAsync(t);
        await flujo.TodasLasRevisionesAsync(t, id, avance: 45);

        var consolidacion = await (await app.SesionAsync(t.Director.Correo)).GetAsync($"/api/profesor/evaluaciones/{id}/consolidacion").JsonAsync();
        var previo = Arreglo(consolidacion.GetProperty("anteriores")).Single();
        Assert.Equal("Entregar el marco teórico.", previo.Texto("descripcion"));

        await flujo.ConsolidarAsync(t, id, enviar: true, avance: 45, cumplidos: [previo.GetProperty("id").GetInt32()],
            nuevos: [new("Entregar la metodología.", DateTime.Today.AddMonths(3))]).EsperarAsync(HttpStatusCode.NoContent);
        await flujo.FirmasAsync(t, id);

        var historial = Arreglo(await MisEvaluacionesAsync(t.Alumno)).ToDictionary(i => i.GetProperty("id").GetInt32());
        var nuevo = Arreglo(historial[id].GetProperty("compromisos")).Single();
        Assert.Equal("Entregar la metodología.", nuevo.Texto("descripcion"));
        Assert.False(nuevo.GetProperty("cumplido").GetBoolean());
        Assert.True(Arreglo(historial[anterior].GetProperty("compromisos")).Single().GetProperty("cumplido").GetBoolean());
    }

    [Fact(DisplayName = "CP-RF-4.5-01 Cálculo de indicadores")]
    public async Task CP_RF_4_5_01()
    {
        var generacion = await Datos.GeneracionAsync(app);
        var t = await Datos.TrabajoAsync(app, semestre: 3, idGeneracion: generacion);
        var periodos = await Datos.PeriodosPasadosAsync(app, 3);
        await Datos.InformeCerradoAsync(app, t, periodos[0], 20);
        await Datos.InformeCerradoAsync(app, t, periodos[1], 35);
        await Datos.InformeCerradoAsync(app, t, periodos[2], 50);
        var coordinador = await app.SesionAsync((await Datos.ProfesorAsync(app, coordinador: true)).Correo);

        // Ficha del alumno
        var ficha = await coordinador.GetAsync($"/api/alumnos/{t.Alumno.Clave}").JsonAsync();
        var titulacion = ficha.GetProperty("titulacion");
        Assert.Equal(50, titulacion.Numero("avance"));
        Assert.Equal(67, Math.Round(titulacion.Numero("esperado")));
        Assert.Equal("En rezago", titulacion.Texto("rezago"));
        Assert.Equal(3, ficha.GetProperty("evaluaciones").GetInt32());

        // Reporte de avance de titulación
        var vista = await coordinador.PostAsJsonAsync("/api/reportes/vista", new { tipo = "avance", generaciones = new[] { generacion }, incluirNoLocalizables = false }).JsonAsync();
        var fila = Fila(vista.GetProperty("tabla"), 0);
        Assert.Equal("50 %", fila["Avance"]);
        Assert.Equal("67 %", fila["Esperado"]);
        Assert.Equal("3", fila["Evaluaciones"]);
        Assert.Equal("En rezago", fila["Rezago por avance"]);
    }

    [Fact(DisplayName = "CP-RF-4.6-01 Carga válida")]
    public async Task CP_RF_4_6_01()
    {
        var t = await Datos.TrabajoAsync(app, semestre: 2);
        var id = await flujo.AbrirAsync(t);
        var alumno = await app.SesionAsync(t.Alumno.Correo);

        await alumno.PostAsync("/api/mi-titulacion/evidencias", Formulario(Datos.Pdf(2 * 1024 * 1024), "avance.pdf"))
            .EsperarAsync(HttpStatusCode.NoContent);

        var doc = await app.ConBaseAsync(db => db.Documento.Where(d => d.IdTesis == t.IdTesis)
            .Select(d => new { d.IdDocumento, d.IdRevision, d.IdTipoDocumento, d.NombreArchivo, d.FechaCarga }).SingleAsync());
        Assert.Equal(id, doc.IdRevision);
        Assert.Equal(TipoDocumento.Avance, doc.IdTipoDocumento);
        Assert.Equal("avance.pdf", doc.NombreArchivo);
        Assert.NotNull(doc.FechaCarga);

        var evidencias = await alumno.GetAsync("/api/mi-titulacion/evidencias").JsonAsync();
        var listada = Arreglo(evidencias.GetProperty("evidencias")).Single();
        Assert.Equal("Avance", listada.Texto("tipo"));
        Assert.Equal(1, listada.GetProperty("informe").GetInt32());

        var descarga = await (await app.SesionAsync(t.Director.Correo)).GetAsync($"/api/documentos/{doc.IdDocumento}").EsperarAsync(HttpStatusCode.OK);
        Assert.Equal("application/pdf", descarga.Content.Headers.ContentType?.MediaType);
        Assert.Equal(2 * 1024 * 1024, (await descarga.Content.ReadAsByteArrayAsync()).Length);
    }

    [Fact(DisplayName = "CP-RF-4.6-02 Archivo excedido o no PDF")]
    public async Task CP_RF_4_6_02()
    {
        var t = await Datos.TrabajoAsync(app, semestre: 2);
        await flujo.AbrirAsync(t);
        var alumno = await app.SesionAsync(t.Alumno.Correo);

        // Un byte arriba del límite de 10 MB (RD-04).
        var grande = await alumno.PostAsync("/api/mi-titulacion/evidencias", Formulario(Datos.Pdf(10 * 1024 * 1024 + 1), "grande.pdf"))
            .EsperarAsync(HttpStatusCode.BadRequest);
        Assert.Equal("El archivo supera los 10 MB.", await grande.MensajeAsync());

        // 12 MB, como en el caso: no debe guardarse (el servidor puede cortarlo antes por tamaño de petición).
        var doce = await alumno.PostAsync("/api/mi-titulacion/evidencias", Formulario(Datos.Pdf(12 * 1024 * 1024), "doce.pdf"));
        Assert.False(doce.IsSuccessStatusCode, $"Se aceptó un archivo de 12 MB ({(int)doce.StatusCode}).");

        var docx = new byte[2048];
        "PK\u0003\u0004"u8.CopyTo(docx);
        var noPdf = await alumno.PostAsync("/api/mi-titulacion/evidencias", Formulario(docx, "avance.docx"))
            .EsperarAsync(HttpStatusCode.BadRequest);
        Assert.Equal("El archivo debe ser un PDF.", await noPdf.MensajeAsync());

        Assert.Equal(0, await app.ConBaseAsync(db => db.Documento.CountAsync(d => d.IdTesis == t.IdTesis)));
    }

    [Fact(DisplayName = "CP-RF-4.7-01 Profesor ajeno al comité")]
    public async Task CP_RF_4_7_01()
    {
        var t = await Datos.TrabajoAsync(app, semestre: 2);
        var id = await flujo.AbrirAsync(t);
        var ajeno = await Datos.ProfesorAsync(app);

        await (await app.SesionAsync(ajeno.Correo)).GetAsync($"/api/profesor/alumnos/{t.IdTesis}").EsperarAsync(HttpStatusCode.NotFound);
        await (await app.SesionAsync(ajeno.Correo)).GetAsync($"/api/profesor/evaluaciones/{id}").EsperarAsync(HttpStatusCode.NotFound);
        await flujo.RevisionAsync(ajeno, id).EsperarAsync(HttpStatusCode.NotFound);

        Assert.Equal(0, await app.ConBaseAsync(db => db.RevisionProfesor.CountAsync(p => p.Rpe == ajeno.Rpe)));
    }

    [Fact(DisplayName = "CP-RF-4.7-02 Cierre por firma")]
    public async Task CP_RF_4_7_02()
    {
        var t = await Datos.TrabajoAsync(app, semestre: 2);
        var id = await flujo.AbrirAsync(t);
        await flujo.TodasLasRevisionesAsync(t, id, avance: 40);
        await flujo.ConsolidarAsync(t, id, enviar: true, avance: 42).EsperarAsync(HttpStatusCode.NoContent);

        foreach (var p in t.Integrantes[..^1])
            await (await app.SesionAsync(p.Correo)).PostAsync($"/api/profesor/evaluaciones/{id}/firmar", null).EsperarAsync(HttpStatusCode.NoContent);
        Assert.Equal("En firma", await flujo.EstadoAsync(t.Director, id));
        await (await app.SesionAsync(t.Integrantes[^1].Correo)).PostAsync($"/api/profesor/evaluaciones/{id}/firmar", null).EsperarAsync(HttpStatusCode.NoContent);

        Assert.Equal("Cerrada", await flujo.EstadoAsync(t.Director, id));
        var cierre = await app.ConBaseAsync(async db => new
        {
            Avance = await db.Tesis.Where(x => x.IdTesis == t.IdTesis).Select(x => x.PorcentajeActual).SingleAsync(),
            Acta = await db.ActaComite.Where(a => a.IdRevision == id).Select(a => a.CveActa).SingleAsync(),
            Pdf = await db.Documento.CountAsync(d => d.IdRevision == id && d.IdTipoDocumento == TipoDocumento.ActaEvaluacion),
        });
        Assert.Equal(42, cierre.Avance);
        Assert.Matches("^AC[0-9]{4}$", cierre.Acta);
        Assert.Equal(1, cierre.Pdf);

        var despues = await flujo.ConsolidarAsync(t, id, enviar: false, comentarios: "Cambio tardío.").EsperarAsync(HttpStatusCode.Conflict);
        Assert.Equal("El informe no está en consolidación.", await despues.MensajeAsync());
    }

    [Fact(DisplayName = "CP-RF-4.8-01 Solo lectura (parte de API)")]
    public async Task CP_RF_4_8_01()
    {
        var t = await Datos.TrabajoAsync(app, semestre: 3);
        var periodos = await Datos.PeriodosPasadosAsync(app, 1);
        await Datos.InformeCerradoAsync(app, t, periodos[0], 30, "Revisar la bibliografía.");
        var id = await flujo.AbrirAsync(t);
        var alumno = await app.SesionAsync(t.Alumno.Correo);

        var cerrado = Arreglo(await MisEvaluacionesAsync(t.Alumno)).Single(i => i.GetProperty("cerrada").GetBoolean());
        Assert.False(string.IsNullOrEmpty(cerrado.Texto("comentarios")));
        Assert.Single(Arreglo(cerrado.GetProperty("compromisos")));

        await alumno.PostAsJsonAsync($"/api/profesor/evaluaciones/{id}/revision",
            new { documento = 100, exposicion = 100, dominio = 100, gradoAvance = 100, avance = 100, comentarios = "Me evalúo yo." })
            .EsperarAsync(HttpStatusCode.Forbidden);
        await alumno.PutAsJsonAsync($"/api/profesor/evaluaciones/{id}/consolidacion",
            new { avance = 100, comentarios = "x", cumplidos = Array.Empty<int>(), nuevos = Array.Empty<object>(), enviar = true })
            .EsperarAsync(HttpStatusCode.Forbidden);
        await alumno.PostAsync($"/api/profesor/evaluaciones/{id}/firmar", null).EsperarAsync(HttpStatusCode.Forbidden);

        Assert.Equal(0, await app.ConBaseAsync(db => db.RevisionProfesor.CountAsync(p => p.IdRevision == id)));
    }

    static MultipartFormDataContent Formulario(byte[] archivo, string nombre) => new()
    {
        { new StringContent(TipoDocumento.Avance.ToString()), "tipo" },
        { new ByteArrayContent(archivo), "archivo", nombre },
    };

    // Fila de una tabla de reporte como diccionario columna → valor.
    public static Dictionary<string, string> Fila(JsonElement tabla, int indice)
    {
        var columnas = tabla.GetProperty("columnas").EnumerateArray().Select(c => c.GetString()!).ToArray();
        var valores = tabla.GetProperty("filas")[indice].EnumerateArray().Select(v => v.GetString()!).ToArray();
        return columnas.Zip(valores).ToDictionary(x => x.First, x => x.Second);
    }
}
