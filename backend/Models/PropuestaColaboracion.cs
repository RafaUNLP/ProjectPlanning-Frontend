using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using backend.DTOs;

namespace backend.Models;

public class PropuestaColaboracion
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public required Guid EtapaId { get; set; }

    [Required]
    public required long OrganizacionProponenteId { get; set; }

    [Required]
    public required string Descripcion { get; set; }

    [Required]
    public required CategoriaColaboracion CategoriaColaboracion { get; set; }

    // 'true' = Parcial, 'false' = Total
    [Required]
    public bool EsParcial { get; set; } 
    public EstadoPropuestaColaboracion Estado { get; set; } = EstadoPropuestaColaboracion.Pendiente;

    [ForeignKey("EtapaId")]
    public Etapa? Etapa { get; set; }
}

public enum EstadoPropuestaColaboracion 
{
    Pendiente = 1,
    Aceptada = 2,
    Rechazada = 3
}