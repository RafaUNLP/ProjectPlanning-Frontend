using System.Linq.Expressions;
using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Repositories;

public class ColaboracionRepository : BaseRepository<Colaboracion>
{
    public ColaboracionRepository(AppDbContext context) : base(context) { }

}