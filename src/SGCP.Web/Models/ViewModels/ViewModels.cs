using System.ComponentModel.DataAnnotations;
using SGCP.Web.Models.Dtos;
namespace SGCP.Web.Models.ViewModels;

public class AccesoViewModel
{
    [Required(ErrorMessage = "Escribe tu usuario.")]
    public string Login { get; set; } = "";
    [Required(ErrorMessage = "Escribe tu contraseña.")]
    public string Clave { get; set; } = "";
    public string? ReturnUrl { get; set; }
}
public class RegistroViewModel
{
    [Required,
        MaxLength(120)]
    public string Nombre { get; set; } = ""; [Required,
        RegularExpression(@"[a-zA-Z0-9._-]{3,80}", ErrorMessage = "Usa 3 a 80 letras, números, punto o guion.")]
    public string Login { get; set; } = ""; [Required,
        EmailAddress]
    public string Correo { get; set; } = ""; [Required,
        MinLength(10,
                ErrorMessage = "Usa al menos 10 caracteres.")]
    public string Clave { get; set; } = ""; [Compare("Clave",
                ErrorMessage = "Las contraseñas deben coincidir.")]
    public string Confirmacion { get; set; } = ""; public string? Telefono { get; set; }
}
public class RecuperarViewModel { [Required, EmailAddress] public string Correo { get; set; } = ""; }
public class RestablecerViewModel
{
    [Required]
    public string Token { get; set; } = "";
    [Required, MinLength(10)]
    public string Clave { get; set; } = "";
    [Compare("Clave")]
    public string Confirmacion { get; set; } = "";
}
public class ListaViewModel<T>
{
    public Pagina<T> Datos { get; set; } = new()
    ;
    public string Busqueda { get; set; } = "";
    public string Filtro { get; set; } = "";
    public List<OpcionDto> Opciones { get; set; } = []
    ;
    public string Extra { get; set; } = "";
}
public class UsuarioFormViewModel
{
    public Guid? Id { get; set; }
    [Required,
        MaxLength(120)]
    public string Nombre { get; set; } = ""; [Required,
        RegularExpression(@"[a-zA-Z0-9._-]{3,80}")]
    public string Login { get; set; } = ""; [Required,
        EmailAddress]
    public string Correo { get; set; } = ""; public string? Telefono { get; set; }
    public string? Clave { get; set; }
    [MinLength(1, ErrorMessage = "Selecciona una responsabilidad.")] public List<Guid> Roles { get; set; } = []; public List<OpcionDto> Disponibles { get; set; } = [];
}
public class ClienteFormViewModel
{
    [RegularExpression("CI|NIT|OTRO|SIN_ESPECIFICAR")]
    public string TipoDocumento { get; set; } = "SIN_ESPECIFICAR";
    public Guid? Id { get; set; }
    [Required,
        MaxLength(120)]
    public string Nombre { get; set; } = ""; [Required] public string Tipo { get; set; } = "Consumidor final"; [Required,
        MaxLength(30)]
    public string Telefono { get; set; } = ""; public string Direccion { get; set; } = ""; public string IdentificacionFiscal { get; set; } = ""; public string CondicionHabitual { get; set; } = "CONTADO"; public string Observaciones { get; set; } = "";
}
public class ClasificacionFormViewModel
{
    public Guid? Id { get; set; }
    [Required,
        MaxLength(20)]
    public string Codigo { get; set; } = ""; [Required,
        MaxLength(100)]
    public string Nombre { get; set; } = ""; public string Familia { get; set; } = "CARNE_FRESCA"; public string Descripcion { get; set; } = "";
}
public class ProductoFormViewModel
{
    public Guid? Id { get; set; }
    [Required,
        MaxLength(20)]
    public string Codigo { get; set; } = ""; [Required,
        MaxLength(120)]
    public string Nombre { get; set; } = ""; [Required,
        MaxLength(60)]
    public string Presentacion { get; set; } = "Pieza"; public string UnidadVenta { get; set; } = "kg"; public string Descripcion { get; set; } = ""; public Guid ClasificacionProductoId { get; set; }
    public List<ClasificacionDto> Clasificaciones { get; set; } = [];
}
public class PrecioFormViewModel
{
    public Guid ProductoId { get; set; }
    public Guid? Id { get; set; }
    public string Producto { get; set; } = ""; [Range(typeof(decimal),
        "0.01",
        "9999999999",
        ParseLimitsInInvariantCulture = true,
        ConvertValueInInvariantCulture = true)]
    public decimal CostoReferencia { get; set; }
    [Range(typeof(decimal),
        "0.01",
        "9999999999",
        ParseLimitsInInvariantCulture = true,
        ConvertValueInInvariantCulture = true)]
    public decimal PrecioVenta { get; set; }
    public DateTime VigenteDesde { get; set; } = Hora.Local(DateTime.UtcNow); public bool ConfirmarMargenNoPositivo { get; set; }
}
public class DetalleFormViewModel
{
    public Guid ProductoId { get; set; }
    [Range(typeof(decimal),
        "0.001",
        "999999",
        ParseLimitsInInvariantCulture = true,
        ConvertValueInInvariantCulture = true)]
    public decimal Cantidad { get; set; } = 1; public string Unidad { get; set; } = "kg"; [Range(typeof(decimal),
        "0.01",
        "9999999",
        ParseLimitsInInvariantCulture = true,
        ConvertValueInInvariantCulture = true)]
    public decimal PrecioUnitario { get; set; }
}
public class CotizacionFormViewModel
{
    public Guid? Id { get; set; }
    public Guid ClienteId { get; set; }
    public string Canal { get; set; } = "LOCAL";
    public DateTime VigenteHasta { get; set; } = Hora.Local(DateTime.UtcNow.AddDays(1))
    ;
    [MinLength(1)]
    public List<DetalleFormViewModel> Detalles { get; set; } = [new()]
    ;
    public List<ClienteDto> Clientes { get; set; } = []
    ;
    public List<ProductoDto> Productos { get; set; } = []
    ;
}
public class ConfirmarPedidoViewModel
{
    public Guid CotizacionId { get; set; }
    public string Codigo { get; set; } = "";
    public string Cliente { get; set; } = "";
    public decimal Total { get; set; }
    public DateTime FechaComprometida { get; set; } = Hora.Local(DateTime.UtcNow.AddDays(1))
    ;
    public string CondicionPago { get; set; } = "CONTADO";
}
public class ConfirmacionViewModel
{
    public string Titulo { get; set; } = "";
    public string Mensaje { get; set; } = "";
    public Guid Id { get; set; }
    public string Valor { get; set; } = "";
    public string Accion { get; set; } = "Estado";
    public bool MotivoRequerido { get; set; }
    public string Motivo { get; set; } = "";
    public string Volver { get; set; } = "/";
}
public class FichaViewModel<T>
{
    public T Datos { get; set; } = default!;
    public List<PedidoDto> Pedidos { get; set; } = []
    ;
    public List<PrecioDto> Precios { get; set; } = []
    ;
    public List<CargoDto> Cargos { get; set; } = [];
    /// <summary>RF-11: registro de la operación sin comprobante (no hay proveedor fiscal). Null si todavía no se registró.</summary>
    public ComprobanteFiscalDto? Fiscal { get; set; }
}
public class ResumenViewModel { public ResumenDto Datos { get; set; } = new(); }
public static class Hora
{
    public static DateTime Local(DateTime utc) => DateTime.SpecifyKind(utc,
        DateTimeKind.Utc).AddHours(-4); public static DateTime Utc(DateTime local) => DateTime.SpecifyKind(local.AddHours(4),
        DateTimeKind.Utc); public static string Mostrar(DateTime utc) => Local(utc).ToString("dd/MM/yyyy HH:mm");
}

// Sprint 2 · S2-P2. Todo [Range] sobre decimal lleva las dos propiedades de cultura invariante:
// sin ellas, una máquina en español devuelve 500 (Errores_encontrados_Sprint1.md, sección 1).
public class AnimalFormViewModel
{
    public Guid? Id { get; set; }
    [Required(ErrorMessage = "Escribe la identificación del animal."), MaxLength(80)]
    public string Identificacion { get; set; } = "";
    [Required(ErrorMessage = "Escribe la procedencia."), MaxLength(300)]
    public string Procedencia { get; set; } = "";
    [Required(ErrorMessage = "Escribe el proveedor."), MaxLength(180)]
    public string Proveedor { get; set; } = "";
    [Range(typeof(decimal), "0.001", "99999.999",
        ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true,
        ErrorMessage = "El peso de ingreso debe estar entre 0.001 y 99999.999 kg.")]
    public decimal PesoIngreso { get; set; }
}
public class AnimalFichaViewModel
{
    public AnimalDto Animal { get; set; } = new();
    public List<ControlSanitarioDto> Controles { get; set; } = [];
    public Dictionary<Guid, string> Nombres { get; set; } = [];
}
public class ControlSanitarioFormViewModel
{
    public Guid AnimalId { get; set; }
    public string Animal { get; set; } = "";
    [Required(ErrorMessage = "Selecciona el resultado del control.")]
    public string Resultado { get; set; } = "";
    [Required(ErrorMessage = "Registra la observación del control."), MaxLength(1000)]
    public string Observacion { get; set; } = "";
    public DateTime Fecha { get; set; } = Hora.Local(DateTime.UtcNow);
}
public class OrdenListaViewModel
{
    public Pagina<OrdenProduccionDto> Datos { get; set; } = new();
    public Dictionary<Guid, string> Animales { get; set; } = [];
    public Dictionary<Guid, string> Nombres { get; set; } = [];
}
public class PrevisionFormViewModel
{
    public Guid? ProductoId { get; set; }
    [Range(typeof(decimal), "0.001", "9999999.999",
        ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true,
        ErrorMessage = "La cantidad estimada debe ser mayor a cero, con hasta tres decimales.")]
    public decimal CantidadEstimada { get; set; } = 1;
    public string Unidad { get; set; } = "kg";
}
public class OrdenFormViewModel
{
    [Required]
    public string Tipo { get; set; } = "FAENEO";
    public Guid? AnimalId { get; set; }
    public DateTime InicioPrevisto { get; set; } = Hora.Local(DateTime.UtcNow.AddHours(1));
    public DateTime FinPrevisto { get; set; } = Hora.Local(DateTime.UtcNow.AddHours(5));
    [MaxLength(1000)]
    public string Observacion { get; set; } = "";
    public List<PrevisionFormViewModel> Previsiones { get; set; } = [];
    public List<AnimalDto> Animales { get; set; } = [];
    public List<ProductoDto> Productos { get; set; } = [];
}
public class OrdenFichaViewModel
{
    public OrdenDetalleDto Detalle { get; set; } = new();
    public AnimalDto? Animal { get; set; }
    public Dictionary<Guid, ProductoDto> Productos { get; set; } = [];
    public Dictionary<Guid, ExistenciaDto> Existencias { get; set; } = [];
    public Dictionary<Guid, string> Nombres { get; set; } = [];
}
public class ConsumoFormViewModel
{
    public Guid OrdenId { get; set; }
    public string Codigo { get; set; } = "";
    [Required(ErrorMessage = "Selecciona la existencia consumida.")]
    public Guid? ExistenciaId { get; set; }
    [Range(typeof(decimal), "0.001", "9999999.999",
        ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true,
        ErrorMessage = "La cantidad consumida debe ser mayor a cero, con hasta tres decimales.")]
    public decimal Cantidad { get; set; }
    // Se genera al abrir el formulario y se conserva en los reintentos: así un doble envío no consume dos veces.
    public Guid ReferenciaUnica { get; set; } = Guid.NewGuid();
    public List<ExistenciaDto> Existencias { get; set; } = [];
}
public class ResultadoFormViewModel
{
    public Guid? ProductoId { get; set; }
    [Required(ErrorMessage = "Escribe el código de lote."), MaxLength(80)]
    public string CodigoLote { get; set; } = "";
    public DateTime Vencimiento { get; set; } = Hora.Local(DateTime.UtcNow.AddDays(7));
}
public class ConfirmarProduccionViewModel
{
    public Guid OrdenId { get; set; }
    public string Codigo { get; set; } = "";
    public string Tipo { get; set; } = "";
    [MinLength(1, ErrorMessage = "Registra al menos un producto resultante.")]
    public List<ResultadoFormViewModel> Resultados { get; set; } = [new()];
    public List<ProductoDto> Productos { get; set; } = [];
}
public class PesajeListaViewModel
{
    public Pagina<PesajeDto> Datos { get; set; } = new();
    public Dictionary<Guid, LoteDto> Lotes { get; set; } = [];
    public Dictionary<Guid, ProductoDto> Productos { get; set; } = [];
    public Dictionary<Guid, string> Nombres { get; set; } = [];
    public HashSet<Guid> ConEntrada { get; set; } = [];
}
public class PesajeFormViewModel
{
    [Required(ErrorMessage = "Selecciona el lote pesado.")]
    public Guid? LoteId { get; set; }
    [Range(typeof(decimal), "0.001", "9999999.999",
        ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true,
        ErrorMessage = "El peso bruto debe ser mayor a cero, con hasta tres decimales.")]
    public decimal PesoBruto { get; set; }
    [Range(typeof(decimal), "0", "9999999.999",
        ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true,
        ErrorMessage = "La tara no puede ser negativa.")]
    public decimal Tara { get; set; }
    [Required(ErrorMessage = "Indica la unidad del pesaje."), MaxLength(20)]
    public string Unidad { get; set; } = "kg";
    [MaxLength(1000)]
    public string Observacion { get; set; } = "";
    public List<LoteDto> Lotes { get; set; } = [];
    public Dictionary<Guid, ProductoDto> Productos { get; set; } = [];
}
public class EntradaListaViewModel
{
    public Pagina<EntradaInventarioDto> Datos { get; set; } = new();
    public Dictionary<Guid, LoteDto> Lotes { get; set; } = [];
    public Dictionary<Guid, ProductoDto> Productos { get; set; } = [];
    public Dictionary<Guid, string> Ubicaciones { get; set; } = [];
    public Dictionary<Guid, string> Nombres { get; set; } = [];
}
public class EntradaFormViewModel
{
    [Required(ErrorMessage = "Selecciona el pesaje que respalda la entrada.")]
    public Guid? PesajeId { get; set; }
    [Required(ErrorMessage = "Indica la ubicación en almacén."), MaxLength(180)]
    public string Ubicacion { get; set; } = "";
    public List<PesajeDto> Pesajes { get; set; } = [];
    public Dictionary<Guid, LoteDto> Lotes { get; set; } = [];
    public Dictionary<Guid, ProductoDto> Productos { get; set; } = [];
}
public class FrioListaViewModel
{
    public Pagina<ControlFrioDto> Datos { get; set; } = new();
    public Dictionary<Guid, LoteDto> Lotes { get; set; } = [];
    public Dictionary<Guid, ProductoDto> Productos { get; set; } = [];
    public Dictionary<Guid, string> Nombres { get; set; } = [];
}
public class FrioFormViewModel
{
    [Required(ErrorMessage = "Selecciona el lote controlado.")]
    public Guid? LoteId { get; set; }
    [Range(typeof(decimal), "-100", "100",
        ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true,
        ErrorMessage = "Registra una temperatura entre -100 y 100 °C.")]
    public decimal Temperatura { get; set; }
    [Required(ErrorMessage = "Selecciona la evaluación del responsable.")]
    public string Evaluacion { get; set; } = "";
    [Required(ErrorMessage = "Indica la ubicación (cámara, mostrador, vehículo)."), MaxLength(180)]
    public string Ubicacion { get; set; } = "";
    public DateTime Fecha { get; set; } = Hora.Local(DateTime.UtcNow);
    [Required(ErrorMessage = "Registra la observación."), MaxLength(1000)]
    public string Observacion { get; set; } = "";
    public List<LoteDto> Lotes { get; set; } = [];
    public Dictionary<Guid, ProductoDto> Productos { get; set; } = [];
}
public class ServicioFormViewModel
{
    public Guid PedidoId { get; set; }
    public string Codigo { get; set; } = "";
    [Required(ErrorMessage = "Describe el servicio prestado."), MaxLength(300)]
    public string Concepto { get; set; } = "Servicio de faeneo";
    [Range(typeof(decimal), "0.01", "9999999.99",
        ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true,
        ErrorMessage = "El importe debe ser mayor a cero, con dos decimales.")]
    public decimal Importe { get; set; }
    [Required(ErrorMessage = "Registra cómo se acordó el servicio con el cliente."), MaxLength(300)]
    public string AcuerdoServicio { get; set; } = "";
    public DateTime PrestadoEn { get; set; } = Hora.Local(DateTime.UtcNow);
}
