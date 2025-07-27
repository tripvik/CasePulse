using SmartPendant.MAUIHybrid.Services;

namespace SmartPendant.MAUIHybrid
{
    public partial class App : Application
    {
        public App(DatabaseInitializationService databaseService)
        {
            // Initialize database on app startup
            Task.Run(async () => await InitializeDatabaseAsync(databaseService));

            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new MainPage()) { Title = "SmartPendant.MAUIHybrid" };
        }

        private async Task InitializeDatabaseAsync(DatabaseInitializationService databaseService)
        {
            try
            {
                await databaseService.InitializeDatabaseAsync();

                // Optional: Check if database exists
                var exists = await databaseService.DatabaseExistsAsync();
                System.Diagnostics.Debug.WriteLine($"Database exists: {exists}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Database initialization error: {ex.Message}");
            }
        }
    }
}
