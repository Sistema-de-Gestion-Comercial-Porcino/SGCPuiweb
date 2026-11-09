namespace SGCP.Web.Services;

public class FrioApi(HttpClient http, IHttpContextAccessor acceso) : ClienteApiBase(http, acceso) { }
