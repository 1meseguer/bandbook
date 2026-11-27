using System.ComponentModel.DataAnnotations;
using BandFlow.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BandFlow.Pages.Bandas;

public class EditModel : PageModel
{
    private readonly BaseDatos _db;
    public EditModel(BaseDatos db) => _db = db;

    public class InputModel
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(100, ErrorMessage = "Máximo 100 caracteres.")]
        public string Nombre { get; set; } = "";

        [Required(ErrorMessage = "El género es obligatorio.")]
        [StringLength(60, ErrorMessage = "Máximo 60 caracteres.")]
        public string Genero { get; set; } = "";

        [Required(ErrorMessage = "La comisión es obligatoria.")]
        [Range(0, 100, ErrorMessage = "La comisión debe estar entre 0 y 100.")]
        public decimal? Comision { get; set; } = 10;

        [Range(1, int.MaxValue, ErrorMessage = "Selecciona una categoría.")]
        public int IdCategoria { get; set; }

        [Required(ErrorMessage = "El contacto es obligatorio.")]
        [StringLength(100, ErrorMessage = "Máximo 100 caracteres.")]
        public string Contacto { get; set; } = "";

        public bool Activa { get; set; } = true;
    }

    [BindProperty] public InputModel Input { get; set; } = new();
    public int? Id { get; set; }
    public List<SelectListItem> Categorias { get; set; } = new();

    private async Task CargarCategoriasAsync()
    {
        Categorias = await _db.QueryAsync(
            "SELECT id_categoria, nombre_categoria FROM Categorias ORDER BY precio_base",
            r => new SelectListItem(r.GetString(1), r.GetInt32(0).ToString()));
    }

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        Id = id;
        try
        {
            await CargarCategoriasAsync();
            if (id == null) return Page();
            var b = await _db.FirstAsync(@"SELECT nombre_banda, genero_musical, porcentaje_comision,
                                                  id_categoria, contacto_principal, activa
                                           FROM Bandas WHERE id_banda = @id",
                r => new InputModel { Nombre = r.GetString(0), Genero = r.GetString(1), Comision = r.GetDecimal(2),
                                      IdCategoria = r.GetInt32(3), Contacto = r.GetString(4), Activa = r.GetBoolean(5) },
                ("@id", id));
            if (b == null) return NotFound();
            Input = b;
        }
        catch (Exception ex) { TempData["Error"] = BaseDatos.Explain(ex); return RedirectToPage("Index"); }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int? id)
    {
        Id = id;
        Input.Nombre = (Input.Nombre ?? "").Trim();
        Input.Genero = (Input.Genero ?? "").Trim();
        Input.Contacto = (Input.Contacto ?? "").Trim();
        try
        {
            await CargarCategoriasAsync();
            if (!ModelState.IsValid) return Page();

            if (id == null)
                await _db.ExecuteAsync(@"INSERT INTO Bandas (nombre_banda, genero_musical, porcentaje_comision,
                                                              id_categoria, contacto_principal, activa)
                                         VALUES (@n, @g, @c, @cat, @ct, @a)",
                    ("@n", Input.Nombre), ("@g", Input.Genero), ("@c", Input.Comision),
                    ("@cat", Input.IdCategoria), ("@ct", Input.Contacto), ("@a", Input.Activa));
            else
                await _db.ExecuteAsync(@"UPDATE Bandas SET nombre_banda = @n, genero_musical = @g,
                                                porcentaje_comision = @c, id_categoria = @cat,
                                                contacto_principal = @ct, activa = @a
                                         WHERE id_banda = @id",
                    ("@n", Input.Nombre), ("@g", Input.Genero), ("@c", Input.Comision),
                    ("@cat", Input.IdCategoria), ("@ct", Input.Contacto), ("@a", Input.Activa), ("@id", id));

            TempData["Ok"] = id == null ? "Banda creada." : "Banda actualizada.";
            return RedirectToPage("Index");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, BaseDatos.Explain(ex));
            return Page();
        }
    }
}
