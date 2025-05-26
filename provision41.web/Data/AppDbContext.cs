using Microsoft.EntityFrameworkCore;
using provision41.web.Models;
namespace provision41.web.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    public DbSet<Truck> Trucks { get; set; }
    public DbSet<DumpLog> DumpLogs { get; set; }
}
