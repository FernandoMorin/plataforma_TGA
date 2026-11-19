using System.Net;
using System.Text.Json;

namespace PlataformaTGA.Pruebas.Infraestructura;

public static class Respuestas
{
    // Falla con el cuerpo de la respuesta a la vista, para saber por qué no fue el código esperado.
    public static async Task<HttpResponseMessage> EsperarAsync(this Task<HttpResponseMessage> peticion, HttpStatusCode esperado)
    {
        var res = await peticion;
        if (res.StatusCode != esperado)
            Assert.Fail($"Se esperaba {(int)esperado} y llegó {(int)res.StatusCode}: {await res.Content.ReadAsStringAsync()}");
        return res;
    }

    public static async Task<JsonElement> JsonAsync(this HttpResponseMessage res) =>
        JsonDocument.Parse(await res.Content.ReadAsStringAsync()).RootElement.Clone();

    public static async Task<JsonElement> JsonAsync(this Task<HttpResponseMessage> peticion) =>
        await (await peticion.EsperarAsync(HttpStatusCode.OK)).JsonAsync();

    public static async Task<string> MensajeAsync(this HttpResponseMessage res) =>
        (await res.JsonAsync()).GetProperty("mensaje").GetString()!;

    public static string Texto(this JsonElement e, string propiedad) => e.GetProperty(propiedad).GetString()!;
    public static decimal Numero(this JsonElement e, string propiedad) => e.GetProperty(propiedad).GetDecimal();
}
