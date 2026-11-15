using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using SGCP.Web.Models;
using SGCP.Web.Services;
var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true).AddEnvironmentVariables();
builder.Services.AddHttpContextAccessor();
builder.Services.AddControllersWithViews(o => { o.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true; o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()); o.Filters.Add<FiltroApi>(); o.ModelBinderProviders.Insert(0, new ProveedorEnlaceDecimalInvariante()); });
void Configurar(HttpClient http) { http.BaseAddress = new Uri(builder.Configuration["Api:UrlBase"] ?? "http://localhost:5081/api/v1/"); http.Timeout = TimeSpan.FromSeconds(20); }
builder.Services.AddHttpClient<ClienteApiBase>(Configurar);
builder.Services.AddHttpClient<AutenticacionApi>(Configurar);
builder.Services.AddHttpClient<UsuariosApi>(Configurar);
builder.Services.AddHttpClient<ClientesApi>(Configurar);
builder.Services.AddHttpClient<CatalogoApi>(Configurar);
builder.Services.AddHttpClient<PreciosApi>(Configurar);
builder.Services.AddHttpClient<CotizacionesApi>(Configurar);
builder.Services.AddHttpClient<PedidosApi>(Configurar);
builder.Services.AddHttpClient<ProduccionApi>(Configurar);
builder.Services.AddHttpClient<SanidadApi>(Configurar);
builder.Services.AddHttpClient<InventarioApi>(Configurar);
builder.Services.AddHttpClient<FrioApi>(Configurar);
builder.Services.AddHttpClient<CobranzaApi>(Configurar);
builder.Services.AddHttpClient<RepartoApi>(Configurar);
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o => { o.LoginPath = "/Cuenta/IniciarSesion"; o.AccessDeniedPath = "/Cuenta/SinPermiso"; o.ExpireTimeSpan = TimeSpan.FromHours(8); o.SlidingExpiration = false; o.Cookie.Name = "SGCP.Sesion"; o.Cookie.HttpOnly = true; o.Cookie.SameSite = SameSiteMode.Lax; o.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always; });
builder.Services.AddAuthorization(o =>
{
    foreach (var permiso in new[] { "usuarios.consultar",
        "usuarios.administrar",
        "clientes.consultar",
        "clientes.administrar",
        "catalogo.consultar",
        "catalogo.administrar",
        "costos.consultar",
        "costos.administrar",
        "cotizaciones.consultar",
        "cotizaciones.administrar",
        "cotizaciones.responder",
        "pedidos.consultar",
        "pedidos.administrar",
        "operacion.consultar",
        "precios.consultar",
        "produccion.consultar",
        "produccion.administrar",
        "inventario.consultar",
        "inventario.administrar",
        "frio.administrar",
        "cargos.administrar",
        "saldos.consultar",
        "pagos.administrar",
        "entregas.administrar",
        "entregas.revisar" })
        o.AddPolicy(permiso, p => p.RequireAuthenticatedUser().RequireClaim("permiso",
        permiso));
});
builder.Services.Configure<RequestLocalizationOptions>(o => { var boliviano = new[] { new CultureInfo("es-BO") }; o.DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(boliviano[0]); o.SupportedCultures = boliviano; o.SupportedUICultures = boliviano; o.RequestCultureProviders.Clear(); });
var app = builder.Build();
if (!app.Environment.IsDevelopment()) { app.UseExceptionHandler("/Cuenta/Error"); app.UseHsts(); app.UseHttpsRedirection(); }
app.Use(async (c, next) => { c.Response.Headers["X-Content-Type-Options"] = "nosniff"; c.Response.Headers["X-Frame-Options"] = "DENY"; c.Response.Headers["Referrer-Policy"] = "same-origin"; await next(); });
app.UseRequestLocalization();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute("default", "{controller=Inicio}/{action=Index}/{id?}");
app.Run();
public partial class Program { }
