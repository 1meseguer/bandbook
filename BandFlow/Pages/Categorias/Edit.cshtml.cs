using System.ComponentModel.DataAnnotations;
using BandFlow.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BandFlow.Pages.Categorias;

public class EditModel : PageModel
{
    private readonly Db _db;
    public EditModel(Db db) => _db = db;

    public class InputModel
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(50, ErrorMessage = "Máximo 50 caracteres.")]
        public string Nombre { get; set; } = "";

        [Required(ErrorMessage = "La tarifa es obligatoria.")]
        [Range(0, 1000000, ErrorMessage = "La tarifa debe estar entre 0 y 1,000,000.")]
        public decimal? Precio { get; set; }
    }

    [BindProperty] public InputModel Input { get; set; } = new();
    public int? Id { get; set; }

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        Id = id;
        if (id == null) return Page();
        try
        {
            var c = await _db.FirstAsync("SELECT nombre_categoria, precio_base FROM Categorias WHERE id_categoria = @id",
                                         r => new InputModel { Nombre = r.GetString(0), Precio = r.GetDecimal(1) }, ("@id", id));
            if (c == null) return NotFound();
            Input = c;
        }
        catch (Exception ex) { TempData["Error"] = Db.Explain(ex); return RedirectToPage("Index"); }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int? id)
    {
        Id = id;
        Input.Nombre = (Input.Nombre ?? "").Trim();
        if (!ModelState.IsValid) return Page();
        try
        {
            if (id == null)
                await _db.ExecuteAsync("INSERT INTO Categorias (nombre_categoria, precio_base) VALUES (@n, @p)",
                                       ("@n", Input.Nombre), ("@p", Input.Precio));
            else
                await _db.ExecuteAsync("UPDATE Categorias SET nombre_categoria = @n, precio_base = @p WHERE id_categoria = @id",
                                       ("@n", Input.Nombre), ("@p", Input.Precio), ("@id", id));
            TempData["Ok"] = id == null ? "Categoría creada." : "Categoría actualizada.";
            return RedirectToPage("Index");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, Db.Explain(ex));
            return Page();
        }
    }
}
