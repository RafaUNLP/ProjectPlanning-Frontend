using System.Net;
using backend.Models;
using System.ComponentModel.DataAnnotations;
using Swashbuckle.AspNetCore.Annotations;
using System.Text.Json.Serialization;

namespace backend.DTOs;

public class RespuestaHttp
{
    /// <summary>
    /// Contenido de la respuesta HTTP.
    /// </summary>
    public string? Contenido { get; set; }

    /// <summary>
    /// Código de estado HTTP.
    /// </summary>
    public HttpStatusCode Status { get; set; }
}

public class ProyectoDTO
{
    /// <summary>
    /// ID único del proyecto.
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// Nombre del proyecto. Este campo es obligatorio.
    /// </summary>
    [Required]
    public string Nombre { get; set; }

    /// <summary>
    /// Descripción detallada del proyecto. Este campo es obligatorio.
    /// </summary>
    [Required]
    public string Descripcion { get; set; }

    /// <summary>
    /// Lista de etapas del proyecto. Este campo es obligatorio.
    /// </summary>
    [Required]
    public List<EtapaDTO> Etapas { get; set; } = new List<EtapaDTO>();
}

public class EtapaDTO
{
    /// <summary>
    /// ID único de la etapa.
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// Nombre de la etapa del proyecto. Este campo es obligatorio.
    /// </summary>
    [Required]
    public string Nombre { get; set; }

    /// <summary>
    /// Descripción de la etapa. Este campo es obligatorio.
    /// </summary>
    [Required]
    public string Descripcion { get; set; }

    /// <summary>
    /// Fecha de inicio de la etapa. Este campo es obligatorio.
    /// </summary>
    [Required]
    public DateTime FechaInicio { get; set; }

    /// <summary>
    /// Fecha de finalización de la etapa. Este campo es obligatorio.
    /// </summary>
    [Required]
    public DateTime FechaFin { get; set; }

    /// <summary>
    /// Categoría de colaboración para la etapa.
    /// </summary>
    public CategoriaColaboracion CategoriaColaboracion { get; set; } = CategoriaColaboracion.SinColaboracion;

    /// <summary>
    /// Descripción adicional sobre la colaboración.
    /// </summary>
    public string? DescripcionColaboracion { get; set; }
}

public class BonitaProcessResponse
{
    public string id { get; set; }
    public string name { get; set; }
    public string version { get; set; }
}

public class BonitaCaseResponse
{
    public long caseId { get; set; }
}

public enum ActivityStateEnum
{
    [JsonPropertyName("failed")] Failed,
    [JsonPropertyName("initializing")] Initializing,
    [JsonPropertyName("ready")] Ready,
    [JsonPropertyName("executing")] Executing,
    [JsonPropertyName("completing")] Completing,
    [JsonPropertyName("completed")] Completed,
    [JsonPropertyName("waiting")] Waiting,
    [JsonPropertyName("skipped")] Skipped,
    [JsonPropertyName("cancelled")] Cancelled,
    [JsonPropertyName("aborted")] Aborted,
    [JsonPropertyName("cancelling subtasks")] CancellingSubtasks,
    [JsonPropertyName("aborting activity with boundary")] AbortingWithBoundary,
    [JsonPropertyName("completing activity with boundary")] CompletingWithBoundary
}

public class BonitaActivityResponse
{
    public string id { get; set; }
    public string name { get; set; }
    // public ActivityStateEnum state { get; set; }
    public string type { get; set; }
    public string priority { get; set; }
    public string actorId { get; set; }

}

public class BonitaUserResponse
{
    public string id { get; set; }
    public string userName { get; set; }
    public string firstName { get; set; }
    public string lastName { get; set; }

    public string enabled { get; set; }
}
