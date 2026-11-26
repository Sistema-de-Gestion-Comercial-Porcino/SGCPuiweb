using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

/// <summary>
/// Ejecutar con --seguimientos, la API del verificador --servidor en 5091
/// y la web en Development en 5092 apuntando a esa API aislada. Nunca usa sgcp.
/// </summary>
public static class SeguimientosIntegracion
{
    public static async Task Ejecutar()
    {
        using var api = new HttpClient { BaseAddress = new Uri("http://127.0.0.1:5091/api/v1/") };
        using var web = Navegador();
        var n = 0;
        void Check(bool ok, string nombre) { if (!ok) throw new Exception("FALLÓ SEGUIMIENTOS: " + nombre); Console.WriteLine($"OK SEGUIMIENTOS {++n} · {nombre}"); }
        async Task<JsonNode> Api(string method, string ruta, object? cuerpo = null)
        {
            using var req = new HttpRequestMessage(new HttpMethod(method), ruta);
            if (cuerpo is not null) req.Content = JsonContent.Create(cuerpo);
            using var r = await api.SendAsync(req);
            var texto = await r.Content.ReadAsStringAsync();
            if (!r.IsSuccessStatusCode) throw new Exception($"{ruta}: {r.StatusCode} {texto}");
            return string.IsNullOrEmpty(texto) ? new JsonObject() : JsonNode.Parse(texto)!;
        }
        string Id(JsonNode nodo) => nodo["id"]!.ToString();
        var sesion = await Api("POST", "auth/login", new { login = "admin", clave = "Prueba-local-2026!" });
        api.DefaultRequestHeaders.Authorization = new("Bearer", sesion["token"]!.ToString());
        var roles = (await Api("GET", "roles")).AsArray().ToDictionary(x => x!["nombre"]!.ToString(), x => Id(x!));
        var sufijo = Guid.NewGuid().ToString("N")[..8];
        var loginFinanzas = "finanzas." + sufijo;
        var loginAdmin = "administrador." + sufijo;
        const string clave = "Cobranza-prueba-2026!";
        foreach (var cuenta in new[] { (loginFinanzas, new[] { "FINANZAS" }), (loginAdmin, new[] { "ADMINISTRADOR", "VENTAS" }) })
            await Api("POST", "usuarios", new { nombre = cuenta.Item1, login = cuenta.Item1, correo = cuenta.Item1 + "@prueba.local", clave, roles = cuenta.Item2.Select(x => roles[x]).ToArray() });
        var cliente = await Api("POST", "clientes", new { nombre = "Cliente de prueba seguimiento " + sufijo, tipo = "Persona", telefono = "70000000", identificacionFiscal = "CI" + sufijo, condicionHabitual = "CONTADO", direccion = "Prueba aislada", observaciones = "" });
        var clienteId = Id(cliente);
        var producto = (await Api("GET", "productos"))["elementos"]!.AsArray().First()!;
        var productoId = Id(producto);
        await Api("POST", $"productos/{productoId}/precios", new { costoReferencia = 28m, precioVenta = 35m, vigenteDesde = DateTime.UtcNow.AddMinutes(-5) });
        var cotizacion = await Api("POST", "cotizaciones", new { clienteId, canal = "LOCAL", vigenteHasta = DateTime.UtcNow.AddDays(1), detalles = new[] { new { productoId, cantidad = 2m, unidad = producto["unidadVenta"]!.ToString(), precioUnitario = 35m } } });
        await Api("POST", $"cotizaciones/{Id(cotizacion)}/presentar");
        await Api("POST", $"cotizaciones/{Id(cotizacion)}/aceptar");
        var pedido = await Api("POST", "pedidos", new { cotizacionId = Id(cotizacion), fechaComprometida = DateTime.UtcNow.AddDays(1), condicionPago = "CREDITO_ACORDADO" });
        var pedidoId = Id(pedido);
        var condicion = await Api("POST", $"pedidos/{pedidoId}/condiciones-pago", new { modalidad = "CREDITO_ACORDADO", importeAutorizado = 70m, vencimiento = DateTime.UtcNow.AddDays(1) });
        var condicionId = Id(condicion);
        await Login(web, loginFinanzas, clave);
        Check((await web.GetAsync("/Cobranza")).StatusCode == HttpStatusCode.OK && (await web.GetAsync("/Precios")).StatusCode == HttpStatusCode.OK,
            "FINANZAS sola abre Cobranza y Precios");
        using var administrador = Navegador();
        await Login(administrador, loginAdmin, clave);
        foreach (var ruta in new[] { "/Seguimientos", "/Cobranza", "/Precios" })
        {
            using var denegado = await administrador.GetAsync(ruta);
            Check(denegado.StatusCode == HttpStatusCode.Found && denegado.Headers.Location!.OriginalString.Contains("SinPermiso"), "Administrador sin autorización financiera: " + ruta);
        }
        var crearRuta = $"/Seguimientos/Crear?pedidoId={pedidoId}&condicionPagoId={condicionId}";
        var crear = await web.GetStringAsync(crearRuta);
        var fechaLocal = DateTime.UtcNow.AddHours(-4).AddDays(1).ToString("yyyy-MM-ddTHH:mm");
        var campos = new Dictionary<string, string> { ["PedidoId"] = pedidoId, ["CondicionPagoId"] = condicionId, ["ProximaFecha"] = fechaLocal };
        using(var sinToken = await web.PostAsync("/Seguimientos/Crear", new FormUrlEncodedContent(campos)))
            Check(sinToken.StatusCode == HttpStatusCode.BadRequest, "Crear exige antifalsificación");
        campos["__RequestVerificationToken"] = Token(crear);
        using(var r = await web.PostAsync("/Seguimientos/Crear", new FormUrlEncodedContent(campos)))
            Check(r.StatusCode == HttpStatusCode.Found, "Crear seguimiento desde crédito por formulario");
        var seguimiento = (await Api("GET", $"seguimientos-cobro?clienteId={clienteId}&estado=PENDIENTE"))["elementos"]!.AsArray().Single()!;
        var id = Id(seguimiento);
        var paginaRuta = $"/Seguimientos?clienteId={clienteId}&estado=PENDIENTE";
        var pagina = await web.GetStringAsync(paginaRuta);
        var visible = WebUtility.HtmlDecode(Regex.Replace(pagina, "<[^>]*>", " "));
        Check(visible.Contains(cliente["nombre"]!.ToString()) && visible.Contains("Vencimiento") && !visible.Contains(clienteId) && !visible.Contains(condicionId) && !visible.Contains(id),
            "La lista muestra etiquetas y fechas sin GUID visibles");
        Check(HoraCorrecta(seguimiento["proximaFecha"]!.ToString(), fechaLocal), "Fecha de Bolivia convertida correctamente a UTC");
        var gestionRuta = $"/Seguimientos/Gestion?id={id}&clienteId={clienteId}";
        var gestion = await web.GetStringAsync(gestionRuta);
        var camposGestion = new Dictionary<string, string> { ["Id"] = id, ["ClienteId"] = clienteId, ["Resultado"] = "Llamé, paga el lunes", ["__RequestVerificationToken"] = Token(gestion) };
        using(var faltaFecha = await web.PostAsync("/Seguimientos/Gestion", new FormUrlEncodedContent(camposGestion)))
            Check(faltaFecha.StatusCode == HttpStatusCode.OK && WebUtility.HtmlDecode(await faltaFecha.Content.ReadAsStringAsync()).Contains("Con saldo pendiente"), "Gestión con deuda requiere próxima fecha");
        Check((await Api("GET", $"seguimientos-cobro?clienteId={clienteId}"))["elementos"]![0]!["fechaGestion"] is null, "Una validación fallida no guarda la gestión");
        camposGestion["ProximaFecha"] = DateTime.UtcNow.AddHours(-4).AddDays(4).ToString("yyyy-MM-ddTHH:mm");
        using(var r = await web.PostAsync("/Seguimientos/Gestion", new FormUrlEncodedContent(camposGestion)))
            Check(r.StatusCode == HttpStatusCode.Found, "Gestión y nueva fecha registradas desde la web");
        pagina = await web.GetStringAsync(paginaRuta);
        Check(WebUtility.HtmlDecode(pagina).Contains("Llamé, paga el lunes"), "La lista conserva el resultado de la última gestión");
        var cerrar = new Dictionary<string, string> { ["id"] = id, ["clienteId"] = clienteId, ["__RequestVerificationToken"] = Token(pagina) };
        using(var r = await web.PostAsync("/Seguimientos/Cerrar", new FormUrlEncodedContent(cerrar)))
            Check(r.StatusCode == HttpStatusCode.Found, "El intento de cierre vuelve a la lista");
        Check(WebUtility.HtmlDecode(await web.GetStringAsync(paginaRuta)).Contains("No se cierra el seguimiento"), "La web explica el rechazo de cierre con saldo");
        Check((await Api("GET", $"saldos?pedidoId={pedidoId}"))["total"]!.GetValue<decimal>() == 70m, "Una promesa no modifica la deuda");
        var cargo = (await Api("GET", $"cargos?pedidoId={pedidoId}"))["elementos"]!.AsArray().Single()!;
        await Api("POST", "pagos", new { clienteId, referenciaUnica = "PAGO-SEGUIMIENTO-" + sufijo, importe = 70m, fecha = DateTime.UtcNow, medio = "Transferencia", aplicaciones = new[] { new { cargoId = Id(cargo), importe = 70m } } });
        using(var r = await web.PostAsync("/Seguimientos/Cerrar", new FormUrlEncodedContent(cerrar)))
            Check(r.StatusCode == HttpStatusCode.Found, "Cierre desde web después de pagar");
        var cerrado = (await Api("GET", $"seguimientos-cobro?clienteId={clienteId}&estado=CERRADO"))["elementos"]!.AsArray().Single()!;
        Check(Id(cerrado) == id, "Filtro CERRADO recupera el seguimiento finalizado");
        Check((await Api("GET", $"seguimientos-cobro?clienteId={Guid.NewGuid()}"))["totalRegistros"]!.GetValue<int>() == 0,
            "El filtro de otro cliente no devuelve seguimientos ajenos");
        var listaCerrados = await web.GetStringAsync($"/Seguimientos?clienteId={clienteId}&estado=CERRADO");
        Check(!listaCerrados.Contains("Registrar gestión") && !listaCerrados.Contains("Cerrar seguimiento"), "El seguimiento cerrado no muestra acciones de modificación");
        using(var r = await web.PostAsync("/Seguimientos/Gestion", new FormUrlEncodedContent(camposGestion)))
            Check(r.StatusCode == HttpStatusCode.OK && WebUtility.HtmlDecode(await r.Content.ReadAsStringAsync()).Contains("no admite otra gestión"), "POST directo tampoco modifica un seguimiento cerrado");
        var intacto = (await Api("GET", $"seguimientos-cobro?clienteId={clienteId}&estado=CERRADO"))["elementos"]!.AsArray().Single()!;
        Check(intacto["resultado"]!.ToString() == "Llamé, paga el lunes" && intacto["fechaGestion"]!.ToString() == cerrado["fechaGestion"]!.ToString(),
            "Un intento posterior al cierre conserva resultado y fecha originales");
        var eventos = (await Api("GET", "auditoria?tamanoPagina=100"))["elementos"]!.AsArray().Where(x => x!["entidadId"]!.ToString() == id).ToList();
        Check(new[] { "CREAR_SEGUIMIENTO_COBRO", "REGISTRAR_GESTION_COBRO", "CERRAR_SEGUIMIENTO_COBRO" }.All(op => eventos.Any(x => x!["operacion"]!.ToString() == op)), "Alta, gestión y cierre quedan en auditoría");
        Check((await web.GetAsync("/Seguimientos?estado=OTRO")).StatusCode == HttpStatusCode.BadRequest, "Un filtro inválido se rechaza sin error de servidor");
        Console.WriteLine($"RESULTADO SEGUIMIENTOS: {n} comprobaciones con API y MySQL aislados.");
        Console.WriteLine($"CUENTA VISUAL DE PRUEBA: {loginFinanzas}; cliente sintético {sufijo}. La clave está definida en este verificador, no pertenece a datos reales.");
    }

    private static HttpClient Navegador() => new(new HttpClientHandler { AllowAutoRedirect = false, CookieContainer = new CookieContainer() }) { BaseAddress = new Uri("http://127.0.0.1:5092") };
    private static string Token(string html) => WebUtility.HtmlDecode(Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
    private static bool HoraCorrecta(string utc, string local) => DateTime.Parse(utc).ToUniversalTime() == DateTime.SpecifyKind(DateTime.Parse(local).AddHours(4), DateTimeKind.Utc);
    private static async Task Login(HttpClient web, string login, string clave)
    {
        var pagina = await web.GetStringAsync("/Cuenta/IniciarSesion");
        using var r = await web.PostAsync("/Cuenta/IniciarSesion", new FormUrlEncodedContent(new Dictionary<string, string> { ["Login"] = login, ["Clave"] = clave, ["__RequestVerificationToken"] = Token(pagina) }));
        if(r.StatusCode != HttpStatusCode.Found) throw new Exception("No se pudo iniciar sesión web de prueba.");
    }
}
