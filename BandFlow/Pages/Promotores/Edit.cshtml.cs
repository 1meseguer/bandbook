using System.ComponentModel.DataAnnotations;
using BandFlow.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BandFlow.Pages.Promotores;

public class EditModel : PageModel
{
    private readonly BaseDatos _db;
    public EditModel(BaseDatos db) => _db = db;

    public class InputModel
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(100, ErrorMessage = "Máximo 100 caracteres.")]
        public string Nombre { get; set; } = "";

        [Required(ErrorMessage = "El teléfono es obligatorio.")]
        [StringLength(20, ErrorMessage = "Máximo 20 caracteres.")]
        [RegularExpression(@"^[0-9+\-\s()]{7,20}$", ErrorMessage = "Teléfono no válido (solo números, espacios, + - y paréntesis).")]
        public string Telefono { get; set; } = "";

        [Required(ErrorMessage = "El correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "Formato de correo no válido.")]
        [StringLength(100, ErrorMessage = "Máximo 100 caracteres.")]
        public string Correo { get; set; } = "";
    }

    [BindProperty] public InputModel Input { get; set; } = new();
    public int? Id { get; set; }

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        Id = id;
        if (id == null) return Page();
        try
        {
            var p = await _db.FirstAsync("SELECT nombre, telefono, correo FROM Promotores WHERE id_promotor = @id",
                r => new InputModel { Nombre = r.GetString(0), Telefono = r.GetString(1), Correo = r.GetString(2) },
                ("@id", id));
            if (p == null) return NotFound();
            Input = p;
        }
        catch (Exception ex) { TempData["Error"] = BaseDatos.Explain(ex); return RedirectToPage("Index"); }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int? id)
    {
        Id = id;
        Input.Nombre = (Input.Nombre ?? "").Trim();
        Input.Telefono = (Input.Telefono ?? "").Trim();
        Input.Correo = (Input.Correo ?? "").Trim();
        if (!ModelState.IsValid) return Page();
        try
        {
            if (id == null)
                await _db.ExecuteAsync("INSERT INTO Promotores (nombre, telefono, correo) VALUES (@n, @t, @c)",
                                       ("@n", Input.Nombre), ("@t", Input.Telefono), ("@c", Input.Correo));
            else
                await _db.ExecuteAsync("UPDATE Promotores SET nombre = @n, telefono = @t, correo = @c WHERE id_promotor = @id",
                                       ("@n", Input.Nombre), ("@t", Input.Telefono), ("@c", Input.Correo), ("@id", id));
            TempData["Ok"] = id == null ? "Promotor creado." : "Promotor actualizado.";
            return RedirectToPage("Index");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, BaseDatos.Explain(ex));
            return Page();
        }
    }
}
