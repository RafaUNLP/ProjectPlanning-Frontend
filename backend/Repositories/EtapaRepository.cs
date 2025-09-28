using System.Linq.Expressions;
using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Repositories;

public class EtapaRepository : BaseRepository<Etapa>
{
    public EtapaRepository(AppDbContext context) : base(context) { }

}