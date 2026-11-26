using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SGCP.Web.Models.Dtos;
using SGCP.Web.Models.ViewModels;
using SGCP.Web.Services;

namespace SGCP.Web.Controllers;

/// <summary>Gestiones de cobro. La API conserva las reglas de saldo y auditoría.</summary>
[Authorize(Policy = "saldos.consultar")]
public class SeguimientosController(CobranzaApi api) : GestionControllerBase
{
    public async Task<IActionResult> Index(Guid? clienteId, string? estado, int pagina = 1)
    {
        if (!string.IsNullOrEmpty(estado) && estado is not ("PENDIENTE" or "REALIZADO" or "CERRADO"))
            return BadRequest("Estado de seguimiento inválido.");
        return View(new SeguimientosViewModel
        {
            ClienteId = clienteId, Estado = estado,
            Clientes = (await api.Todos<ClienteDto>("clientes")).OrderBy(x => x.Nombre).ToList(),
            Datos = await api.Get<Pagina<SeguimientoCobroDto>>(Query("seguimientos-cobro", null, "estado", estado,
                pagina, 20, "clienteId", clienteId?.ToString()))
        });
    }

    [Authorize(Policy = "pagos.administrar")]
    public async Task<IActionResult> Crear(Guid pedidoId, Guid condicionPagoId)
    {
        var m = await Completar(new NuevoSeguimientoViewModel { PedidoId = pedidoId, CondicionPagoId = condicionPagoId });
        return m is null ? NotFound() : View(m);
    }

    [HttpPost, Authorize(Policy = "pagos.administrar")]
    public async Task<IActionResult> Crear(NuevoSeguimientoViewModel m)
    {
        var completo = await Completar(m);
        if (completo is null) return NotFound();
        if (m.Condicion!.Modalidad != "CREDITO_ACORDADO")
            ModelState.AddModelError("", "Solo se abre un seguimiento para una condición a crédito.");
        if (ModelState.IsValid && await Intentar(async () => await api.Post<CreadoDto>(
            $"condiciones-pago/{m.CondicionPagoId}/seguimientos", new { ProximaFecha = Hora.Utc(m.ProximaFecha!.Value) })))
        {
            Correcto("Seguimiento creado. El saldo y la entrega conservan su estado.");
            return RedirectToAction(nameof(Index), new { clienteId = m.Pedido!.ClienteId, estado = "PENDIENTE" });
        }
        return View(m);
    }

    [Authorize(Policy = "pagos.administrar")]
    public async Task<IActionResult> Gestion(Guid id, Guid clienteId)
    {
        var m = await Completar(new GestionCobroViewModel { Id = id, ClienteId = clienteId });
        return m is null ? NotFound() : View(m);
    }

    [HttpPost, Authorize(Policy = "pagos.administrar")]
    public async Task<IActionResult> Gestion(GestionCobroViewModel m)
    {
        if (await Completar(m) is null) return NotFound();
        if (m.Saldo > 0 && m.ProximaFecha is null)
            ModelState.AddModelError(nameof(m.ProximaFecha), "Con saldo pendiente indica la próxima fecha de gestión.");
        if (ModelState.IsValid && await Intentar(async () => await api.Post<object>($"seguimientos-cobro/{m.Id}/gestion",
            new { Resultado = m.Resultado.Trim(), ProximaFecha = m.ProximaFecha is DateTime fecha ? Hora.Utc(fecha) : (DateTime?)null })))
        {
            Correcto("Gestión registrada y conservada en auditoría. Una promesa de pago no cancela la deuda.");
            return RedirectToAction(nameof(Index), new { clienteId = m.ClienteId });
        }
        return View(m);
    }

    [HttpPost, Authorize(Policy = "pagos.administrar")]
    public async Task<IActionResult> Cerrar(Guid id, Guid clienteId)
    {
        if (await Buscar(id, clienteId) is null) return NotFound();
        var error = await IntentarConCodigo(async () => await api.Post<object>($"seguimientos-cobro/{id}/cerrar"));
        if (error is null) Correcto("Seguimiento cerrado. El historial se conserva.");
        else TempData["Error"] = error.Message;
        return RedirectToAction(nameof(Index), new { clienteId });
    }

    private async Task<NuevoSeguimientoViewModel?> Completar(NuevoSeguimientoViewModel m)
    {
        m.Pedido = await api.Get<PedidoDto>($"pedidos/{m.PedidoId}");
        m.Condicion = (await api.Get<List<CondicionPagoDto>>($"pedidos/{m.PedidoId}/condiciones-pago"))
            .SingleOrDefault(x => x.Id == m.CondicionPagoId);
        return m.Condicion is null ? null : m;
    }

    private async Task<SeguimientoCobroDto?> Buscar(Guid id, Guid clienteId) =>
        (await api.Todos<SeguimientoCobroDto>($"seguimientos-cobro?clienteId={clienteId}")).SingleOrDefault(x => x.Id == id);

    private async Task<GestionCobroViewModel?> Completar(GestionCobroViewModel m)
    {
        m.Seguimiento = await Buscar(m.Id, m.ClienteId);
        if (m.Seguimiento is null) return null;
        m.Saldo = (await api.Get<SaldosDto>($"saldos?pedidoId={m.Seguimiento.PedidoId}")).Total;
        return m;
    }
}
