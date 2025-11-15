using backend.Data;
using backend.Models;

namespace backend.Repositories;

public class ProyectoRepository : BaseRepository<Proyecto>
{
    public ProyectoRepository(AppDbContext context) : base(context) { }

}