using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace backend.Models;

public class Proyecto{
    
    [Key] public Guid Id{ get; set; } 
    public required string Nombre { get; set; }
    public required string Descripcion { get; set; }
    public List<Etapa> Etapas { get; set; } = [];

}