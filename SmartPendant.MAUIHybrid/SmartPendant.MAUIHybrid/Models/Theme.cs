using MudBlazor;


namespace SmartPendant.MAUIHybrid.Models
{
    public class Theme
    {
        public static MudTheme AdminPanelTheme()
        {
            var theme = new MudTheme()
            {
                PaletteLight = new()
                {
                    Primary = "#2196F3",
                    Black = "#000000",
                    White = "#FFFFFF",
                    Info = "#0288D1",
                    Success = "#4CAF50",
                    Warning = "#FFA000",
                    Error = "#F44336",
                    Dark = "#424242",
                    TextPrimary = "rgba(0,0,0, 0.87)",
                    TextSecondary = "rgba(0,0,0, 0.60)",
                    TextDisabled = "rgba(0,0,0, 0.38)",
                    ActionDefault = "#2196F3",
                    ActionDisabled = "rgba(0,0,0, 0.26)",
                    ActionDisabledBackground = "rgba(0,0,0, 0.12)",
                    Background = "#FFFFFF",
                    BackgroundGray = "#F5F5F5",
                    Surface = "#FFFFFF",
                    DrawerBackground = "#FFFFFF",
                    DrawerText = "rgba(0,0,0, 0.87)",
                    DrawerIcon = "rgba(0,0,0, 0.54)",
                    AppbarBackground = "#2196F3",
                    AppbarText = "rgba(255,255,255, 0.87)",
                    LinesDefault = "rgba(0,0,0, 0.12)",
                    LinesInputs = "rgba(0,0,0, 0.3)",
                    TableLines = "rgba(224, 224, 224, 1)",
                    TableStriped = "rgba(0,0,0, 0.02)",
                    Divider = "rgba(0,0,0, 0.12)",
                    DividerLight = "rgba(0,0,0, 0.06)"
                },
                PaletteDark = new()
                {
                    Primary = "#2196F3",
                    Black = "#121212",
                    Info = "#03DAC6",
                    Success = "#4CAF50",
                    Warning = "#FFC107",
                    Error = "#F44336",
                    Dark = "#121212",
                    TextPrimary = "rgba(255,255,255, 0.87)",
                    TextSecondary = "rgba(255,255,255, 0.60)",
                    TextDisabled = "rgba(255,255,255, 0.38)",
                    ActionDefault = "#2196F3",
                    ActionDisabled = "rgba(255,255,255, 0.26)",
                    ActionDisabledBackground = "rgba(255,255,255, 0.12)",
                    Background = "#121212",
                    BackgroundGray = "#1E1E1E",
                    Surface = "#1E1E1E",
                    DrawerBackground = "#1E1E1E",
                    DrawerText = "rgba(255,255,255, 0.60)",
                    DrawerIcon = "rgba(255,255,255, 0.60)",
                    AppbarBackground = "#272727",
                    AppbarText = "rgba(255,255,255, 0.87)",
                    LinesDefault = "rgba(255,255,255, 0.12)",
                    LinesInputs = "rgba(255,255,255, 0.3)",
                    TableLines = "rgba(255,255,255, 0.12)",
                    TableStriped = "rgba(255,255,255, 0.06)",
                    Divider = "rgba(255,255,255, 0.12)",
                    DividerLight = "rgba(255,255,255, 0.06)"
                },
                LayoutProperties = new LayoutProperties(),
                Typography = new Typography()
                {
                    Default = new DefaultTypography()
                    {
                        FontFamily = new[] { "Roboto", "Arial", "sans-serif" },
                    },
                    Subtitle1 = new Subtitle1Typography()
                    {
                        FontSize = "1.25rem"
                    },
                }
            };
            return theme;
        }
    }
}
