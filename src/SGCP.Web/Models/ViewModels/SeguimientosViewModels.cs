using System.ComponentModel.DataAnnotations;
using SGCP.Web.Models.Dtos;

namespace SGCP.Web.Models.ViewModels;

public class SeguimientosViewModel
{
    public Guid? ClienteId { get; set; }
    public string? Estado { get; set; }
    public List<ClienteDto> Clientes { get; set; } = [];
    public Pagina<SeguimientoCobroDto> Datos { get; set; } = new();
}

public class NuevoSeguimientoViewModel
{
    public Guid PedidoId { get; set; }
    public Guid CondicionPagoId { get; set; }
    [Required(ErrorMessage = "Indica la próxima fecha de gestión.")]
    public DateTime? ProximaFecha { get; set; }
    public PedidoDto? Pedido { get; set; }
    public CondicionPagoDto? Condicion { get; set; }
}

public class GestionCobroViewModel
{
    public Guid Id { get; set; }
    public Guid ClienteId { get; set; }
    [Required(ErrorMessage = "Escribe el resultado de la gestión."), MaxLength(500, ErrorMessage = "El resultado admite hasta 500 caracteres.")]
    public string Resultado { get; set; } = "";
    public DateTime? ProximaFecha { get; set; }
    public SeguimientoCobroDto? Seguimiento { get; set; }
    public decimal Saldo { get; set; }
}
