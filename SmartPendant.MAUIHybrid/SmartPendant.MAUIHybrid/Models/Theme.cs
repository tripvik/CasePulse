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
                    // Calming medical blue with professional contrast
                    Primary = "#2E7DC6",
                    PrimaryContrastText = "#FFFFFF",

                    // Soft teal for secondary actions (non-critical)
                    Secondary = "#4A9B8E",
                    SecondaryContrastText = "#FFFFFF",

                    // Warm gray for tertiary elements
                    Tertiary = "#6B7280",
                    TertiaryContrastText = "#FFFFFF",

                    // Clean, clinical backgrounds
                    Background = "#FAFBFC",
                    Surface = "#FFFFFF",

                    // Professional header
                    AppbarBackground = "#FFFFFF",
                    AppbarText = "#1F2937",

                    // Clean sidebar
                    DrawerBackground = "#F8FAFC",
                    DrawerText = "#374151",
                    DrawerIcon = "#6B7280",

                    // High contrast text for readability
                    TextPrimary = "#1F2937",
                    TextSecondary = "#6B7280",
                    TextDisabled = "rgba(31, 41, 55, 0.4)",

                    // Accessible action colors
                    ActionDefault = "#6B7280",
                    ActionDisabled = "rgba(31, 41, 55, 0.3)",
                    ActionDisabledBackground = "rgba(31, 41, 55, 0.1)",

                    // Subtle lines and dividers
                    LinesDefault = "#D1D5DB",
                    LinesInputs = "#9CA3AF",
                    Divider = "#E5E7EB",

                    // Clean table styling
                    TableLines = "#E5E7EB",
                    TableStriped = "rgba(46, 125, 198, 0.03)",
                    TableHover = "rgba(46, 125, 198, 0.06)",

                    // Medical-appropriate alert colors
                    Error = "#DC2626", // Clear red for critical alerts
                    ErrorContrastText = "#FFFFFF",

                    Success = "#059669", // Medical green for positive outcomes
                    SuccessContrastText = "#FFFFFF",

                    Warning = "#D97706", // Amber for caution
                    WarningContrastText = "#FFFFFF",

                    Info = "#2563EB", // Professional blue for information
                    InfoContrastText = "#FFFFFF",
                },

                PaletteDark = new PaletteDark()
                {
                    // Softer primary for dark mode (easier on eyes during night shifts)
                    Primary = "#60A5FA",
                    PrimaryContrastText = "#1E3A8A",

                    // Muted teal for secondary
                    Secondary = "#6EE7B7",
                    SecondaryContrastText = "#064E3B",

                    // Warm gray for tertiary
                    Tertiary = "#A1A1AA",
                    TertiaryContrastText = "#27272A",

                    // Dark medical backgrounds
                    Background = "#0F172A",
                    Surface = "#1E293B",

                    // Dark header
                    AppbarBackground = "#1E293B",
                    AppbarText = "#F1F5F9",

                    // Dark sidebar
                    DrawerBackground = "#0F172A",
                    DrawerText = "#CBD5E1",
                    DrawerIcon = "#94A3B8",

                    // High contrast text for dark mode
                    TextPrimary = "#F1F5F9",
                    TextSecondary = "#CBD5E1",
                    TextDisabled = "rgba(241, 245, 249, 0.4)",

                    // Dark mode actions
                    ActionDefault = "#94A3B8",
                    ActionDisabled = "rgba(241, 245, 249, 0.3)",
                    ActionDisabledBackground = "rgba(241, 245, 249, 0.1)",

                    // Dark lines and dividers
                    LinesDefault = "#475569",
                    LinesInputs = "#64748B",
                    Divider = "#334155",

                    // Dark table styling
                    TableLines = "#334155",
                    TableStriped = "rgba(96, 165, 250, 0.05)",
                    TableHover = "rgba(96, 165, 250, 0.1)",

                    // Dark mode alert colors (slightly muted for comfort)
                    Error = "#F87171",
                    ErrorContrastText = "#7F1D1D",

                    Success = "#34D399",
                    SuccessContrastText = "#064E3B",

                    Warning = "#FBBF24",
                    WarningContrastText = "#92400E",

                    Info = "#60A5FA",
                    InfoContrastText = "#1E3A8A",
                },

                LayoutProperties = new LayoutProperties()
                {
                    // Slightly smaller radius for more clinical feel
                    DefaultBorderRadius = "8px",
                    // Standard height for easy navigation
                    AppbarHeight = "64px",
                    // Adequate space for medical navigation
                    DrawerWidthLeft = "280px",
                    // Compact mini drawer
                    DrawerMiniWidthLeft = "64px"
                },

                Typography = new Typography()
                {
                    Default = new DefaultTypography()
                    {
                        // Clear, readable font stack
                        FontFamily = ["Inter", "Roboto", "Arial", "sans-serif"],
                        FontSize = ".9rem", // Slightly larger for better readability
                        FontWeight = "400",
                        LineHeight = "1.5", // Better line spacing for readability
                        LetterSpacing = "0"
                    },
                    H1 = new H1Typography() { FontSize = "2.5rem", FontWeight = "600", LineHeight = "1.2", LetterSpacing = "-.025em" },
                    H2 = new H2Typography() { FontSize = "2rem", FontWeight = "600", LineHeight = "1.25", LetterSpacing = "-.025em" },
                    H3 = new H3Typography() { FontSize = "1.75rem", FontWeight = "600", LineHeight = "1.3", LetterSpacing = "0" },
                    H4 = new H4Typography() { FontSize = "1.5rem", FontWeight = "600", LineHeight = "1.35", LetterSpacing = "0" },
                    H5 = new H5Typography() { FontSize = "1.25rem", FontWeight = "600", LineHeight = "1.4", LetterSpacing = "0" },
                    H6 = new H6Typography() { FontSize = "1.125rem", FontWeight = "600", LineHeight = "1.4", LetterSpacing = "0" },
                    Subtitle1 = new Subtitle1Typography() { FontSize = "1rem", FontWeight = "500", LineHeight = "1.5", LetterSpacing = "0" },
                    Subtitle2 = new Subtitle2Typography() { FontSize = ".9rem", FontWeight = "500", LineHeight = "1.5", LetterSpacing = "0" },
                    Body1 = new Body1Typography() { FontSize = "1rem", FontWeight = "400", LineHeight = "1.6", LetterSpacing = "0" },
                    Body2 = new Body2Typography() { FontSize = ".9rem", FontWeight = "400", LineHeight = "1.5", LetterSpacing = "0" },
                    Button = new ButtonTypography() { FontSize = ".875rem", FontWeight = "500", LineHeight = "1.5", LetterSpacing = ".025em", TextTransform = "none" },
                    Caption = new CaptionTypography() { FontSize = ".8rem", FontWeight = "400", LineHeight = "1.5", LetterSpacing = ".025em" },
                    Overline = new OverlineTypography() { FontSize = ".75rem", FontWeight = "500", LineHeight = "1.5", LetterSpacing = ".1em", TextTransform = "uppercase" }
                },
            };

            return theme;
        }
    }
}