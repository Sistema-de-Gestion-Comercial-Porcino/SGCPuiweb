using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SGCP.Web.Models.Dtos;
using SGCP.Web.Services;
namespace SGCP.Web.Controllers;

[Authorize(Policy = "auditoria.consultar")]
public class AuditoriaController(UsuariosApi api) : Controller
{
    public async Task<IActionResult> Index(int pagina = 1) => View(await api.Get<Pagina<AuditoriaDto>>($"auditoria?pagina={pagina}&tamanoPagina=20"));
}
public class AuditoriaDto
{
    public DateTime Fecha { get; set; }
    public string Operacion { get; set; } = "";
    public string Entidad { get; set; } = "";
    public string Usuario { get; set; } = "";
}
