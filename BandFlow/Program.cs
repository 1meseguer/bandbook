using System.Globalization;
using BandFlow.Data;

// Cultura es-MX: punto decimal y moneda en pesos
var mx = new CultureInfo("es-MX");
CultureInfo.DefaultThreadCurrentCulture = mx;
CultureInfo.DefaultThreadCurrentUICulture = mx;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
// La cadena de conexión viene de user-secrets (nunca del cliente ni del repositorio)
builder.Services.AddSingleton<Db>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();

app.Run();
