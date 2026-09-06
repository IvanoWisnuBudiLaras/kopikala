using MudBlazor;

namespace KopiKala.Theme;

public static class KopiKalaTheme
{
    public static MudTheme CreateTheme()
    {
        return new MudTheme
        {
            PaletteLight = new PaletteLight
            {
                Primary = "#5D4037",             // Walnut Warm Brown
                PrimaryContrastText = "#FAF9F6",
                Secondary = "#4A6C6F",           // Muted Vintage Teal
                SecondaryContrastText = "#FAF9F6",
                Tertiary = "#B5A642",            // Brushed Brass / Warm Antique Gold
                TertiaryContrastText = "#2A2421",
                AppbarBackground = "#2A2421",    // Deep Espresso / Charcoal
                AppbarText = "#FAF9F6",          // Ivory
                Background = "#FAF9F6",          // Ivory Canvas
                BackgroundGray = "#F2EFE9",      // Soft Parchment / Warm Linen
                Surface = "#FFFFFF",
                TextPrimary = "#2A2421",         // Deep Charcoal
                TextSecondary = "#6D645E",       // Muted Warm Walnut Gray
                TextDisabled = "#A39E98",
                ActionDefault = "#5D4037",
                ActionDisabled = "#C8C4BD",
                ActionDisabledBackground = "#E5E1D8",
                LinesDefault = "#E2DDD3",        // Warm Hairline Border
                LinesInputs = "#C5BFB5",
                Divider = "#E8E3D9",
                TableLines = "#EFECE5",
                Success = "#4E7D55",             // Sage Green
                Warning = "#C87D32",             // Warm Amber Terracotta
                Error = "#A63A3A",               // Vintage Crimson
                Info = "#4A6C6F",                // Muted Teal
                DrawerBackground = "#FAF9F6",
                DrawerText = "#2A2421"
            },
            PaletteDark = new PaletteDark
            {
                Primary = "#D2B48C",             // Warm Latte / Tan
                PrimaryContrastText = "#1E1A18",
                Secondary = "#7E9F9F",           // Muted Teal Light
                SecondaryContrastText = "#1E1A18",
                Tertiary = "#D4AF37",            // Antique Gold
                TertiaryContrastText = "#1E1A18",
                AppbarBackground = "#1A1715",    // Dark Roast
                AppbarText = "#FAF9F6",
                Background = "#1E1A18",          // Coffee Bean
                BackgroundGray = "#282320",
                Surface = "#26211E",
                TextPrimary = "#FAF9F6",
                TextSecondary = "#B8B1A8",
                LinesDefault = "#3D3530",
                LinesInputs = "#544A43",
                Divider = "#38312C",
                TableLines = "#342E29",
                Success = "#6B9B73",
                Warning = "#E09748",
                Error = "#C45B5B",
                Info = "#7E9F9F",
                DrawerBackground = "#1E1A18",
                DrawerText = "#FAF9F6"
            },
            Typography = new Typography
            {
                Default = new DefaultTypography
                {
                    FontFamily = new[] { "Plus Jakarta Sans", "Roboto", "Helvetica", "Arial", "sans-serif" },
                    FontSize = "0.95rem",
                    FontWeight = "400",
                    LineHeight = "1.6"
                },
                H1 = new H1Typography
                {
                    FontFamily = new[] { "Playfair Display", "Georgia", "serif" },
                    FontSize = "3.25rem",
                    FontWeight = "700",
                    LineHeight = "1.2",
                    LetterSpacing = "-.02em"
                },
                H2 = new H2Typography
                {
                    FontFamily = new[] { "Playfair Display", "Georgia", "serif" },
                    FontSize = "2.5rem",
                    FontWeight = "700",
                    LineHeight = "1.25"
                },
                H3 = new H3Typography
                {
                    FontFamily = new[] { "Playfair Display", "Georgia", "serif" },
                    FontSize = "2rem",
                    FontWeight = "600",
                    LineHeight = "1.3"
                },
                H4 = new H4Typography
                {
                    FontFamily = new[] { "Playfair Display", "Georgia", "serif" },
                    FontSize = "1.5rem",
                    FontWeight = "600",
                    LineHeight = "1.35"
                },
                H5 = new H5Typography
                {
                    FontFamily = new[] { "Plus Jakarta Sans", "Roboto", "sans-serif" },
                    FontSize = "1.25rem",
                    FontWeight = "600",
                    LineHeight = "1.4"
                },
                H6 = new H6Typography
                {
                    FontFamily = new[] { "Plus Jakarta Sans", "Roboto", "sans-serif" },
                    FontSize = "1.05rem",
                    FontWeight = "600",
                    LineHeight = "1.4"
                },
                Button = new ButtonTypography
                {
                    FontFamily = new[] { "Plus Jakarta Sans", "Roboto", "sans-serif" },
                    FontSize = "0.9rem",
                    FontWeight = "600",
                    LetterSpacing = ".02em",
                    TextTransform = "none"
                }
            },
            LayoutProperties = new LayoutProperties
            {
                DefaultBorderRadius = "12px",
                AppbarHeight = "72px"
            }
        };
    }
}
