using MudBlazor;

namespace SmartPendant.MAUIHybrid.Models
{
    public class Theme
    {
        public static MudTheme AdminPanelTheme()
        {
            var theme = new MudTheme()
            {
                PaletteLight = new PaletteLight()
                {
                    Primary = "#6750A4",
                    PrimaryContrastText = "#FFFFFF",

                    Secondary = "#625B71",
                    SecondaryContrastText = "#FFFFFF",

                    Tertiary = "#7D5260",
                    TertiaryContrastText = "#FFFFFF",

                    Background = "#FFFBFE",
                    Surface = "#FFFBFE",

                    AppbarBackground = "#FFFBFE",
                    AppbarText = "#1C1B1F",

                    DrawerBackground = "#FFFBFE",
                    DrawerText = "#1C1B1F",
                    DrawerIcon = "#1C1B1F",

                    TextPrimary = "#1C1B1F",
                    TextSecondary = "#49454F",
                    TextDisabled = "rgba(28, 27, 31, 0.38)",

                    ActionDefault = "#49454F",
                    ActionDisabled = "rgba(28, 27, 31, 0.26)",
                    ActionDisabledBackground = "rgba(28, 27, 31, 0.12)",

                    LinesDefault = "#79747E",
                    LinesInputs = "#79747E",
                    Divider = "#79747E",

                    TableLines = "#E7E0EC",
                    TableStriped = "rgba(103, 80, 164, 0.04)",
                    TableHover = "rgba(103, 80, 164, 0.08)",

                    Error = "#B3261E",
                    ErrorContrastText = "#FFFFFF",

                    Success = "#2E7D32",
                    SuccessContrastText = "#FFFFFF",

                    Warning = "#FFAB00",
                    WarningContrastText = "#000000",

                    Info = "#2196F3",
                    InfoContrastText = "#FFFFFF",
                },

                PaletteDark = new PaletteDark()
                {
                    Primary = "#D0BCFF",
                    PrimaryContrastText = "#381E72",

                    Secondary = "#CCC2DC",
                    SecondaryContrastText = "#332D41",

                    Tertiary = "#EFB8C8",
                    TertiaryContrastText = "#492532",

                    Background = "#1C1B1F",
                    Surface = "#1C1B1F",

                    AppbarBackground = "#1C1B1F",
                    AppbarText = "#E6E1E5",

                    DrawerBackground = "#1C1B1F",
                    DrawerText = "#E6E1E5",
                    DrawerIcon = "#E6E1E5",

                    TextPrimary = "#E6E1E5",
                    TextSecondary = "#CAC4D0",
                    TextDisabled = "rgba(230, 225, 229, 0.38)",

                    ActionDefault = "#C4C6C9",
                    ActionDisabled = "rgba(230, 225, 229, 0.26)",
                    ActionDisabledBackground = "rgba(230, 225, 229, 0.12)",

                    LinesDefault = "#938F99",
                    LinesInputs = "#938F99",
                    Divider = "#938F99",

                    TableLines = "#444449",
                    TableStriped = "rgba(208, 188, 255, 0.04)",
                    TableHover = "rgba(208, 188, 255, 0.08)",

                    Error = "#F2B8B5",
                    ErrorContrastText = "#601410",

                    Success = "#A5D6A7",
                    SuccessContrastText = "#1B5E20",

                    Warning = "#FFD54F",
                    WarningContrastText = "#000000",

                    Info = "#90CAF9",
                    InfoContrastText = "#0D47A1",
                },

                LayoutProperties = new LayoutProperties()
                {
                    DefaultBorderRadius = "16px",
                    AppbarHeight = "64px",
                    DrawerWidthLeft = "250px",
                    DrawerMiniWidthLeft = "72px"
                },

                Typography = new Typography()
                {
                    Default = new DefaultTypography()
                    {
                        FontFamily = ["Roboto", "Helvetica", "Arial", "sans-serif"],
                        FontSize = ".875rem",
                        FontWeight = "400",
                        LineHeight = "1.43",
                        LetterSpacing = ".01071em"
                    },
                    H1 = new H1Typography() { FontSize = "6rem", FontWeight = "300", LineHeight = "1.167", LetterSpacing = "-.01562em" },
                    H2 = new H2Typography() { FontSize = "3.75rem", FontWeight = "300", LineHeight = "1.2", LetterSpacing = "-.00833em" },
                    H3 = new H3Typography() { FontSize = "3rem", FontWeight = "400", LineHeight = "1.167", LetterSpacing = "0" },
                    H4 = new H4Typography() { FontSize = "2.125rem", FontWeight = "400", LineHeight = "1.235", LetterSpacing = ".00735em" },
                    H5 = new H5Typography() { FontSize = "1.5rem", FontWeight = "400", LineHeight = "1.334", LetterSpacing = "0" },
                    H6 = new H6Typography() { FontSize = "1.25rem", FontWeight = "500", LineHeight = "1.6", LetterSpacing = ".0075em" },
                    Subtitle1 = new Subtitle1Typography() { FontSize = "1rem", FontWeight = "500", LineHeight = "1.75", LetterSpacing = ".00938em" },
                    Subtitle2 = new Subtitle2Typography() { FontSize = ".875rem", FontWeight = "500", LineHeight = "1.57", LetterSpacing = ".00714em" },
                    Body1 = new Body1Typography() { FontSize = "1rem", FontWeight = "400", LineHeight = "1.5", LetterSpacing = ".03125em" },
                    Body2 = new Body2Typography() { FontSize = ".875rem", FontWeight = "400", LineHeight = "1.43", LetterSpacing = ".01786em" },
                    Button = new ButtonTypography() { FontSize = ".875rem", FontWeight = "500", LineHeight = "1.75", LetterSpacing = ".02857em" },
                    Caption = new CaptionTypography() { FontSize = ".75rem", FontWeight = "400", LineHeight = "1.66", LetterSpacing = ".03333em" },
                    Overline = new OverlineTypography() { FontSize = ".6875rem", FontWeight = "400", LineHeight = "2.66", LetterSpacing = ".08333em", TextTransform = "uppercase" }
                },
            };

            return theme;
        }
    }
}
