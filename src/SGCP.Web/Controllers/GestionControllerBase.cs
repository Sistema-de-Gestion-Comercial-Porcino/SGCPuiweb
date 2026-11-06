using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SGCP.Web.Models.Dtos;
using SGCP.Web.Models.ViewModels;
using SGCP.Web.Services;
namespace SGCP.Web.Controllers;

public abstract class GestionControllerBase : Controller
{
    protected async Task<bool> Intentar(Func<Task> accion)
    {
        try
        { await accion(); return true; }
        catch (ApiException e) when (e.Estado != 401
        && e.Estado != 403)
        { ModelState.AddModelError("", e.Message); return false; }
    }
    protected void Correcto(string texto) => TempData["Exito"] = texto;
    protected string Query(string ruta,
            string? busqueda,
            string? nombreFiltro,
            string? filtro,
            int pagina,
            int tamano,
            string? extraNombre = null,
            string? extra = null) => Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString(ruta,
            new Dictionary<string,
            string?> { { "busqueda", busqueda },
                    { nombreFiltro ?? "filtro", filtro },
                    { "pagina", pagina.ToString() },
                    { "tamanoPagina", tamano.ToString() },
                    { extraNombre ?? "extra", extra } });
    protected static T Convertir<T>(object valor) => System.Text.Json.JsonSerializer.Deserialize<T>(System.Text.Json.JsonSerializer.Serialize(valor))!;
}
