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
            bool useFileService = builder.Configuration.GetValue<bool>("UseFileRecording");
            if (useFileService)
            {
                builder.Services.AddSingleton<ITranscriptionService, FileTranscriptionService>();
            }
            else
            {
                builder.Services.AddSingleton<ITranscriptionService, SpeechTranscriptionService>();
            }
            builder.Services.AddSingleton<IStorageService,BlobStorageService>();
            builder.Services.AddSingleton<BLEService>();

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
