using System.Security.Claims;
using SGCP.Web.Models.Dtos;
namespace SGCP.Web.Models.ViewModels;

public static class AccesoPerfiles
{
    // ADMINISTRACION solo se reconoce para consumir una API anterior durante una actualización escalonada.
    public static bool EsRaiz(ClaimsPrincipal actor) => actor.IsInRole("SUPERADMIN") || actor.IsInRole("ADMINISTRACION");
    public static int Nivel(string rol) => rol switch { "SUPERADMIN" or "ADMINISTRACION" => 100, "PROPIETARIO" => 80, "ADMINISTRADOR" => 60, _ => 10 };
    public static bool PuedeGestionar(ClaimsPrincipal actor, UsuarioDto usuario) => EsRaiz(actor) ||
        (actor.FindFirstValue("sub") != usuario.Id.ToString() && (actor.IsInRole("PROPIETARIO") || !usuario.Roles.Any(r => r.Nombre == "FINANZAS")) &&
        actor.FindAll("rol").Select(r => Nivel(r.Value)).DefaultIfEmpty(0).Max() > usuario.Roles.Select(r => Nivel(r.Nombre)).DefaultIfEmpty(0).Max());
    public static string Espacio(ClaimsPrincipal actor) => EsRaiz(actor) ? "Administración del sistema" :
        actor.IsInRole("PROPIETARIO") ? "Dirección del negocio" : actor.IsInRole("ADMINISTRADOR") ? "Administración operativa" :
        actor.IsInRole("COMPRADOR") ? "Portal del comprador" : "Gestión del negocio";
}
