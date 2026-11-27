using System.Data;
using BandFlow.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using MySqlConnector;

namespace BandFlow.Pages.Cotizaciones;

public record CotDetalleVm(int Id, string Banda, string Promotor, DateTime Inicio, DateTime Fin, int Duracion,
                           decimal Base, bool Luces, decimal CostoLuces, decimal Comision, decimal Extras,
                           decimal Total, string Estado);
public record ExtraVm(string Nombre, int Cantidad, decimal Precio);
public record EventoVm(int Id, string Lugar, string Estado, string? GoogleId);

public class DetalleModel : PageModel
{
    private readonly BaseDatos _db;
    public DetalleModel(BaseDatos db) => _db = db;

    public CotDetalleVm? Cot { get; set; }
    public List<ExtraVm> Extras { get; set; } = new();
    public List<SelectListItem> Catalogo { get; set; } = new();
    public EventoVm? Evento { get; set; }
    public string? Error { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        try
        {
            Cot = await _db.FirstAsync(@"
                SELECT c.id_cotizacion, b.nombre_banda, p.nombre, c.fecha_inicio, c.fecha_fin, c.duracion_minutos,
                       c.costo_base, c.incluye_luces, c.costo_luces, c.monto_comision_banda,
                       c.monto_extras_manager, c.costo_total, c.estado_cotizacion
                FROM Cotizaciones c
                JOIN Bandas b ON b.id_banda = c.id_banda
                JOIN Promotores p ON p.id_promotor = c.id_promotor
                WHERE c.id_cotizacion = @id",
                r => new CotDetalleVm(r.GetInt32(0), r.GetString(1), r.GetString(2), r.GetDateTime(3), r.GetDateTime(4),
                                      r.GetInt32(5), r.GetDecimal(6), r.GetBoolean(7), r.GetDecimal(8), r.GetDecimal(9),
                                      r.GetDecimal(10), r.GetDecimal(11), r.GetString(12)),
                ("@id", id));
            if (Cot == null) return NotFound();

            Extras = await _db.QueryAsync(@"
                SELECT x.nombre_extra, ce.cantidad, ce.precio_unitario
                FROM Cotizacion_Extras ce JOIN Catalogo_Extras x ON x.id_extra = ce.id_extra
                WHERE ce.id_cotizacion = @id ORDER BY x.nombre_extra",
                r => new ExtraVm(r.GetString(0), r.GetInt32(1), r.GetDecimal(2)), ("@id", id));

            Catalogo = await _db.QueryAsync(
                "SELECT id_extra, CONCAT(nombre_extra, ' (', FORMAT(precio_sugerido, 2), ')') FROM Catalogo_Extras ORDER BY nombre_extra",
                r => new SelectListItem(r.GetString(1), r.GetInt32(0).ToString()));

            Evento = await _db.FirstAsync(
                "SELECT id_evento, lugar_evento, estado_evento, google_event_id FROM Eventos WHERE id_cotizacion = @id",
                r => new EventoVm(r.GetInt32(0), r.GetString(1), r.GetString(2), r.IsDBNull(3) ? null : r.GetString(3)),
                ("@id", id));
        }
        catch (Exception ex) { Error = BaseDatos.Explain(ex); }
        return Page();
    }

    // Agrega un extra: el procedimiento valida y recalcula totales en una transacción
    public async Task<IActionResult> OnPostAgregarExtraAsync(int id, int extraId, int cantidad)
    {
        try
        {
            if (extraId <= 0) throw new InvalidOperationException("Selecciona un extra.");
            await using var cn = await _db.OpenAsync();
            await using var cmd = new MySqlCommand("sp_agregar_extra", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("p_id_cotizacion", id);
            cmd.Parameters.AddWithValue("p_id_extra", extraId);
            cmd.Parameters.AddWithValue("p_cantidad", cantidad);
            await cmd.ExecuteNonQueryAsync();
            TempData["Ok"] = "Extra agregado y total recalculado.";
        }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        catch (Exception ex) { TempData["Error"] = BaseDatos.Explain(ex); }
        return RedirectToPage(new { id });
    }

    // Aprueba: cambia el estado y crea el evento en UNA transacción (procedimiento).
    // Si hay traslape, el trigger cancela todo y se muestra el mensaje.
    public async Task<IActionResult> OnPostAprobarAsync(int id, string? lugar)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(lugar)) throw new InvalidOperationException("Indica el lugar del evento.");
            await using var cn = await _db.OpenAsync();
            await using var cmd = new MySqlCommand("sp_aprobar_cotizacion", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("p_id_cotizacion", id);
            cmd.Parameters.AddWithValue("p_lugar", lugar.Trim());
            await cmd.ExecuteNonQueryAsync();
            TempData["Ok"] = "Cotización aprobada y evento agendado.";
        }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        catch (Exception ex) { TempData["Error"] = BaseDatos.Explain(ex); }
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostRechazarAsync(int id)
    {
        try
        {
            var n = await _db.ExecuteAsync(
                "UPDATE Cotizaciones SET estado_cotizacion = 'Rechazada' WHERE id_cotizacion = @id AND estado_cotizacion = 'Pendiente'",
                ("@id", id));
            if (n > 0) TempData["Ok"] = "Cotización rechazada.";
            else TempData["Error"] = "Solo se pueden rechazar cotizaciones pendientes.";
        }
        catch (Exception ex) { TempData["Error"] = BaseDatos.Explain(ex); }
        return RedirectToPage(new { id });
    }
}
