using System.Globalization;
using System.Text.Json;
using SGCP.Web.Models.Dtos;
namespace SGCP.Web.Models.ViewModels;

// Sprint 4 · S4-P2. Reportes por perfil (RF-15), constancia (RF-14) y leyenda fiscal (RF-11 no habilitado).

/// <summary>Respuesta de `GET reportes/{tipo}`: la misma tabla que usan el PDF y el Excel.</summary>
public class TablaReporteDto
{
    public string Titulo { get; set; } = "";
    public List<string> Columnas { get; set; } = [];
    public List<List<JsonElement>> Filas { get; set; } = [];
}

public class ComprobanteFiscalDto
{
    public Guid Id { get; set; }
    public Guid PedidoId { get; set; }
    public string ReferenciaUnica { get; set; } = "";
    public string Estado { get; set; } = "";
    public string NumeroFiscal { get; set; } = "";
    public string RespuestaProveedor { get; set; } = "";
    public DateTime Fecha { get; set; }
}

/// <summary>Los filtros viajan iguales a la consulta y a la descarga: el archivo trae exactamente lo que muestra la pantalla.</summary>
public class FiltroReporteViewModel
{
    public string? Tipo { get; set; }
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    public Guid? ProductoId { get; set; }
    public Guid? ClasificacionId { get; set; }
    public Guid? ClienteId { get; set; }
    public string? Estado { get; set; }

    /// <summary>Los mismos filtros, tal como los escribe la pantalla, para volver a ella o armar los botones de descarga.</summary>
    public string Pantalla(string? formato = null) => Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString("", new Dictionary<string, string?>
    {
        ["formato"] = formato,
        ["tipo"] = Tipo,
        ["desde"] = Desde?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        ["hasta"] = Hasta?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        ["productoId"] = ProductoId?.ToString(),
        ["clasificacionId"] = ClasificacionId?.ToString(),
        ["clienteId"] = ClienteId?.ToString(),
        ["estado"] = Estado
    }.Where(x => !string.IsNullOrEmpty(x.Value)));

    /// <summary>Fechas de pantalla (días de Bolivia) a UTC: el día completo, de 00:00 a 23:59:59.</summary>
    public string Consulta()
    {
        var p = new Dictionary<string, string?>();
        if (Desde is not null)
            p["desde"] = Hora.Utc(Desde.Value.Date).ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
        if (Hasta is not null)
            p["hasta"] = Hora.Utc(Hasta.Value.Date.AddDays(1).AddSeconds(-1)).ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
        if (ProductoId is not null && Reportes.AdmiteProducto(Tipo))
            p["productoId"] = ProductoId.ToString();
        if (ClasificacionId is not null && Reportes.AdmiteProducto(Tipo))
            p["clasificacionId"] = ClasificacionId.ToString();
        if (ClienteId is not null && Reportes.AdmiteCliente(Tipo))
            p["clienteId"] = ClienteId.ToString();
        if (!string.IsNullOrWhiteSpace(Estado))
            p["estado"] = Estado;
        return Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString("", p);
    }
}

public class ReportesViewModel
{
    public List<string> Disponibles { get; set; } = [];
    public FiltroReporteViewModel Filtro { get; set; } = new();
    public TablaReporteDto? Tabla { get; set; }
    public string? Error { get; set; }
    public List<ProductoDto> Productos { get; set; } = [];
    public List<ClasificacionDto> Clasificaciones { get; set; } = [];
    public List<ClienteDto> Clientes { get; set; } = [];
    public bool EsComprador { get; set; }
}

/// <summary>Nombres, filtros admitidos y formato de las celdas de cada reporte, según el contrato de SGCPapi.</summary>
public static class Reportes
{
    public static string Nombre(string tipo) => tipo switch
    {
        "CLIENTES" => "Clientes",
        "COTIZACIONES" => "Cotizaciones",
        "PEDIDOS" => "Pedidos",
        "PRODUCCION" => "Producción por producto y lote",
        "INVENTARIO" => "Inventario",
        "PAGOS" => "Pagos y señas",
        "ENTREGAS" => "Entregas",
        _ => tipo
    };

    public static bool AdmiteProducto(string? tipo) => tipo is not ("CLIENTES" or "PAGOS");
    public static bool AdmiteCliente(string? tipo) => tipo is not ("PRODUCCION" or "INVENTARIO");

    public static string[] Estados(string? tipo) => tipo switch
    {
        "CLIENTES" => ["ACTIVO", "INACTIVO"],
        "COTIZACIONES" => ["BORRADOR", "PRESENTADA", "ACEPTADA", "RECHAZADA", "VENCIDA"],
        "PEDIDOS" => ["CONFIRMADO", "PENDIENTE_ACUERDO", "CANCELADO", "CERRADO"],
        "PRODUCCION" => ["PENDIENTE_PESAJE", "CONFIRMADO", "BLOQUEADO"],
        "INVENTARIO" => ["APTO", "BLOQUEADO"],
        "PAGOS" => ["REGISTRADO", "ANULADO"],
        "ENTREGAS" => ["PREPARADA", "DESPACHADA", "ENTREGADA", "OBSERVADA", "CANCELADA"],
        _ => []
    };

    /// <summary>La API manda fechas en UTC y lo indica en el nombre de la columna; la pantalla las muestra en hora de Bolivia.</summary>
    public static string Columna(string c) => c.EndsWith(" UTC") ? c[..^4] + " (Bolivia)" : c;

    public static string Celda(string columna, JsonElement valor)
    {
        switch (valor.ValueKind)
        {
            case JsonValueKind.Null:
            case JsonValueKind.Undefined:
                return "—";
            case JsonValueKind.True:
                return "Sí";
            case JsonValueKind.False:
                return "No";
            case JsonValueKind.Number:
                var n = valor.GetDecimal();
                return columna.EndsWith("Bs") ? n.ToString("N2", CultureInfo.GetCultureInfo("es-BO")) : n.ToString("0.###", CultureInfo.GetCultureInfo("es-BO"));
            default:
                var texto = valor.ToString();
                if (columna.EndsWith("UTC") && DateTime.TryParse(texto, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var fecha))
                    return Hora.Mostrar(fecha);
                return texto.Replace('_', ' ');
        }
    }
}
