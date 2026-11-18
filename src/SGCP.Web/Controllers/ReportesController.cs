using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SGCP.Web.Models.Dtos;
using SGCP.Web.Models.ViewModels;
using SGCP.Web.Services;
namespace SGCP.Web.Controllers;

/// <summary>
/// RF-15 · reportes por perfil, de solo lectura. El menú de reportes sale de `GET reportes`: cada perfil ve solo
/// los que le corresponden. La descarga usa el mismo endpoint autorizado y los mismos filtros que la consulta.
/// El comprador entra por aquí mismo y la API lo limita a sus datos, sin costos ni márgenes.
/// </summary>
[Authorize(Policy = "reportes.consultar")]
public class ReportesController(ReportesApi api) : GestionControllerBase
{
    public async Task<IActionResult> Index(FiltroReporteViewModel filtro)
    {
        var comprador = User.IsInRole("COMPRADOR");
        var m = new ReportesViewModel
        {
            Disponibles = await api.Get<List<string>>("reportes"),
            Filtro = filtro,
            EsComprador = comprador
        };
        if (User.HasClaim("permiso", "catalogo.consultar"))
        {
            m.Productos = (await api.Todos<ProductoDto>("productos")).OrderBy(x => x.Nombre).ToList();
            m.Clasificaciones = (await api.Todos<ClasificacionDto>("clasificaciones")).OrderBy(x => x.Nombre).ToList();
        }
        // El comprador no elige cliente: la API siempre lo limita al suyo.
        if (!comprador && User.HasClaim("permiso", "clientes.consultar"))
            m.Clientes = (await api.Todos<ClienteDto>("clientes")).OrderBy(x => x.Nombre).ToList();
        if (filtro.Tipo is not null)
        {
            if (!m.Disponibles.Contains(filtro.Tipo))
                return Forbid();
            var error = await IntentarConCodigo(async () => m.Tabla = await api.Get<TablaReporteDto>($"reportes/{filtro.Tipo}{filtro.Consulta()}"));
            m.Error = error?.Message;
        }
        return View(m);
    }

    /// <summary>PDF o Excel con exactamente los filtros de la pantalla: mismo endpoint autorizado, sin camino alterno.</summary>
    public async Task<IActionResult> Exportar(string formato, FiltroReporteViewModel filtro)
    {
        if (filtro.Tipo is null || formato is not ("pdf" or "xlsx"))
            return BadRequest();
        try
        {
            var archivo = await api.Descargar($"reportes/{filtro.Tipo}/exportar/{formato}{filtro.Consulta()}", $"SGCP_{filtro.Tipo}.{formato}");
            Response.Headers.CacheControl = "no-store";
            return File(archivo.Contenido, archivo.Tipo, archivo.Nombre);
        }
        catch (ApiException e) when (e.Estado is not (401 or 403))
        {
            TempData["Error"] = e.Message;
            return Redirect("/Reportes" + filtro.Pantalla());
        }
    }
}
