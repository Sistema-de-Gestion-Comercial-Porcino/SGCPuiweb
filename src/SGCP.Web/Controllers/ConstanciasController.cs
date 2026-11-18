using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SGCP.Web.Services;

namespace SGCP.Web.Controllers;

/// <summary>
/// RF-14 · constancia de entrega para Ventas y el comprador, que no administran entregas.
/// No decide quién la ve: la API exige saldos.consultar o ser el comprador del pedido, y solo la emite
/// con recepción conforme confirmada en el servidor.
/// </summary>
[Authorize]
public class ConstanciasController(ReportesApi api) : GestionControllerBase
{
    /// <summary>Desde la ficha del pedido: la API toma la entrega ENTREGADA más reciente.</summary>
    public Task<IActionResult> Pedido(Guid id) => Bajar($"pedidos/{id}/constancia");

    /// <summary>Desde el reporte de entregas, que trae el id de cada entrega.</summary>
    public Task<IActionResult> Entrega(Guid id) => Bajar($"entregas/{id}/constancia");

    private async Task<IActionResult> Bajar(string ruta)
    {
        try
        {
            var archivo = await api.Descargar(ruta, "Constancia.pdf");
            Response.Headers.CacheControl = "no-store";
            return File(archivo.Contenido, "application/pdf", archivo.Nombre);
        }
        catch (ApiException e) when (e.Estado is not (401 or 403))
        {
            TempData["Error"] = e.Codigo switch
            {
                "RECEPCION_PENDIENTE" => "Todavía no hay constancia: la entrega no tiene una recepción confirmada en el servidor.",
                "RECEPCION_OBSERVADA" => "Esta entrega no tiene constancia definitiva: la recepción se registró con observaciones.",
                _ => e.Message
            };
            var volver = Request.Headers.Referer.ToString();
            return Uri.TryCreate(volver, UriKind.Absolute, out var u) && Url.IsLocalUrl(u.PathAndQuery) && u.Host == Request.Host.Host
                ? LocalRedirect(u.PathAndQuery)
                : Redirect("/");
        }
    }
}
