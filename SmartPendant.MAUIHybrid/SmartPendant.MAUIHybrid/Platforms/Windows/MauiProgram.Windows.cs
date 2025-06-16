using SmartPendant.MAUIHybrid.Abstractions;
using SmartPendant.MAUIHybrid.Platforms.Windows;


namespace SmartPendant.MAUIHybrid
{
    public static partial class MauiProgram
    {
        static partial void RegisterPlatformServices(IServiceCollection services)
        {
            services.AddSingleton<IOrchestrationService, WindowsOrchestrorService>();
        }
    }
}
