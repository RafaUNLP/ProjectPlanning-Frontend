using System.Linq.Expressions;
using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Repositories;

public class ProyectoRepository : BaseRepository<Proyecto>
{
    public ProyectoRepository(AppDbContext context) : base(context) { }

}