using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SGCP.Web.Models.Dtos;
using SGCP.Web.Models.ViewModels;
using SGCP.Web.Services;
namespace SGCP.Web.Controllers;

[Authorize]
public class InicioController(ClienteApiBase api) : Controller
{
    public async Task<IActionResult> Index() => View(new ResumenViewModel { Datos = await api.Get<ResumenDto>("resumen") });
}
