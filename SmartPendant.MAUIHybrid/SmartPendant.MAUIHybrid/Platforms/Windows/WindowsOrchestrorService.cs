using MudBlazor;
using SmartPendant.MAUIHybrid.Abstractions;
using SmartPendant.MAUIHybrid.Models;
using SmartPendant.MAUIHybrid.Services;

namespace SmartPendant.MAUIHybrid.Platforms.Windows
{
    public class WindowsOrchestrationService : IOrchestrationService
    {
        #region Fields
        private readonly AudioPipelineManager _pipelineManager;
        private readonly IConversationRepository _conversationRepository;
        private readonly IDayJournalRepository _dayJournalRepository;
        private readonly ConversationInsightService _conversationInsightService;
        private readonly DailyJournalInsightService _dailyJournalInsightService;
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
        public event EventHandler? ConversationCompleted;
        public event EventHandler<(string message, Severity severity)>? Notify;
        public event EventHandler<(bool isRecording, bool isDeviceConnected, bool isStateChanging)>? SetStateEvent;
        #endregion

        #region Constructor
        public WindowsOrchestrationService(
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
            
            _pipelineManager.StateHasChanged += (s, e) => StateHasChanged?.Invoke(s, e);
            _pipelineManager.ConversationCompleted += OnConversationCompletedAsync;
            _pipelineManager.Notify += (s, e) => Notify?.Invoke(s, e);
            _pipelineManager.SetStateEvent += (s, e) => SetStateEvent?.Invoke(s, e);
            _pipelineManager.SetStateEvent += (object? s, (bool isRecording, bool isDeviceConnected, bool isStateChanging) state) =>
            {
                SetState(state.isRecording, state.isDeviceConnected, state.isStateChanging);
            };
        }
        #endregion

        #region Public Methods
        public async Task StartAsync()
        {
            if (IsRecording) return;
            SetState(isStateChanging: true);

            var (success, errorMessage) = await _pipelineManager.StartPipelineAsync();
            if (!success)
            {
                Notify?.Invoke(this, (errorMessage ?? "An unknown error occurred.", Severity.Error));
                SetState(isRecording: false, isDeviceConnected: false, isStateChanging: false);
            }
        }

        public async Task StopAsync()
        {
            if (!IsRecording && !IsStateChanging) return;
            SetState(isStateChanging: true);
            await _pipelineManager.StopPipelineAsync();
            //Event set in PiplineManager.StopPipelineAsync
            //SetState(isRecording: false, isDeviceConnected: false, isStateChanging: false);
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
                Notify?.Invoke(this, ("Conversation saved", Severity.Success));
            }
        }

        public async Task SaveCurrentDayAsync()
        {
            var dayRecord = CurrentDay;
            if (dayRecord != null)
            {
                await _dayJournalRepository.SaveAsync(dayRecord);
                Notify?.Invoke(this, ("Day record saved", Severity.Success));
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
            await _pipelineManager.DisposeAsync();
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}