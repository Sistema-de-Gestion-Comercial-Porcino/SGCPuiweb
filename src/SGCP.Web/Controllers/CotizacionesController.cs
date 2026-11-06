using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SGCP.Web.Models.Dtos;
using SGCP.Web.Models.ViewModels;
using SGCP.Web.Services;
namespace SGCP.Web.Controllers;

[Authorize(Policy = "cotizaciones.consultar")]
public class CotizacionesController(CotizacionesApi api) : GestionControllerBase
{
    public async Task<IActionResult> Index(string? busqueda,
            string? filtro,
            int pagina = 1,
            int tamanoPagina = 20) => View(new ListaViewModel<CotizacionDto>
            {
                Busqueda = busqueda ?? "",
                Filtro = filtro ?? "",
                Datos = await api.Get<Pagina<CotizacionDto>>(Query("cotizaciones",
                            busqueda,
                            "estado",
                            filtro,
                            pagina,
                            tamanoPagina))
            });
    public async Task<IActionResult> Detalle(Guid id) => View(new FichaViewModel<CotizacionDto> { Datos = await api.Get<CotizacionDto>("cotizaciones/" + id) });
    private async Task Cargar(CotizacionFormViewModel m) { m.Clientes = await api.Todos<ClienteDto>("clientes?activo=true"); m.Productos = await api.Todos<ProductoDto>("productos?activo=true"); }
    [Authorize(Policy = "cotizaciones.administrar")]
    public async Task<IActionResult> Formulario(Guid? id)
    {
        var m = new CotizacionFormViewModel();
        if (id is not null)
        {
            var c = await api.Get<CotizacionDto>("cotizaciones/" + id);
            m = new()
            {
                Id = id,
                ClienteId = c.ClienteId,
                Canal = c.Canal,
                VigenteHasta = Hora.Local(c.VigenteHasta),
                Detalles = c.Detalles.Select(x => new DetalleFormViewModel
                {
                    ProductoId = x.ProductoId,
                    Cantidad = x.Cantidad,
                    Unidad = x.Unidad,
                    PrecioUnitario = x.PrecioUnitario
                })
                    .ToList()
            };
        }
        await Cargar(m);
        return View(m);
    }
    [HttpPost, Authorize(Policy = "cotizaciones.administrar")]
    public async Task<IActionResult> Formulario(CotizacionFormViewModel m)
    {
        Guid id = Guid.Empty;
        if (ModelState.IsValid && await Intentar(async () =>
        {
            var cuerpo = new
            {
                m.ClienteId,
                m.Canal,
                VigenteHasta = Hora.Utc(m.VigenteHasta),
                m.Detalles
            };
            var r = m.Id is null ? await api.Post<CreadoDto>("cotizaciones",
                cuerpo) : await api.Put<CreadoDto>("cotizaciones/" + m.Id,
                cuerpo);
            id = r.Id;
        }))
        { Correcto("Borrador guardado. Preséntalo para registrar la respuesta del cliente."); return RedirectToAction(nameof(Detalle), new { id }); }
        await Cargar(m);
        return View(m);
    }
    [HttpPost, Authorize(Policy = "cotizaciones.administrar")] public async Task<IActionResult> Presentar(Guid id) { try { await api.Post<object>($"cotizaciones/{id}/presentar"); Correcto("Cotización presentada."); } catch (ApiException e) when (e.Estado == 409) { TempData["Error"] = e.Message; } return RedirectToAction(nameof(Detalle), new { id }); }
    [Authorize(Policy = "cotizaciones.responder")]
    public async Task<IActionResult> Responder(Guid id, string valor)
    {
        var c = await api.Get<CotizacionDto>("cotizaciones/" + id);
        return View("Confirmacion", new ConfirmacionViewModel
        {
            Id = id,
            Valor = valor,
            Accion = "Responder",
            Titulo = valor == "aceptar" ? "Registrar aceptación" : "Registrar rechazo o cambios",
            Mensaje = c.Codigo + " · " + c.Cliente + ". La respuesta se conservará en el historial. Rechazar no crea un pedido.",
            MotivoRequerido = valor != "aceptar",
            Volver = "/Cotizaciones/Detalle/" + id
        });
    }
    [HttpPost, Authorize(Policy = "cotizaciones.responder")]
    public async Task<IActionResult> Responder(ConfirmacionViewModel m)
    {
        if (m.Valor != "aceptar"
        && string.IsNullOrWhiteSpace(m.Motivo))
            ModelState.AddModelError("Motivo",
            "Escribe el motivo.");
        if (!ModelState.IsValid
                                        || !await Intentar(async () => await api.Post<object>($"cotizaciones/{m.Id}/" + (m.Valor == "aceptar" ? "aceptar" : "rechazar"), new { m.Motivo })))
            return View("Confirmacion",
            m);
        Correcto("Respuesta registrada.");
        return RedirectToAction(nameof(Detalle),
            new { id = m.Id });
    }
    [Authorize(Policy = "cotizaciones.administrar")]
    public IActionResult NuevaVersion(Guid id) => View("Confirmacion", new ConfirmacionViewModel
    {
        Id = id,
        Accion = "NuevaVersion",
        Titulo = "Preparar una nueva versión",
        Mensaje = "Se conserva la versión anterior. El nuevo borrador tendrá vigencia inicial de 24 horas, que podrás editar antes de presentarlo.",
        Volver = "/Cotizaciones/Detalle/" + id
    });
    [HttpPost, Authorize(Policy = "cotizaciones.administrar")]
    public async Task<IActionResult> NuevaVersion(ConfirmacionViewModel m)
    {
        Guid id = Guid.Empty;
        if (!await Intentar(async () => id = (await api.Post<CreadoDto>($"cotizaciones/{m.Id}/nueva-version", new { vigenteHasta = DateTime.UtcNow.AddDays(1) })).Id))
            return View("Confirmacion",
            m);
        return RedirectToAction(nameof(Formulario),
            new { id });
    }
}
