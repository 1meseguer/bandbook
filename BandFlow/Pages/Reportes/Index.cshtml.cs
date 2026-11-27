using BandFlow.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BandFlow.Pages.Reportes;

public record IngresoBandaVm(string Banda, string Genero, long Shows, decimal Comisiones, decimal Servicios, decimal Total);
public record IngresoMesVm(string Mes, long Shows, decimal Total);

public class IndexModel : PageModel
{
    private readonly BaseDatos _db;
    public IndexModel(BaseDatos db) => _db = db;

    [BindProperty(SupportsGet = true)] public string? Buscar { get; set; }
    [BindProperty(SupportsGet = true)] public string? Genero { get; set; }

    public List<string> Generos { get; set; } = new();
    public List<IngresoBandaVm> PorBanda { get; set; } = new();
    public List<IngresoMesVm> PorMes { get; set; } = new();
    public decimal TotalGeneral => PorBanda.Sum(x => x.Total);
    public string? Error { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            Generos = await _db.QueryAsync("SELECT DISTINCT genero_musical FROM Bandas ORDER BY genero_musical",
                                           r => r.GetString(0));

            // Usa la vista v_ingresos_por_banda unida con Bandas (multitabla)
            PorBanda = await _db.QueryAsync(@"
                SELECT v.nombre_banda, b.genero_musical, v.shows_aprobados,
                       v.ingreso_comisiones, v.ingreso_servicios, v.ingreso_agencia
                FROM v_ingresos_por_banda v
                JOIN Bandas b ON b.id_banda = v.id_banda
                WHERE (@q IS NULL OR v.nombre_banda LIKE CONCAT('%', @q, '%'))
                  AND (@g IS NULL OR b.genero_musical = @g)
                ORDER BY v.ingreso_agencia DESC, v.nombre_banda",
                r => new IngresoBandaVm(r.GetString(0), r.GetString(1), r.GetInt64(2),
                                        r.GetDecimal(3), r.GetDecimal(4), r.GetDecimal(5)),
                ("@q", string.IsNullOrWhiteSpace(Buscar) ? null : Buscar.Trim()),
                ("@g", string.IsNullOrWhiteSpace(Genero) ? null : Genero));

            PorMes = await _db.QueryAsync(@"
                SELECT DATE_FORMAT(fecha_inicio, '%Y-%m'), COUNT(*),
                       SUM(monto_comision_banda + costo_luces + monto_extras_manager)
                FROM Cotizaciones
                WHERE estado_cotizacion = 'Aprobada'
                GROUP BY DATE_FORMAT(fecha_inicio, '%Y-%m')
                ORDER BY 1",
                r => new IngresoMesVm(r.GetString(0), r.GetInt64(1), r.GetDecimal(2)));
        }
        catch (Exception ex) { Error = BaseDatos.Explain(ex); }
    }
}
