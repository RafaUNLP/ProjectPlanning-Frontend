using System.ComponentModel.DataAnnotations;

namespace backend.Models;
public class Organizacion
{
    [Key]
    public Guid Id { get; set; }
    public required string Nombre { get; set; }
    public required string Contraseña { get; set; } //hay que hashearla
    public List<Proyecto> Proyectos { get; set; } = [];
    public List<Colaboracion> ColaboracionesComprometida { get; set; } = [];
}
