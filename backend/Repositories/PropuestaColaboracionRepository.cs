using backend.Data;
using backend.Models;

namespace backend.Repositories;

public class PropuestaColaboracionRepository : BaseRepository<PropuestaColaboracion>
{
    public PropuestaColaboracionRepository(AppDbContext context) : base(context) { }
}