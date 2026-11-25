using System.ComponentModel.DataAnnotations;
using SGCP.Web.Models.Dtos;
namespace SGCP.Web.Models.ViewModels;

// Sprint 5 · P1. Reservas, incidencias y movimientos de stock (CU09, RF-04, RF-18, RNF-06).
// La regla la valida la API; aquí solo se calcula lo que la pantalla necesita mostrar.

/// <summary>Un detalle de pedido con lo que ya tiene reservado y lo que le falta.</summary>
public class DetallePorPreparar
{
    public DetalleOperativoDto Detalle { get; set; } = new();
    /// <summary>Reservas del detalle que no fueron liberadas (activas o ya consumidas por el despacho).</summary>
    public List<ReservaDto> Reservas { get; set; } = [];
    public decimal Reservado => Reservas.Where(x => x.Estado != "LIBERADA").Sum(x => x.Cantidad);
    public decimal Falta => Math.Max(0, Detalle.CantidadConfirmada - Reservado);
    public bool Completo => Falta == 0;
}

public class PedidoPorPreparar
{
    public PedidoOperativoDto Pedido { get; set; } = new();
    public List<DetallePorPreparar> Detalles { get; set; } = [];
    public bool Completo => Detalles.Count > 0 && Detalles.All(x => x.Completo);

    /// <summary>Agrupa las reservas por detalle. Es la única lógica de la pantalla y la cubre SGCP.Web.Verificacion.</summary>
    public static List<PedidoPorPreparar> Armar(IEnumerable<PedidoOperativoDto> pedidos, IEnumerable<ReservaDto> reservas)
    {
        var porDetalle = reservas.GroupBy(x => x.DetallePedidoId).ToDictionary(g => g.Key, g => g.OrderBy(x => x.Fecha).ToList());
        return pedidos.Select(p => new PedidoPorPreparar
        {
            Pedido = p,
            Detalles = p.Detalles.Select(d => new DetallePorPreparar
            {
                Detalle = d,
                Reservas = porDetalle.TryGetValue(d.Id, out var r) ? r : []
            }).ToList()
        }).ToList();
    }
}

public class PedidosPorPrepararViewModel
{
    public List<PedidoPorPreparar> Pedidos { get; set; } = [];
    public Dictionary<Guid, ExistenciaDto> Existencias { get; set; } = [];
    public Dictionary<Guid, string> Nombres { get; set; } = [];
    public bool SoloPendientes { get; set; } = true;
}

public class ReservaFormViewModel
{
    public Guid PedidoId { get; set; }
    public Guid DetallePedidoId { get; set; }
    [Required(ErrorMessage = "Elige la existencia de donde sale el producto.")]
    public Guid? ExistenciaId { get; set; }
    [Range(typeof(decimal), "0.001", "999999.999", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true,
        ErrorMessage = "Indica una cantidad mayor a cero, con hasta tres decimales.")]
    public decimal Cantidad { get; set; }
    // Solo para mostrar
    public string Pedido { get; set; } = "";
    public string Cliente { get; set; } = "";
    public string Producto { get; set; } = "";
    public string Unidad { get; set; } = "";
    public decimal Confirmada { get; set; }
    public decimal Reservado { get; set; }
    public decimal Falta => Math.Max(0, Confirmada - Reservado);
    /// <summary>Existencias del mismo producto, aptas y con disponible. Una previsión nunca aparece: no es existencia.</summary>
    public List<ExistenciaDto> Existencias { get; set; } = [];
    /// <summary>Lotes con disponible que no se ofrecen porque ya tienen una reserva activa para este producto del pedido.</summary>
    public List<string> LotesYaReservados { get; set; } = [];
}

public class IncidenciasViewModel
{
    public Pagina<IncidenciaDto> Datos { get; set; } = new();
    /// <summary>pendientes, resueltas o todas.</summary>
    public string Estado { get; set; } = "pendientes";
    public Dictionary<Guid, AnimalDto> Animales { get; set; } = [];
    public Dictionary<Guid, LoteDto> Lotes { get; set; } = [];
    public Dictionary<Guid, ProductoDto> Productos { get; set; } = [];
    public Dictionary<Guid, string> Nombres { get; set; } = [];

    public string Origen(IncidenciaDto i)
    {
        if (i.AnimalId is Guid a)
            return "Animal " + (Animales.TryGetValue(a, out var animal) ? animal.Identificacion : a.ToString()[..8]);
        if (i.LoteId is Guid l && Lotes.TryGetValue(l, out var lote))
            return "Lote " + lote.Codigo + (Productos.TryGetValue(lote.ProductoId, out var p) ? " · " + p.Nombre : "");
        return i.LoteId is Guid otro ? "Lote " + otro.ToString()[..8] : "Sin origen registrado";
    }
}

public class MovimientosViewModel
{
    public ExistenciaDto Existencia { get; set; } = new();
    public Pagina<MovimientoStockDto> Datos { get; set; } = new();
    public Dictionary<Guid, string> Nombres { get; set; } = [];

    public static string Tipo(string tipo) => tipo switch
    {
        "ENTRADA" => "Entrada física",
        "CONSUMO" => "Consumo en producción",
        "DESPACHO" => "Despacho de entrega",
        "ANULACION_ENTRADA" => "Anulación de entrada",
        _ => tipo
    };
}
