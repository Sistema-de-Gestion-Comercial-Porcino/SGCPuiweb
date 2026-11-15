using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SGCP.Web.Models.Dtos;
using SGCP.Web.Models.ViewModels;
using SGCP.Web.Services;
namespace SGCP.Web.Controllers;

/// <summary>RF-08 · transportes propios o externos. Se desactivan, nunca se borran.</summary>
[Authorize(Policy = "entregas.administrar")]
public class TransportesController(RepartoApi api) : GestionControllerBase
{
    public async Task<IActionResult> Index(int pagina = 1, int tamanoPagina = 20) =>
        View(await api.Get<Pagina<TransporteDto>>($"transportes?pagina={pagina}&tamanoPagina={tamanoPagina}"));

    public async Task<IActionResult> Formulario(Guid? id)
    {
        if (id is null)
            return View(new TransporteFormViewModel());
        var t = (await api.Todos<TransporteDto>("transportes")).FirstOrDefault(x => x.Id == id);
        if (t is null)
            return NotFound();
        return View(new TransporteFormViewModel
        {
            Id = t.Id,
            Codigo = t.Codigo,
            Modalidad = t.Modalidad,
            Transportista = t.Transportista,
            Contacto = t.Contacto,
            Placa = t.Placa,
            DescripcionVehiculo = t.DescripcionVehiculo,
            Conservacion = t.Conservacion
        });
    }

    [HttpPost]
    public async Task<IActionResult> Formulario(TransporteFormViewModel m)
    {
        var cuerpo = new
        {
            Codigo = m.Codigo.Trim(),
            m.Modalidad,
            Transportista = m.Transportista.Trim(),
            Contacto = m.Contacto.Trim(),
            Placa = m.Placa.Trim(),
            DescripcionVehiculo = m.DescripcionVehiculo.Trim(),
            Conservacion = m.Conservacion.Trim()
        };
        if (ModelState.IsValid && await Intentar(async () =>
        {
            if (m.Id is null)
                await api.Post<CreadoDto>("transportes", cuerpo);
            else
                await api.Put<CreadoDto>($"transportes/{m.Id}", cuerpo);
        }))
        {
            Correcto(m.Id is null ? "Transporte registrado." : "Transporte actualizado.");
            return RedirectToAction(nameof(Index));
        }
        return View(m);
    }

    [HttpPost]
    public async Task<IActionResult> Estado(Guid id, bool activo)
    {
        if (await Intentar(async () => await api.Post<object>($"transportes/{id}/estado", new { Activo = activo })))
            Correcto(activo ? "Transporte activado." : "Transporte desactivado. Su historial se conserva.");
        else
            TempData["Error"] = string.Join(" ", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage));
        return RedirectToAction(nameof(Index));
    }
}

/// <summary>RF-08 · rutas: transporte, responsable y entregas a domicilio.</summary>
[Authorize(Policy = "entregas.administrar")]
public class RutasController(RepartoApi api) : GestionControllerBase
{
    public async Task<IActionResult> Index(int pagina = 1, int tamanoPagina = 20) => View(new RutasViewModel
    {
        Datos = await api.Get<Pagina<RutaDto>>($"rutas?pagina={pagina}&tamanoPagina={tamanoPagina}"),
        Transportes = (await api.Todos<TransporteDto>("transportes")).ToDictionary(x => x.Id),
        Responsables = (await api.Get<List<ResponsableDto>>("responsables-entrega")).ToDictionary(x => x.Id, x => x.Nombre)
    });

    public async Task<IActionResult> Nueva() => View(await Completar(new RutaFormViewModel()));

    [HttpPost]
    public async Task<IActionResult> Nueva(RutaFormViewModel m)
    {
        if (ModelState.IsValid && await Intentar(async () => await api.Post<CreadoDto>("rutas", new
        {
            m.TransporteId,
            m.ResponsableId,
            Fecha = Hora.Utc(m.Fecha),
            Zona = m.Zona.Trim()
        })))
        {
            Correcto("Ruta planificada. Ahora prepara las entregas a domicilio en esta ruta.");
            return RedirectToAction(nameof(Index));
        }
        return View(await Completar(m));
    }

    /// <summary>El servidor exige entregas registradas y conflictos resueltos. Las capturas que siguen en un teléfono las bloquea la app Android.</summary>
    [HttpPost]
    public async Task<IActionResult> Finalizar(Guid id)
    {
        var error = await IntentarConCodigo(async () => await api.Post<object>($"rutas/{id}/finalizar"));
        if (error is null)
            Correcto("Ruta finalizada. No resuelve observaciones ni salda deudas.");
        else
            TempData["Error"] = error.Codigo == "RUTA_PENDIENTE"
                ? "La ruta todavía tiene entregas sin recepción sincronizada o conflictos sin revisar."
                : error.Message;
        return RedirectToAction(nameof(Index));
    }

    private async Task<RutaFormViewModel> Completar(RutaFormViewModel m)
    {
        m.Transportes = (await api.Todos<TransporteDto>("transportes")).Where(x => x.Activo).OrderBy(x => x.Codigo).ToList();
        m.Responsables = await api.Get<List<ResponsableDto>>("responsables-entrega");
        return m;
    }
}

/// <summary>
/// RF-07 · entregas. Esta pantalla muestra solo el estado de la entrega, nunca el saldo:
/// entrega y deuda son independientes (criterio del Sprint 3).
/// </summary>
[Authorize(Policy = "entregas.administrar")]
public class EntregasController(RepartoApi api) : GestionControllerBase
{
    public async Task<IActionResult> Index(int pagina = 1, int tamanoPagina = 20) => View(new EntregasViewModel
    {
        Datos = await api.Get<Pagina<EntregaDto>>($"entregas?pagina={pagina}&tamanoPagina={tamanoPagina}"),
        Pedidos = await Pedidos(),
        Responsables = (await api.Get<List<ResponsableDto>>("responsables-entrega")).ToDictionary(x => x.Id, x => x.Nombre),
        Rutas = (await api.Todos<RutaDto>("rutas")).ToDictionary(x => x.Id),
        UsuarioId = Guid.TryParse(User.FindFirstValue("sub"), out var id) ? id : null
    });

    [Authorize(Policy = "pedidos.consultar")]
    public async Task<IActionResult> Preparar(Guid? pedidoId) => View(await Completar(new EntregaFormViewModel { PedidoId = pedidoId }));

    [HttpPost, Authorize(Policy = "pedidos.consultar")]
    public async Task<IActionResult> Preparar(EntregaFormViewModel m)
    {
        var domicilio = m.Modalidad == "DOMICILIO";
        if (domicilio && m.RutaId is null)
            ModelState.AddModelError(nameof(m.RutaId), "La entrega a domicilio necesita una ruta.");
        var error = ModelState.IsValid ? await IntentarConCodigo(async () => await api.Post<CreadoDto>("entregas", new
        {
            m.PedidoId,
            RutaId = domicilio ? m.RutaId : null,
            m.ResponsableId,
            m.Modalidad,
            FechaPrevista = Hora.Utc(m.FechaPrevista),
            m.OrdenEnRuta
        })) : null;
        if (ModelState.IsValid && error is null)
        {
            Correcto("Entrega preparada. Despáchala cuando la condición de pago esté resuelta.");
            return RedirectToAction(nameof(Index));
        }
        if (error is not null)
            ModelState.AddModelError("", error.Codigo switch
            {
                "PEDIDO_INCOMPLETO" => "Reserva toda la cantidad confirmada de cada producto antes de preparar la entrega.",
                "RUTA_INVALIDA" => "La ruta debe estar planificada y ser del mismo responsable.",
                _ => error.Message
            });
        return View(await Completar(m));
    }

    [HttpPost]
    public async Task<IActionResult> Despachar(Guid id)
    {
        var error = await IntentarConCodigo(async () => await api.Post<object>($"entregas/{id}/despachar"));
        if (error is null)
            Correcto("Entrega despachada. Ya aparece en la app del responsable para descargar.");
        else
            TempData["Error"] = error.Codigo switch
            {
                "CONDICION_PENDIENTE" => "Antes de despachar, registra la condición de pago acordada del pedido (Cobranza).",
                "PAGO_PENDIENTE" => "El pedido es al contado y todavía tiene saldo: el despacho al contado requiere el pago completo.",
                "CREDITO_NO_AUTORIZADO" => "El crédito no alcanza o ya venció. Revisa la condición de pago del pedido.",
                _ => error.Message
            };
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Reasignar(Guid id)
    {
        var e = (await api.Todos<EntregaDto>("entregas")).FirstOrDefault(x => x.Id == id);
        if (e is null)
            return NotFound();
        var pedidos = await Pedidos();
        return View(await CompletarReasignar(new ReasignarFormViewModel
        {
            Id = e.Id,
            VersionActual = e.Version,
            ResponsableId = e.ResponsableId,
            RutaId = e.RutaId,
            Descripcion = pedidos.TryGetValue(e.PedidoId, out var p) ? $"{p.Codigo} · {p.Cliente}" : "Entrega"
        }));
    }

    [HttpPost]
    public async Task<IActionResult> Reasignar(ReasignarFormViewModel m)
    {
        if (ModelState.IsValid && await Intentar(async () => await api.Post<object>($"entregas/{m.Id}/reasignar", new
        {
            m.ResponsableId,
            m.RutaId,
            m.VersionActual,
            Motivo = m.Motivo.Trim()
        })))
        {
            Correcto("Entrega reasignada. Las capturas hechas sobre la versión anterior quedarán en conflicto para revisión.");
            return RedirectToAction(nameof(Index));
        }
        return View(await CompletarReasignar(m));
    }

    /// <summary>Carga manual de la recepción (retiro en el local o respaldo del teléfono), con el contrato de sincronización.</summary>
    public async Task<IActionResult> Recepcion(Guid id)
    {
        var e = (await api.Todos<EntregaDto>("entregas")).FirstOrDefault(x => x.Id == id);
        if (e is null)
            return NotFound();
        var pedidos = await Pedidos();
        return View(new RecepcionFormViewModel
        {
            EntregaId = e.Id,
            VersionBase = e.Version,
            Descripcion = pedidos.TryGetValue(e.PedidoId, out var p) ? $"{p.Codigo} · {p.Cliente}" : "Entrega"
        });
    }

    [HttpPost, RequestSizeLimit(4_000_000)]
    public async Task<IActionResult> Recepcion(RecepcionFormViewModel m)
    {
        if (!m.Conforme && string.IsNullOrWhiteSpace(m.Observacion))
            ModelState.AddModelError(nameof(m.Observacion), "Si la recepción no es conforme, escribe la observación.");
        if (m.Fotos.Count is < 1 or > 3)
            ModelState.AddModelError(nameof(m.Fotos), "Adjunta de 1 a 3 fotos de evidencia.");
        if (decimal.Round(m.PesoRecibido, 3) != m.PesoRecibido)
            ModelState.AddModelError(nameof(m.PesoRecibido), "El peso admite hasta tres decimales.");
        var evidencias = new List<object>();
        foreach (var f in m.Fotos)
        {
            var tipo = f.ContentType is "image/png" ? "image/png" : f.ContentType is "image/jpeg" ? "image/jpeg" : null;
            if (tipo is null || f.Length > 524288)
            { ModelState.AddModelError(nameof(m.Fotos), "Cada foto debe ser JPEG o PNG de hasta 512 KiB."); break; }
            using var copia = new MemoryStream();
            await f.CopyToAsync(copia);
            evidencias.Add(new { Tipo = tipo, ContenidoBase64 = Convert.ToBase64String(copia.ToArray()) });
        }
        var autor = User.FindFirstValue("sub");
        if (ModelState.IsValid)
        {
            SincronizacionDto? r = null;
            if (await Intentar(async () => r = await api.Post<SincronizacionDto>("recepciones/sincronizar", new
            {
                Recepciones = new[] { new
                {
                    m.ClaveUnica,
                    m.EntregaId,
                    m.VersionBase,
                    AutorId = autor,
                    FechaReal = Hora.Utc(m.FechaReal),
                    m.PesoRecibido,
                    m.Unidad,
                    m.Conforme,
                    Receptor = m.Receptor.Trim(),
                    Observacion = m.Observacion?.Trim() ?? "",
                    Evidencias = evidencias
                } }
            })))
            {
                var estado = r?.Resultados.FirstOrDefault()?.Estado;
                Correcto(estado == "CONFLICTO"
                    ? "La recepción quedó en conflicto: la entrega cambió. Una persona autorizada debe revisarla."
                    : m.Conforme ? "Recepción confirmada: la entrega quedó ENTREGADA. El saldo del cliente no cambia."
                    : "Recepción confirmada con observaciones: la entrega quedó OBSERVADA. No es un rechazo.");
                return RedirectToAction(nameof(Index));
            }
        }
        m.Fotos = [];
        return View(m);
    }

    private async Task<Dictionary<Guid, PedidoDto>> Pedidos() =>
        User.HasClaim("permiso", "pedidos.consultar") ? (await api.Todos<PedidoDto>("pedidos")).ToDictionary(x => x.Id) : [];

    private async Task<EntregaFormViewModel> Completar(EntregaFormViewModel m)
    {
        var conEntrega = (await api.Todos<EntregaDto>("entregas")).Where(x => x.Estado != "CANCELADA").Select(x => x.PedidoId).ToHashSet();
        m.Pedidos = (await api.Todos<PedidoDto>("pedidos"))
            .Where(x => x.EstadoComercial == "CONFIRMADO" && (!conEntrega.Contains(x.Id) || x.Id == m.PedidoId))
            .OrderBy(x => x.FechaComprometida).ToList();
        m.Responsables = await api.Get<List<ResponsableDto>>("responsables-entrega");
        m.Rutas = (await api.Todos<RutaDto>("rutas")).Where(x => x.Estado == "PLANIFICADA").OrderBy(x => x.Fecha).ToList();
        return m;
    }

    private async Task<ReasignarFormViewModel> CompletarReasignar(ReasignarFormViewModel m)
    {
        m.Responsables = await api.Get<List<ResponsableDto>>("responsables-entrega");
        m.Rutas = (await api.Todos<RutaDto>("rutas")).Where(x => x.Estado == "PLANIFICADA").OrderBy(x => x.Fecha).ToList();
        return m;
    }
}

/// <summary>RNF-01 · revisión humana de los conflictos de sincronización. Nunca se resuelven solos.</summary>
[Authorize(Policy = "entregas.revisar")]
public class RecepcionesController(RepartoApi api) : GestionControllerBase
{
    public async Task<IActionResult> Conflictos(int pagina = 1, int tamanoPagina = 20)
    {
        var puede = User.HasClaim("permiso", "entregas.administrar");
        return View(new ConflictosViewModel
        {
            Datos = await api.Get<Pagina<RecepcionDto>>($"recepciones/conflictos?pagina={pagina}&tamanoPagina={tamanoPagina}"),
            PuedeVerEntregas = puede,
            Entregas = puede ? (await api.Todos<EntregaDto>("entregas")).ToDictionary(x => x.Id) : [],
            Pedidos = User.HasClaim("permiso", "pedidos.consultar") ? (await api.Todos<PedidoDto>("pedidos")).ToDictionary(x => x.Id) : []
        });
    }

    public async Task<IActionResult> Revisar(Guid id) => View(await Completar(new RevisarFormViewModel { Id = id }));

    [HttpPost]
    public async Task<IActionResult> Revisar(RevisarFormViewModel m)
    {
        var error = ModelState.IsValid ? await IntentarConCodigo(async () => await api.Post<object>($"recepciones/{m.Id}/revisar", new
        {
            m.Aplicar,
            m.VersionActual,
            Motivo = m.Motivo.Trim()
        })) : null;
        if (ModelState.IsValid && error is null)
        {
            Correcto(m.Aplicar ? "Captura aplicada: la entrega toma el resultado de la recepción." : "Captura descartada. Se conserva con su evidencia y el motivo.");
            return RedirectToAction(nameof(Conflictos));
        }
        if (error is not null)
            ModelState.AddModelError("", error.Codigo switch
            {
                "VERSION_CONFLICTO" => "La entrega cambió mientras revisabas. Vuelve a abrir la revisión.",
                "RECEPCION_NO_APLICABLE" => "No se puede aplicar: la entrega ya tiene una recepción o no está despachada. Puedes descartarla.",
                _ => error.Message
            });
        return View(await Completar(m));
    }

    public async Task<IActionResult> Evidencia(Guid id, Guid evidenciaId)
    {
        var archivo = await api.Archivo($"recepciones/{id}/evidencias/{evidenciaId}");
        if (archivo is null)
            return NotFound();
        Response.Headers.CacheControl = "no-store";
        return File(archivo.Value.Contenido, archivo.Value.Tipo);
    }

    private async Task<RevisarFormViewModel> Completar(RevisarFormViewModel m)
    {
        m.Detalle = await api.Get<RecepcionDetalleDto>($"recepciones/{m.Id}");
        if (User.HasClaim("permiso", "entregas.administrar"))
        {
            m.Entrega = (await api.Todos<EntregaDto>("entregas")).FirstOrDefault(x => x.Id == m.Detalle.Recepcion.EntregaId);
            if (m.Entrega is not null)
                m.VersionActual = m.Entrega.Version;
        }
        if (m.Entrega is not null && User.HasClaim("permiso", "pedidos.consultar"))
            m.Pedido = await api.Get<PedidoDto>($"pedidos/{m.Entrega.PedidoId}");
        return m;
    }
}
