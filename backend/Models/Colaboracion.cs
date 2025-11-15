using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

public class Colaboracion //CHEQUEAR qué se necesita del cloud, incorporar observaciones
{
    [Key]
    public Guid Id { get; set; }
    public required string Descripcion { get; set; } = string.Empty;
    public required CategoriaColaboracion CategoriaColaboracion { get; set; }
    public required Guid EtapaId { get; set; }
    public Guid? OrganizacionComprometidaId { get; set; } //tendrá valor cuando alguien se haga cargo de ella
    /*En el proceso de auditoría, tendremos que buscar la organizacion por Id y mandar  la colaboracion
    con un campo 'usuarioColaborador' que tenga el nombre de la misma en Bonita, para que se 
    pueda efectuar el envío de un mail */
    [Column(TypeName = "timestamp")] public DateTime? FechaRealizacion { get; set; } //será null si no se realizó
    public List<Observacion> Observaciones { get; set; } = [];
    public bool Realizada() => this.FechaRealizacion != null;
}
public enum CategoriaColaboracion{
    Economica = 1, Material = 2, ManoDeObra = 3, Otra = 4
}
