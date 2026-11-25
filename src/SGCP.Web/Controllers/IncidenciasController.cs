using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SGCP.Web.Models.Dtos;
using SGCP.Web.Models.ViewModels;
using SGCP.Web.Services;
namespace SGCP.Web.Controllers;

/// <summary>
/// RF-18 · «queda incidencia». La abre la API al rechazar un animal en sanidad, un lote por calidad o frío,
/// o un derivado que consumió un insumo rechazado. Resolverla registra la decisión y no rehabilita nada (v4.3.2).
/// </summary>
[Authorize(Policy = "inventario.consultar")]
public class IncidenciasController(InventarioApi api) : GestionControllerBase
{
    public async Task<IActionResult> Index(string estado = "pendientes", int pagina = 1, int tamanoPagina = 20)
    {
        estado = estado is "resueltas" or "todas" ? estado : "pendientes";
        var filtro = estado switch { "resueltas" => "&resuelta=true", "pendientes" => "&resuelta=false", _ => "" };
        var m = new IncidenciasViewModel
        {
            Estado = estado,
            Datos = await api.Get<Pagina<IncidenciaDto>>($"incidencias?pagina={pagina}&tamanoPagina={tamanoPagina}{filtro}"),
            Lotes = (await api.Todos<LoteDto>("lotes")).ToDictionary(x => x.Id),
            Productos = await Nombres.Productos(api),
            Nombres = await Nombres.Usuarios(api, User)
        };
        // Los animales los lee quien tiene produccion.consultar; sin ese permiso el origen se muestra abreviado.
        if (User.HasClaim("permiso", "produccion.consultar"))
            m.Animales = (await api.Todos<AnimalDto>("animales")).ToDictionary(x => x.Id);
        return View(m);
    }

    [Authorize(Policy = "produccion.administrar")]
    public IActionResult Resolver(Guid id) => View("Confirmacion", new ConfirmacionViewModel
    {
        Id = id,
        Accion = "Resolver",
        Titulo = "Resolver incidencia",
        Mensaje = "Registra qué se decidió. Resolver no rehabilita el animal ni el lote: siguen bloqueados y no se pueden faenar, reservar ni despachar.",
        MotivoRequerido = true,
        Volver = "/Incidencias"
    });

    [HttpPost, Authorize(Policy = "produccion.administrar")]
    public async Task<IActionResult> Resolver(ConfirmacionViewModel m)
    {
        if (string.IsNullOrWhiteSpace(m.Motivo))
            ModelState.AddModelError(nameof(m.Motivo), "Registra la resolución.");
        if (!ModelState.IsValid || !await Intentar(async () => await api.Post<object>($"incidencias/{m.Id}/resolver", new { m.Motivo })))
            return View("Confirmacion", m);
        Correcto("Incidencia resuelta. El animal o lote sigue bloqueado.");
        return RedirectToAction(nameof(Index));
    }
}
