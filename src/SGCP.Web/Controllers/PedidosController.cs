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
    public async Task<IActionResult> Detalle(Guid id)
    {
        var m = new FichaViewModel<PedidoDto>
        {
            Datos = await api.Get<PedidoDto>("pedidos/" + id),
            // RF-20: los cargos de producto y de servicio se muestran por separado, nunca sumados en una línea.
            Cargos = User.HasClaim("permiso", "cargos.administrar") ? await api.Todos<CargoDto>($"cargos?pedidoId={id}") : []
        };
        if (User.HasClaim("permiso", "fiscal.administrar"))
        {
            try
            { m.Fiscal = await api.Get<ComprobanteFiscalDto>($"pedidos/{id}/registro-fiscal"); }
            catch (ApiException e) when (e.Estado == 404) { }
        }
        return View(m);
    }

    /// <summary>
    /// RF-11 condicional. No hay proveedor fiscal confirmado (Decisiones.md, 11/11/2026): se registra la operación
    /// SIN_PROVEEDOR con una referencia única, sin número fiscal. Repetirlo devuelve el mismo registro.
    /// </summary>
    [HttpPost, Authorize(Policy = "fiscal.administrar")]
    public async Task<IActionResult> RegistroFiscal(Guid id)
    {
        ComprobanteFiscalDto? r = null;
        if (await Intentar(async () => r = await api.Post<ComprobanteFiscalDto>($"pedidos/{id}/registro-fiscal")))
            Correcto($"Operación registrada sin comprobante fiscal. Referencia {r?.ReferenciaUnica}: la venta sigue su curso.");
        else
            TempData["Error"] = string.Join(" ", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage));
        return RedirectToAction(nameof(Detalle), new { id });
    }
    [Authorize(Policy = "cargos.administrar")]
    public async Task<IActionResult> Servicio(Guid id)
    {
        var p = await api.Get<PedidoDto>("pedidos/" + id);
        return View(new ServicioFormViewModel { PedidoId = id, Codigo = p.Codigo + " · " + p.Cliente });
    }
    [HttpPost, Authorize(Policy = "cargos.administrar")]
    public async Task<IActionResult> Servicio(ServicioFormViewModel m)
    {
        if (ModelState.IsValid && await Intentar(async () => await api.Post<CreadoDto>("cargos", new
        {
            m.PedidoId,
            m.Concepto,
            m.Importe,
            m.AcuerdoServicio,
            PrestadoEn = Hora.Utc(m.PrestadoEn)
        })))
        {
            Correcto("Cargo de servicio registrado por separado del cargo de productos.");
            return RedirectToAction(nameof(Detalle), new { id = m.PedidoId });
        }
        return View(m);
    }
    [Authorize(Policy = "pedidos.administrar")]
    public async Task<IActionResult> Confirmar(Guid cotizacionId)
    {
        var c = await api.Get<CotizacionDto>("cotizaciones/" + cotizacionId);
        return View(new ConfirmarPedidoViewModel
        {
            CotizacionId = cotizacionId,
            Codigo = c.Codigo,
            Cliente = c.Cliente,
            Total = c.Total ?? 0
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
    /// <summary>
    /// RF-20 · corrección de un cargo de servicio mal registrado. El cargo no se borra: queda en la tabla como anulado,
    /// con su motivo, y deja de sumar al saldo. La API rechaza anular un cargo con pagos aplicados (CARGO_CON_PAGOS).
    /// </summary>
    [Authorize(Policy = "cargos.administrar")]
    public async Task<IActionResult> AnularCargo(Guid id, Guid pedidoId)
    {
        var cargo = (await api.Todos<CargoDto>($"cargos?pedidoId={pedidoId}")).FirstOrDefault(x => x.Id == id);
        if (cargo is null)
            return NotFound();
        if (cargo.Tipo != "SERVICIO" || cargo.Anulado)
        {
            TempData["Error"] = cargo.Anulado ? "Ese cargo ya está anulado." : "Desde aquí solo se anulan cargos de servicio; el cargo de producto sigue al pedido.";
            return RedirectToAction(nameof(Detalle), new { id = pedidoId });
        }
        return View("Confirmacion", new ConfirmacionViewModel
        {
            Id = id,
            Valor = pedidoId.ToString(),
            Accion = nameof(AnularCargo),
            Titulo = $"Anular cargo: {cargo.Concepto} · Bs {cargo.Importe:N2}",
            Mensaje = "El cargo no se borra: queda en la tabla marcado como anulado, con el motivo, y deja de sumar al saldo del cliente. " +
                "Si ya tiene pagos aplicados no se puede anular.",
            MotivoRequerido = true,
            Volver = "/Pedidos/Detalle/" + pedidoId
        });
    }

    [HttpPost, Authorize(Policy = "cargos.administrar")]
    public async Task<IActionResult> AnularCargo(ConfirmacionViewModel m)
    {
        if (string.IsNullOrWhiteSpace(m.Motivo))
            ModelState.AddModelError(nameof(m.Motivo), "Registra el motivo de la anulación.");
        if (!Guid.TryParse(m.Valor, out var pedidoId))
            return BadRequest();
        if (!ModelState.IsValid || !await Intentar(async () => await api.Post<object>($"cargos/{m.Id}/anular", new { Motivo = m.Motivo.Trim() })))
            return View("Confirmacion", m);
        Correcto("Cargo anulado. Sigue visible en el pedido y ya no suma al saldo del cliente.");
        return RedirectToAction(nameof(Detalle), new { id = pedidoId });
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
