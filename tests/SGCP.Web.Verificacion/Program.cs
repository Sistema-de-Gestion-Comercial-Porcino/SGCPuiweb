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
// Sprint 5 · P1 · reservas, incidencias y movimientos
var detA = new DetalleOperativoDto { Id = Guid.NewGuid(), Producto = "Lomo de cerdo", CantidadConfirmada = 5m, Unidad = "kg" };
var detB = new DetalleOperativoDto { Id = Guid.NewGuid(), Producto = "Costilla de cerdo", CantidadConfirmada = 2.5m, Unidad = "kg" };
var pedidoOp = new PedidoOperativoDto { Id = Guid.NewGuid(), Codigo = "PED-001", Detalles = [detA, detB] };
var reservas = new List<ReservaDto> {
    new() { DetallePedidoId = detA.Id, Cantidad = 2m, Estado = "ACTIVA" },
    new() { DetallePedidoId = detA.Id, Cantidad = 3m, Estado = "LIBERADA" },
    new() { DetallePedidoId = detB.Id, Cantidad = 2.5m, Estado = "ACTIVA" } };
var cola = PedidoPorPreparar.Armar([pedidoOp], reservas);
Check(cola[0].Detalles[0].Reservado == 2m && cola[0].Detalles[0].Falta == 3m, "Una reserva liberada no cuenta como reservada");
Check(cola[0].Detalles[1].Completo && !cola[0].Completo, "El pedido queda completo solo cuando todos sus productos están reservados");
var sobre = PedidoPorPreparar.Armar([pedidoOp], [new() { DetallePedidoId = detB.Id, Cantidad = 9m, Estado = "ACTIVA" }]);
Check(sobre[0].Detalles[1].Falta == 0m, "Lo que falta reservar nunca es negativo");
var rechazado = new AnimalDto { Id = Guid.NewGuid(), Identificacion = "CERDO-002" };
var vistaInc = new IncidenciasViewModel { Animales = new() { [rechazado.Id] = rechazado } };
Check(vistaInc.Origen(new IncidenciaDto { AnimalId = rechazado.Id }) == "Animal CERDO-002", "La incidencia muestra el animal por su identificación, no por su GUID");
var incApi = JsonSerializer.Deserialize<IncidenciaDto>("""{"id":"098e7092-907e-4956-9d67-6ed5f294e43d","loteId":null,"animalId":"deaf9920-0ddc-4593-9eef-67fad8531710","motivo":"Fiebre al ingreso","resuelta":false,"resolucion":"","resueltaEn":null}""", jsonWeb)!;
Check(incApi.AnimalId is not null && incApi.LoteId is null && !incApi.Resuelta && incApi.ResueltaEn is null, "Contrato de GET incidencias");
Check(MovimientosViewModel.Tipo("ANULACION_ENTRADA") == "Anulación de entrada" && MovimientosViewModel.Tipo("OTRO") == "OTRO", "Tipos de movimiento legibles, sin perder los desconocidos");
// Sprint 5 · P2 · S-1 · frío sobre un lote o sobre una entrega
List<System.ComponentModel.DataAnnotations.ValidationResult> ErroresFrio(FrioFormViewModel m) => m.Validate(new(m)).ToList();
Check(ErroresFrio(new() { Objetivo = FrioFormViewModel.ObjetivoEntrega }).Any(x => x.MemberNames.Contains(nameof(FrioFormViewModel.EntregaId)))
    && !ErroresFrio(new() { Objetivo = FrioFormViewModel.ObjetivoEntrega, EntregaId = Guid.NewGuid() }).Any(), "Frío en transporte exige la entrega y no pide lote");
Check(ErroresFrio(new() { Objetivo = FrioFormViewModel.ObjetivoLote }).Any(x => x.MemberNames.Contains(nameof(FrioFormViewModel.LoteId)))
    && ErroresFrio(new() { Objetivo = "OTRO", LoteId = Guid.NewGuid() }).Any(), "Frío en cámara exige el lote y no se acepta otro tipo de control");
// Revisión del Sprint 2 · frío de una cámara completa con la hora de inicio de la falla
var lectura = new DateTime(2026, 11, 26, 10, 0, 0);
Check(ErroresFrio(new() { Objetivo = FrioFormViewModel.ObjetivoCamara, Evaluacion = "RECHAZADO", Fecha = lectura }).Any(x => x.MemberNames.Contains(nameof(FrioFormViewModel.InicioFalla)))
    && !ErroresFrio(new() { Objetivo = FrioFormViewModel.ObjetivoCamara, Evaluacion = "RECHAZADO", Fecha = lectura, InicioFalla = lectura.AddHours(-3) }).Any()
    && !ErroresFrio(new() { Objetivo = FrioFormViewModel.ObjetivoCamara, Evaluacion = "APTO", Fecha = lectura }).Any(),
    "Rechazar una cámara completa exige la hora desde la que estuvo fuera de temperatura; una lectura apta no");
Check(ErroresFrio(new() { Objetivo = FrioFormViewModel.ObjetivoLote, LoteId = Guid.NewGuid(), Fecha = lectura, InicioFalla = lectura.AddMinutes(5) }).Any(x => x.MemberNames.Contains(nameof(FrioFormViewModel.InicioFalla))),
    "El inicio de la falla no puede ser posterior a la lectura");
Check(SGCP.Web.Services.Nombres.Responsable([], Guid.NewGuid()) == "Personal del sistema",
    "Un responsable sin nombre conocido no muestra un fragmento de su identificador");
var existenciaApi = JsonSerializer.Deserialize<ExistenciaDto>("""{"id":"098e7092-907e-4956-9d67-6ed5f294e43d","procedencia":"Granja Norte","orden":"OP-0003","ingreso":"2026-11-26T14:00:00Z","ingresoResponsableId":"deaf9920-0ddc-4593-9eef-67fad8531710"}""", jsonWeb)!;
Check(existenciaApi.Procedencia == "Granja Norte" && existenciaApi.Orden == "OP-0003" && existenciaApi.Ingreso is not null && existenciaApi.IngresoResponsableId is not null,
    "Contrato de GET existencias con la procedencia, la orden y quién ingresó el lote a la cámara");
var frioApi = JsonSerializer.Deserialize<MisEntregasDto>("""{"entregas":[{"id":"098e7092-907e-4956-9d67-6ed5f294e43d","version":1,"pedidoId":"deaf9920-0ddc-4593-9eef-67fad8531710","pedido":"PED-018","rutaId":null,"fechaPrevista":"2026-11-25T23:18:13Z","estado":"DESPACHADA","cliente":"Restaurante S5","direccion":"Av. Cañoto 456","telefono":"70011122"}],"detalles":[]}""", jsonWeb)!;
Check(frioApi.Entregas.Single().Pedido == "PED-018" && frioApi.Entregas[0].Cliente == "Restaurante S5", "Contrato de GET mis-entregas para nombrar la entrega controlada");
// Sprint 5 · P2 · decimales: la coma nunca es separador de miles («6,2» no puede volverse 62)
Check(SGCP.Web.Models.EnlaceDecimalInvariante.Leer("6.2") == 6.2m && SGCP.Web.Models.EnlaceDecimalInvariante.Leer("6,2") == 6.2m
    && SGCP.Web.Models.EnlaceDecimalInvariante.Leer(" 10.25 ") == 10.25m && SGCP.Web.Models.EnlaceDecimalInvariante.Leer("-3,5") == -3.5m, "Decimal con punto o con una coma decimal");
Check(SGCP.Web.Models.EnlaceDecimalInvariante.Leer("1.000,5") is null && SGCP.Web.Models.EnlaceDecimalInvariante.Leer("1,000,5") is null
    && SGCP.Web.Models.EnlaceDecimalInvariante.Leer("1,000.5") is null && SGCP.Web.Models.EnlaceDecimalInvariante.Leer("abc") is null, "Separadores de miles o mezclas ambiguas se rechazan en lugar de multiplicar el número");
Console.WriteLine($"RESULTADO WEB: {numero} comprobaciones de compatibilidad correctas.");
if (args.Contains("--seguimientos")) await SeguimientosIntegracion.Ejecutar();
