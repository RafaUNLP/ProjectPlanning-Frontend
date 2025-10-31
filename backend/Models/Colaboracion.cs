using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace backend.Models;

public class Colaboracion
{
    [Key]
    public Guid Id { get; set; }
    public required string Descripcion { get; set; } = string.Empty;
    public required CategoriaColaboracion CategoriaColaboracion { get; set; }
    public required Guid EtapaId { get; set; }
    [Column(TypeName = "timestamp")] public DateTime? FechaRealizacion { get; set; } //será null si no se realizó
}
public enum CategoriaColaboracion{
    Economica = 1, Material = 2, ManoDeObra = 3, Otra = 4
}
