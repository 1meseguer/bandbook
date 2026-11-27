using BandFlow.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BandFlow.Pages.Promotores;

public record PromotorVm(int Id, string Nombre, string Telefono, string Correo, long Cotizaciones);

public class IndexModel : PageModel
{
    private readonly Db _db;
    public IndexModel(Db db) => _db = db;

    [BindProperty(SupportsGet = true)] public string? Buscar { get; set; }
    public List<PromotorVm> Lista { get; set; } = new();
    public string? Error { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            Lista = await _db.QueryAsync(@"
                SELECT p.id_promotor, p.nombre, p.telefono, p.correo,
                       (SELECT COUNT(*) FROM Cotizaciones c WHERE c.id_promotor = p.id_promotor)
                FROM Promotores p
                WHERE @q IS NULL OR p.nombre LIKE CONCAT('%', @q, '%') OR p.correo LIKE CONCAT('%', @q, '%')
                ORDER BY p.nombre",
                r => new PromotorVm(r.GetInt32(0), r.GetString(1), r.GetString(2), r.GetString(3), r.GetInt64(4)),
                ("@q", string.IsNullOrWhiteSpace(Buscar) ? null : Buscar.Trim()));
        }
        catch (Exception ex) { Error = Db.Explain(ex); }
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        try
        {
            var n = await _db.ExecuteAsync("DELETE FROM Promotores WHERE id_promotor = @id", ("@id", id));
            TempData[n > 0 ? "Ok" : "Error"] = n > 0 ? "Promotor eliminado." : "El promotor ya no existe.";
        }
        catch (Exception ex) { TempData["Error"] = Db.Explain(ex); }
        return RedirectToPage();
    }
}
