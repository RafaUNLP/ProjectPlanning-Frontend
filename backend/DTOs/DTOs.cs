using System.Net;
using backend.Models;
using System.ComponentModel.DataAnnotations;
using Swashbuckle.AspNetCore.Annotations;

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
