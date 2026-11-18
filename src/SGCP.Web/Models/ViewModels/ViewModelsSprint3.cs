using System.ComponentModel.DataAnnotations;
using SGCP.Web.Models.Dtos;
namespace SGCP.Web.Models.ViewModels;

// Sprint 3 · S3-P2. Todo [Range] sobre decimal lleva ParseLimitsInInvariantCulture y ConvertValueInInvariantCulture
// (Errores_encontrados_Sprint1.md, sección 1). Importes con dos decimales; pesos con tres.

/// <summary>Traducciones de estados para pantalla. Entrega y deuda se nombran por separado.</summary>
public static class Textos
{
    public static string Condicion(string c) => c switch
    {
        "CONTADO" => "Contado",
        "CREDITO_ACORDADO" => "Crédito acordado",
        "CREDITO_VIGENTE" => "Crédito vigente",
        "CREDITO_VENCIDO" => "Crédito vencido",
        "SIN_ACUERDO" => "Sin condición acordada",
        _ => c.Replace('_', ' ')
    };

    public static string Entrega(string e) => e switch
    {
        "PREPARADA" => "Preparada",
        "DESPACHADA" => "Despachada",
        "ENTREGADA" => "Entregada",
        "OBSERVADA" => "Observada",
        "CANCELADA" => "Cancelada",
        _ => e.Replace('_', ' ')
    };

    /// <summary>Observada no es un error: la entrega ocurrió con reparos. Por eso no usa el color de peligro.</summary>
    public static string ClaseEntrega(string e) => e switch
    {
        "ENTREGADA" => "",
        "OBSERVADA" => "aviso",
        "CANCELADA" => "muted-badge",
        _ => "muted-badge"
    };
}

public class SaldosViewModel
{
    public List<ClienteDto> Clientes { get; set; } = [];
    public Guid? ClienteId { get; set; }
    public ClienteDto? Cliente { get; set; }
    public SaldosDto Saldos { get; set; } = new();
    public List<PagoDto> Pagos { get; set; } = [];
    public Dictionary<Guid, PedidoDto> Pedidos { get; set; } = [];
    public bool PuedeRegistrar { get; set; }

    public decimal Por(Func<SaldoCargoDto, bool> filtro) => Saldos.Cargos.Where(filtro).Sum(x => x.Saldo);
}

public class CondicionFormViewModel
{
    public Guid PedidoId { get; set; }
    [Required(ErrorMessage = "Elige la modalidad acordada.")]
    public string Modalidad { get; set; } = "CONTADO";
    [Range(typeof(decimal), "0", "9999999999.99", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true,
        ErrorMessage = "Indica un importe entre 0 y 9.999.999.999,99.")]
    public decimal ImporteAutorizado { get; set; }
    [DataType(DataType.DateTime)]
    public DateTime? Vencimiento { get; set; }
    public PedidoDto? Pedido { get; set; }
    public List<CondicionPagoDto> Historial { get; set; } = [];
}

public class AplicacionFila
{
    public Guid CargoId { get; set; }
    public string Pedido { get; set; } = "";
    public string Concepto { get; set; } = "";
    public string Tipo { get; set; } = "";
    public decimal Saldo { get; set; }
    [Range(typeof(decimal), "0", "9999999999.99", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true,
        ErrorMessage = "El importe aplicado no puede ser negativo.")]
    public decimal? Importe { get; set; }
}

public class PagoFormViewModel
{
    public Guid ClienteId { get; set; }
    public string Cliente { get; set; } = "";
    [Required(ErrorMessage = "Escribe la referencia del pago (recibo, comprobante o transferencia)."), MaxLength(80)]
    public string ReferenciaUnica { get; set; } = "";
    [Required(ErrorMessage = "Escribe el medio de pago."), MaxLength(80)]
    public string Medio { get; set; } = "Efectivo";
    public DateTime Fecha { get; set; } = Hora.Local(DateTime.UtcNow);
    public List<AplicacionFila> Aplicaciones { get; set; } = [];
}

public class SenasViewModel
{
    public PedidoDto Pedido { get; set; } = new();
    public List<SenaDto> Senas { get; set; } = [];
    public Dictionary<Guid, PagoDto> Pagos { get; set; } = [];
    public List<SaldoCargoDto> Cargos { get; set; } = [];
    public SenaFormViewModel Nueva { get; set; } = new();
}

public class SenaFormViewModel
{
    public Guid PedidoId { get; set; }
    [Required(ErrorMessage = "Escribe la referencia de la seña."), MaxLength(80)]
    public string ReferenciaUnica { get; set; } = "";
    [Range(typeof(decimal), "0.01", "9999999999.99", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true,
        ErrorMessage = "La seña debe ser mayor a cero.")]
    public decimal Importe { get; set; }
    [Required(ErrorMessage = "Escribe el medio."), MaxLength(80)]
    public string Medio { get; set; } = "Efectivo";
    public DateTime Fecha { get; set; } = Hora.Local(DateTime.UtcNow);
}

/// <summary>Anular un pago o resolver una seña: siempre con motivo, nunca un borrado.</summary>
public class MotivoFormViewModel
{
    public Guid Id { get; set; }
    public string Titulo { get; set; } = "";
    public string Explicacion { get; set; } = "";
    public string Volver { get; set; } = "/";
    [Required(ErrorMessage = "Escribe el motivo."), MaxLength(1000)]
    public string Motivo { get; set; } = "";
}

public class TransporteFormViewModel
{
    public Guid? Id { get; set; }
    [Required(ErrorMessage = "Escribe el código."), MaxLength(80)]
    public string Codigo { get; set; } = "";
    [Required]
    public string Modalidad { get; set; } = "PROPIO";
    [Required(ErrorMessage = "Escribe el transportista."), MaxLength(180)]
    public string Transportista { get; set; } = "";
    [Required(ErrorMessage = "Escribe el contacto."), MaxLength(100)]
    public string Contacto { get; set; } = "";
    [Required(ErrorMessage = "Escribe la placa."), MaxLength(40)]
    public string Placa { get; set; } = "";
    [Required(ErrorMessage = "Describe el vehículo."), MaxLength(300)]
    public string DescripcionVehiculo { get; set; } = "";
    [Required(ErrorMessage = "Describe las condiciones de conservación."), MaxLength(300)]
    public string Conservacion { get; set; } = "";
}

public class RutasViewModel
{
    public Pagina<RutaDto> Datos { get; set; } = new();
    public Dictionary<Guid, TransporteDto> Transportes { get; set; } = [];
    public Dictionary<Guid, string> Responsables { get; set; } = [];
}

public class RutaFormViewModel
{
    [Required(ErrorMessage = "Elige el transporte.")]
    public Guid? TransporteId { get; set; }
    [Required(ErrorMessage = "Elige el responsable de entrega.")]
    public Guid? ResponsableId { get; set; }
    public DateTime Fecha { get; set; } = Hora.Local(DateTime.UtcNow).AddHours(1);
    [Required(ErrorMessage = "Escribe la zona."), MaxLength(180)]
    public string Zona { get; set; } = "";
    public List<TransporteDto> Transportes { get; set; } = [];
    public List<ResponsableDto> Responsables { get; set; } = [];
}

public class EntregasViewModel
{
    public Pagina<EntregaDto> Datos { get; set; } = new();
    public Dictionary<Guid, PedidoDto> Pedidos { get; set; } = [];
    public Dictionary<Guid, string> Responsables { get; set; } = [];
    public Dictionary<Guid, RutaDto> Rutas { get; set; } = [];
    public Guid? UsuarioId { get; set; }
}

public class EntregaFormViewModel
{
    [Required(ErrorMessage = "Elige el pedido.")]
    public Guid? PedidoId { get; set; }
    [Required]
    public string Modalidad { get; set; } = "DOMICILIO";
    [Required(ErrorMessage = "Elige el responsable.")]
    public Guid? ResponsableId { get; set; }
    public Guid? RutaId { get; set; }
    public DateTime FechaPrevista { get; set; } = Hora.Local(DateTime.UtcNow).AddHours(3);
    [Range(1, 999, ErrorMessage = "La posición en la ruta va de 1 a 999.")]
    public int OrdenEnRuta { get; set; } = 1;
    public List<PedidoDto> Pedidos { get; set; } = [];
    public List<ResponsableDto> Responsables { get; set; } = [];
    public List<RutaDto> Rutas { get; set; } = [];
}

public class ReasignarFormViewModel
{
    public Guid Id { get; set; }
    public int VersionActual { get; set; }
    [Required(ErrorMessage = "Elige el nuevo responsable.")]
    public Guid? ResponsableId { get; set; }
    public Guid? RutaId { get; set; }
    [Required(ErrorMessage = "Escribe el motivo."), MaxLength(1000)]
    public string Motivo { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public List<ResponsableDto> Responsables { get; set; } = [];
    public List<RutaDto> Rutas { get; set; } = [];
}

/// <summary>Carga manual de la recepción desde la web (retiro en el local), con el mismo contrato que Android.</summary>
public class RecepcionFormViewModel
{
    public Guid EntregaId { get; set; }
    public int VersionBase { get; set; }
    /// <summary>Se genera al abrir el formulario: si se envía dos veces, la API devuelve la misma recepción.</summary>
    public Guid ClaveUnica { get; set; } = Guid.NewGuid();
    public string Descripcion { get; set; } = "";
    [Required(ErrorMessage = "Escribe quién recibe."), MaxLength(180)]
    public string Receptor { get; set; } = "";
    public DateTime FechaReal { get; set; } = Hora.Local(DateTime.UtcNow);
    [Range(typeof(decimal), "0.001", "999999999", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true,
        ErrorMessage = "Indica un peso positivo con hasta tres decimales.")]
    public decimal PesoRecibido { get; set; }
    [Required]
    public string Unidad { get; set; } = "kg";
    public bool Conforme { get; set; } = true;
    [MaxLength(1000)]
    public string Observacion { get; set; } = "";
    public List<IFormFile> Fotos { get; set; } = [];
}

public class ConflictosViewModel
{
    public Pagina<RecepcionDto> Datos { get; set; } = new();
    public Dictionary<Guid, PedidoDto> Pedidos { get; set; } = [];
}

public class RevisarFormViewModel
{
    public Guid Id { get; set; }
    public int VersionActual { get; set; }
    public bool Aplicar { get; set; }
    [Required(ErrorMessage = "Escribe el motivo de la revisión."), MaxLength(1000)]
    public string Motivo { get; set; } = "";
    public RecepcionDetalleDto? Detalle { get; set; }
    public EntregaActualDto? Entrega { get; set; }
    public PedidoDto? Pedido { get; set; }
}
