using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

public class Auditoria
{
    [Key]
    public Guid Id { get; set; }
    public required string Username { get; set; }
    [Column(TypeName = "timestamp")] public DateTime Fecha { get; set; } //será null si no se realizó
}