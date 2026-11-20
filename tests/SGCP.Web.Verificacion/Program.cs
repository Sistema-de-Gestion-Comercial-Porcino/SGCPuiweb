using System.Security.Claims;
using System.Text.Json;
using SGCP.Web.Models.Dtos;
using SGCP.Web.Models.ViewModels;

var numero = 0;
void Check(bool cumple, string nombre) { if (!cumple) throw new Exception(nombre); Console.WriteLine($"OK WEB {++numero} · {nombre}"); }
JsonElement Celda(object? valor) => JsonSerializer.SerializeToElement(valor);
var clienteId = Guid.NewGuid(); var entregaId = Guid.NewGuid();
var clientes = new List<ClienteDto> { new() { Id=clienteId, Nombre="Cliente de prueba", IdentificacionFiscal="123456" } };
var anterior = new TablaReporteDto { Titulo="Clientes", Columnas=["Id","Nombre"], Filas=[[Celda(clienteId),Celda("Cliente de prueba")]] };
anterior.Normalizar(clientes);
Check(anterior.Columnas[0]=="Documento (CI/NIT)" && anterior.Filas[0][0].GetString()=="123456", "API anterior: documento sustituye al GUID");
anterior.Normalizar(clientes);
Check(anterior.Filas[0][0].GetString()=="123456", "Normalización idempotente");
var sinDocumento = new TablaReporteDto { Titulo="Clientes", Columnas=["Id"], Filas=[[Celda(Guid.NewGuid())]] };
sinDocumento.Normalizar([]);
Check(sinDocumento.Filas[0][0].GetString()=="Sin registrar", "Nunca se usa GUID como sustituto del documento");
var entregas = new TablaReporteDto { Titulo="Entregas", Columnas=["Pedido","Estado","Entrega"],
    Filas=[[Celda("PED-001"),Celda("ENTREGADA"),Celda(entregaId)],[Celda("PED-002"),Celda("OBSERVADA"),Celda(Guid.NewGuid())]] };
entregas.Normalizar([]);
Check(!entregas.Columnas.Contains("Entrega") && entregas.Filas.All(f=>f.Count==2), "Columna interna eliminada de la tabla visible");
Check(entregas.Constancias?.Count==2 && entregas.Constancias[0]==entregaId && entregas.Constancias[1] is null, "Enlace de constancia conservado solo para entrega confirmada");
var actual = new TablaReporteDto { Titulo="Entregas", Columnas=["Pedido","Estado"], Filas=[[Celda("PED-001"),Celda("ENTREGADA")]], Constancias=[entregaId] };
actual.Normalizar([]);
Check(actual.Constancias![0]==entregaId && actual.Columnas.Count==2, "Contrato nuevo conserva metadatos sin columnas internas");
ClaimsPrincipal Actor(params string[] roles) => new(new ClaimsIdentity(roles.Select(r=>new Claim("rol",r)),"test","name","rol"));
Check(AccesoPerfiles.PuedeGestionar(Actor("ADMINISTRACION"),new UsuarioDto {Roles=[new(){Nombre="VENTAS"}]}), "Web nueva conserva gestión al consumir la API anterior");
Check(!AccesoPerfiles.PuedeGestionar(Actor("ADMINISTRADOR"),new UsuarioDto {Roles=[new(){Nombre="PROPIETARIO"}]}), "Administrador no puede gestionar propietario");
var jsonWeb = new JsonSerializerOptions(JsonSerializerDefaults.Web);
var oferta = JsonSerializer.Deserialize<CotizacionDto>("""{"estado":"PRESENTADA","vigenteHasta":"2026-11-21T12:00:00Z","total":12.50,"detalles":[{"precioUnitario":5,"subtotal":12.50}]}""", jsonWeb)!;
Check(!oferta.PendienteRevision && oferta.Total == 12.50m && oferta.Detalles[0].PrecioUnitario == 5m, "Contrato anterior de cotización preparada conserva sus importes");
var solicitud = JsonSerializer.Deserialize<CotizacionDto>("""{"estado":"BORRADOR","vigenteHasta":null,"total":null,"detalles":[{"precioUnitario":null,"subtotal":null}]}""", jsonWeb)!;
Check(solicitud.PendienteRevision && solicitud.Total is null && solicitud.Detalles[0].PrecioUnitario is null, "Solicitud sin preparar admite fechas e importes pendientes");
solicitud.Detalles = [new() { Cantidad = 2.5m, PrecioReferencia = 5m }];
Check(solicitud.TotalReferencia == 12.50m && solicitud.Total is null, "Precio de catálogo muestra estimación sin convertirla en oferta aceptable");
solicitud.Detalles.Add(new() { Cantidad = 1m });
Check(solicitud.TotalReferencia is null, "Producto sin precio no se suma como si fuera gratuito");
Console.WriteLine($"RESULTADO WEB: {numero} comprobaciones de compatibilidad correctas.");
