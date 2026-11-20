using System.ComponentModel.DataAnnotations;
using SGCP.Web.Models.Dtos;

namespace SGCP.Web.Models.ViewModels;

public class DetalleSolicitudViewModel
{
    public Guid ProductoId { get; set; }
    [Range(typeof(decimal), "0.001", "999999.999", ParseLimitsInInvariantCulture = true,
        ConvertValueInInvariantCulture = true)]
    public decimal Cantidad { get; set; } = 1;
}

public class SolicitudCotizacionViewModel
{
    public Guid? Id { get; set; }
    [Required, MinLength(1), MaxLength(100)]
    public List<DetalleSolicitudViewModel> Detalles { get; set; } = [new()];
    public List<ProductoDto> Productos { get; set; } = [];
}
