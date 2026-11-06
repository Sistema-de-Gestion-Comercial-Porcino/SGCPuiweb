using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ModelBinding;
namespace SGCP.Web.Services;

public class FiltroApi : IAsyncExceptionFilter
{
    public async Task OnExceptionAsync(ExceptionContext c)
    {
        if (c.Exception is not ApiException e)
            return;
        c.ExceptionHandled = true;
        if (e.Estado == 401)
        {
            await c.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            c.Result = new RedirectToActionResult("IniciarSesion",
                "Cuenta",
                new { returnUrl = c.HttpContext.Request.Path.ToString() });
            return;
        }
        if (e.Estado == 403)
        { c.Result = new RedirectToActionResult("SinPermiso", "Cuenta", null); return; }
        c.Result = new ViewResult
        {
            ViewName = "Fallo",
            StatusCode = e.Estado,
            ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(),
                        c.ModelState)
            {
                ["Mensaje"] = e.Message,
                ["Codigo"] = e.Codigo
            }
        };
    }
}
