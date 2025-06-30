using MudBlazor;
using SmartPendant.MAUIHybrid.Models;

namespace SmartPendant.MAUIHybrid.Services
{
    public class LayoutService
    {
        #region Fields

        private readonly UserPreferencesService _userPreferencesService;
        private UserPreferences _userPreferences;
        private bool _systemPreferences;

        #endregion

        #region Properties

        public DarkLightMode DarkModeToggle { get; private set; } = DarkLightMode.System;
        public bool IsDarkMode { get; private set; } = true;
        public MudTheme CurrentTheme { get; private set; } = new MudTheme();

        #endregion

        #region Events

        public event EventHandler? MajorUpdateOccured;

        #endregion

        #region Constructor

        public LayoutService(UserPreferencesService userPreferencesService)
        {
            _userPreferencesService = userPreferencesService ?? throw new ArgumentNullException(nameof(userPreferencesService));
            _userPreferences = new UserPreferences();
        }

        #endregion

        #region Public Methods

        public void SetDarkMode(bool value)
        {
            IsDarkMode = value;
            OnMajorUpdateOccured();
        }

        public async Task ApplyUserPreferences(bool isDarkModeDefaultTheme)
        {
            _systemPreferences = isDarkModeDefaultTheme;
            var loadedPreferences = await _userPreferencesService.LoadUserPreferences();

            if (loadedPreferences != null)
            {
                _userPreferences = loadedPreferences;
                DarkModeToggle = _userPreferences.DarkLightTheme;
            }
            else
            {
                _userPreferences = new UserPreferences { DarkLightTheme = DarkLightMode.System };
                await _userPreferencesService.SaveUserPreferences(_userPreferences);
                DarkModeToggle = DarkLightMode.System;
            }

            IsDarkMode = DarkModeToggle switch
            {
                DarkLightMode.Dark => true,
                DarkLightMode.Light => false,
                DarkLightMode.System => isDarkModeDefaultTheme,
                _ => isDarkModeDefaultTheme
            };

            OnMajorUpdateOccured();
        }

        public async Task OnSystemPreferenceChanged(bool newValue)
        {
            _systemPreferences = newValue;
            if (DarkModeToggle == DarkLightMode.System)
            {
                IsDarkMode = newValue;
                OnMajorUpdateOccured();
            }

            await Task.CompletedTask;
        }

        public async Task ToggleDarkMode()
        {
            switch (DarkModeToggle)
            {
                case DarkLightMode.System:
                    DarkModeToggle = DarkLightMode.Light;
                    IsDarkMode = false;
                    break;
                case DarkLightMode.Light:
                    DarkModeToggle = DarkLightMode.Dark;
                    IsDarkMode = true;
                    break;
                case DarkLightMode.Dark:
                    DarkModeToggle = DarkLightMode.System;
                    IsDarkMode = _systemPreferences;
                    break;
            }

            _userPreferences.DarkLightTheme = DarkModeToggle;
            await _userPreferencesService.SaveUserPreferences(_userPreferences);
            OnMajorUpdateOccured();
        }

        public void SetBaseTheme(MudTheme theme)
        {
            CurrentTheme = theme ?? throw new ArgumentNullException(nameof(theme));
            OnMajorUpdateOccured();
        }

        #endregion

        #region Private / Protected Methods

        protected virtual void OnMajorUpdateOccured() => MajorUpdateOccured?.Invoke(this, EventArgs.Empty);

        #endregion
    }
}