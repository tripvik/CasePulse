using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using Blazored.LocalStorage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MudBlazor.Services;
using SmartPendant.MAUIHybrid.Abstractions;
using SmartPendant.MAUIHybrid.Services;
using System.Reflection;
using System.ClientModel;


// Platform-specific using directives to resolve service implementations
#if ANDROID
using SmartPendant.MAUIHybrid.Platforms.Android;
#elif WINDOWS
using SmartPendant.MAUIHybrid.Platforms.Windows;
#endif

namespace SmartPendant.MAUIHybrid
{
    public static partial class MauiProgram
    {
        public static IServiceProvider Services { get; private set; } = default!;

        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            ConfigureApp(builder);
            ConfigureServices(builder);
            ConfigureDevelopmentServices(builder);

            var mauiApp = builder.Build();
            Services = mauiApp.Services;
            return mauiApp;
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
        private static void ConfigureServices(MauiAppBuilder builder)
        {
            // Register core, platform-agnostic services
            builder.Services.AddMauiBlazorWebView();
            builder.Services.AddMudServices();
            builder.Services.AddBlazoredLocalStorage();

            // Register application-specific singleton services
            builder.Services.AddScoped<IConversationService, LocalStorageConversationService>();
            builder.Services.AddSingleton<AudioPipelineManager>();
            builder.Services.AddSingleton<InsightService>();
            builder.Services.AddScoped<UserPreferencesService>();
            builder.Services.AddScoped<LayoutService>();
            builder.Services.AddSingleton<IStorageService, BlobStorageService>();

            var openAIKey = builder.Configuration["Azure:OpenAI:ApiKey"];
            var openAIEndpoint = builder.Configuration["Azure:OpenAI:Endpoint"];
            var openAIDeployment = builder.Configuration["Azure:OpenAI:DeploymentName"];

            if (string.IsNullOrEmpty(openAIDeployment) || string.IsNullOrEmpty(openAIKey) || string.IsNullOrEmpty(openAIEndpoint))
            {
                throw new InvalidOperationException("Azure OpenAI configuration (DeploymentName, ApiKey, or Endpoint) is missing.");
            }

            var azureOpenAi = new AzureOpenAIClient(new Uri(openAIEndpoint), new ApiKeyCredential(openAIKey));
            var chatClient = azureOpenAi.GetChatClient(openAIDeployment).AsIChatClient();
            builder.Services.AddChatClient(chatClient).UseFunctionInvocation().UseLogging();
            
            // Register services that have different implementations per platform
            RegisterPlatformDependentServices(builder);
        }

        private static void RegisterPlatformDependentServices(MauiAppBuilder builder)
        {
            var useMockData = builder.Configuration.GetValue<bool>("UseMockData");
            var useFileRecording = builder.Configuration.GetValue<bool>("UseFileRecording");
            var useBLE = builder.Configuration.GetValue<bool>("UseBLE");
            var useOpenAI = builder.Configuration.GetValue<bool>("UseOpenAI");

#if WINDOWS

            builder.Services.AddSingleton<IOrchestrationService, WindowsOrchestrationService>();
#elif ANDROID
            // On Android, register the real orchestrator and other services.
            builder.Services.AddSingleton<IOrchestrationService, AndroidOrchestrationService>();
#endif
            if (useMockData)
            {
                builder.Services.AddSingleton<IConnectionService, MockConnectionService>();
                builder.Services.AddSingleton<ITranscriptionService, MockTranscriptionService>();
            }
            else
            {
                // Connection service
                if (useBLE)
                    builder.Services.AddSingleton<IConnectionService, BLEService>();
                else
                    builder.Services.AddSingleton<IConnectionService, BluetoothClassicService>();

                // Transcription service
                if (useFileRecording)
                    builder.Services.AddSingleton<ITranscriptionService, FileTranscriptionService>();
                else if (useOpenAI)
                    builder.Services.AddSingleton<ITranscriptionService, OpenAITranscriptionService>();
                else
                    builder.Services.AddSingleton<ITranscriptionService, SpeechTranscriptionService>();
            }
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