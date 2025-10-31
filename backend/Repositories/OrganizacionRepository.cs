using System.Linq.Expressions;
using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Repositories;

public class OrganizacionRepository : BaseRepository<Organizacion>
{
    public OrganizacionRepository(AppDbContext context) : base(context) { }

}