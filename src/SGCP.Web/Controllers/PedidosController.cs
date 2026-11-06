using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SGCP.Web.Models.Dtos;
using SGCP.Web.Models.ViewModels;
using SGCP.Web.Services;
namespace SGCP.Web.Controllers;

[Authorize(Policy = "pedidos.consultar")]
public class PedidosController(PedidosApi api) : GestionControllerBase
{
    public async Task<IActionResult> Index(string? busqueda,
            string? filtro,
            int pagina = 1,
            int tamanoPagina = 20) => View(new ListaViewModel<PedidoDto>
            {
                Busqueda = busqueda ?? "",
                Filtro = filtro ?? "",
                Datos = await api.Get<Pagina<PedidoDto>>(Query("pedidos",
                            busqueda,
                            "estadoComercial",
                            filtro,
                            pagina,
                            tamanoPagina))
            });
    public async Task<IActionResult> Detalle(Guid id) => View(new FichaViewModel<PedidoDto> { Datos = await api.Get<PedidoDto>("pedidos/" + id) });
    [Authorize(Policy = "pedidos.administrar")]
    public async Task<IActionResult> Confirmar(Guid cotizacionId)
    {
        var c = await api.Get<CotizacionDto>("cotizaciones/" + cotizacionId);
        return View(new ConfirmarPedidoViewModel
        {
            CotizacionId = cotizacionId,
            Codigo = c.Codigo,
            Cliente = c.Cliente,
            Total = c.Total
        });
    }
    [HttpPost, Authorize(Policy = "pedidos.administrar")]
    public async Task<IActionResult> Confirmar(ConfirmarPedidoViewModel m)
    {
        Guid id = Guid.Empty;
        if (ModelState.IsValid
        && await Intentar(async () => id = (await api.Post<CreadoDto>("pedidos", new
        {
            m.CotizacionId,
            FechaComprometida = Hora.Utc(m.FechaComprometida),
            m.CondicionPago
        })).Id))
        { Correcto("Pedido confirmado. Precio y costo históricos conservados."); return RedirectToAction(nameof(Detalle), new { id }); }
        return View(m);
    }
    [Authorize(Policy = "pedidos.administrar")]
    public async Task<IActionResult> Cancelar(Guid id)
    {
        var p = await api.Get<PedidoDto>("pedidos/" + id);
        return View("Confirmacion", new ConfirmacionViewModel
        {
            Id = id,
            Accion = "Cancelar",
            Titulo = "Cancelar pedido",
            Mensaje = p.Codigo + " · " + p.Cliente + ". El pedido y sus importes permanecerán en el historial.",
            MotivoRequerido = true,
            Volver = "/Pedidos/Detalle/" + id
        });
    }
    [HttpPost, Authorize(Policy = "pedidos.administrar")]
    public async Task<IActionResult> Cancelar(ConfirmacionViewModel m)
    {
        if (string.IsNullOrWhiteSpace(m.Motivo))
            ModelState.AddModelError("Motivo",
            "Registra el motivo.");
        if (!ModelState.IsValid
                        || !await Intentar(async () => await api.Post<object>($"pedidos/{m.Id}/cancelar", new { m.Motivo })))
            return View("Confirmacion",
            m);
        Correcto("Pedido cancelado; historial conservado.");
        return RedirectToAction(nameof(Detalle),
            new { id = m.Id });
    }
}
