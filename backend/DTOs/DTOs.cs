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
    public required string Nombre { get; set; }

    /// <summary>
    /// Descripción detallada del proyecto. Este campo es obligatorio.
    /// </summary>
    [Required]
    public required string Descripcion { get; set; }

    /// <summary>
    /// POR AHORA exijo el ID de la org dueña, más adelante metemos un login con JWT local y lo toma de ahí
    /// </summary>
    [Required] public required long OrganizacionId { get; set; }

    /// <summary>
    /// Lista de etapas del proyecto. Este campo es obligatorio.
    /// </summary>
    [Required]
    public List<EtapaDTO> Etapas { get; set; } = new List<EtapaDTO>();
    public bool Completado { get; set; } = false;
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
    public bool RequiereColaboracion { get; set; }

    /// <summary>
    /// Descripción adicional sobre la colaboración.
    /// </summary>
    public string? DescripcionColaboracion { get; set; }
    public Guid? ColaboracionId { get; set; }
    public bool Completada { get; set; } = false;
}

public class CrearOrganizacionDTO
{
    /// <summary>
    /// Nombre único de la organización.
    /// </summary>
    [Required] public required string Nombre { get; set; }

    /// <summary>
    /// Nombre único de la organización.
    /// </summary>
    [Required] public required string Contraseña { get; set; }
}
public class OrganizacionDTO //para ocultar la contraseña
{
   public long Id { get; set; }
    public string UserName { get; set; }
    public string Nombre { get; set; }
    public string Apellido { get; set; }
    public string Rol { get; set; } 
    public bool Enabled { get; set; }
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

public class LoginRequestDTO
{
    /// <summary>
    /// Nombre de usuario (para Bonita).
    /// </summary>
    [Required]
    public required string Username { get; set; }

    /// <summary>
    /// Contraseña (para Bonita).
    /// </summary>
    [Required]
    public required string Password { get; set; }
}

public class BonitaSession
{
    public required string JSessionId { get; set; }
    public required string BonitaToken { get; set; }
}

public class BonitaVariable
{
    public string value { get; set; }
    public string type { get; set; }
}

public class ObservacionDTO
{
    public Guid? Id { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public required Guid ColaboracionId { get; set; }
    public required DateTime FechaCarga { get; set; }
    public DateTime? FechaRealizacion { get; set; } //será null si no se realizó
    public bool Realizada {get; set;}
    public long? CaseId { get; set; } 
}
public class PropuestaColaboracionDTO
{
    [Required]
    public required Guid EtapaId { get; set; }

    [Required]
    public required long OrganizacionProponenteId { get; set; }

    [Required]
    public required string Descripcion { get; set; }

    [Required]
    public required CategoriaColaboracion CategoriaColaboracion { get; set; }

    [Required]
    public bool EsParcial { get; set; }
}

public class CrearColaboracionDTO
{
    public required string Proyecto { get; set; }
    public required string Descripcion { get; set; }
    public required CategoriaColaboracion CategoriaColaboracion { get; set; }
    public required Guid ProyectoId { get; set; }
    public required Guid EtapaId { get; set; }
    public required long OrganizacionProyectoId { get; set; } // Dueño del proyecto
    public required long OrganizacionComprometidaId { get; set; } // Quien ayuda
    public DateTime? FechaRealizacion { get; set; } // Nullable para inicializar vacío
}

public class ColaboracionDTO
{
    public Guid? Id { get; set; }
    public required string Proyecto { get; set; }
    public required string Descripcion { get; set; }
    public required CategoriaColaboracion CategoriaColaboracion { get; set; }
    public required Guid ProyectoId { get; set; }
    public required Guid EtapaId { get; set; }
    [JsonPropertyName("organizacionId")]
    public required long OrganizacionProyectoId { get; set; } // Dueño del proyecto
    public required long OrganizacionComprometidaId { get; set; } // Quien ayuda
    public DateTime? FechaRealizacion { get; set; } // Nullable para inicializar vacío
    public List<ObservacionDTO> Observaciones { get; set; } = [];
}

public enum CategoriaColaboracion{
    Economica = 1, Material = 2, ManoDeObra = 3, Otra = 4
}

public class BonitaMembershipResponse
{
    public string user_id { get; set; }
    public string role_id { get; set; }
    public string group_id { get; set; }
    public string assigned_date { get; set; }
}

public class BonitaRoleResponse
{
    public string id { get; set; }
    public string name { get; set; }
    public string displayName { get; set; }
}