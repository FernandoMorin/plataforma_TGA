using System.Net;
using System.Net.Http.Json;
using PlataformaTGA.Pruebas.Infraestructura;

namespace PlataformaTGA.Pruebas.Evaluaciones;

// Los pasos del informe por la API, tal como los hace cada integrante del comité:
public class FlujoInforme(Aplicacion app)
{
    public record Compromiso(string Descripcion, DateTime Fecha);

    // Al consultar al alumno se abre el informe del periodo; devuelve su id.
    public async Task<int> AbrirAsync(TrabajoPrueba t)
    {
        var detalle = await (await app.SesionAsync(t.Director.Correo)).GetAsync($"/api/profesor/alumnos/{t.IdTesis}").JsonAsync();
        var abierto = detalle.GetProperty("informes").EnumerateArray().Select(i => i.GetProperty("informe"))
            .Single(i => i.Texto("estado") == "Abierta");
        return abierto.GetProperty("id").GetInt32();
    }

    public async Task<HttpResponseMessage> RevisionAsync(ProfesorPrueba p, int idRevision, decimal avance = 40, string comentarios = "Buen avance.") =>
        await (await app.SesionAsync(p.Correo)).PostAsJsonAsync($"/api/profesor/evaluaciones/{idRevision}/revision",
            new { documento = 80, exposicion = 80, dominio = 80, gradoAvance = 80, avance, comentarios });

    public async Task TodasLasRevisionesAsync(TrabajoPrueba t, int idRevision, decimal avance = 40)
    {
        foreach (var p in t.Integrantes)
            await RevisionAsync(p, idRevision, avance).EsperarAsync(HttpStatusCode.NoContent);
    }

    public async Task<HttpResponseMessage> ConsolidarAsync(TrabajoPrueba t, int idRevision, bool enviar, decimal avance = 40,
        string comentarios = "Comentarios del comité.", int[]? cumplidos = null, Compromiso[]? nuevos = null) =>
        await (await app.SesionAsync(t.Director.Correo)).PutAsJsonAsync($"/api/profesor/evaluaciones/{idRevision}/consolidacion", new
        {
            documento = 85, exposicion = 85, dominio = 85, gradoAvance = 85, avance, comentarios,
            cumplidos = cumplidos ?? [],
            nuevos = (nuevos ?? []).Select(n => new { descripcion = n.Descripcion, fecha = n.Fecha }),
            enviar,
        });

    public async Task FirmasAsync(TrabajoPrueba t, int idRevision)
    {
        foreach (var p in t.Integrantes)
            await (await app.SesionAsync(p.Correo)).PostAsync($"/api/profesor/evaluaciones/{idRevision}/firmar", null)
                .EsperarAsync(HttpStatusCode.NoContent);
    }

    public async Task<string> EstadoAsync(ProfesorPrueba p, int idRevision) =>
        (await (await app.SesionAsync(p.Correo)).GetAsync($"/api/profesor/evaluaciones/{idRevision}").JsonAsync()).Texto("estado");

    // El informe completo hasta Cerrada.
    public async Task<int> CerrarAsync(TrabajoPrueba t, decimal avance, int[]? cumplidos = null, Compromiso[]? nuevos = null)
    {
        var id = await AbrirAsync(t);
        await TodasLasRevisionesAsync(t, id, avance);
        await ConsolidarAsync(t, id, enviar: true, avance, cumplidos: cumplidos, nuevos: nuevos).EsperarAsync(HttpStatusCode.NoContent);
        await FirmasAsync(t, id);
        return id;
    }
}
