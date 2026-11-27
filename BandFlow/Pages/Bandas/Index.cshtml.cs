using BandFlow.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BandFlow.Pages.Bandas;

public record BandaVm(int Id, string Nombre, string Genero, string Categoria, decimal Precio, decimal Comision, bool Activa);

public class IndexModel : PageModel
{
    private readonly BaseDatos _db;
    public IndexModel(BaseDatos db) => _db = db;

    [BindProperty(SupportsGet = true)] public string? Buscar { get; set; }
    public List<BandaVm> Lista { get; set; } = new();
    public string? Error { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            Lista = await _db.QueryAsync(@"
                SELECT b.id_banda, b.nombre_banda, b.genero_musical, c.nombre_categoria,
                       c.precio_base, b.porcentaje_comision, b.activa
                FROM Bandas b
                JOIN Categorias c ON c.id_categoria = b.id_categoria
                WHERE @q IS NULL OR b.nombre_banda LIKE CONCAT('%', @q, '%')
                                 OR b.genero_musical LIKE CONCAT('%', @q, '%')
                ORDER BY b.nombre_banda",
                r => new BandaVm(r.GetInt32(0), r.GetString(1), r.GetString(2), r.GetString(3),
                                 r.GetDecimal(4), r.GetDecimal(5), r.GetBoolean(6)),
                ("@q", string.IsNullOrWhiteSpace(Buscar) ? null : Buscar.Trim()));
        }
        catch (Exception ex) { Error = BaseDatos.Explain(ex); }
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        try
        {
            var n = await _db.ExecuteAsync("DELETE FROM Bandas WHERE id_banda = @id", ("@id", id));
            TempData[n > 0 ? "Ok" : "Error"] = n > 0 ? "Banda eliminada." : "La banda ya no existe.";
        }
        catch (Exception ex) { TempData["Error"] = BaseDatos.Explain(ex); }
        return RedirectToPage();
    }
}
