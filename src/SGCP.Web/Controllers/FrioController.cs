using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SGCP.Web.Models.Dtos;
using SGCP.Web.Models.ViewModels;
using SGCP.Web.Services;
namespace SGCP.Web.Controllers;

/// <summary>
/// RF-17 · registro manual del control de frío, sobre un lote en cámara o sobre una entrega en transporte.
/// El sistema no mide: registra lo que el responsable leyó y evaluó. La API decide quién controla qué:
/// los lotes piden inventario.administrar y las entregas, que la entrega sea del usuario (o gestión global).
/// </summary>
[Authorize(Policy = "frio.administrar")]
public class FrioController(FrioApi api) : GestionControllerBase
{
    private bool PuedeLotes => User.HasClaim("permiso", "inventario.administrar");
    private bool PuedeEntregas => User.HasClaim("permiso", "entregas.administrar");

    public async Task<IActionResult> Index(int pagina = 1, int tamanoPagina = 20)
    {
        var entregas = PuedeEntregas ? await Entregas() : [];
        return View(new FrioListaViewModel
        {
            Datos = await api.Get<Pagina<ControlFrioDto>>($"controles-frio?pagina={pagina}&tamanoPagina={tamanoPagina}"),
            Lotes = User.HasClaim("permiso", "inventario.consultar") ? (await api.Todos<LoteDto>("lotes")).ToDictionary(x => x.Id) : [],
            Entregas = entregas.ToDictionary(x => x.Entrega.Id, x => x.Texto),
            Productos = await Nombres.Productos(api),
            Nombres = await Nombres.Usuarios(api, User)
        });
    }

    public async Task<IActionResult> Registrar(Guid? loteId, Guid? entregaId) =>
        View(await Completar(new FrioFormViewModel
        {
            LoteId = loteId,
            EntregaId = entregaId,
            // Quien no controla lotes (ENTREGA) empieza directamente en «entrega en transporte».
            Objetivo = entregaId is not null || !PuedeLotes ? FrioFormViewModel.ObjetivoEntrega : FrioFormViewModel.ObjetivoLote
        }));

    [HttpPost]
    public async Task<IActionResult> Registrar(FrioFormViewModel m)
    {
        if (m.Objetivo == FrioFormViewModel.ObjetivoLote && !PuedeLotes)
            ModelState.AddModelError(nameof(m.Objetivo), "Tu perfil registra frío sobre entregas, no sobre lotes en cámara.");
        if (m.Objetivo == FrioFormViewModel.ObjetivoEntrega && !PuedeEntregas)
            ModelState.AddModelError(nameof(m.Objetivo), "Tu perfil registra frío sobre lotes en cámara, no sobre entregas.");
        var esEntrega = m.Objetivo == FrioFormViewModel.ObjetivoEntrega;
        if (ModelState.IsValid && await Intentar(async () => await api.Post<CreadoDto>("controles-frio", new
        {
            // Se manda uno u otro, nunca los dos: la API lo exige (REFERENCIA_FRIO_INVALIDA).
            LoteId = esEntrega ? null : m.LoteId,
            EntregaId = esEntrega ? m.EntregaId : null,
            m.Temperatura,
            m.Evaluacion,
            m.Ubicacion,
            Fecha = Hora.Utc(m.Fecha),
            m.Observacion
        })))
        {
            Correcto(esEntrega && m.Evaluacion == "RECHAZADO"
                ? "Control registrado. Al ser rechazado, los lotes reservados para ese pedido quedaron bloqueados."
                : "Control de frío registrado con su responsable y evaluación.");
            return RedirectToAction(nameof(Index));
        }
        return View(await Completar(m));
    }

    private async Task<FrioFormViewModel> Completar(FrioFormViewModel m)
    {
        m.PuedeLotes = PuedeLotes;
        m.PuedeEntregas = PuedeEntregas;
        // Sin inventario.administrar no se pide GET lotes: la API respondería 403 y la pantalla entera fallaría.
        m.Lotes = PuedeLotes ? (await api.Todos<LoteDto>("lotes")).OrderBy(x => x.Codigo).ToList() : [];
        m.Productos = await Nombres.Productos(api);
        m.Entregas = PuedeEntregas
            ? (await Entregas()).Where(x => x.Entrega.Estado == "DESPACHADA").OrderBy(x => x.Entrega.FechaPrevista)
                .Select(x => new KeyValuePair<Guid, string>(x.Entrega.Id, x.Texto)).ToList()
            : [];
        return m;
    }

    /// <summary>
    /// Entregas visibles para el usuario (la API ya filtra: gestión global ve todas; el responsable, las suyas) con un texto legible.
    /// El código del pedido sale de GET pedidos si hay permiso; si no, de GET mis-entregas, que trae pedido y cliente del responsable.
    /// </summary>
    private async Task<List<(EntregaDto Entrega, string Texto)>> Entregas()
    {
        var entregas = await api.Todos<EntregaDto>("entregas");
        var textos = new Dictionary<Guid, string>();
        if (User.HasClaim("permiso", "pedidos.consultar"))
        {
            var pedidos = (await api.Todos<PedidoDto>("pedidos")).ToDictionary(x => x.Id);
            foreach (var e in entregas)
                if (pedidos.TryGetValue(e.PedidoId, out var p))
                    textos[e.Id] = $"{p.Codigo} · {p.Cliente}";
        }
        else
        {
            foreach (var a in (await api.Get<MisEntregasDto>("mis-entregas")).Entregas)
                textos[a.Id] = $"{a.Pedido} · {a.Cliente}";
        }
        return entregas.Select(e => (e, textos.GetValueOrDefault(e.Id) ?? $"Entrega prevista {Hora.Mostrar(e.FechaPrevista)}")).ToList();
    }
}
