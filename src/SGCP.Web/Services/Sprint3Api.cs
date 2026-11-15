using System.Net.Http.Headers;
namespace SGCP.Web.Services;

/// <summary>RF-05, RF-06, RF-19 · condición de pago, pagos, saldos y señas.</summary>
public class CobranzaApi(HttpClient http, IHttpContextAccessor acceso) : ClienteApiBase(http, acceso) { }

/// <summary>RF-07, RF-08 · transportes, rutas, entregas y recepción. Incluye la descarga de evidencias.</summary>
public class RepartoApi(HttpClient http, IHttpContextAccessor acceso) : ClienteApiBase(http, acceso)
{
    /// <summary>La evidencia exige el token: la web la pide a la API y la reenvía al navegador.</summary>
    public async Task<(byte[] Contenido, string Tipo)?> Archivo(string ruta)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, ruta);
        var token = Token();
        if (!string.IsNullOrWhiteSpace(token))
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        try
        {
            using var res = await Http.SendAsync(req);
            if (!res.IsSuccessStatusCode)
                return null;
            return (await res.Content.ReadAsByteArrayAsync(), res.Content.Headers.ContentType?.MediaType ?? "image/jpeg");
        }
        catch (HttpRequestException) { return null; }
        catch (TaskCanceledException) { return null; }
    }
}
