using Microsoft.EntityFrameworkCore;

namespace SmartPendant.MAUIHybrid.Data
{
    public class SmartPendantDbContext : DbContext
    {
        public DbSet<Foo> Foos { get; set; }

        public SmartPendantDbContext(DbContextOptions<SmartPendantDbContext> options)
            : base(options) { }
    }

    public class Foo
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}