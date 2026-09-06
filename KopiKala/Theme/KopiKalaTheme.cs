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
                AppbarBackground = "#FFFFFF",    // Background Putih Bersih
                AppbarText = "#2A2421",          // Teks/Objek Hitam Charcoal
                Background = "#FAF9F6",          // Ivory Canvas
                BackgroundGray = "#F2EFE9",      // Soft Parchment / Warm Linen
                Surface = "#FFFFFF",             // Kartu Putih
                TextPrimary = "#2A2421",         // Teks Hitam Charcoal
                TextSecondary = "#5C534D",       // Teks Sekunder Kontras
                TextDisabled = "#9E9891",
                ActionDefault = "#2A2421",       // Ikon Hitam Charcoal
                ActionDisabled = "#C8C4BD",
                ActionDisabledBackground = "#E5E1D8",
                LinesDefault = "#E2DDD3",        // Warm Hairline Border
                LinesInputs = "#B0A89C",
                Divider = "#E8E3D9",
                TableLines = "#EFECE5",
                Success = "#3E6B44",             // Sage Green Kontras
                Warning = "#B86A1D",             // Amber Terracotta Kontras
                Error = "#9E2B2B",               // Vintage Crimson
                Info = "#3A595C",                // Muted Teal Kontras
                DrawerBackground = "#FFFFFF",
                DrawerText = "#2A2421"
            },
            PaletteDark = new PaletteDark
            {
                Primary = "#D2B48C",             // Warm Latte / Tan
                PrimaryContrastText = "#1E1A18",
                Secondary = "#8FAFAF",           // Light Vintage Teal
                SecondaryContrastText = "#1E1A18",
                Tertiary = "#D4AF37",            // Antique Gold
                TertiaryContrastText = "#1E1A18",
                AppbarBackground = "#1A1715",    // Dark Roast
                AppbarText = "#FAF9F6",          // Putih Gading
                Background = "#1E1A18",          // Coffee Bean Dark
                BackgroundGray = "#282320",
                Surface = "#26211E",             // Dark Wood Surface
                TextPrimary = "#FAF9F6",         // Teks Putih Gading
                TextSecondary = "#C5BEB5",       // Teks Sekunder Lembut
                TextDisabled = "#706963",
                ActionDefault = "#FAF9F6",       // Ikon Putih Gading
                ActionDisabled = "#4A433E",
                ActionDisabledBackground = "#332C28",
                LinesDefault = "#3D3530",
                LinesInputs = "#544A43",
                Divider = "#38312C",
                TableLines = "#342E29",
                Success = "#74A87C",
                Warning = "#E09748",
                Error = "#D96868",
                Info = "#8FAFAF",
                DrawerBackground = "#1A1715",
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
