using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SGCP.Web.Models.Dtos;
using SGCP.Web.Models.ViewModels;
using SGCP.Web.Services;
namespace SGCP.Web.Controllers;

[Authorize(Policy = "clientes.consultar")]
public class ClientesController(ClientesApi api) : GestionControllerBase
{
    public async Task<IActionResult> Index(string? busqueda,
            string? filtro,
            string? extra,
            int pagina = 1,
            int tamanoPagina = 20) => View(new ListaViewModel<ClienteDto>
            {
                Busqueda = busqueda ?? "",
                Filtro = filtro ?? "",
                Extra = extra ?? "",
                Datos = await api.Get<Pagina<ClienteDto>>(Query("clientes",
                            busqueda,
                            "activo",
                            filtro,
                            pagina,
                            tamanoPagina,
                            "tipo",
                            extra))
            });
    public async Task<IActionResult> Detalle(Guid id) => View(new FichaViewModel<ClienteDto> { Datos = await api.Get<ClienteDto>("clientes/" + id), Pedidos = User.HasClaim("permiso", "pedidos.consultar") ? (await api.Get<Pagina<PedidoDto>>("pedidos?clienteId=" + id)).Elementos : [] });
    [Authorize(Policy = "clientes.administrar")] public async Task<IActionResult> Formulario(Guid? id) => View(id is null ? new ClienteFormViewModel() : Convertir<ClienteFormViewModel>(await api.Get<ClienteDto>("clientes/" + id)));
    [HttpPost, Authorize(Policy = "clientes.administrar")]
    public async Task<IActionResult> Formulario(ClienteFormViewModel m)
    {
        if (ModelState.IsValid && await Intentar(async () =>
        {
            if (m.Id is null)
                await api.Post<object>("clientes",
                m);
            else
                await api.Put<object>("clientes/" + m.Id,
                m);
        }))
        { Correcto("Ficha de cliente guardada."); return RedirectToAction(nameof(Index)); }
        return View(m);
    }
    [Authorize(Policy = "clientes.administrar")]
    public async Task<IActionResult> Estado(Guid id, string valor)
    {
        var c = await api.Get<ClienteDto>("clientes/" + id);
        return View("Confirmacion", new ConfirmacionViewModel
        {
            Id = id,
            Valor = valor,
            Titulo = valor == "true" ? "Activar cliente" : "Desactivar cliente",
            Mensaje = c.Nombre + ". Se revisarán pedidos, cotizaciones y cuentas pendientes antes de desactivar. Su historial se conserva.",
            Volver = "/Clientes"
        });
    }
    [HttpPost, Authorize(Policy = "clientes.administrar")] public async Task<IActionResult> Estado(ConfirmacionViewModel m) { if (!await Intentar(async () => await api.Post<object>($"clientes/{m.Id}/" + (m.Valor == "true" ? "activar" : "desactivar")))) return View("Confirmacion", m); Correcto("Estado del cliente actualizado."); return RedirectToAction(nameof(Index)); }
}
