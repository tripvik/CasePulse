using Blazored.LocalStorage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MudBlazor.Services;
using SmartPendant.MAUIHybrid.Abstractions;
using SmartPendant.MAUIHybrid.Services;
using System.Reflection;

namespace SmartPendant.MAUIHybrid
{
    public static partial class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            ConfigureApp(builder);
            ConfigureServices(builder);
            ConfigureDevelopmentServices(builder);

            return builder.Build();
        }

        #region App Configuration

        private static void ConfigureApp(MauiAppBuilder builder)
        {
            builder.UseMauiApp<App>()
                   .ConfigureFonts(fonts => fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular"));

            builder.ConfigureAppSettings();
        }

        private static void ConfigureAppSettings(this MauiAppBuilder builder)
        {
            var assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
            var baseConfigFileName = $"{assemblyName}.appsettings.json";
            var devConfigFileName = $"{assemblyName}.appsettings.development.json";
            var assembly = Assembly.GetExecutingAssembly();

            using var baseStream = assembly.GetManifestResourceStream(baseConfigFileName);
            if (baseStream != null) builder.Configuration.AddJsonStream(baseStream);

            using var devStream = assembly.GetManifestResourceStream(devConfigFileName);
            if (devStream != null) builder.Configuration.AddJsonStream(devStream);
        }

        #endregion

        #region Service Registration

        //implemetation in respective platfoms.
        static partial void RegisterPlatformServices(IServiceCollection services);

        private static void ConfigureServices(MauiAppBuilder builder)
        {
            RegisterCoreServices(builder);
            RegisterTranscriptionServices(builder);
            RegisterConnectionServices(builder);
            RegisterStorageServices(builder);
            RegisterPlatformServices(builder.Services);
        }

        private static void RegisterCoreServices(MauiAppBuilder builder)
        {
            builder.Services.AddMauiBlazorWebView();
            builder.Services.AddBlazoredLocalStorage();
            builder.Services.AddMudServices();
            builder.Services.AddScoped<UserPreferencesService>();
            builder.Services.AddScoped<LayoutService>();
            builder.Services.AddSingleton<ConversationService>();
        }

        private static void RegisterTranscriptionServices(MauiAppBuilder builder)
        {
            var config = builder.Configuration;
            var useMockData = config.GetValue<bool>("UseMockData");
            var useFileService = config.GetValue<bool>("UseFileRecording");
            var useOpenAI = config.GetValue<bool>("UseOpenAI");

            if (useMockData)
            {
                builder.Services.AddSingleton<ITranscriptionService, MockTranscriptionService>();
                return;
            }

            if (useFileService)
            {
                builder.Services.AddSingleton<ITranscriptionService, FileTranscriptionService>();
            }
            else if (useOpenAI)
            {
                builder.Services.AddSingleton<ITranscriptionService, OpenAITranscriptionService>();
            }
            else
            {
                builder.Services.AddSingleton<ITranscriptionService, SpeechTranscriptionService>();
            }
        }

        private static void RegisterConnectionServices(MauiAppBuilder builder)
        {
            var config = builder.Configuration;
            var useMockData = config.GetValue<bool>("UseMockData");
            var useBLE = config.GetValue<bool>("UseBLE");

            if (useMockData)
            {
                builder.Services.AddSingleton<IConnectionService, MockConnectionService>();
            }
            else if (useBLE)
            {
                builder.Services.AddSingleton<IConnectionService, BLEService>();
            }
            else
            {
                builder.Services.AddSingleton<IConnectionService, BluetoothClassicService>();
            }
        }


        private static void RegisterStorageServices(MauiAppBuilder builder)
        {
            builder.Services.AddSingleton<IStorageService, BlobStorageService>();
        }

        #endregion

        #region Development Configuration

        private static void ConfigureDevelopmentServices(MauiAppBuilder builder)
        {
#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif
        }

        #endregion
    }
}