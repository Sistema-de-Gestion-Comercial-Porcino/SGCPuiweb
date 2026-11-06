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
        if (!vacio && decimal.TryParse(texto, NumberStyles.Number, CultureInfo.InvariantCulture, out var numero))
        { contexto.Result = ModelBindingResult.Success(numero); return Task.CompletedTask; }
        contexto.ModelState.TryAddModelError(contexto.ModelName, "Escribe un número válido usando punto decimal.");
        return Task.CompletedTask;
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
