using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;
namespace SGCP.Web.Models;

public class EnlaceDecimalInvariante : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext contexto)
    {
        var valor = contexto.ValueProvider.GetValue(contexto.ModelName);
        if (valor == ValueProviderResult.None)
            return Task.CompletedTask;
        contexto.ModelState.SetModelValue(contexto.ModelName, valor);
        var texto = valor.FirstValue;
        var vacio = string.IsNullOrWhiteSpace(texto);
        if (vacio && contexto.ModelMetadata.IsReferenceOrNullableType)
        { contexto.Result = ModelBindingResult.Success(null); return Task.CompletedTask; }
        if (!vacio && Leer(texto!) is decimal numero)
        { contexto.Result = ModelBindingResult.Success(numero); return Task.CompletedTask; }
        contexto.ModelState.TryAddModelError(contexto.ModelName, "Escribe un número válido usando punto decimal.");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Punto decimal siempre; una sola coma decimal también («6,2» = 6.2, como se escribe en Bolivia).
    /// Nunca se aceptan separadores de miles: con NumberStyles.Number, «6,2» se leía como 62 sin avisar.
    /// Un texto con punto y coma a la vez («1.000,5») es ambiguo y se rechaza.
    /// </summary>
    public static decimal? Leer(string texto)
    {
        texto = texto.Trim();
        if (texto.Contains(',') && (texto.Contains('.') || texto.Count(x => x == ',') > 1))
            return null;
        const NumberStyles estilo = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint
            | NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite;
        return decimal.TryParse(texto.Replace(',', '.'), estilo, CultureInfo.InvariantCulture, out var numero) ? numero : null;
    }
}
public class ProveedorEnlaceDecimalInvariante : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext contexto)
    {
        var tipo = Nullable.GetUnderlyingType(contexto.Metadata.ModelType) ?? contexto.Metadata.ModelType;
        return tipo == typeof(decimal) ? new EnlaceDecimalInvariante() : null;
    }
}
