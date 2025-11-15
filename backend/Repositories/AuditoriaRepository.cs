using backend.Data;
using backend.Models;

namespace backend.Repositories;

public class AuditoriaRepository : BaseRepository<Auditoria>
{
    public AuditoriaRepository(AppDbContext context) : base(context) { }

    public async Task<int> CountMonthlyAuditoriesByUsername(string username)
    {
        return (await this.FilterAsync(a => a.Fecha.Month == DateTime.Now.Month && a.Username == username)).Count();
    }
}