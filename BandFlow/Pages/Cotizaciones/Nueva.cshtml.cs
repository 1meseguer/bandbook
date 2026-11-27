using System.ComponentModel.DataAnnotations;
using System.Data;
using BandFlow.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using MySqlConnector;

namespace BandFlow.Pages.Cotizaciones;

public class NuevaModel : PageModel
{
    private readonly BaseDatos _db;
    public NuevaModel(BaseDatos db) => _db = db;

    public class InputModel
    {
        [Range(1, int.MaxValue, ErrorMessage = "Selecciona una banda.")]
        public int IdBanda { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Selecciona un promotor.")]
        public int IdPromotor { get; set; }

        [Required(ErrorMessage = "La fecha y hora son obligatorias.")]
        public DateTime? FechaInicio { get; set; }

        [Required(ErrorMessage = "La duración es obligatoria.")]
        [Range(15, 720, ErrorMessage = "La duración debe estar entre 15 y 720 minutos.")]
        public int? Duracion { get; set; } = 90;

        public bool IncluyeLuces { get; set; }

        [Range(0, 1000000, ErrorMessage = "El costo de luces debe ser 0 o mayor.")]
        public decimal? CostoLuces { get; set; } = 0;
    }

    [BindProperty] public InputModel Input { get; set; } = new();
    public List<SelectListItem> Bandas { get; set; } = new();
    public List<SelectListItem> Promotores { get; set; } = new();

    private async Task CargarListasAsync()
    {
        Bandas = await _db.QueryAsync(@"
            SELECT b.id_banda, CONCAT(b.nombre_banda, ' - ', c.nombre_categoria, ' (', FORMAT(c.precio_base, 2), ')')
            FROM Bandas b JOIN Categorias c ON c.id_categoria = b.id_categoria
            WHERE b.activa = TRUE ORDER BY b.nombre_banda",
            r => new SelectListItem(r.GetString(1), r.GetInt32(0).ToString()));
        Promotores = await _db.QueryAsync("SELECT id_promotor, nombre FROM Promotores ORDER BY nombre",
            r => new SelectListItem(r.GetString(1), r.GetInt32(0).ToString()));
    }

    public async Task<IActionResult> OnGetAsync()
    {
        try { await CargarListasAsync(); }
        catch (Exception ex) { ModelState.AddModelError(string.Empty, BaseDatos.Explain(ex)); }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            await CargarListasAsync();
            if (Input.FechaInicio != null && Input.FechaInicio <= DateTime.Now)
                ModelState.AddModelError("Input.FechaInicio", "La fecha solicitada debe ser futura.");
            if (Input.IncluyeLuces && (Input.CostoLuces ?? 0) <= 0)
                ModelState.AddModelError("Input.CostoLuces", "Si incluye luces, indica el costo.");
            if (!ModelState.IsValid) return Page();

            // El cálculo de base, comisión y total lo hace el procedimiento almacenado (una sola vez).
            await using var cn = await _db.OpenAsync();
            await using var cmd = new MySqlCommand("sp_registrar_cotizacion", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("p_id_banda", Input.IdBanda);
            cmd.Parameters.AddWithValue("p_id_promotor", Input.IdPromotor);
            cmd.Parameters.AddWithValue("p_fecha_inicio", Input.FechaInicio);
            cmd.Parameters.AddWithValue("p_duracion_min", Input.Duracion);
            cmd.Parameters.AddWithValue("p_incluye_luces", Input.IncluyeLuces);
            cmd.Parameters.AddWithValue("p_costo_luces", Input.IncluyeLuces ? Input.CostoLuces : 0m);
            var salida = new MySqlParameter("p_id_cotizacion", MySqlDbType.Int32) { Direction = ParameterDirection.Output };
            cmd.Parameters.Add(salida);
            await cmd.ExecuteNonQueryAsync();

            var idNuevo = Convert.ToInt32(salida.Value);
            TempData["Ok"] = $"Cotización #{idNuevo} registrada. Ahora puedes agregar extras y aprobarla.";
            return RedirectToPage("Detalle", new { id = idNuevo });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, BaseDatos.Explain(ex));
            return Page();
        }
    }
}
