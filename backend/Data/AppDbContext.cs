using Microsoft.EntityFrameworkCore;
using backend.Models;

namespace backend.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Proyecto> Proyecto { get; set; }
    public DbSet<Etapa> Etapa { get; set; }
    public DbSet<Organizacion> Organizacion { get; set; }
    public DbSet<Colaboracion> Colaboracion { get; set; }
}
