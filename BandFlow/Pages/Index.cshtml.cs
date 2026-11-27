using BandFlow.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BandFlow.Pages;

public class IndexModel : PageModel
{
    private readonly BaseDatos _db;
    public IndexModel(BaseDatos db) => _db = db;
    public long Bandas { get; set; }
    public long Pendientes { get; set; }
    public long Eventos { get; set; }
    public decimal Ingresos { get; set; }
    public string? Error { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            Bandas = await _db.CountAsync("SELECT COUNT(*) FROM Bandas WHERE activa = TRUE");
            Pendientes = await _db.CountAsync("SELECT COUNT(*) FROM Cotizaciones WHERE estado_cotizacion = 'Pendiente'");
            Eventos = await _db.CountAsync("SELECT COUNT(*) FROM Eventos WHERE estado_evento = 'Programado'");
            var r = await _db.QueryAsync("SELECT COALESCE(SUM(ingreso_agencia), 0) FROM v_ingresos_por_banda",
                                         x => x.GetDecimal(0));
            Ingresos = r.FirstOrDefault();
        }
        catch (Exception ex) { Error = BaseDatos.Explain(ex); }
    }
}
