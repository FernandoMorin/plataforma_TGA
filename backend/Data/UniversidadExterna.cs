using MySql.Data.MySqlClient;

namespace PlataformaTGA.Data;

// Un estudiante tal como lo tiene el registro de la universidad. Los cinco primeros campos se copian
// al importar; Estatus (Inscrito | Egresado | Baja) solo avisa si el registro sigue vigente.
public record EstudianteUniversidad(int Clave, string Nombre, string ApellidoPaterno, string? ApellidoMaterno, string Correo, string? Estatus);

// Lectura del registro de la universidad (en desarrollo, el esquema TEMPORAL; ver database/temporal.sql).
// SOLO LECTURA: no expone escrituras, y su usuario de MySQL solo tiene SELECT sobre una tabla.
// Es opcional: sin la cadena ConnectionStrings:UniversidadDb el resto del sistema funciona y la importación
// responde 503. Por eso usa MySqlConnection directo y no un DbContext, que exigiría la cadena al arrancar.
public class UniversidadExterna(IConfiguration config)
{
    public const int MaxResultados = 50;
    public const int ClaveMinima = 100_000, ClaveMaxima = 999_999;

    // Columnas explícitas: la tabla real tendrá más datos personales que la Plataforma no guarda.
    const string Columnas = "SELECT clave_alumno, nombre, apellido_paterno, apellido_materno, correo_institucional, estatus FROM estudiante";

    string? Cadena => config.GetConnectionString("UniversidadDb");
    public bool Configurado => !string.IsNullOrWhiteSpace(Cadena);

    // Por claves (6 cifras) y, si se da, por nombre. Devuelve solo los que existen.
    // Lanza MySqlException si el registro no responde, para no confundirlo con «no encontrado».
    public async Task<List<EstudianteUniversidad>> Buscar(IReadOnlyCollection<int> claves, string? nombre, CancellationToken ct = default)
    {
        var validas = claves.Where(c => c is >= ClaveMinima and <= ClaveMaxima).Distinct().Take(MaxResultados).ToArray();
        var texto = nombre?.Trim();
        if (!Configurado || (validas.Length == 0 && string.IsNullOrEmpty(texto))) return [];

        await using var conexion = new MySqlConnection(Cadena);
        await conexion.OpenAsync(ct);
        var encontrados = new Dictionary<int, EstudianteUniversidad>();

        if (validas.Length > 0)
        {
            // Un parámetro por clave: nunca se interpolan valores en el SQL.
            var marcadores = string.Join(", ", validas.Select((_, i) => "@c" + i));
            await using var cmd = new MySqlCommand($"{Columnas} WHERE clave_alumno IN ({marcadores})", conexion) { CommandTimeout = 10 };
            for (var i = 0; i < validas.Length; i++) cmd.Parameters.AddWithValue("@c" + i, validas[i]);
            await Leer(cmd, encontrados, ct);
        }
        if (!string.IsNullOrEmpty(texto))
        {
            await using var cmd = new MySqlCommand(
                $"{Columnas} WHERE CONCAT_WS(' ', nombre, apellido_paterno, apellido_materno) LIKE @n ORDER BY apellido_paterno, nombre LIMIT {MaxResultados}",
                conexion) { CommandTimeout = 10 };
            // Los comodines que escriba el usuario se toman como texto.
            cmd.Parameters.AddWithValue("@n", "%" + texto.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%");
            await Leer(cmd, encontrados, ct);
        }
        return [.. encontrados.Values];
    }

    static async Task Leer(MySqlCommand cmd, Dictionary<int, EstudianteUniversidad> destino, CancellationToken ct)
    {
        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
            destino[r.GetInt32(0)] = new EstudianteUniversidad(
                r.GetInt32(0), r.GetString(1), r.GetString(2), r.IsDBNull(3) ? null : r.GetString(3), r.GetString(4), r.IsDBNull(5) ? null : r.GetString(5));
    }
}
