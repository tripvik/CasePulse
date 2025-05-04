using Microsoft.AspNetCore.Components;
using MudBlazor;
using SmartPendant.MAUIHybrid.Models;
using SmartPendant.MAUIHybrid.Services;


namespace SmartPendant.MAUIHybrid.Components.Layout
{
    public partial class MainLayout : LayoutComponentBase, IDisposable
    {
        [Inject] private LayoutService LayoutService { get; set; }

        private MudThemeProvider _mudThemeProvider;
        private bool _drawerOpen = false;
        private bool _isDesktop
        {
            get
            {
                return (DeviceInfo.Platform == DevicePlatform.WinUI ||
                        DeviceInfo.Platform == DevicePlatform.UWP ||
                        DeviceInfo.Platform == DevicePlatform.macOS);
            }
        }
        protected override void OnInitialized()
        {
            LayoutService.SetBaseTheme(Theme.AdminPanelTheme());
            base.OnInitialized();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            if (firstRender)
            {
                await ApplyUserPreferences();
                await _mudThemeProvider.WatchSystemPreference(OnSystemPreferenceChanged);
                StateHasChanged();
            }
        }

        private async Task ApplyUserPreferences()
        {
            var defaultDarkMode = await _mudThemeProvider.GetSystemPreference();
            await LayoutService.ApplyUserPreferences(defaultDarkMode);
        }

        private async Task OnSystemPreferenceChanged(bool newValue)
        {
            await LayoutService.OnSystemPreferenceChanged(newValue);
        }

        public void Dispose()
        {
            LayoutService.MajorUpdateOccured -= LayoutServiceOnMajorUpdateOccured;
        }

        private void ToggleDrawer()
        {
            _drawerOpen = !_drawerOpen;
        }
        private void LayoutServiceOnMajorUpdateOccured(object sender, EventArgs e) => StateHasChanged();
    }
}
