namespace SGCP.Web.Models.Dtos;

public class SeguimientoCobroDto
{
    public Guid Id { get; set; }
    public Guid CondicionPagoId { get; set; }
    public Guid PedidoId { get; set; }
    public Guid ClienteId { get; set; }
    public string Pedido { get; set; } = "";
    public string Cliente { get; set; } = "";
    public DateTime? Vencimiento { get; set; }
    public DateTime? FechaGestion { get; set; }
    public string Resultado { get; set; } = "";
    public DateTime ProximaFecha { get; set; }
    public string Estado { get; set; } = "";
}
