using Android.Content;
using MudBlazor;
using SmartPendant.MAUIHybrid.Abstractions;
using SmartPendant.MAUIHybrid.Models;
using SmartPendant.MAUIHybrid.Services;
using System.Diagnostics;

namespace SmartPendant.MAUIHybrid.Platforms.Android
{
    public class AndroidOrchestrationService : IOrchestrationService
    {
        #region Fields
        private readonly AudioPipelineManager _pipelineManager;
        private readonly IConversationRepository _conversationRepository;
        private readonly IDayJournalRepository _dayJournalRepository;
        private readonly ConversationInsightService _conversationInsightService;
        private readonly DailyJournalInsightService _dailyJournalInsightService;
        private readonly Intent _serviceIntent;
        private bool _isGeneratingInsight;
        #endregion

        #region Properties
        public bool IsRecording { get; private set; }
        public bool IsDeviceConnected { get; private set; }
        public bool IsStateChanging { get; private set; }
        public bool IsGeneratingInsight => _isGeneratingInsight;
        public ConversationRecord CurrentConversation => _pipelineManager.CurrentConversation;
        public DayRecord CurrentDay => _pipelineManager.CurrentDay;
        #endregion

        #region Events
        public event EventHandler? StateHasChanged;
        public event EventHandler<(string message, Severity severity)>? Notify;
        public event EventHandler? ConversationCompleted;
        public event EventHandler<(bool isRecording, bool isDeviceConnected, bool isStateChanging)>? SetStateEvent;
        #endregion

        #region Constructor
        public AndroidOrchestrationService(
            AudioPipelineManager pipelineManager,
            IConversationRepository conversationRepository,
            IDayJournalRepository dayJournalRepository,
            ConversationInsightService conversationInsightService,
            DailyJournalInsightService dailyJournalInsightService)
        {
            _pipelineManager = pipelineManager;
            _conversationRepository = conversationRepository;
            _dayJournalRepository = dayJournalRepository;
            _conversationInsightService = conversationInsightService;
            _dailyJournalInsightService = dailyJournalInsightService;
            _serviceIntent = new Intent(Platform.CurrentActivity ?? throw new InvalidOperationException("CurrentActivity is null"), typeof(AudioProcessingService));
            
            // Forward events from the pipeline manager to the UI
            _pipelineManager.StateHasChanged += (s, e) => StateHasChanged?.Invoke(s, e);
            _pipelineManager.ConversationCompleted += OnConversationCompletedAsync;
            _pipelineManager.Notify += (s, e) => Notify?.Invoke(s, e);
            _pipelineManager.SetStateEvent += (object? s, (bool isRecording, bool isDeviceConnected, bool isStateChanging) state) =>
            {
                SetState(state.isRecording, state.isDeviceConnected, state.isStateChanging);
            };
        }
        #endregion

        #region Public Methods
        public Task StartAsync()
        {
            if (IsRecording) return Task.CompletedTask;

            SetState(isStateChanging: true);
            try
            {
                // The actual logic is now inside the Android Service,
                // which will be started here. The service will then start the pipeline.
                Platform.CurrentActivity?.StartForegroundService(_serviceIntent);
                //Event set in _pipelineManager.StartPipelineAsync
                //SetState(isRecording: true, isDeviceConnected: true, isStateChanging: false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to start foreground service: {ex.Message}");
                Notify?.Invoke(this, ($"Start failed: {ex.Message}", Severity.Error));
                SetState(isStateChanging: false);
            }
            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            if (!IsRecording && !IsStateChanging) return Task.CompletedTask;

            SetState(isStateChanging: true);
            try
            {
                // This will trigger the OnDestroy method in the service,
                // which in turn calls StopPipelineAsync.
                Platform.CurrentActivity?.StopService(_serviceIntent);
                //Event set in _pipelineManager.StopPipelineAsync
                //SetState(isRecording: false, isDeviceConnected: false, isStateChanging: false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to stop foreground service: {ex.Message}");
                Notify?.Invoke(this, ($"Stop failed: {ex.Message}", Severity.Error));
                SetState(isStateChanging: false); // Reset state even on failure
            }
            return Task.CompletedTask;
        }

        public async Task GenerateInsightAsync(bool interim = false, CancellationToken cancellationToken = default)
        {
            var conversationRecord = CurrentConversation;
            var dayRecord = CurrentDay;

            var todaysConversations = await _conversationRepository.GetConversationsByDateAsync(DateTime.Now.Date);

            dayRecord.Conversations = dayRecord.Conversations
                .UnionBy(todaysConversations, c => c.Id)
                .ToList();

            if (conversationRecord is null || !conversationRecord.Transcript.Any())
            {
                Notify?.Invoke(this, ("There is no transcript content to generate insights from.", Severity.Info));
                return;
            }

            _isGeneratingInsight = true;
            StateHasChanged?.Invoke(this, EventArgs.Empty);

            try
            {
                await _conversationInsightService.GenerateAndApplyInsightAsync(conversationRecord, cancellationToken);
                await _dailyJournalInsightService.GenerateAndApplyDailyInsightAsync(dayRecord, cancellationToken);
                
                if (interim)
                    Notify?.Invoke(this, ("Insights generated", Severity.Success));
            }
            catch (OperationCanceledException)
            {
                Notify?.Invoke(this, ("Insight generation was canceled.", Severity.Warning));
                throw;
            }
            catch (Exception ex)
            {
                Notify?.Invoke(this, ($"Failed to generate insight: {ex.Message}", Severity.Error));
                throw;
            }
            finally
            {
                _isGeneratingInsight = false;
                StateHasChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public async Task SaveCurrentConversationAsync()
        {
            var conversationModel = CurrentConversation;
            if (conversationModel != null)
            {
                await _conversationRepository.SaveConversationAsync(conversationModel);
            }
        }

        public async Task SaveCurrentDayAsync()
        {
            var dayRecord = CurrentDay;
            if (dayRecord != null)
            {
                await _dayJournalRepository.SaveAsync(dayRecord);
            }
        }
        #endregion

        #region Private Methods
        private void SetState(bool? isRecording = null, bool? isDeviceConnected = null, bool? isStateChanging = null)
        {
            IsRecording = isRecording ?? IsRecording;
            IsDeviceConnected = isDeviceConnected ?? IsDeviceConnected;
            IsStateChanging = isStateChanging ?? IsStateChanging;
            StateHasChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Handles the completion of a conversation, triggers insight generation and saving.
        /// </summary>
        private async void OnConversationCompletedAsync(object? sender, EventArgs e)
        {
            try
            {
                // First, generate the AI insight for the completed conversation.
                await GenerateInsightAsync();

                // Next, save the conversation with the generated insights.
                await SaveCurrentConversationAsync();
                await SaveCurrentDayAsync();
            }
            catch (Exception ex)
            {
                Notify?.Invoke(this, ($"A critical error occurred during conversation finalization: {ex.Message}", Severity.Error));
            }
        }
        #endregion

        #region IAsyncDisposable
        public async ValueTask DisposeAsync()
        {
            await StopAsync();
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}