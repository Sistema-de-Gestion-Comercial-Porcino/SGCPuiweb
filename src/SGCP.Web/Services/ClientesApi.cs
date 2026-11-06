namespace SGCP.Web.Services;

public class ClientesApi(HttpClient http, IHttpContextAccessor acceso) : ClienteApiBase(http, acceso) { }
