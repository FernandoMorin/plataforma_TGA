using Microsoft.EntityFrameworkCore;

namespace PlataformaTGA.Data;

// shortcut: vacío hasta el scaffolding de plataforma_TGA (paso 3), que lo reemplaza con las entidades.
public partial class TgaDbContext(DbContextOptions<TgaDbContext> options) : DbContext(options);
