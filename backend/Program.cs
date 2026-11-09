using Microsoft.EntityFrameworkCore;
using PlataformaTGA.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddDbContext<TgaDbContext>(o =>
    o.UseMySQL(builder.Configuration.GetConnectionString("TgaDb")
        ?? throw new InvalidOperationException("Falta ConnectionStrings:TgaDb (dotnet user-secrets).")));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Sin UseHttpsRedirection: el backend solo se llama a través del proxy de Next.js, por HTTP local.

app.MapGet("/api/salud", async (TgaDbContext db) =>
    Results.Ok(new { api = "ok", baseDeDatos = await db.Database.CanConnectAsync() ? "ok" : "sin conexión" }));

app.Run();
