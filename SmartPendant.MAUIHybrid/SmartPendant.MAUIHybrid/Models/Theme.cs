using MudBlazor;


namespace SmartPendant.MAUIHybrid.Models
{
    public class Theme
    {
        public static MudTheme AdminPanelTheme()
        {
            var theme = new MudTheme()
            {
                //PaletteLight = AdminPanelLightPalette,
                //PaletteDark = AdminPanelDarkPalette,
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

        #region AdminPanel

        private static readonly PaletteLight AdminPanelLightPalette = new()
        {
            Black = "#110e2d",
            AppbarText = "#424242",
            AppbarBackground = "rgba(255,255,255,0.8)",
            DrawerBackground = "#ffffff",
            GrayLight = "#e8e8e8",
            GrayLighter = "#f9f9f9"
        };

        private static readonly PaletteDark AdminPanelDarkPalette = new()
        {
            Primary = "#4e3eb1",
        };
        #endregion
    }

}
