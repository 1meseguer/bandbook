using BandFlow.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BandFlow.Pages.Categorias;

public record CategoriaVm(int Id, string Nombre, decimal Precio, long Bandas);

public class IndexModel : PageModel
{
    private readonly BaseDatos _db;
    public IndexModel(BaseDatos db) => _db = db;

    [BindProperty(SupportsGet = true)] public string? Buscar { get; set; }
    public List<CategoriaVm> Lista { get; set; } = new();
    public string? Error { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            Lista = await _db.QueryAsync(@"
                SELECT c.id_categoria, c.nombre_categoria, c.precio_base,
                       (SELECT COUNT(*) FROM Bandas b WHERE b.id_categoria = c.id_categoria)
                FROM Categorias c
                WHERE @q IS NULL OR c.nombre_categoria LIKE CONCAT('%', @q, '%')
                ORDER BY c.precio_base",
                r => new CategoriaVm(r.GetInt32(0), r.GetString(1), r.GetDecimal(2), r.GetInt64(3)),
                ("@q", string.IsNullOrWhiteSpace(Buscar) ? null : Buscar.Trim()));
        }
        catch (Exception ex) { Error = BaseDatos.Explain(ex); }
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        try
        {
            var n = await _db.ExecuteAsync("DELETE FROM Categorias WHERE id_categoria = @id", ("@id", id));
            TempData[n > 0 ? "Ok" : "Error"] = n > 0 ? "Categoría eliminada." : "La categoría ya no existe.";
        }
        catch (Exception ex) { TempData["Error"] = BaseDatos.Explain(ex); }
        return RedirectToPage();
    }
}
