using System.Net;

namespace backend.DTOs;
public class RespuestaHttp
{
    public string? Contenido { get; set; }
    public HttpStatusCode Status { get; set; }
}