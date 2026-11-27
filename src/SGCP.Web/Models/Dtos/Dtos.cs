namespace SGCP.Web.Models.Dtos;

public class Pagina<T>
{
    public List<T> Elementos { get; set; } = []
    ;
    public int PaginaActual { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("pagina")]
    public int PaginaNumero { get; set; }
    public int TamanoPagina { get; set; } = 20;
    public int TotalRegistros { get; set; }
    public int TotalPaginas { get; set; }
}
public class OpcionDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = "";
    public string[]
    Permisos
    { get; set; } = []
    ;
    public string Descripcion { get; set; } = "";
}
public class UsuarioDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Login { get; set; } = "";
    public string Correo { get; set; } = "";
    public string? Telefono { get; set; }
    public string Estado { get; set; } = "";
    public DateTime? UltimoAcceso { get; set; }
    public List<OpcionDto> Roles { get; set; } = []
    ;
}
public class PerfilDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Login { get; set; } = "";
    public string[]
    Roles
    { get; set; } = []
    ;
    public string[]
    Permisos
    { get; set; } = []
    ;
    public Guid? ClienteId { get; set; }
}
public class AccesoDto { public string Token { get; set; } = ""; public DateTime Expira { get; set; } public PerfilDto Usuario { get; set; } = new(); }
public class ClienteDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Tipo { get; set; } = "";
    public string Telefono { get; set; } = "";
    public string Direccion { get; set; } = "";
    public string IdentificacionFiscal { get; set; } = "";
    public string TipoDocumento { get; set; } = "SIN_ESPECIFICAR";
    public string CondicionHabitual { get; set; } = "CONTADO";
    public bool Activo { get; set; }
    public string Observaciones { get; set; } = "";
}
public class ClasificacionDto
{
    public Guid Id { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string Familia { get; set; } = "CARNE_FRESCA";
    public string Descripcion { get; set; } = "";
    public bool Activo { get; set; } = true;
}
public class PrecioDto
{
    public Guid Id { get; set; }
    public Guid ProductoId { get; set; }
    public decimal? CostoReferencia { get; set; }
    public decimal PrecioVenta { get; set; }
    public DateTime VigenteDesde { get; set; }
    public DateTime? VigenteHasta { get; set; }
    public decimal Margen { get; set; }
}
public class ProductoDto
{
    public Guid Id { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string Presentacion { get; set; } = "";
    public string UnidadVenta { get; set; } = "kg";
    public string Descripcion { get; set; } = "";
    public bool Activo { get; set; }
    public Guid ClasificacionProductoId { get; set; }
    public ClasificacionDto Clasificacion { get; set; } = new()
    ;
    public PrecioDto? PrecioVigente { get; set; }
}
public class DetalleCotizacionDto
{
    public Guid ProductoId { get; set; }
    public string Producto { get; set; } = "";
    public decimal Cantidad { get; set; }
    public string Unidad { get; set; } = "kg";
    public decimal? PrecioUnitario { get; set; }
    public decimal? Subtotal { get; set; }
    public decimal? PrecioReferencia { get; set; }
    public decimal? SubtotalReferencia => PrecioReferencia is decimal precio
        ? decimal.Round(Cantidad * precio, 2, MidpointRounding.AwayFromZero) : null;
}
public class CotizacionDto
{
    public Guid Id { get; set; }
    public string Codigo { get; set; } = "";
    public int Version { get; set; }
    public string Estado { get; set; } = "BORRADOR";
    public DateTime Fecha { get; set; }
    public DateTime? VigenteHasta { get; set; }
    public bool PendienteRevision => Estado == "BORRADOR" && VigenteHasta is null;
    public string Canal { get; set; } = "LOCAL";
    public Guid ClienteId { get; set; }
    public string Cliente { get; set; } = "";
    public string MotivoRespuesta { get; set; } = "";
    public Guid? CotizacionOrigenId { get; set; }
    public Guid? PedidoId { get; set; }
    public CotizacionDto? VersionPosterior { get; set; }
    public decimal? Total { get; set; }
    public decimal? TotalReferencia => Detalles.Count > 0 && Detalles.All(x => x.SubtotalReferencia.HasValue)
        ? Detalles.Sum(x => x.SubtotalReferencia ?? 0) : null;
    public List<DetalleCotizacionDto> Detalles { get; set; } = []
    ;
}
public class DetallePedidoDto
{
    public string Producto { get; set; } = "";
    public decimal CantidadConfirmada { get; set; }
    public string Unidad { get; set; } = "";
    public decimal PrecioAcordado { get; set; }
    public decimal? CostoReferenciaAcordado { get; set; }
    public decimal Subtotal { get; set; }
    public string Estado { get; set; } = "";
}
public class PedidoDto
{
    public Guid Id { get; set; }
    public string Codigo { get; set; } = "";
    public DateTime Fecha { get; set; }
    public DateTime FechaComprometida { get; set; }
    public string EstadoComercial { get; set; } = "";
    public string EstadoPreparacion { get; set; } = "";
    public string CondicionPago { get; set; } = "";
    public Guid ClienteId { get; set; }
    public string Cliente { get; set; } = "";
    public Guid CotizacionId { get; set; }
    public string Cotizacion { get; set; } = "";
    public string MotivoCancelacion { get; set; } = "";
    public decimal? CostoEstimadoConfirmado { get; set; }
    public decimal? MargenEstimadoConfirmado { get; set; }
    public decimal Total { get; set; }
    public List<DetallePedidoDto> Detalles { get; set; } = []
    ;
}
public class ResumenDto
{
    public int? ClientesActivos { get; set; }
    public int? ProductosActivos { get; set; }
    public int? CotizacionesVigentes { get; set; }
    public int? PedidosConfirmados { get; set; }
    public List<PedidoDto> UltimosPedidos { get; set; } = []
    ;
}
public class CreadoDto { public Guid Id { get; set; } }

// Sprint 2 · producción, sanidad, almacén, frío y cargos (S2-P2).
public class AnimalDto
{
    public Guid Id { get; set; }
    public string Identificacion { get; set; } = "";
    public string Procedencia { get; set; } = "";
    public string Proveedor { get; set; } = "";
    public decimal PesoIngreso { get; set; }
    public string Estado { get; set; } = "PENDIENTE";
    public DateTime FechaIngreso { get; set; }
    public bool Activo { get; set; }
}
public class ControlSanitarioDto
{
    public Guid Id { get; set; }
    public Guid AnimalId { get; set; }
    public string Resultado { get; set; } = "";
    public string Observacion { get; set; } = "";
    public Guid ResponsableId { get; set; }
    public DateTime Fecha { get; set; }
}
public class OrdenProduccionDto
{
    public Guid Id { get; set; }
    public string Codigo { get; set; } = "";
    public string Tipo { get; set; } = "FAENEO";
    public string Estado { get; set; } = "PLANIFICADA";
    public Guid? AnimalId { get; set; }
    public Guid ResponsableId { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime InicioPrevisto { get; set; }
    public DateTime FinPrevisto { get; set; }
    public DateTime? InicioReal { get; set; }
    public DateTime? FinReal { get; set; }
    public string Observacion { get; set; } = "";
}
public class PrevisionDto
{
    public Guid Id { get; set; }
    public Guid ProductoId { get; set; }
    public decimal CantidadEstimada { get; set; }
    public string Unidad { get; set; } = "";
}
public class ResultadoProduccionDto
{
    public Guid Id { get; set; }
    public Guid ProductoId { get; set; }
    public string Producto { get; set; } = "";
    public string TipoCarneODerivado { get; set; } = "";
    public string Familia { get; set; } = "";
    public Guid LoteId { get; set; }
    public string Lote { get; set; } = "";
    public string Estado { get; set; } = "";
    public decimal? CantidadReal { get; set; }
    public string Unidad { get; set; } = "";
}
public class InsumoDto
{
    public Guid Id { get; set; }
    public Guid ReferenciaUnica { get; set; }
    public Guid ExistenciaId { get; set; }
    public decimal Cantidad { get; set; }
    public string Unidad { get; set; } = "";
    public Guid ResponsableId { get; set; }
    public DateTime Fecha { get; set; }
}
public class OrdenDetalleDto
{
    public OrdenProduccionDto Orden { get; set; } = new();
    public List<PrevisionDto> Previsiones { get; set; } = [];
    public List<ResultadoProduccionDto> Resultados { get; set; } = [];
    public List<InsumoDto> Insumos { get; set; } = [];
}
public class LoteDto
{
    public Guid Id { get; set; }
    public string Codigo { get; set; } = "";
    public Guid ProductoId { get; set; }
    public Guid? OrdenProduccionId { get; set; }
    public DateTime FechaProduccion { get; set; }
    public DateTime Vencimiento { get; set; }
    public string Unidad { get; set; } = "";
    public string Procedencia { get; set; } = "";
    public string ReferenciaExterna { get; set; } = "";
    public string Estado { get; set; } = "APTO";
}
public class PesajeDto
{
    public Guid Id { get; set; }
    public Guid LoteId { get; set; }
    public Guid ProductoId { get; set; }
    public Guid ResponsableId { get; set; }
    public decimal PesoBruto { get; set; }
    public decimal Tara { get; set; }
    public decimal Neto { get; set; }
    public string Unidad { get; set; } = "";
    public DateTime Fecha { get; set; }
    public string Observacion { get; set; } = "";
}
public class EntradaInventarioDto
{
    public Guid Id { get; set; }
    public Guid LoteId { get; set; }
    public Guid ProductoId { get; set; }
    public Guid PesajeId { get; set; }
    public Guid ExistenciaId { get; set; }
    public Guid ResponsableId { get; set; }
    public DateTime Fecha { get; set; }
    public decimal Cantidad { get; set; }
    public bool Anulada { get; set; }
    public DateTime? AnuladaEn { get; set; }
    public string MotivoAnulacion { get; set; } = "";
}
public class NombreUsuarioDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = "";
}
public class ExistenciaDto
{
    public Guid Id { get; set; }
    public Guid ProductoId { get; set; }
    public string Producto { get; set; } = "";
    public Guid LoteId { get; set; }
    public string Lote { get; set; } = "";
    public string Unidad { get; set; } = "";
    public string Ubicacion { get; set; } = "";
    public decimal CantidadFisica { get; set; }
    public string Estado { get; set; } = "";
    public DateTime Vencimiento { get; set; }
    public decimal Reservada { get; set; }
    public decimal Disponible { get; set; }
    public string Procedencia { get; set; } = "";
    public string? Orden { get; set; }
    public DateTime? Ingreso { get; set; }
    public Guid? IngresoResponsableId { get; set; }
}
public class ControlFrioDto
{
    public Guid Id { get; set; }
    public Guid? EntregaId { get; set; }
    public Guid? LoteId { get; set; }
    public Guid ResponsableId { get; set; }
    public string Unidad { get; set; } = "°C";
    public decimal Temperatura { get; set; }
    public string Ubicacion { get; set; } = "";
    public string Evaluacion { get; set; } = "";
    public string Observacion { get; set; } = "";
    public DateTime Fecha { get; set; }
    public DateTime? InicioFalla { get; set; }
}
public class CargoDto
{
    public Guid Id { get; set; }
    public Guid PedidoId { get; set; }
    public Guid? DetallePedidoId { get; set; }
    public string Tipo { get; set; } = "PRODUCTO";
    public string Concepto { get; set; } = "";
    public decimal Importe { get; set; }
    public string AcuerdoServicio { get; set; } = "";
    public DateTime? PrestadoEn { get; set; }
    public DateTime Fecha { get; set; }
    public bool Anulado { get; set; }
    public string MotivoAnulacion { get; set; } = "";
}

// Sprint 5 · P1. Contratos de GET pedidos-operativos, reservas, incidencias y movimientos-stock.
public class PedidoOperativoDto
{
    public Guid Id { get; set; }
    public string Codigo { get; set; } = "";
    public DateTime FechaComprometida { get; set; }
    public string EstadoPreparacion { get; set; } = "";
    public string Cliente { get; set; } = "";
    public List<DetalleOperativoDto> Detalles { get; set; } = [];
}
public class DetalleOperativoDto
{
    public Guid Id { get; set; }
    public Guid ProductoId { get; set; }
    public string Producto { get; set; } = "";
    public decimal CantidadConfirmada { get; set; }
    public string Unidad { get; set; } = "";
    public string Estado { get; set; } = "";
}
public class ReservaDto
{
    public Guid Id { get; set; }
    public Guid DetallePedidoId { get; set; }
    public Guid ExistenciaId { get; set; }
    public Guid ResponsableId { get; set; }
    public decimal Cantidad { get; set; }
    public string Estado { get; set; } = "";
    public DateTime Fecha { get; set; }
    public string MotivoLiberacion { get; set; } = "";
}
public class IncidenciaDto
{
    public Guid Id { get; set; }
    public Guid? LoteId { get; set; }
    public Guid? AnimalId { get; set; }
    public Guid ResponsableId { get; set; }
    public string Motivo { get; set; } = "";
    public DateTime Fecha { get; set; }
    public bool Resuelta { get; set; }
    public string Resolucion { get; set; } = "";
    public DateTime? ResueltaEn { get; set; }
}
public class MovimientoStockDto
{
    public Guid Id { get; set; }
    public Guid ExistenciaId { get; set; }
    public Guid ResponsableId { get; set; }
    public Guid ReferenciaId { get; set; }
    public string Tipo { get; set; } = "";
    public decimal Cantidad { get; set; }
    public DateTime Fecha { get; set; }
    public string Motivo { get; set; } = "";
}
