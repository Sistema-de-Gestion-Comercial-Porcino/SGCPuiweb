using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using SGCP.Web.Models.Dtos;
namespace SGCP.Web.Services;

public class ApiException(int estado,
        string codigo,
        string mensaje) : Exception(mensaje)
{ public int Estado { get; } = estado; public string Codigo { get; } = codigo; }
public class ClienteApiBase(HttpClient http, IHttpContextAccessor acceso)
{
    /// <summary>Para clientes que necesitan algo distinto de JSON, como descargar una evidencia.</summary>
    protected HttpClient Http => http;
    protected string? Token() => acceso.HttpContext?.User.FindFirstValue("jwt");
    public async Task<T> Pedir<T>(HttpMethod metodo, string ruta, object? cuerpo = null)
    {
        using var req = new HttpRequestMessage(metodo, ruta);
        var token = acceso.HttpContext?.User.FindFirstValue("jwt");
        if (!string.IsNullOrWhiteSpace(token))
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (cuerpo is not null)
            req.Content = JsonContent.Create(cuerpo);
        HttpResponseMessage res;
        try
        { res = await http.SendAsync(req); }
        catch (HttpRequestException)
        {
            throw new ApiException(503,
                "API_NO_DISPONIBLE",
                "No podemos conectar con el servidor. Revisa la conexión y vuelve a intentar.");
        }
        catch (TaskCanceledException)
        {
            throw new ApiException(503,
                "TIEMPO_AGOTADO",
                "El servidor tardó demasiado en responder. Vuelve a intentar.");
        }
        using (res)
        {
            var texto = await res.Content.ReadAsStringAsync();
            if (!res.IsSuccessStatusCode)
            {
                string codigo = "ERROR", mensaje = "No se pudo completar la operación.";
                try
                { using var j = JsonDocument.Parse(texto); codigo = j.RootElement.GetProperty("codigo").GetString() ?? codigo; mensaje = j.RootElement.GetProperty("mensaje").GetString() ?? mensaje; }
                catch (JsonException) { }
                catch (KeyNotFoundException) { }
                throw new ApiException((int)res.StatusCode, codigo, mensaje);
            }
            return string.IsNullOrWhiteSpace(texto) ? default! : JsonSerializer.Deserialize<T>(texto, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        }
    }
    public Task<T> Get<T>(string ruta) => Pedir<T>(HttpMethod.Get, ruta);
    public Task<T> Post<T>(string ruta, object? cuerpo = null) => Pedir<T>(HttpMethod.Post, ruta, cuerpo);
    public Task<T> Put<T>(string ruta, object cuerpo) => Pedir<T>(HttpMethod.Put, ruta, cuerpo);
    public async Task<List<T>> Todos<T>(string ruta)
    {
        List<T> datos = [];
        var pagina = 1;
        while (true)
        { var r = await Get<Pagina<T>>(ruta + (ruta.Contains('?') ? "&" : "?") + "tamanoPagina=100&pagina=" + pagina); datos.AddRange(r.Elementos); if (pagina++ >= r.TotalPaginas) break; }
        return datos;
    }
}
