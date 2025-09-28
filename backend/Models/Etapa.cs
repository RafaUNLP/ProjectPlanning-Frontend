using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace backend.Models;

public class Etapa{
    [Key]
    public Guid Id { get; set; }
    public required Guid ProyectoId { get; set; }
    public required string Nombre { get; set; }
    public required string Descripcion { get; set; }
    [Column(TypeName = "timestamp")] public required DateTime FechaInicio { get; set; }
    [Column(TypeName = "timestamp")] public required DateTime FechaFin { get; set; }
    [DefaultValue(0)]//valor default en BD
    public CategoriaColaboracion CategoriaColaboracion { get; set; } = CategoriaColaboracion.SinColaboracion;//valor default al instanciación
    public string? DescripcionColaboracion { get; set; }
}

public enum CategoriaColaboracion{
    SinColaboracion = 0, Economica = 1, Material = 2, ManoDeObra = 3, Otra = 4
}