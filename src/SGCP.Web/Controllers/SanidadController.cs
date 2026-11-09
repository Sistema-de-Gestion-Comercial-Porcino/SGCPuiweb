using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SGCP.Web.Models.Dtos;
using SGCP.Web.Models.ViewModels;
using SGCP.Web.Services;
namespace SGCP.Web.Controllers;

/// <summary>RF-18 · animales y control sanitario. Un animal rechazado queda bloqueado y no se faena.</summary>
[Authorize(Policy = "produccion.consultar")]
public class SanidadController(SanidadApi api) : GestionControllerBase
{
    public async Task<IActionResult> Index(int pagina = 1, int tamanoPagina = 20) =>
        View(new ListaViewModel<AnimalDto>
        {
            Datos = await api.Get<Pagina<AnimalDto>>($"animales?pagina={pagina}&tamanoPagina={tamanoPagina}")
        });

    public async Task<IActionResult> Detalle(Guid id)
    {
        var animal = await Buscar(id);
        if (animal is null)
            return NotFound();
        return View(new AnimalFichaViewModel
        {
            Animal = animal,
            Controles = await api.Get<List<ControlSanitarioDto>>($"animales/{id}/controles-sanitarios"),
            Nombres = await Nombres.Usuarios(api, User)
        });
    }

    [Authorize(Policy = "produccion.administrar")]
    public async Task<IActionResult> Formulario(Guid? id)
    {
        if (id is null)
            return View(new AnimalFormViewModel());
        var a = await Buscar(id.Value);
        if (a is null)
            return NotFound();
        return View(new AnimalFormViewModel
        {
            Id = a.Id,
            Identificacion = a.Identificacion,
            Procedencia = a.Procedencia,
            Proveedor = a.Proveedor,
            PesoIngreso = a.PesoIngreso
        });
    }

    [HttpPost, Authorize(Policy = "produccion.administrar")]
    public async Task<IActionResult> Formulario(AnimalFormViewModel m)
    {
        var cuerpo = new { m.Identificacion, m.Procedencia, m.Proveedor, m.PesoIngreso };
        if (ModelState.IsValid && await Intentar(async () =>
        {
            if (m.Id is null)
                await api.Post<CreadoDto>("animales", cuerpo);
            else
                await api.Put<CreadoDto>("animales/" + m.Id, cuerpo);
        }))
        {
            Correcto("Animal registrado. Queda pendiente hasta su control sanitario.");
            return RedirectToAction(nameof(Index));
        }
        return View(m);
    }

    [Authorize(Policy = "produccion.administrar")]
    public async Task<IActionResult> Control(Guid id)
    {
        var a = await Buscar(id);
        if (a is null)
            return NotFound();
        return View(new ControlSanitarioFormViewModel { AnimalId = id, Animal = a.Identificacion + " · " + a.Procedencia });
    }

    [HttpPost, Authorize(Policy = "produccion.administrar")]
    public async Task<IActionResult> Control(ControlSanitarioFormViewModel m)
    {
        if (ModelState.IsValid && await Intentar(async () => await api.Post<CreadoDto>($"animales/{m.AnimalId}/controles-sanitarios", new
        {
            m.Resultado,
            m.Observacion,
            Fecha = Hora.Utc(m.Fecha)
        })))
        {
            Correcto(m.Resultado == "RECHAZADO"
                ? "Control registrado. El animal quedó rechazado y no podrá faenarse."
                : "Control registrado. El animal quedó apto para faeneo.");
            return RedirectToAction(nameof(Detalle), new { id = m.AnimalId });
        }
        return View(m);
    }

    // La API no expone GET animales/{id}; se busca en el listado completo.
    private async Task<AnimalDto?> Buscar(Guid id) =>
        (await api.Todos<AnimalDto>("animales")).FirstOrDefault(x => x.Id == id);
}
