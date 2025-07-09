using SmartPendant.MAUIHybrid.Data;

namespace SmartPendant.MAUIHybrid.Services
{
    public class DatabaseInitializationService
    {
        private readonly SmartPendantDbContext _context;

        public DatabaseInitializationService(SmartPendantDbContext context)
        {
            _context = context;
        }

        public async Task InitializeDatabaseAsync()
        {
            await _context.Database.EnsureCreatedAsync();
        }

        public async Task<bool> DatabaseExistsAsync()
        {
            return await _context.Database.CanConnectAsync();
        }
    }
}
