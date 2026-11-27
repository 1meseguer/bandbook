using System.Globalization;
using BandFlow.Data;

var mx = new CultureInfo("es-MX");
CultureInfo.DefaultThreadCurrentCulture = mx;
CultureInfo.DefaultThreadCurrentUICulture = mx;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
// Se conecta usando user-secrets
builder.Services.AddSingleton<BaseDatos>();

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
