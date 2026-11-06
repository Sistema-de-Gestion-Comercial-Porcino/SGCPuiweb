using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SGCP.Web.Models.Dtos;
using SGCP.Web.Models.ViewModels;
using SGCP.Web.Services;
namespace SGCP.Web.Controllers;

[Authorize(Policy = "costos.consultar")]
public class PreciosController(PreciosApi api) : GestionControllerBase
{
    public async Task<IActionResult> Index(string? busqueda,
            int pagina = 1,
            int tamanoPagina = 20) => View(new ListaViewModel<ProductoDto>
            {
                Busqueda = busqueda ?? "",
                Datos = await api.Get<Pagina<ProductoDto>>(Query("productos",
                    busqueda,
                    null,
                    null,
                    pagina,
                    tamanoPagina))
            });
    public async Task<IActionResult> Historial(Guid id) => View(new FichaViewModel<ProductoDto> { Datos = await api.Get<ProductoDto>("productos/" + id), Precios = await api.Get<List<PrecioDto>>($"productos/{id}/precios") });
    [Authorize(Policy = "costos.administrar")] public async Task<IActionResult> Formulario(Guid productoId, Guid? id) { var producto = await api.Get<ProductoDto>("productos/" + productoId); var m = new PrecioFormViewModel { ProductoId = productoId, Producto = producto.Nombre }; if (id is not null) { var dto = (await api.Get<List<PrecioDto>>($"productos/{productoId}/precios")).Single(x => x.Id == id); m.Id = id; m.CostoReferencia = dto.CostoReferencia ?? 0; m.PrecioVenta = dto.PrecioVenta; m.VigenteDesde = Hora.Local(dto.VigenteDesde); } return View(m); }
    [HttpPost, Authorize(Policy = "costos.administrar")]
    public async Task<IActionResult> Formulario(PrecioFormViewModel m)
    {
        if (ModelState.IsValid && await Intentar(async () =>
        {
            var cuerpo = new
            {
                m.CostoReferencia,
                m.PrecioVenta,
                VigenteDesde = Hora.Utc(m.VigenteDesde),
                m.ConfirmarMargenNoPositivo
            };
            if (m.Id is null)
                await api.Post<object>($"productos/{m.ProductoId}/precios",
                cuerpo);
            else
                await api.Put<object>("precios/" + m.Id,
                cuerpo);
        }))
        { Correcto("Vigencia guardada. Los acuerdos históricos se mantienen."); return RedirectToAction(nameof(Historial), new { id = m.ProductoId }); }
        return View(m);
    }
    [Authorize(Policy = "costos.administrar")]
    public IActionResult Finalizar(Guid id,
            Guid productoId) => View("Confirmacion",
            new ConfirmacionViewModel
            {
                Id = id,
                Valor = productoId.ToString(),
                Accion = "Finalizar",
                Titulo = "Finalizar vigencia",
                Mensaje = "La vigencia se cerrará en este momento. Los pedidos confirmados conservan su costo y precio.",
                Volver = "/Precios/Historial/" + productoId
            });
    [HttpPost, Authorize(Policy = "costos.administrar")]
    public async Task<IActionResult> Finalizar(ConfirmacionViewModel m)
    {
        if (!await Intentar(async () => await api.Post<object>($"precios/{m.Id}/finalizar", new { vigenteHasta = DateTime.UtcNow })))
            return View("Confirmacion",
            m);
        Correcto("Vigencia finalizada.");
        return RedirectToAction(nameof(Historial),
            new { id = m.Valor });
    }
}
