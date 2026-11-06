using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SGCP.Web.Models.Dtos;
using SGCP.Web.Models.ViewModels;
using SGCP.Web.Services;
namespace SGCP.Web.Controllers;

[Authorize(Policy = "catalogo.administrar")]
public class ClasificacionesController(CatalogoApi api) : GestionControllerBase
{
    public async Task<IActionResult> Index(string? busqueda,
            string? filtro,
            int pagina = 1,
            int tamanoPagina = 20) => View(new ListaViewModel<ClasificacionDto>
            {
                Busqueda = busqueda ?? "",
                Filtro = filtro ?? "",
                Datos = await api.Get<Pagina<ClasificacionDto>>(Query("clasificaciones",
                            busqueda,
                            "familia",
                            filtro,
                            pagina,
                            tamanoPagina))
            });
    public async Task<IActionResult> Formulario(Guid? id) => View(id is null ? new ClasificacionFormViewModel() : Convertir<ClasificacionFormViewModel>(await api.Get<ClasificacionDto>("clasificaciones/" + id)));
    [HttpPost]
    public async Task<IActionResult> Formulario(ClasificacionFormViewModel m)
    {
        if (ModelState.IsValid
        && await Intentar(async () =>
        {
            if (m.Id is null)
                await api.Post<object>("clasificaciones",
            m);
            else
                await api.Put<object>("clasificaciones/" + m.Id,
            m);
        }))
        { Correcto("Clasificación guardada."); return RedirectToAction(nameof(Index)); }
        return View(m);
    }
    public async Task<IActionResult> Estado(Guid id, string valor)
    {
        var c = await api.Get<ClasificacionDto>("clasificaciones/" + id);
        return View("Confirmacion", new ConfirmacionViewModel
        {
            Id = id,
            Valor = valor,
            Titulo = "Cambiar estado de clasificación",
            Mensaje = c.Nombre + ". Una clasificación con productos activos no puede desactivarse.",
            Volver = "/Clasificaciones"
        });
    }
    [HttpPost] public async Task<IActionResult> Estado(ConfirmacionViewModel m) { if (!await Intentar(async () => await api.Post<object>($"clasificaciones/{m.Id}/estado", new { activo = m.Valor == "true" }))) return View("Confirmacion", m); Correcto("Estado actualizado."); return RedirectToAction(nameof(Index)); }
}
