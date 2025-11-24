using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using backend.DTOs;

namespace backend.Models;

public class Etapa{
    [Key]
    public Guid Id { get; set; }
    public Guid ProyectoId { get; set; }
    public required string Nombre { get; set; }
    public required string Descripcion { get; set; }
    [Column(TypeName = "timestamp")] public required DateTime FechaInicio { get; set; }
    [Column(TypeName = "timestamp")] public required DateTime FechaFin { get; set; }
    public required bool RequiereColaboracion { get; set; }
    public Guid? ColaboracionId { get; set; }
}