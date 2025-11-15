using backend.Data;
using backend.Models;

namespace backend.Repositories;

public class OrganizacionRepository : BaseRepository<Organizacion>
{
    public OrganizacionRepository(AppDbContext context) : base(context) { }

}