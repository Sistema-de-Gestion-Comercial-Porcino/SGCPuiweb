using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SGCP.Web.Models.Dtos;
using SGCP.Web.Models.ViewModels;
using SGCP.Web.Services;
namespace SGCP.Web.Controllers;

/// <summary>RF-04 · órdenes de faeneo y fabricación, consumos y resultados por lote.</summary>
[Authorize(Policy = "produccion.consultar")]
public class ProduccionController(ProduccionApi api) : GestionControllerBase
{
    public async Task<IActionResult> Index(int pagina = 1, int tamanoPagina = 20) =>
        View(new OrdenListaViewModel
        {
            Datos = await api.Get<Pagina<OrdenProduccionDto>>($"ordenes-produccion?pagina={pagina}&tamanoPagina={tamanoPagina}"),
            Animales = (await api.Todos<AnimalDto>("animales")).ToDictionary(x => x.Id, x => x.Identificacion),
            Nombres = await Nombres.Usuarios(api, User)
        });

    public async Task<IActionResult> Detalle(Guid id)
    {
        var detalle = await api.Get<OrdenDetalleDto>("ordenes-produccion/" + id);
        List<ExistenciaDto> existencias = detalle.Insumos.Count == 0 ? [] : await api.Todos<ExistenciaDto>("existencias");
        return View(new OrdenFichaViewModel
        {
            Detalle = detalle,
            Animal = detalle.Orden.AnimalId is null ? null
                : (await api.Todos<AnimalDto>("animales")).FirstOrDefault(x => x.Id == detalle.Orden.AnimalId),
            Productos = await Nombres.Productos(api),
            Existencias = existencias.ToDictionary(x => x.Id),
            Nombres = await Nombres.Usuarios(api, User)
        });
    }

    [Authorize(Policy = "produccion.administrar")]
    public async Task<IActionResult> Nueva() => View(await Completar(new OrdenFormViewModel()));

    [HttpPost, Authorize(Policy = "produccion.administrar")]
    public async Task<IActionResult> Nueva(OrdenFormViewModel m)
    {
        m.Previsiones = m.Previsiones.Where(x => x.ProductoId is not null).ToList();
        if (m.Tipo == "FAENEO" && m.AnimalId is null)
            ModelState.AddModelError(nameof(m.AnimalId), "Un faeneo requiere seleccionar el animal.");
        if (m.FinPrevisto <= m.InicioPrevisto)
            ModelState.AddModelError(nameof(m.FinPrevisto), "El fin previsto debe ser posterior al inicio.");
        if (!ModelState.IsValid)
            return View(await Completar(m));
        Guid id = Guid.Empty;
        var error = await IntentarConCodigo(async () => id = (await api.Post<CreadoDto>("ordenes-produccion", new
        {
            m.Tipo,
            AnimalId = m.Tipo == "FAENEO" ? m.AnimalId : null,
            InicioPrevisto = Hora.Utc(m.InicioPrevisto),
            FinPrevisto = Hora.Utc(m.FinPrevisto),
            m.Observacion,
            Previsiones = m.Previsiones.Select(x => new { x.ProductoId, x.CantidadEstimada, x.Unidad })
        })).Id);
        if (error is null)
        {
            Correcto("Orden planificada. Las cantidades previstas no suman disponibilidad.");
            return RedirectToAction(nameof(Detalle), new { id });
        }
        ModelState.AddModelError("", error.Codigo == "ANIMAL_RECHAZADO" && m.AnimalId is not null
            ? await MotivoRechazo(m.AnimalId.Value)
            : error.Message);
        return View(await Completar(m));
    }

    [HttpPost, Authorize(Policy = "produccion.administrar")]
    public async Task<IActionResult> Iniciar(Guid id)
    {
        if (await Intentar(async () => await api.Post<object>($"ordenes-produccion/{id}/iniciar")))
            Correcto("Orden en proceso. Se registró el inicio real.");
        else
            TempData["Error"] = string.Join(" ", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage));
        return RedirectToAction(nameof(Detalle), new { id });
    }

    [Authorize(Policy = "produccion.administrar")]
    public async Task<IActionResult> Consumo(Guid id)
    {
        var d = await api.Get<OrdenDetalleDto>("ordenes-produccion/" + id);
        return View(await Completar(new ConsumoFormViewModel { OrdenId = id, Codigo = d.Orden.Codigo }));
    }

    [HttpPost, Authorize(Policy = "produccion.administrar")]
    public async Task<IActionResult> Consumo(ConsumoFormViewModel m)
    {
        if (ModelState.IsValid && await Intentar(async () => await api.Post<CreadoDto>($"ordenes-produccion/{m.OrdenId}/consumos", new
        {
            m.ExistenciaId,
            m.Cantidad,
            m.ReferenciaUnica
        })))
        {
            Correcto("Consumo registrado. El insumo se descontó de su existencia.");
            return RedirectToAction(nameof(Detalle), new { id = m.OrdenId });
        }
        return View(await Completar(m));
    }

    [Authorize(Policy = "produccion.administrar")]
    public async Task<IActionResult> Confirmar(Guid id)
    {
        var d = await api.Get<OrdenDetalleDto>("ordenes-produccion/" + id);
        var m = new ConfirmarProduccionViewModel { OrdenId = id, Codigo = d.Orden.Codigo, Tipo = d.Orden.Tipo };
        // Se precargan las previsiones como punto de partida: el responsable ajusta lo que realmente salió.
        if (d.Previsiones.Count > 0)
            m.Resultados = d.Previsiones.Select(x => new ResultadoFormViewModel { ProductoId = x.ProductoId }).ToList();
        m.Productos = await Activos();
        return View(m);
    }

    [HttpPost, Authorize(Policy = "produccion.administrar")]
    public async Task<IActionResult> Confirmar(ConfirmarProduccionViewModel m)
    {
        m.Resultados = m.Resultados.Where(x => x.ProductoId is not null || !string.IsNullOrWhiteSpace(x.CodigoLote)).ToList();
        if (m.Resultados.Count == 0)
            ModelState.AddModelError("", "Registra al menos un producto resultante.");
        if (m.Resultados.Any(x => x.ProductoId is null))
            ModelState.AddModelError("", "Cada fila necesita su producto.");
        if (m.Resultados.GroupBy(x => x.CodigoLote.Trim().ToUpperInvariant()).Any(g => g.Count() > 1))
            ModelState.AddModelError("", "Cada producto resultante va en su propio lote: no repitas el código.");
        if (ModelState.IsValid && await Intentar(async () => await api.Post<object>($"ordenes-produccion/{m.OrdenId}/confirmar", new
        {
            Resultados = m.Resultados.Select(x => new { x.ProductoId, CodigoLote = x.CodigoLote.Trim(), Vencimiento = Hora.Utc(x.Vencimiento) })
        })))
        {
            Correcto($"Orden confirmada con {m.Resultados.Count} lote(s). Falta pesarlos y confirmar su entrada en almacén.");
            return RedirectToAction(nameof(Detalle), new { id = m.OrdenId });
        }
        m.Productos = await Activos();
        if (m.Resultados.Count == 0)
            m.Resultados = [new()];
        return View(m);
    }

    [Authorize(Policy = "produccion.administrar")]
    public async Task<IActionResult> Anular(Guid id)
    {
        var d = await api.Get<OrdenDetalleDto>("ordenes-produccion/" + id);
        return View("Confirmacion", new ConfirmacionViewModel
        {
            Id = id,
            Accion = "Anular",
            Titulo = "Anular orden de producción",
            Mensaje = d.Orden.Codigo + ". La orden se conserva en el historial con su motivo; no se borra.",
            MotivoRequerido = true,
            Volver = "/Produccion/Detalle/" + id
        });
    }

    [HttpPost, Authorize(Policy = "produccion.administrar")]
    public async Task<IActionResult> Anular(ConfirmacionViewModel m)
    {
        if (string.IsNullOrWhiteSpace(m.Motivo))
            ModelState.AddModelError(nameof(m.Motivo), "Registra el motivo.");
        if (!ModelState.IsValid || !await Intentar(async () => await api.Post<object>($"ordenes-produccion/{m.Id}/anular", new { m.Motivo })))
            return View("Confirmacion", m);
        Correcto("Orden anulada; historial conservado.");
        return RedirectToAction(nameof(Detalle), new { id = m.Id });
    }

    // ANIMAL_RECHAZADO llega con un texto técnico; el personal necesita saber qué animal y por qué.
    private async Task<string> MotivoRechazo(Guid animalId)
    {
        var animal = (await api.Todos<AnimalDto>("animales")).FirstOrDefault(x => x.Id == animalId);
        var controles = await api.Get<List<ControlSanitarioDto>>($"animales/{animalId}/controles-sanitarios");
        var ultimo = controles.OrderByDescending(x => x.Fecha).FirstOrDefault();
        var nombre = animal?.Identificacion ?? "seleccionado";
        if (ultimo is null)
            return $"El animal {nombre} todavía no tiene control sanitario. Regístralo en Sanidad antes de planificar el faeneo.";
        if (ultimo.Resultado == "RECHAZADO")
            return $"No se puede faenar el animal {nombre}: fue rechazado en el control sanitario del {Hora.Mostrar(ultimo.Fecha)}. Motivo: {ultimo.Observacion}";
        return $"El animal {nombre} no está disponible para faeneo (estado: {animal?.Estado.Replace('_', ' ').ToLowerInvariant()}).";
    }

    private async Task<List<ProductoDto>> Activos() =>
        (await api.Todos<ProductoDto>("productos?activo=true")).OrderBy(x => x.Nombre).ToList();

    private async Task<OrdenFormViewModel> Completar(OrdenFormViewModel m)
    {
        // Se listan todos los animales activos, incluso los rechazados, para que el intento muestre el motivo.
        m.Animales = (await api.Todos<AnimalDto>("animales")).Where(x => x.Activo && x.Estado != "FAENADO").OrderBy(x => x.Identificacion).ToList();
        m.Productos = await Activos();
        return m;
    }

    private async Task<ConsumoFormViewModel> Completar(ConsumoFormViewModel m)
    {
        m.Existencias = (await api.Todos<ExistenciaDto>("existencias")).Where(x => x.Disponible > 0).ToList();
        return m;
    }
}
