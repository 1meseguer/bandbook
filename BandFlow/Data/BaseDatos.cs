using System.Data;
using System.Globalization;
using MySqlConnector;

namespace BandFlow.Data;

/// <summary>
/// Capa de acceso a MySQL. Todas las consultas usan parámetros (evita inyección SQL).
/// La cadena de conexión se lee de user-secrets: ConnectionStrings:BandFlow
/// </summary>
public class BaseDatos
{
    private readonly string _cs;

    public BaseDatos(IConfiguration config)
    {
        _cs = config.GetConnectionString("BandFlow") ?? throw new InvalidOperationException("Falto la cadena de conexion.");
    }

    // Abrir la conexion a la bd.
    public async Task<MySqlConnection> OpenAsync()
    {
        var cn = new MySqlConnection(_cs);
        await cn.OpenAsync();
        return cn;
    }

    public async Task<List<T>> QueryAsync<T>(string sql, Func<MySqlDataReader, T> map, params (string Name, object? Value)[] ps)
    {
        var lista = new List<T>();
        await using var cn = await OpenAsync();
        await using var cmd = new MySqlCommand(sql, cn);
        foreach (var (n, v) in ps) cmd.Parameters.AddWithValue(n, v ?? DBNull.Value);
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) lista.Add(map(r));
        return lista;
    }

    public async Task<T?> FirstAsync<T>(string sql, Func<MySqlDataReader, T> map, params (string Name, object? Value)[] ps)
    {
        var l = await QueryAsync(sql, map, ps);
        return l.Count > 0 ? l[0] : default;
    }

    public async Task<int> ExecuteAsync(string sql, params (string Name, object? Value)[] ps)
    {
        await using var cn = await OpenAsync();
        await using var cmd = new MySqlCommand(sql, cn);
        foreach (var (n, v) in ps) cmd.Parameters.AddWithValue(n, v ?? DBNull.Value);
        return await cmd.ExecuteNonQueryAsync();
    }

    public async Task<long> CountAsync(string sql)
    {
        await using var cn = await OpenAsync();
        await using var cmd = new MySqlCommand(sql, cn);
        return Convert.ToInt64(await cmd.ExecuteScalarAsync());
    }

    // Errores de SQL a mensajes.
    public static string Explain(Exception ex)
    {
        if (ex is MySqlException m)
        {
            return m.Number switch
            {
                1644 => m.Message, // SIGNAL de triggers y procedimientos
                1062 => "Ya existe un registro con ese valor (dato duplicado).",
                1451 => "No se puede eliminar: tiene registros relacionados.",
                1452 => "El dato relacionado seleccionado no existe.",
                3819 => "Algún valor no cumple las reglas de la base de datos (revisa montos, porcentajes y estados).",
                1045 or 1042 or 2002 or 0 => "No se pudo conectar con la base de datos. Verifica que MySQL esté encendido.",
                _ => "Error de base de datos. Intenta de nuevo."
            };
        }
        return "Ocurrio un error . Intenta de nuevo.";
    }
}

public static class Fmt
{
    private static readonly CultureInfo Mx = new("es-MX");
    public static string Money(decimal v) => v.ToString("C", Mx);
    public static string Fecha(DateTime d) => d.ToString("dd/MM/yyyy HH:mm", Mx);
}
