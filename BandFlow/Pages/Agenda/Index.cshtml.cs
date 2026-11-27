using BandFlow.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;

namespace BandFlow.Pages.Agenda;

public record AgendaVm(int IdEvento, string Banda, string Promotor, DateTime Inicio, DateTime Fin,
                       string Lugar, string Estado, string? GoogleId, decimal Total);

public class IndexModel : PageModel
{
    private readonly BaseDatos _db;
    public IndexModel(BaseDatos db) => _db = db;

    [BindProperty(SupportsGet = true)] public string? Estado { get; set; }
    public List<AgendaVm> Lista { get; set; } = new();
    public string? Error { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            // Usa la vista v_agenda_eventos (une 4 tablas)
            Lista = await _db.QueryAsync(@"
                SELECT id_evento, nombre_banda, promotor, fecha_inicio, fecha_fin, lugar_evento,
                       estado_evento, google_event_id, costo_total
                FROM v_agenda_eventos
                WHERE @e IS NULL OR estado_evento = @e
                ORDER BY fecha_inicio",
                r => new AgendaVm(r.GetInt32(0), r.GetString(1), r.GetString(2), r.GetDateTime(3), r.GetDateTime(4),
                                  r.GetString(5), r.GetString(6), r.IsDBNull(7) ? null : r.GetString(7), r.GetDecimal(8)),
                ("@e", string.IsNullOrWhiteSpace(Estado) ? null : Estado));
        }
        catch (Exception ex) { Error = BaseDatos.Explain(ex); }
    }
}
