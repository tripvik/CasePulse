using Blazored.LocalStorage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MudBlazor.Services;
using SmartPendant.MAUIHybrid.Services;
using System;
using System.Reflection;

namespace SmartPendant.MAUIHybrid
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });
            builder.ConfigureAppSettings();
            builder.Services.AddMauiBlazorWebView();
            builder.Services.AddBlazoredLocalStorage();
            builder.Services.AddMudServices();
            builder.Services.AddScoped<UserPreferencesService>();
            builder.Services.AddScoped<LayoutService>();
            //builder.Services.AddSingleton<ITranscriptionService>(sp =>
            //{
            //    return new FileTranscriptionService();
            //}
            //);
            builder.Services.AddSingleton<ITranscriptionService>(sp =>
            {
                var endpoint = builder.Configuration["Azure:Speech:Endpoint"] ?? throw new InvalidOperationException("Azure Speech Endpoint is not configured. Please check your appsettings.json or environment variables.");
                var subscriptionKey = builder.Configuration["Azure:Speech:Key"] ?? throw new InvalidOperationException("Azure Speech Subscription Key is not configured. Please check your appsettings.json or environment variables."); ;
                var uri = new Uri(endpoint);
                return new TranscriptionService(uri, subscriptionKey);
            }
            );

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }

    private static void ConfigureAppSettings(this MauiAppBuilder builder)
        {
            string baseConfigFileName = $"{Assembly.GetExecutingAssembly().GetName().Name}.appsettings.json";
            string devConfigFileName = $"{Assembly.GetExecutingAssembly().GetName().Name}.appsettings.development.json";
            var assembly = Assembly.GetExecutingAssembly();

            // Load the base appsettings.json
            using var baseStream = assembly.GetManifestResourceStream(baseConfigFileName);
            if (baseStream != null)
            {
                builder.Configuration.AddJsonStream(baseStream);
            }

            // Check if appsettings.development.json exists and load it
            using var devStream = assembly.GetManifestResourceStream(devConfigFileName);
            if (devStream != null)
            {
                builder.Configuration.AddJsonStream(devStream);
            }
        }
    }
}
