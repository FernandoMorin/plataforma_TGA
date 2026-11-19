using System.Collections.Concurrent;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MySql.Data.MySqlClient;
using PlataformaTGA.Data;

namespace PlataformaTGA.Pruebas.Infraestructura;

// La API completa en memoria contra la base plataforma_TGA_pruebas, que se borra y se vuelve a crear
// con las migraciones al empezar cada corrida. La cadena sale de los user-secrets del backend (mismo
// usuario y contraseña); solo cambia el nombre de la base, así no queda ninguna credencial en el repositorio.
public class Aplicacion : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string BasePruebas = "plataforma_TGA_pruebas";

    readonly string carpetaDocumentos = Path.Combine(Path.GetTempPath(), $"tga-pruebas-{Guid.NewGuid():N}");
    readonly ConcurrentDictionary<string, HttpClient> sesiones = new();

    // Periodo académico en curso: el que usa la apertura automática de informes.
    public int PeriodoActual { get; private set; }

    static string Cadena()
    {
        var original = new ConfigurationBuilder().AddUserSecrets(typeof(Program).Assembly).Build().GetConnectionString("TgaDb")
            ?? throw new InvalidOperationException("Falta ConnectionStrings:TgaDb en los user-secrets del backend.");
        return new MySqlConnectionStringBuilder(original) { Database = BasePruebas }.ConnectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Fuera de Development: no corre el seeder y la cookie de sesión es solo HTTPS (como en producción).
        builder.UseEnvironment("Pruebas");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:TgaDb"] = Cadena(),
            ["Documentos:Carpeta"] = carpetaDocumentos,
        }));
    }

    public async Task InitializeAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TgaDbContext>();
        // Candado: estas pruebas solo pueden borrar la base de pruebas.
        if (db.Database.GetDbConnection().Database != BasePruebas)
            throw new InvalidOperationException($"Las pruebas solo corren sobre {BasePruebas}.");
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
        PeriodoActual = await Datos.PeriodoAsync(this, DateTime.Today.AddDays(-30), DateTime.Today.AddDays(90), "Periodo en curso");
    }

    public new async Task DisposeAsync()
    {
        foreach (var c in sesiones.Values) c.Dispose();
        await base.DisposeAsync();
        if (Directory.Exists(carpetaDocumentos)) Directory.Delete(carpetaDocumentos, recursive: true);
    }

    public async Task<T> ConBaseAsync<T>(Func<TgaDbContext, Task<T>> accion)
    {
        await using var scope = Services.CreateAsyncScope();
        return await accion(scope.ServiceProvider.GetRequiredService<TgaDbContext>());
    }

    public Task ConBaseAsync(Func<TgaDbContext, Task> accion) => ConBaseAsync(async db => { await accion(db); return 0; });

    public HttpClient ClienteSinSesion() =>
        CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), HandleCookies = true });

    // Una sesión por usuario durante toda la corrida: el login tiene un límite de 60 por minuto.
    public async Task<HttpClient> SesionAsync(string correo, bool egresado = false)
    {
        if (sesiones.TryGetValue(correo, out var existente)) return existente;
        var cliente = ClienteSinSesion();
        var res = await cliente.PostAsJsonAsync("/api/auth/login", new { correo, contrasena = Datos.Contrasena, portalEgresados = egresado });
        if (!res.IsSuccessStatusCode)
            throw new InvalidOperationException($"No se pudo iniciar sesión como {correo}: {(int)res.StatusCode} {await res.Content.ReadAsStringAsync()}");
        return sesiones[correo] = cliente;
    }
}

[CollectionDefinition(Nombre)]
public class ColeccionPlataforma : ICollectionFixture<Aplicacion>
{
    public const string Nombre = "Plataforma";
}
