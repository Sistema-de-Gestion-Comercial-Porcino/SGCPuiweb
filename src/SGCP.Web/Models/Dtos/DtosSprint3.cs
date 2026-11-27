namespace SGCP.Web.Models.Dtos;

// Sprint 3 · S3-P2. Contratos de SGCPapi para cobranza, transporte, rutas, entregas y recepción.

/// <summary>Una fila de `GET saldos`: el saldo se calcula en la API (cargos menos pagos aplicados), no se guarda.</summary>
public class SaldoCargoDto
{
    public Guid CargoId { get; set; }
    public Guid PedidoId { get; set; }
    public Guid ClienteId { get; set; }
    public string Tipo { get; set; } = "";
    public string Concepto { get; set; } = "";
    public decimal Importe { get; set; }
    public decimal Aplicado { get; set; }
    public decimal Saldo { get; set; }
    /// <summary>SIN_ACUERDO, CONTADO, CREDITO_VIGENTE o CREDITO_VENCIDO.</summary>
    public string Condicion { get; set; } = "";
    public DateTime? Vencimiento { get; set; }
}

public class SaldosDto
{
    public List<SaldoCargoDto> Cargos { get; set; } = [];
    public decimal Total { get; set; }
}

public class CondicionPagoDto
{
    public Guid Id { get; set; }
    public Guid PedidoId { get; set; }
    public string Modalidad { get; set; } = "";
    public decimal ImporteAutorizado { get; set; }
    public DateTime FechaAcuerdo { get; set; }
    public DateTime? Vencimiento { get; set; }
    public Guid ResponsableId { get; set; }
    public bool Activa { get; set; }
}

public class PagoDto
{
    public Guid Id { get; set; }
    public Guid ClienteId { get; set; }
    public string ReferenciaUnica { get; set; } = "";
    public decimal Importe { get; set; }
    public DateTime Fecha { get; set; }
    public string Medio { get; set; } = "";
    /// <summary>PAGO o SENA.</summary>
    public string Tipo { get; set; } = "";
    public Guid ResponsableId { get; set; }
    public bool Anulado { get; set; }
    public string MotivoAnulacion { get; set; } = "";
}

public class SenaDto
{
    public Guid Id { get; set; }
    public Guid PagoId { get; set; }
    public Guid PedidoId { get; set; }
    public string AcuerdoResolucion { get; set; } = "";
    public DateTime? ResueltaEn { get; set; }
}

public class TransporteDto
{
    public Guid Id { get; set; }
    public string Codigo { get; set; } = "";
    public string Modalidad { get; set; } = "";
    public string Transportista { get; set; } = "";
    public string Contacto { get; set; } = "";
    public string Placa { get; set; } = "";
    public string DescripcionVehiculo { get; set; } = "";
    public string Conservacion { get; set; } = "";
    public bool Activo { get; set; }
}

public class RutaDto
{
    public Guid Id { get; set; }
    public string Codigo { get; set; } = "";
    public Guid TransporteId { get; set; }
    public Guid ResponsableId { get; set; }
    public DateTime Fecha { get; set; }
    public string Zona { get; set; } = "";
    public string Estado { get; set; } = "";
    public DateTime? SalidaReal { get; set; }
    public DateTime? RetornoReal { get; set; }
}

public class EntregaDto
{
    public Guid Id { get; set; }
    public Guid PedidoId { get; set; }
    public Guid? RutaId { get; set; }
    public Guid ResponsableId { get; set; }
    public string Modalidad { get; set; } = "";
    public string Estado { get; set; } = "";
    public int Version { get; set; }
    public DateTime FechaPrevista { get; set; }
    public DateTime? FechaReal { get; set; }
    public DateTime? FechaConfirmacion { get; set; }
    public string Receptor { get; set; } = "";
    public string ObservacionRecepcion { get; set; } = "";
    public int OrdenEnRuta { get; set; }
}

public class ResponsableDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = "";
    /// <summary>ENTREGA (repartidor) o VENTAS (vendedor, solo para retiro en el local).</summary>
    public string Rol { get; set; } = "";
}

public class RecepcionDto
{
    public Guid Id { get; set; }
    public Guid EntregaId { get; set; }
    public Guid ClaveUnica { get; set; }
    public int VersionBase { get; set; }
    public Guid AutorId { get; set; }
    public DateTime FechaReal { get; set; }
    public DateTime FechaSincronizacion { get; set; }
    public decimal PesoRecibido { get; set; }
    public string Unidad { get; set; } = "";
    public bool Conforme { get; set; }
    public string Receptor { get; set; } = "";
    public string Observacion { get; set; } = "";
    public string Estado { get; set; } = "";
    public string MotivoConflicto { get; set; } = "";
    public string Resolucion { get; set; } = "";
    // Solo en GET recepciones/conflictos (API v4.1.1): estado actual de la entrega, para revisar sin entregas.administrar.
    public int? VersionEntrega { get; set; }
    public string? EstadoEntrega { get; set; }
    public Guid? PedidoId { get; set; }
    public string? Pedido { get; set; }
}

/// <summary>Versión y estado actuales de la entrega, tal como los devuelve GET recepciones/{id}.</summary>
public class EntregaActualDto
{
    public Guid Id { get; set; }
    public int Version { get; set; }
    public string Estado { get; set; } = "";
    public Guid PedidoId { get; set; }
}

public class EvidenciaResumenDto
{
    public Guid Id { get; set; }
    public string Tipo { get; set; } = "";
    public string Sha256 { get; set; } = "";
}

public class RecepcionDetalleDto
{
    public RecepcionDto Recepcion { get; set; } = new();
    public EntregaActualDto? Entrega { get; set; }
    public List<EvidenciaResumenDto> Evidencias { get; set; } = [];
}

public class ResultadoSincronizacionDto
{
    public Guid ClaveUnica { get; set; }
    public Guid RecepcionId { get; set; }
    public string Estado { get; set; } = "";
    public bool Duplicada { get; set; }
}

public class SincronizacionDto
{
    public List<ResultadoSincronizacionDto> Resultados { get; set; } = [];
}

/// <summary>GET mis-entregas: entregas despachadas a domicilio del responsable autenticado, con su pedido y cliente.</summary>
public class MisEntregasDto
{
    public List<EntregaAsignadaDto> Entregas { get; set; } = [];
}
public class EntregaAsignadaDto
{
    public Guid Id { get; set; }
    public string Pedido { get; set; } = "";
    public string Cliente { get; set; } = "";
    public DateTime FechaPrevista { get; set; }
}
