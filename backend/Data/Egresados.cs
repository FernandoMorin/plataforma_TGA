using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace PlataformaTGA.Data;

public record EmpleoVista(int Id, string Tipo, string? Puesto, string? Empresa, string? Area, DateTime? Inicio, DateTime? Fin, bool? Relacion, decimal? Sueldo)
{
    // Vigente = sin fecha de fin o con fin futura (BASE_DE_DATOS 3.11: trabajo_actual se calcula así).
    public bool Vigente => Fin is null || Fin >= DateTime.Today;
}

// Reglas de egresados que comparten el coordinador (E1-E4) y el egresado (G1-G5).
public static class Egresados
{
    public const int Localizable = 1, NoLocalizable = 2;
    public static readonly string[] Tipos = ["Empleo", "Beca", "Otro"];

    public static IQueryable<EmpleoVista> Empleos(TgaDbContext db, int clave) =>
        db.Empleo.AsNoTracking().Where(e => e.ClaveAlumno == clave).Select(e => new EmpleoVista(
            e.IdEmpleo, e.Tipo, e.DetalleEmpleo.Select(d => d.Puesto).FirstOrDefault(), e.IdEmpresaNavigation!.Nombre, e.AreaProfesional,
            e.DetalleEmpleo.Select(d => d.FechaInicio).FirstOrDefault(), e.DetalleEmpleo.Select(d => d.FechaFinal).FirstOrDefault(),
            e.DetalleEmpleo.Select(d => d.RelacionPosgrado).FirstOrDefault(), e.DetalleEmpleo.Select(d => d.Sueldo).FirstOrDefault()));

    // Situación laboral calculada: con un empleo vigente de tipo Empleo → Con trabajo; Beca → Becario; Otro → Otro.
    public static string Situacion(IEnumerable<EmpleoVista> empleos)
    {
        var vigentes = empleos.Where(e => e.Vigente).Select(e => e.Tipo).ToList();
        return vigentes.Contains("Empleo") ? "Con trabajo" : vigentes.Contains("Beca") ? "Becario" : vigentes.Contains("Otro") ? "Otro" : "Sin trabajo";
    }

    // El empleo que se muestra como actual: el vigente más reciente, con preferencia por un Empleo.
    public static EmpleoVista? Actual(IEnumerable<EmpleoVista> empleos) =>
        empleos.Where(e => e.Vigente).OrderBy(e => e.Tipo == "Empleo" ? 0 : e.Tipo == "Beca" ? 1 : 2).ThenByDescending(e => e.Inicio).FirstOrDefault();

    // Tiempo para emplearse: meses del egreso al primer inicio posterior al egreso, y su rango para reportes.
    public static (int? Meses, string? Rango) TiempoParaEmplearse(DateTime? egreso, IEnumerable<EmpleoVista> empleos)
    {
        if (egreso is null) return (null, null);
        var inicio = empleos.Where(e => e.Inicio >= egreso).Min(e => e.Inicio);
        if (inicio is null) return (null, null);
        var meses = (inicio.Value.Year - egreso.Value.Year) * 12 + inicio.Value.Month - egreso.Value.Month - (inicio.Value.Day < egreso.Value.Day ? 1 : 0);
        meses = Math.Max(0, meses);
        return (meses, meses <= 6 ? "0 a 6 meses" : meses <= 12 ? "6 a 12 meses" : meses <= 24 ? "12 a 24 meses" : "Más de 24 meses");
    }

    // «Sin actualizar»: su última actualización tiene más de un año.
    public static bool SinActualizar(DateTime? ultima) => ultima is null || ultima < DateTime.Today.AddYears(-1);

    // Junio es el mes de actualización obligatoria: se pide si aún no ha actualizado este junio.
    public const int MesActualizacion = 6;
    public static bool DebeActualizar(DateTime? ultima)
    {
        var hoy = DateTime.Today;
        return hoy.Month == MesActualizacion && !(ultima is { } u && u.Year == hoy.Year && u.Month == MesActualizacion);
    }

    static char Inicial(string? s)
    {
        var limpio = (s ?? "").Trim().Normalize(NormalizationForm.FormD)
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).FirstOrDefault(char.IsLetter);
        return limpio == default ? 'X' : char.ToUpperInvariant(limpio);
    }

    // Identificador = año de la generación + iniciales (nombre, paterno, materno) + consecutivo: 2022RSC01.
    public static async Task<string> Identificador(TgaDbContext db, int anio, string? nombre, string? paterno, string? materno)
    {
        var prefijo = $"{anio}{Inicial(nombre)}{Inicial(paterno)}{Inicial(materno)}";
        var usados = await db.Egresado.Where(e => e.IdentificadorEgresado!.StartsWith(prefijo)).Select(e => e.IdentificadorEgresado!).ToListAsync();
        var siguiente = usados.Select(u => int.TryParse(u[prefijo.Length..], out var n) ? n : 0).DefaultIfEmpty(0).Max() + 1;
        return $"{prefijo}{siguiente:D2}";
    }
}
