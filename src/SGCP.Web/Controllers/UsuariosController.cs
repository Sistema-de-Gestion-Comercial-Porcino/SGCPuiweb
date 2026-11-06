using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SGCP.Web.Models.Dtos;
using SGCP.Web.Models.ViewModels;
using SGCP.Web.Services;
namespace SGCP.Web.Controllers;

[Authorize(Policy = "usuarios.consultar")]
public class UsuariosController(UsuariosApi api) : GestionControllerBase
{
    public async Task<IActionResult> Index(string? busqueda,
            string? filtro,
            string? extra,
            int pagina = 1,
            int tamanoPagina = 20) => View(new ListaViewModel<UsuarioDto>
            {
                Busqueda = busqueda ?? "",
                Filtro = filtro ?? "",
                Extra = extra ?? "",
                Datos = await api.Get<Pagina<UsuarioDto>>(Query("usuarios",
                            busqueda,
                            "estado",
                            filtro,
                            pagina,
                            tamanoPagina,
                            "rol",
                            extra)),
                Opciones = await api.Get<List<OpcionDto>>("roles")
            });
    [Authorize(Policy = "usuarios.administrar")]
    public async Task<IActionResult> Formulario(Guid? id)
    {
        var m = new UsuarioFormViewModel();
        if (id is not null)
        {
            var u = await api.Get<UsuarioDto>("usuarios/" + id);
            m = new()
            {
                Id = id,
                Nombre = u.Nombre,
                Login = u.Login,
                Correo = u.Correo,
                Telefono = u.Telefono,
                Roles = u.Roles.Select(x => x.Id).ToList()
            };
        }
        m.Disponibles = await api.Get<List<OpcionDto>>("roles");
        return View(m);
    }
    [HttpPost, Authorize(Policy = "usuarios.administrar")]
    public async Task<IActionResult> Formulario(UsuarioFormViewModel m)
    {
        if (m.Id is null && string.IsNullOrWhiteSpace(m.Clave))
            ModelState.AddModelError("Clave", "Define una contraseña inicial de al menos 10 caracteres.");
        if (!string.IsNullOrEmpty(m.Clave) && m.Clave.Length < 10)
            ModelState.AddModelError("Clave", "Usa al menos 10 caracteres.");
        if (ModelState.IsValid && await Intentar(async () =>
        {
            if (m.Id is null)
                await api.Post<object>("usuarios",
                new { m.Nombre, m.Login, m.Correo, m.Telefono, m.Clave, m.Roles });
            else
                await api.Put<object>("usuarios/" + m.Id,
                new { m.Nombre, m.Login, m.Correo, m.Telefono, m.Clave, m.Roles });
        }))
        { Correcto("Usuario guardado. Los cambios de permisos requieren iniciar sesión nuevamente."); return RedirectToAction(nameof(Index)); }
        m.Disponibles = await api.Get<List<OpcionDto>>("roles");
        return View(m);
    }
    [Authorize(Policy = "usuarios.administrar")]
    public async Task<IActionResult> Estado(Guid id, string valor)
    {
        var u = await api.Get<UsuarioDto>("usuarios/" + id);
        return View("Confirmacion", new ConfirmacionViewModel
        {
            Id = id,
            Valor = valor,
            Titulo = valor == "ACTIVO" ? "Habilitar usuario" : "Desactivar usuario",
            Mensaje = u.Nombre + " · " + u.Login + ". Se cerrarán sus sesiones. Se conserva su historial y debe permanecer un administrador habilitado.",
            Volver = "/Usuarios"
        });
    }
    [HttpPost, Authorize(Policy = "usuarios.administrar")] public async Task<IActionResult> Estado(ConfirmacionViewModel m) { if (!await Intentar(async () => await api.Post<object>($"usuarios/{m.Id}/estado", new { estado = m.Valor }))) return View("Confirmacion", m); Correcto("Estado de usuario actualizado."); return RedirectToAction(nameof(Index)); }
}
