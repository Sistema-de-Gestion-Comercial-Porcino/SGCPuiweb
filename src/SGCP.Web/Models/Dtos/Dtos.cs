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
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }
}
public class CotizacionDto
{
    public Guid Id { get; set; }
    public string Codigo { get; set; } = "";
    public int Version { get; set; }
    public string Estado { get; set; } = "BORRADOR";
    public DateTime Fecha { get; set; }
    public DateTime VigenteHasta { get; set; }
    public string Canal { get; set; } = "LOCAL";
    public Guid ClienteId { get; set; }
    public string Cliente { get; set; } = "";
    public string MotivoRespuesta { get; set; } = "";
    public Guid? CotizacionOrigenId { get; set; }
    public Guid? PedidoId { get; set; }
    public CotizacionDto? VersionPosterior { get; set; }
    public decimal Total { get; set; }
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
