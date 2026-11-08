using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SGCP.Web.Models.Dtos;
using SGCP.Web.Models.ViewModels;
using SGCP.Web.Services;
namespace SGCP.Web.Controllers;

/// <summary>
/// RF-04 · almacén. Pesaje y entrada física son dos pasos distintos y no se mezclan:
/// el pesaje no cambia el stock; la entrada física lo suma una sola vez por lote y producto.
/// </summary>
[Authorize(Policy = "inventario.consultar")]
public class AlmacenController(InventarioApi api) : GestionControllerBase
{
    public async Task<IActionResult> Existencias(Guid? productoId, int pagina = 1, int tamanoPagina = 20)
    {
        var ruta = $"existencias?pagina={pagina}&tamanoPagina={tamanoPagina}" + (productoId is null ? "" : "&productoId=" + productoId);
        return View(new ListaViewModel<ExistenciaDto>
        {
            Filtro = productoId?.ToString() ?? "",
            Datos = await api.Get<Pagina<ExistenciaDto>>(ruta),
            Opciones = (await api.Todos<ProductoDto>("productos")).OrderBy(x => x.Nombre)
                .Select(x => new OpcionDto { Id = x.Id, Nombre = x.Nombre }).ToList()
        });
    }

    [Authorize(Policy = "inventario.administrar")]
    public async Task<IActionResult> Pesajes(int pagina = 1, int tamanoPagina = 20) =>
        View(new PesajeListaViewModel
        {
            Datos = await api.Get<Pagina<PesajeDto>>($"pesajes?pagina={pagina}&tamanoPagina={tamanoPagina}"),
            Lotes = (await api.Todos<LoteDto>("lotes")).ToDictionary(x => x.Id),
            Productos = await Nombres.Productos(api),
            Nombres = await Nombres.Usuarios(api, User),
            ConEntrada = (await api.Todos<EntradaInventarioDto>("entradas-inventario")).Where(x => !x.Anulada).Select(x => x.PesajeId).ToHashSet()
        });

    [Authorize(Policy = "inventario.administrar")]
    public async Task<IActionResult> Pesar(Guid? loteId) => View(await Completar(new PesajeFormViewModel { LoteId = loteId }));

    [HttpPost, Authorize(Policy = "inventario.administrar")]
    public async Task<IActionResult> Pesar(PesajeFormViewModel m)
    {
        if (m.Tara >= m.PesoBruto)
            ModelState.AddModelError(nameof(m.Tara), "La tara debe ser menor al peso bruto.");
        if (ModelState.IsValid && await Intentar(async () => await api.Post<CreadoDto>("pesajes", new
        {
            m.LoteId,
            m.PesoBruto,
            m.Tara,
            m.Unidad,
            m.Observacion
        })))
        {
            Correcto($"Pesaje registrado: neto {(m.PesoBruto - m.Tara).ToString("0.###")} {m.Unidad}. La existencia todavía no cambió; falta confirmar la entrada física.");
            return RedirectToAction(nameof(Pesajes));
        }
        return View(await Completar(m));
    }

    [Authorize(Policy = "inventario.administrar")]
    public async Task<IActionResult> Entradas(int pagina = 1, int tamanoPagina = 20) =>
        View(new EntradaListaViewModel
        {
            Datos = await api.Get<Pagina<EntradaInventarioDto>>($"entradas-inventario?pagina={pagina}&tamanoPagina={tamanoPagina}"),
            Lotes = (await api.Todos<LoteDto>("lotes")).ToDictionary(x => x.Id),
            Productos = await Nombres.Productos(api),
            Ubicaciones = (await api.Todos<ExistenciaDto>("existencias")).ToDictionary(x => x.Id, x => x.Ubicacion),
            Nombres = await Nombres.Usuarios(api, User)
        });

    [Authorize(Policy = "inventario.administrar")]
    public async Task<IActionResult> Entrada(Guid? pesajeId) => View(await Completar(new EntradaFormViewModel { PesajeId = pesajeId }));

    [HttpPost, Authorize(Policy = "inventario.administrar")]
    public async Task<IActionResult> Entrada(EntradaFormViewModel m)
    {
        if (!ModelState.IsValid)
            return View(await Completar(m));
        var error = await IntentarConCodigo(async () => await api.Post<CreadoDto>("entradas-inventario", new { m.PesajeId, m.Ubicacion }));
        if (error is null)
        {
            Correcto("Entrada física confirmada. El stock del lote ya está disponible en Existencias.");
            return RedirectToAction(nameof(Existencias));
        }
        ModelState.AddModelError("", error.Codigo == "ENTRADA_YA_REGISTRADA" ? await YaRegistrada(m.PesajeId!.Value) : error.Message);
        return View(await Completar(m));
    }

    [Authorize(Policy = "inventario.administrar")]
    public IActionResult Anular(Guid id) => View("Confirmacion", new ConfirmacionViewModel
    {
        Id = id,
        Accion = "Anular",
        Titulo = "Anular entrada física",
        Mensaje = "La entrada se conserva en el historial con su motivo. Solo se anula stock que no fue reservado, consumido ni despachado.",
        MotivoRequerido = true,
        Volver = "/Almacen/Entradas"
    });

    [HttpPost, Authorize(Policy = "inventario.administrar")]
    public async Task<IActionResult> Anular(ConfirmacionViewModel m)
    {
        if (string.IsNullOrWhiteSpace(m.Motivo))
            ModelState.AddModelError(nameof(m.Motivo), "Registra el motivo.");
        if (!ModelState.IsValid || !await Intentar(async () => await api.Post<object>($"entradas-inventario/{m.Id}/anular", new { m.Motivo })))
            return View("Confirmacion", m);
        Correcto("Entrada anulada; historial conservado.");
        return RedirectToAction(nameof(Entradas));
    }

    // ENTRADA_YA_REGISTRADA: en lugar de un error rojo sin datos, se dice cuándo y quién la registró.
    private async Task<string> YaRegistrada(Guid pesajeId)
    {
        var pesaje = (await api.Todos<PesajeDto>("pesajes")).FirstOrDefault(x => x.Id == pesajeId);
        var previa = pesaje is null ? null : (await api.Todos<EntradaInventarioDto>("entradas-inventario"))
            .FirstOrDefault(x => !x.Anulada && x.LoteId == pesaje.LoteId && x.ProductoId == pesaje.ProductoId);
        if (previa is null)
            return "Este lote y producto ya tienen una entrada física registrada.";
        var nombres = await Nombres.Usuarios(api, User);
        return $"Este lote ya tiene su entrada física: la registró {Nombres.Responsable(nombres, previa.ResponsableId)} el {Hora.Mostrar(previa.Fecha)} " +
            $"con {previa.Cantidad.ToString("0.###")} {pesaje!.Unidad}. No se vuelve a sumar al stock.";
    }

    private async Task<PesajeFormViewModel> Completar(PesajeFormViewModel m)
    {
        var pesados = (await api.Todos<PesajeDto>("pesajes")).Select(x => x.LoteId).ToHashSet();
        m.Lotes = (await api.Todos<LoteDto>("lotes"))
            .Where(x => x.Estado == "APTO" && (!pesados.Contains(x.Id) || x.Id == m.LoteId))
            .OrderBy(x => x.Codigo).ToList();
        m.Productos = await Nombres.Productos(api);
        return m;
    }

    private async Task<EntradaFormViewModel> Completar(EntradaFormViewModel m)
    {
        var confirmados = (await api.Todos<EntradaInventarioDto>("entradas-inventario")).Where(x => !x.Anulada).Select(x => x.PesajeId).ToHashSet();
        m.Pesajes = (await api.Todos<PesajeDto>("pesajes"))
            .Where(x => !confirmados.Contains(x.Id) || x.Id == m.PesajeId)
            .OrderByDescending(x => x.Fecha).ToList();
        m.Lotes = (await api.Todos<LoteDto>("lotes")).ToDictionary(x => x.Id);
        m.Productos = await Nombres.Productos(api);
        return m;
    }
}
