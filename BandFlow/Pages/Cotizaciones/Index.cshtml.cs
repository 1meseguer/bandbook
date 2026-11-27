using BandFlow.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BandFlow.Pages.Cotizaciones;

public record CotizacionVm(int Id, string Banda, string Promotor, DateTime Inicio, decimal Total, string Estado);

public class IndexModel : PageModel
{
    private readonly Db _db;
    public IndexModel(Db db) => _db = db;

    [BindProperty(SupportsGet = true)] public string? Buscar { get; set; }
    [BindProperty(SupportsGet = true)] public string? Estado { get; set; }
    [BindProperty(SupportsGet = true)] public DateTime? Desde { get; set; }
    [BindProperty(SupportsGet = true)] public DateTime? Hasta { get; set; }

    public List<CotizacionVm> Lista { get; set; } = new();
    public string? Error { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            Lista = await _db.QueryAsync(@"
                SELECT c.id_cotizacion, b.nombre_banda, p.nombre, c.fecha_inicio, c.costo_total, c.estado_cotizacion
                FROM Cotizaciones c
                JOIN Bandas b     ON b.id_banda = c.id_banda
                JOIN Promotores p ON p.id_promotor = c.id_promotor
                WHERE (@e IS NULL OR c.estado_cotizacion = @e)
                  AND (@q IS NULL OR b.nombre_banda LIKE CONCAT('%', @q, '%')
                                  OR p.nombre LIKE CONCAT('%', @q, '%'))
                  AND (@d IS NULL OR c.fecha_inicio >= @d)
                  AND (@h IS NULL OR c.fecha_inicio < DATE_ADD(@h, INTERVAL 1 DAY))
                ORDER BY c.fecha_inicio DESC",
                r => new CotizacionVm(r.GetInt32(0), r.GetString(1), r.GetString(2),
                                      r.GetDateTime(3), r.GetDecimal(4), r.GetString(5)),
                ("@e", string.IsNullOrWhiteSpace(Estado) ? null : Estado),
                ("@q", string.IsNullOrWhiteSpace(Buscar) ? null : Buscar.Trim()),
                ("@d", Desde), ("@h", Hasta));
        }
        catch (Exception ex) { Error = Db.Explain(ex); }
    }
}
