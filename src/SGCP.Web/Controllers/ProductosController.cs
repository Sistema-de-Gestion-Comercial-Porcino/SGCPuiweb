using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SGCP.Web.Models.Dtos;
using SGCP.Web.Models.ViewModels;
using SGCP.Web.Services;
namespace SGCP.Web.Controllers;

[Authorize(Policy = "catalogo.consultar")]
public class ProductosController(CatalogoApi api) : GestionControllerBase
{
    public async Task<IActionResult> Index(string? busqueda,
            string? filtro,
            string? extra,
            int pagina = 1,
            int tamanoPagina = 20) => View(new ListaViewModel<ProductoDto>
            {
                Busqueda = busqueda ?? "",
                Filtro = filtro ?? "",
                Extra = extra ?? "",
                Datos = await api.Get<Pagina<ProductoDto>>(Query("productos",
                            busqueda,
                            "familia",
                            filtro,
                            pagina,
                            tamanoPagina,
                            "activo",
                            extra))
            });
    public async Task<IActionResult> Detalle(Guid id) => View(new FichaViewModel<ProductoDto> { Datos = await api.Get<ProductoDto>("productos/" + id), Precios = User.HasClaim("permiso", "costos.consultar") ? await api.Get<List<PrecioDto>>($"productos/{id}/precios") : [] });
    [Authorize(Policy = "catalogo.administrar")] public async Task<IActionResult> Formulario(Guid? id) { var m = id is null ? new ProductoFormViewModel() : Convertir<ProductoFormViewModel>(await api.Get<ProductoDto>("productos/" + id)); m.Clasificaciones = await api.Todos<ClasificacionDto>("clasificaciones?activo=true"); return View(m); }
    [HttpPost, Authorize(Policy = "catalogo.administrar")]
    public async Task<IActionResult> Formulario(ProductoFormViewModel m)
    {
        if (ModelState.IsValid
        && await Intentar(async () =>
        {
            if (m.Id is null)
                await api.Post<object>("productos",
            m);
            else
                await api.Put<object>("productos/" + m.Id,
            m);
        }))
        { Correcto("Producto guardado. Esta operación no crea existencias."); return RedirectToAction(nameof(Index)); }
        m.Clasificaciones = await api.Todos<ClasificacionDto>("clasificaciones?activo=true");
        return View(m);
    }
    [Authorize(Policy = "catalogo.administrar")]
    public async Task<IActionResult> Estado(Guid id, string valor)
    {
        var c = await api.Get<ProductoDto>("productos/" + id);
        return View("Confirmacion", new ConfirmacionViewModel
        {
            Id = id,
            Valor = valor,
            Titulo = "Cambiar estado de producto",
            Mensaje = c.Nombre + ". Se conserva su historial. Las operaciones pendientes pueden impedir desactivarlo.",
            Volver = "/Productos"
        });
    }
    [HttpPost, Authorize(Policy = "catalogo.administrar")] public async Task<IActionResult> Estado(ConfirmacionViewModel m) { if (!await Intentar(async () => await api.Post<object>($"productos/{m.Id}/estado", new { activo = m.Valor == "true" }))) return View("Confirmacion", m); Correcto("Estado del producto actualizado."); return RedirectToAction(nameof(Index)); }
}
