using backend.Data;
using backend.Models;

namespace backend.Repositories;

public class EtapaRepository : BaseRepository<Etapa>
{
    public EtapaRepository(AppDbContext context) : base(context) { }

}