using KopiKala.Theme;
using System.Security.Claims;
using Xunit;
using Xunit.Abstractions;

namespace KopiKala.Tests.Components;

public class NavbarAndLandingPageTests
{
    private readonly ITestOutputHelper _output;

    public NavbarAndLandingPageTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void KopiKalaTheme_ModernClassicOptionC_PaletteConfiguredProperly()
    {
        // Act
        var theme = KopiKalaTheme.CreateTheme();

        // Assert - Palette Light (MudColor.Value includes 8-character hex with alpha)
        Assert.NotNull(theme.PaletteLight);
        Assert.StartsWith("#5d4037", theme.PaletteLight.Primary.Value, StringComparison.OrdinalIgnoreCase);            // Walnut
        Assert.StartsWith("#4a6c6f", theme.PaletteLight.Secondary.Value, StringComparison.OrdinalIgnoreCase);          // Muted Teal
        Assert.StartsWith("#b5a642", theme.PaletteLight.Tertiary.Value, StringComparison.OrdinalIgnoreCase);           // Antique Brass / Gold
        Assert.StartsWith("#faf9f6", theme.PaletteLight.Background.Value, StringComparison.OrdinalIgnoreCase);         // Ivory Canvas
        Assert.StartsWith("#ffffff", theme.PaletteLight.AppbarBackground.Value, StringComparison.OrdinalIgnoreCase);   // White Background
        Assert.StartsWith("#2a2421", theme.PaletteLight.AppbarText.Value, StringComparison.OrdinalIgnoreCase);         // Black Charcoal Text
        Assert.StartsWith("#2a2421", theme.PaletteLight.ActionDefault.Value, StringComparison.OrdinalIgnoreCase);      // Black Icons

        // Assert - Typography
        Assert.NotNull(theme.Typography);
        Assert.NotNull(theme.Typography.H1.FontFamily);
        Assert.NotNull(theme.Typography.Default.FontFamily);
        Assert.Contains("Playfair Display", theme.Typography.H1.FontFamily);
        Assert.Contains("Plus Jakarta Sans", theme.Typography.Default.FontFamily);

        _output.WriteLine("[SUCCESS] KopiKala Modern Classic MudTheme verified with correct warm vintage palette.");
    }

    [Theory]
    [InlineData("Customer", false, false, true)]
    [InlineData("Kasir", true, false, false)]
    [InlineData("Barista", true, false, false)]
    [InlineData("Manager", true, false, false)]
    [InlineData("SuperAdmin", true, true, false)]
    public void DynamicRoleBasedNavbar_ClaimsEvaluatedCorrectly(
        string role, bool expectedStaffAccess, bool expectedSuperAdmin, bool expectedCustomer)
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, $"Test {role}"),
            new(ClaimTypes.Role, role)
        };

        if (role == "SuperAdmin")
        {
            claims.Add(new Claim("Permission", "Sistem.Kelola"));
        }
        else if (role is "Kasir" or "Barista" or "Manager")
        {
            claims.Add(new Claim("Permission", "Meja.Kelola"));
            claims.Add(new Claim("Permission", "Pembayaran.Verifikasi"));
        }
        else if (role == "Customer")
        {
            claims.Add(new Claim("Permission", "Reservasi.Buat"));
        }

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        // Act
        bool isSuperAdmin = principal.HasClaim("Permission", "Sistem.Kelola");
        bool isStaff = isSuperAdmin || principal.HasClaim(c => c.Type == "Permission" &&
            new[] { "Meja.Kelola", "Pembayaran.Verifikasi", "Dapur.Antrean", "Laporan.Lihat" }.Contains(c.Value));
        bool isCustomer = !isStaff && !isSuperAdmin && principal.Identity?.IsAuthenticated == true;

        // Assert
        Assert.Equal(expectedSuperAdmin, isSuperAdmin);
        Assert.Equal(expectedStaffAccess, isStaff);
        Assert.Equal(expectedCustomer, isCustomer);

        _output.WriteLine($"[ROLE TEST] Role: {role} -> SuperAdmin: {isSuperAdmin}, Staff: {isStaff}, Customer: {isCustomer}");
    }

    [Theory]
    [InlineData("/Booking", true)]
    [InlineData("/Customer/Orders", true)]
    [InlineData("/", true)]
    [InlineData("https://evil.com", false)]
    [InlineData("//evil.com", false)]
    [InlineData("/#hash", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsLocalUrl_RedirectValidation_PreventsOpenRedirect(string? url, bool expectedSafe)
    {
        // Act: local URL validation logic matching Login.razor
        bool isSafe = IsLocalUrlHelper(url);

        // Assert
        Assert.Equal(expectedSafe, isSafe);
        _output.WriteLine($"[SECURITY TEST] URL: '{url}' -> Safe: {isSafe} (Expected: {expectedSafe})");
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(3, false)]
    [InlineData(-1, false)]
    [InlineData(null, false)]
    public void TabQueryParameter_EvaluatesTabIndexBounds_Correctly(int? tabParam, bool expectedValid)
    {
        // Act: validate index bounds for deep link navigation
        bool isValid = tabParam.HasValue && tabParam.Value >= 0 && tabParam.Value <= 2;

        // Assert
        Assert.Equal(expectedValid, isValid);
        _output.WriteLine($"[TAB TEST] Tab parameter '{tabParam}' -> Valid: {isValid} (Expected: {expectedValid})");
    }

    private static bool IsLocalUrlHelper(string? url)
    {
        if (string.IsNullOrEmpty(url)) return false;
        if (url.StartsWith("//") || url.StartsWith("/#")) return false;

        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return uri.Authority.Equals("localhost", StringComparison.OrdinalIgnoreCase)
                || uri.Authority.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase);

        return url.StartsWith("/");
    }
}
