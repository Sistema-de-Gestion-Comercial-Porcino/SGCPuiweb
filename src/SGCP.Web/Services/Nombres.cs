using System.Security.Claims;
using SGCP.Web.Models.Dtos;
namespace SGCP.Web.Services;

/// <summary>Traduce identificadores de responsables y productos a nombres legibles para las pantallas.</summary>
public static class Nombres
{
    public static async Task<Dictionary<Guid, string>> Usuarios(ClienteApiBase api, ClaimsPrincipal usuario)
    {
        Dictionary<Guid, string> nombres = [];
        // Solo id y nombre: cualquier perfil del personal ve quién registró algo sin ver login ni datos personales.
        if (!usuario.IsInRole("COMPRADOR") || usuario.HasClaim("permiso", "operacion.consultar"))
        {
            try
            {
                foreach (var u in await api.Get<List<NombreUsuarioDto>>("usuarios/nombres"))
                    nombres[u.Id] = u.Nombre;
            }
            catch (ApiException) { }
        }
        if (Guid.TryParse(usuario.FindFirstValue("sub"), out var propio))
            nombres[propio] = usuario.Identity?.Name ?? "Tu cuenta";
        return nombres;
    }

    public static async Task<Dictionary<Guid, ProductoDto>> Productos(ClienteApiBase api) =>
        (await api.Todos<ProductoDto>("productos")).ToDictionary(x => x.Id);

    public static string Responsable(Dictionary<Guid, string> nombres, Guid id) =>
        nombres.TryGetValue(id, out var nombre) ? nombre : "Personal del sistema";
}
