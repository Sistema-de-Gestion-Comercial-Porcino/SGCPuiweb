using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SGCP.Web.Models.Dtos;
using SGCP.Web.Models.ViewModels;
using SGCP.Web.Services;
namespace SGCP.Web.Controllers;

/// <summary>
/// RF-05, RF-06, RF-19 · condición de pago, pagos, saldos y señas.
/// Esta pantalla muestra solo el estado de la deuda. El estado de la entrega vive en /Entregas:
/// que una entrega esté entregada no dice nada del saldo, y pagar no marca una entrega.
/// </summary>
[Authorize(Policy = "saldos.consultar")]
public class CobranzaController(CobranzaApi api) : GestionControllerBase
{
    public async Task<IActionResult> Index(Guid? clienteId)
    {
        var m = new SaldosViewModel
        {
            Clientes = (await api.Todos<ClienteDto>("clientes")).OrderBy(x => x.Nombre).ToList(),
            ClienteId = clienteId,
            PuedeRegistrar = User.HasClaim("permiso", "pagos.administrar")
        };
        if (clienteId is not null)
        {
            m.Cliente = m.Clientes.FirstOrDefault(x => x.Id == clienteId);
            m.Saldos = await api.Get<SaldosDto>($"saldos?clienteId={clienteId}");
            m.Pedidos = await PedidosDe(clienteId.Value);
            if (m.PuedeRegistrar)
                m.Pagos = await api.Todos<PagoDto>($"pagos?clienteId={clienteId}");
        }
        return View(m);
    }

    // ---------- Pagos ----------

    [Authorize(Policy = "pagos.administrar")]
    public async Task<IActionResult> Pago(Guid clienteId) => View(await CompletarPago(new PagoFormViewModel { ClienteId = clienteId }));

    [HttpPost, Authorize(Policy = "pagos.administrar")]
    public async Task<IActionResult> Pago(PagoFormViewModel m)
    {
        var aplicaciones = m.Aplicaciones.Where(x => x.Importe is > 0).Select(x => new { x.CargoId, Importe = x.Importe!.Value }).ToList();
        if (aplicaciones.Count == 0)
            ModelState.AddModelError("", "Indica cuánto se aplica a al menos un cargo.");
        if (ModelState.IsValid && await Intentar(async () => await api.Post<CreadoDto>("pagos", new
        {
            m.ClienteId,
            ReferenciaUnica = m.ReferenciaUnica.Trim(),
            Importe = aplicaciones.Sum(x => x.Importe),
            Fecha = Hora.Utc(m.Fecha),
            Medio = m.Medio.Trim(),
            Aplicaciones = aplicaciones
        })))
        {
            Correcto($"Pago de Bs {aplicaciones.Sum(x => x.Importe):N2} registrado y aplicado. El estado de las entregas no cambia.");
            return RedirectToAction(nameof(Index), new { clienteId = m.ClienteId });
        }
        var importes = m.Aplicaciones.ToDictionary(x => x.CargoId, x => x.Importe);
        var completo = await CompletarPago(m);
        foreach (var fila in completo.Aplicaciones)
            fila.Importe = importes.GetValueOrDefault(fila.CargoId);
        return View(completo);
    }

    [Authorize(Policy = "pagos.administrar")]
    public IActionResult Anular(Guid id, Guid clienteId) => View("Motivo", new MotivoFormViewModel
    {
        Id = id,
        Titulo = "Anular pago",
        Explicacion = "El pago no se borra: queda anulado con su motivo y deja de restar del saldo.",
        Volver = $"/Cobranza?clienteId={clienteId}"
    });

    [HttpPost, Authorize(Policy = "pagos.administrar")]
    public async Task<IActionResult> Anular(MotivoFormViewModel m)
    {
        if (ModelState.IsValid && await Intentar(async () => await api.Post<object>($"pagos/{m.Id}/anular", new { Motivo = m.Motivo.Trim() })))
        {
            Correcto("Pago anulado. Se conserva en el historial con su motivo.");
            return LocalRedirect(m.Volver);
        }
        return View("Motivo", m);
    }

    // ---------- Condición de pago acordada (RF-05) ----------

    [Authorize(Policy = "pagos.administrar")]
    public async Task<IActionResult> Condicion(Guid pedidoId) => View(await CompletarCondicion(new CondicionFormViewModel { PedidoId = pedidoId }));

    [HttpPost, Authorize(Policy = "pagos.administrar")]
    public async Task<IActionResult> Condicion(CondicionFormViewModel m)
    {
        var credito = m.Modalidad == "CREDITO_ACORDADO";
        if (credito && m.Vencimiento is null)
            ModelState.AddModelError(nameof(m.Vencimiento), "El crédito necesita un vencimiento acordado: no hay plazo por defecto.");
        if (credito && m.ImporteAutorizado <= 0)
            ModelState.AddModelError(nameof(m.ImporteAutorizado), "Indica el importe de crédito autorizado.");
        if (ModelState.IsValid && await Intentar(async () => await api.Post<CreadoDto>($"pedidos/{m.PedidoId}/condiciones-pago", new
        {
            m.Modalidad,
            ImporteAutorizado = credito ? m.ImporteAutorizado : 0m,
            Vencimiento = credito ? Hora.Utc(m.Vencimiento!.Value) : (DateTime?)null
        })))
        {
            Correcto(credito ? "Crédito acordado. Autorizar crédito no cobra ni liquida la deuda." : "Condición de contado registrada.");
            return RedirectToAction(nameof(Condicion), new { pedidoId = m.PedidoId });
        }
        return View(await CompletarCondicion(m));
    }

    // ---------- Señas (RF-19) ----------

    [Authorize(Policy = "pagos.administrar")]
    public async Task<IActionResult> Senas(Guid pedidoId) => View(await CompletarSenas(pedidoId, new SenaFormViewModel { PedidoId = pedidoId }));

    [HttpPost, Authorize(Policy = "pagos.administrar")]
    public async Task<IActionResult> Senas([Bind(Prefix = "Nueva")] SenaFormViewModel m)
    {
        if (ModelState.IsValid && await Intentar(async () => await api.Post<CreadoDto>("senas", new
        {
            m.PedidoId,
            ReferenciaUnica = m.ReferenciaUnica.Trim(),
            m.Importe,
            Fecha = Hora.Utc(m.Fecha),
            Medio = m.Medio.Trim()
        })))
        {
            Correcto("Seña registrada con dinero recibido. Se aplica una sola vez a un cargo del pedido.");
            return RedirectToAction(nameof(Senas), new { pedidoId = m.PedidoId });
        }
        return View(await CompletarSenas(m.PedidoId, m));
    }

    [HttpPost, Authorize(Policy = "pagos.administrar")]
    public async Task<IActionResult> AplicarSena(Guid id, Guid pedidoId, Guid cargoId)
    {
        var error = await IntentarConCodigo(async () => await api.Post<object>($"senas/{id}/aplicar", new { CargoId = cargoId }));
        if (error is null)
            Correcto("Seña aplicada al cargo. No se puede aplicar otra vez.");
        else
            TempData["Error"] = error.Codigo == "SENA_YA_RESUELTA" ? "Esta seña ya se aplicó o tiene un acuerdo de resolución: no se aplica dos veces." : error.Message;
        return RedirectToAction(nameof(Senas), new { pedidoId });
    }

    [Authorize(Policy = "pagos.administrar")]
    public IActionResult Resolver(Guid id, Guid pedidoId) => View("Motivo", new MotivoFormViewModel
    {
        Id = id,
        Titulo = "Registrar acuerdo de resolución",
        Explicacion = "Si la seña se cancela, se anota lo que se acordó con el cliente. El sistema no devuelve ni retiene dinero por su cuenta.",
        Volver = $"/Cobranza/Senas?pedidoId={pedidoId}"
    });

    [HttpPost, Authorize(Policy = "pagos.administrar")]
    public async Task<IActionResult> Resolver(MotivoFormViewModel m)
    {
        if (ModelState.IsValid && await Intentar(async () => await api.Post<object>($"senas/{m.Id}/resolver", new { Motivo = m.Motivo.Trim() })))
        {
            Correcto("Acuerdo de resolución registrado.");
            return LocalRedirect(m.Volver);
        }
        return View("Motivo", m);
    }

    // ---------- Auxiliares ----------

    private async Task<Dictionary<Guid, PedidoDto>> PedidosDe(Guid clienteId) =>
        (await api.Todos<PedidoDto>("pedidos")).Where(x => x.ClienteId == clienteId).ToDictionary(x => x.Id);

    private async Task<PagoFormViewModel> CompletarPago(PagoFormViewModel m)
    {
        var clientes = await api.Todos<ClienteDto>("clientes");
        m.Cliente = clientes.FirstOrDefault(x => x.Id == m.ClienteId)?.Nombre ?? "Cliente";
        var pedidos = await PedidosDe(m.ClienteId);
        var saldos = await api.Get<SaldosDto>($"saldos?clienteId={m.ClienteId}");
        m.Aplicaciones = saldos.Cargos.Where(x => x.Saldo > 0).Select(x => new AplicacionFila
        {
            CargoId = x.CargoId,
            Pedido = pedidos.TryGetValue(x.PedidoId, out var p) ? p.Codigo : "Pedido",
            Concepto = x.Concepto,
            Tipo = x.Tipo,
            Saldo = x.Saldo
        }).ToList();
        return m;
    }

    private async Task<CondicionFormViewModel> CompletarCondicion(CondicionFormViewModel m)
    {
        m.Pedido = await api.Get<PedidoDto>($"pedidos/{m.PedidoId}");
        m.Historial = await api.Get<List<CondicionPagoDto>>($"pedidos/{m.PedidoId}/condiciones-pago");
        return m;
    }

    private async Task<SenasViewModel> CompletarSenas(Guid pedidoId, SenaFormViewModel nueva)
    {
        var pedido = await api.Get<PedidoDto>($"pedidos/{pedidoId}");
        return new SenasViewModel
        {
            Pedido = pedido,
            Senas = await api.Todos<SenaDto>($"senas?pedidoId={pedidoId}"),
            Pagos = (await api.Todos<PagoDto>($"pagos?clienteId={pedido.ClienteId}")).ToDictionary(x => x.Id),
            Cargos = (await api.Get<SaldosDto>($"saldos?pedidoId={pedidoId}")).Cargos,
            Nueva = nueva
        };
    }
}
