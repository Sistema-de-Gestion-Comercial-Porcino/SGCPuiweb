using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SGCP.Web.Models.Dtos;
using SGCP.Web.Models.ViewModels;
using SGCP.Web.Services;
namespace SGCP.Web.Controllers;

/// <summary>RF-17 · registro manual del control de frío. El sistema no mide: registra lo que el responsable leyó y evaluó.</summary>
[Authorize(Policy = "frio.administrar")]
public class FrioController(FrioApi api) : GestionControllerBase
{
    public async Task<IActionResult> Index(int pagina = 1, int tamanoPagina = 20) =>
        View(new FrioListaViewModel
        {
            Datos = await api.Get<Pagina<ControlFrioDto>>($"controles-frio?pagina={pagina}&tamanoPagina={tamanoPagina}"),
            Lotes = User.HasClaim("permiso", "inventario.consultar") ? (await api.Todos<LoteDto>("lotes")).ToDictionary(x => x.Id) : [],
            Productos = await Nombres.Productos(api),
            Nombres = await Nombres.Usuarios(api, User)
        });

    [Authorize(Policy = "inventario.consultar")]
    public async Task<IActionResult> Registrar(Guid? loteId) => View(await Completar(new FrioFormViewModel { LoteId = loteId }));

    [HttpPost, Authorize(Policy = "inventario.consultar")]
    public async Task<IActionResult> Registrar(FrioFormViewModel m)
    {
        if (ModelState.IsValid && await Intentar(async () => await api.Post<CreadoDto>("controles-frio", new
        {
            m.LoteId,
            m.Temperatura,
            m.Evaluacion,
            m.Ubicacion,
            Fecha = Hora.Utc(m.Fecha),
            m.Observacion
        })))
        {
            Correcto("Control de frío registrado con su responsable y evaluación.");
            return RedirectToAction(nameof(Index));
        }
        return View(await Completar(m));
    }

    private async Task<FrioFormViewModel> Completar(FrioFormViewModel m)
    {
        m.Lotes = (await api.Todos<LoteDto>("lotes")).OrderBy(x => x.Codigo).ToList();
        m.Productos = await Nombres.Productos(api);
        return m;
    }
}
