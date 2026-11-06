using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SGCP.Web.Models.Dtos;
using SGCP.Web.Models.ViewModels;
using SGCP.Web.Services;
namespace SGCP.Web.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
public class CuentaController(AutenticacionApi api) : GestionControllerBase
{
    [HttpGet] public IActionResult IniciarSesion(string? returnUrl) => View(new AccesoViewModel { ReturnUrl = returnUrl });
    [HttpPost]
    public async Task<IActionResult> IniciarSesion(AccesoViewModel m)
    {
        if (!ModelState.IsValid)
            return View(m);
        try
        {
            var r = await api.Post<AccesoDto>("auth/login", new { m.Login, m.Clave });
            List<Claim> claims = [new("sub", r.Usuario.Id.ToString()),
                    new("name", r.Usuario.Nombre),
                    new("login", r.Usuario.Login),
                    new("jwt", r.Token)];
            claims.AddRange(r.Usuario.Roles.Select(x => new Claim("rol", x)));
            claims.AddRange(r.Usuario.Permisos.Select(x => new Claim("permiso", x)));
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme, "name", "rol")),
                    new AuthenticationProperties { ExpiresUtc = r.Expira, IsPersistent = false });
            return !string.IsNullOrEmpty(m.ReturnUrl)
                && Url.IsLocalUrl(m.ReturnUrl) ? LocalRedirect(m.ReturnUrl) : RedirectToAction("Index", "Inicio");
        }
        catch (ApiException e) { ModelState.AddModelError("", e.Message); m.Clave = ""; return View(m); }
    }
    [HttpGet] public IActionResult Registro() => View(new RegistroViewModel());
    [HttpPost]
    public async Task<IActionResult> Registro(RegistroViewModel m)
    {
        if (!ModelState.IsValid)
            return View(m);
        if (!await Intentar(async () => await api.Post<object>("auth/registro-comprador", new
        {
            m.Nombre,
            m.Login,
            m.Correo,
            m.Clave,
            m.Telefono
        })))
            return View(m);
        return View("RegistroListo");
    }
    [HttpGet] public IActionResult Recuperar() => View(new RecuperarViewModel());
    [HttpPost] public async Task<IActionResult> Recuperar(RecuperarViewModel m) { if (!ModelState.IsValid) return View(m); if (!await Intentar(async () => await api.Post<object>("auth/recuperar", new { m.Correo }))) return View(m); Correcto("Si tu cuenta está habilitada, recibirás un enlace válido durante 30 minutos."); return RedirectToAction(nameof(Recuperar)); }
    [HttpGet] public IActionResult Restablecer(string token) => View(new RestablecerViewModel { Token = token });
    [HttpPost] public async Task<IActionResult> Restablecer(RestablecerViewModel m) { if (!ModelState.IsValid) return View(m); if (!await Intentar(async () => await api.Post<object>("auth/restablecer", new { m.Token, m.Clave }))) return View(m); Correcto("Contraseña actualizada. Inicia sesión con tu nueva contraseña."); return RedirectToAction(nameof(IniciarSesion)); }
    [HttpPost, Authorize] public async Task<IActionResult> Salir() { try { await api.Post<object>("auth/cerrar-sesion"); } catch (ApiException) { } await HttpContext.SignOutAsync(); return RedirectToAction(nameof(IniciarSesion)); }
    public IActionResult SinPermiso() { Response.StatusCode = 403; return View(); }
    public IActionResult Error() => View("Fallo");
}
