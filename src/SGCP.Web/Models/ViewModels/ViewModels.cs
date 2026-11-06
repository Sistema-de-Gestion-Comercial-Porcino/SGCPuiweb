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
}
public class ResumenViewModel { public ResumenDto Datos { get; set; } = new(); }
public static class Hora
{
    public static DateTime Local(DateTime utc) => DateTime.SpecifyKind(utc,
        DateTimeKind.Utc).AddHours(-4); public static DateTime Utc(DateTime local) => DateTime.SpecifyKind(local.AddHours(4),
        DateTimeKind.Utc); public static string Mostrar(DateTime utc) => Local(utc).ToString("dd/MM/yyyy HH:mm");
}
