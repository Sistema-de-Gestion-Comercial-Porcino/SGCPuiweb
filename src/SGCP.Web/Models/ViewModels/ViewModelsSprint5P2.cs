using System.ComponentModel.DataAnnotations;
using SGCP.Web.Models.Dtos;
namespace SGCP.Web.Models.ViewModels;

/// <summary>
/// CU09 «Recibir productos» · lote comprado a otro proveedor (POST lotes/externos). Nace APTO y sin cantidad:
/// después se pesa y se le da entrada física como a cualquier lote, y recién ahí suma stock.
/// </summary>
public class LoteExternoFormViewModel : IValidatableObject
{
    [Required(ErrorMessage = "Selecciona el producto recibido.")]
    public Guid? ProductoId { get; set; }
    [Required(ErrorMessage = "Escribe el código del lote."), MaxLength(80, ErrorMessage = "El código admite hasta 80 caracteres.")]
    public string Codigo { get; set; } = "";
    [Required(ErrorMessage = "Escribe la referencia del proveedor (guía, factura o remito)."), MaxLength(180)]
    public string Referencia { get; set; } = "";
    [Required(ErrorMessage = "Indica la procedencia (proveedor y origen)."), MaxLength(500)]
    public string Procedencia { get; set; } = "";
    [Required(ErrorMessage = "Indica la fecha de producción.")]
    public DateTime? FechaProduccion { get; set; }
    [Required(ErrorMessage = "Indica la fecha de vencimiento.")]
    public DateTime? Vencimiento { get; set; }
    public List<ProductoDto> Productos { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var hoy = Hora.Local(DateTime.UtcNow).Date;
        if (FechaProduccion?.Date > hoy)
            yield return new ValidationResult("La fecha de producción no puede ser futura.", [nameof(FechaProduccion)]);
        if (Vencimiento?.Date <= hoy)
            yield return new ValidationResult("No se recibe un lote vencido: el vencimiento tiene que ser posterior a hoy.", [nameof(Vencimiento)]);
        if (FechaProduccion is not null && Vencimiento is not null && Vencimiento <= FechaProduccion)
            yield return new ValidationResult("El vencimiento tiene que ser posterior a la producción.", [nameof(Vencimiento)]);
    }

    /// <summary>
    /// Fechas de calendario en hora de Bolivia a UTC: la producción a las 00:00 (nunca queda en el futuro)
    /// y el vencimiento a las 23:59, para que el lote valga todo ese día.
    /// </summary>
    public DateTime ProduccionUtc() => Hora.Utc(FechaProduccion!.Value.Date);
    public DateTime VencimientoUtc() => Hora.Utc(Vencimiento!.Value.Date.AddHours(23).AddMinutes(59));
}
