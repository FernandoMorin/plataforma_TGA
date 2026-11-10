using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using PlataformaTGA.Auth;
using PlataformaTGA.Data;

var builder = WebApplication.CreateBuilder(args);

// Actas en PDF. Licencia Community: gratuita para proyectos académicos y organizaciones con ingresos menores a 1 M USD.
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddDbContext<TgaDbContext>(o =>
    o.UseMySQL(builder.Configuration.GetConnectionString("TgaDb")
        ?? throw new InvalidOperationException("Falta ConnectionStrings:TgaDb (dotnet user-secrets).")));

builder.Services.AddScoped<ValidarSesion>();
builder.Services.AddSingleton<UniversidadExterna>();
builder.Services.AddSingleton<Correo>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(o =>
{
    o.Cookie.Name = "TGA.Sesion";
    o.Cookie.HttpOnly = true;
    // Strict + mismo origen (proxy de Next.js) cubre CSRF sin tokens extra.
    o.Cookie.SameSite = SameSiteMode.Strict;
    o.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    o.EventsType = typeof(ValidarSesion);
});
builder.Services.AddAuthorization();

// Límite por IP para el login, además del bloqueo de 5 intentos por cuenta.
// shortcut: todas las peticiones llegan desde Next.js, así que la IP es la del proxy y el límite es global;
// Next.js reenvía tal cual el X-Forwarded-For del cliente (falsificable) y no agrega la IP real.
// Al implantar: que el servidor web del frente fije X-Forwarded-For y activar UseForwardedHeaders con KnownProxies.
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = 429;
    o.AddPolicy("auth", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "desconocida",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 60, Window = TimeSpan.FromMinutes(1) }));
    // Recuperación de contraseña: más estricto, porque cada solicitud puede mandar un correo.
    o.AddPolicy("recuperacion", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "desconocida",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 30, Window = TimeSpan.FromMinutes(10) }));
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    using var scope = app.Services.CreateScope();
    await SeederDesarrollo.SembrarAsync(scope.ServiceProvider.GetRequiredService<TgaDbContext>());
}

// Sin UseHttpsRedirection: el backend solo se llama a través del proxy de Next.js, por HTTP local.

app.UseAuthentication();
app.Use(async (ctx, next) =>
{
    if (ctx.User.HasClaim(Seguridad.ClaimCambio, "1") && !Seguridad.PermitidoSinCambio(ctx.Request.Path))
    {
        ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
        await ctx.Response.WriteAsJsonAsync(new { mensaje = "Cambia tu contraseña para continuar." });
        return;
    }
    await next();
});
app.UseRateLimiter();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/api/salud", async (TgaDbContext db) =>
    Results.Ok(new { api = "ok", baseDeDatos = await db.Database.CanConnectAsync() ? "ok" : "sin conexión" }));

app.Run();
