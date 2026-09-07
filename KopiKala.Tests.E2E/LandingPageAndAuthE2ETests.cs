using System.Text.RegularExpressions;
using KopiKala.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using Xunit;
using Xunit.Abstractions;

namespace KopiKala.Tests.E2E;

public class LandingPageAndAuthE2ETests : IClassFixture<KopiKalaServerFixture>
{
    private readonly KopiKalaServerFixture _fixture;
    private readonly ITestOutputHelper _output;

    public LandingPageAndAuthE2ETests(KopiKalaServerFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    [Fact]
    public async Task Scenario1_PublicLandingPage_RendersProperlyWithoutBrokenImages()
    {
        var context = await _fixture.Browser!.NewContextAsync();
        var page = await context.NewPageAsync();

        try
        {
            // 1. Buka beranda publik
            _output.WriteLine($"[E2E] Membuka URL: {_fixture.BaseUrl}");
            await page.GotoAsync(_fixture.BaseUrl);
            await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

            // 2. Verifikasi Hero Banner Text
            var heroTitle = page.Locator(".kopi-hero-title");
            Assert.True(await heroTitle.IsVisibleAsync(), "Judul Hero Banner harus tampil di halaman beranda.");
            var heroText = await heroTitle.InnerTextAsync();
            Assert.Contains("Nikmati Kopi Terbaik", heroText);

            var subtitle = page.Locator(".kopi-hero-subtitle");
            Assert.True(await subtitle.IsVisibleAsync(), "Sub-judul Hero Banner harus tampil.");

            // 3. Verifikasi CTA Buttons
            var pesanMejaBtn = page.Locator(".kopi-hero-cta");
            Assert.True(await pesanMejaBtn.IsVisibleAsync(), "Tombol CTA 'Pesan Meja Sekarang' harus tampil.");

            // 4. Verifikasi Status Navbar Tamu (NotAuthorized)
            var masukNavBtn = page.Locator(".kopi-appbar").GetByText("Masuk");
            var daftarNavBtn = page.Locator(".kopi-appbar").GetByText("Daftar");
            Assert.True(await masukNavBtn.IsVisibleAsync(), "Navbar tamu harus menampilkan tombol 'Masuk'.");
            Assert.True(await daftarNavBtn.IsVisibleAsync(), "Navbar tamu harus menampilkan tombol 'Daftar'.");

            // 5. Verifikasi bahwa tidak ada gambar rusak (broken images)
            var imageElements = await page.Locator("img").AllAsync();
            _output.WriteLine($"[E2E] Memverifikasi {imageElements.Count} elemen gambar pada beranda...");

            foreach (var img in imageElements)
            {
                var src = await img.GetAttributeAsync("src");
                var naturalWidth = await img.EvaluateAsync<int>("el => el.naturalWidth");
                _output.WriteLine($"  → Gambar '{src}' - naturalWidth: {naturalWidth}px");
                Assert.True(naturalWidth > 0, $"Gambar '{src}' tidak boleh rusak (naturalWidth > 0).");
            }

            _output.WriteLine("[SUCCESS] Skenario 1 (Tampilan Publik) berhasil diverifikasi!");
        }
        finally
        {
            await context.CloseAsync();
        }
    }

    [Fact]
    public async Task Scenario2_FullUserJourney_LoginSuperAdmin_DynamicNavbar_Logout()
    {
        var context = await _fixture.Browser!.NewContextAsync();
        var page = await context.NewPageAsync();

        try
        {
            // 1. Buka beranda
            await page.GotoAsync(_fixture.BaseUrl);
            await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

            // 2. Klik Masuk
            await page.Locator(".kopi-appbar").GetByText("Masuk").ClickAsync();
            await page.WaitForURLAsync(new Regex(".*/Account/Login.*"));
            _output.WriteLine("[E2E] Berada di halaman Login.");

            // 3. Isi kredensial SuperAdmin demo
            await page.Locator("input[name='email']").FillAsync("superadmin@kopikala.com");
            await page.Locator("input[name='password']").FillAsync("AdminKopi123!");

            // 4. Klik Submit Masuk dan tunggu navigasi
            await page.Locator("button[type='submit']").ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

            _output.WriteLine($"[E2E] URL setelah login: {page.Url}");

            // 5. Verifikasi Navbar Dinamis memunculkan tombol SuperAdmin
            var superAdminNavBtn = page.Locator(".kopi-appbar").GetByText("SuperAdmin");
            Assert.True(await superAdminNavBtn.IsVisibleAsync(), "Navbar SuperAdmin harus menampilkan tombol 'SuperAdmin'.");

            // 6. Verifikasi tombol Masuk tamu sudah hilang
            var masukBtn = page.Locator(".kopi-appbar").GetByText("Masuk");
            Assert.False(await masukBtn.IsVisibleAsync(), "Tombol Masuk tamu harus hilang setelah login.");

            _output.WriteLine("[SUCCESS] Skenario 2 (SuperAdmin Auth & Dynamic Navbar) berhasil diverifikasi!");
        }
        finally
        {
            await context.CloseAsync();
        }
    }

    [Fact]
    public async Task Scenario2_FullUserJourney_LoginStaffKasir_DynamicNavbar()
    {
        var context = await _fixture.Browser!.NewContextAsync();
        var page = await context.NewPageAsync();

        try
        {
            // 1. Masuk ke halaman login
            await page.GotoAsync($"{_fixture.BaseUrl}/Account/Login");
            await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

            // 2. Isi akun Kasir Demo
            await page.Locator("input[name='email']").FillAsync("kasir@kopikala.com");
            await page.Locator("input[name='password']").FillAsync("KasirKopi123!");

            // 3. Submit dan tunggu navigasi
            await page.Locator("button[type='submit']").ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

            _output.WriteLine($"[E2E Kasir] URL setelah login: {page.Url}");

            // 4. Verifikasi Navbar memunculkan Portal Staf
            var portalStafBtn = page.Locator(".kopi-appbar").GetByText("Portal Staf");
            Assert.True(await portalStafBtn.IsVisibleAsync(), "Navbar Staf Kasir harus menampilkan tombol 'Portal Staf'.");

            _output.WriteLine("[SUCCESS] Skenario 2 (Staf Kasir Dynamic Navbar) berhasil diverifikasi!");
        }
        finally
        {
            await context.CloseAsync();
        }
    }

    [Fact]
    public async Task Scenario3_MonkeyChaosTesting_RapidRandomClicks_ResilienceVerified()
    {
        var context = await _fixture.Browser!.NewContextAsync();
        var page = await context.NewPageAsync();

        try
        {
            _output.WriteLine("[MONKEY TEST] Memulai Chaos UI Resilience Testing pada sirkuit Blazor Server...");
            await page.GotoAsync(_fixture.BaseUrl);
            await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

            var random = new Random(42);
            var interactiveElements = await page.Locator("button, a, .kopi-feature-card, .kopi-menu-card, input").AllAsync();

            _output.WriteLine($"[MONKEY TEST] Ditemukan {interactiveElements.Count} elemen interaktif. Menjalankan 25 aksi acak cepat...");

            for (var i = 0; i < 25; i++)
            {
                try
                {
                    if (interactiveElements.Count > 0)
                    {
                        var target = interactiveElements[random.Next(interactiveElements.Count)];
                        if (await target.IsVisibleAsync())
                        {
                            // Aksi acak: hover, klik cepat
                            if (random.Next(2) == 0)
                            {
                                await target.HoverAsync(new LocatorHoverOptions { Timeout = 500 });
                            }
                            else
                            {
                                await target.ClickAsync(new LocatorClickOptions { Timeout = 500, Force = true });
                            }
                        }
                    }
                }
                catch
                {
                    // Ignored in chaos mode — simulating erratic user behavior
                }

                await Task.Delay(40); // Rapid interaction
            }

            // Verifikasi bahwa Blazor Server Circuit tidak crash / tidak memunculkan dialog unhandled error
            var errorUi = page.Locator("#blazor-error-ui");
            var isErrorDisplayed = await errorUi.EvaluateAsync<bool>("el => el && window.getComputedStyle(el).display !== 'none'");
            Assert.False(isErrorDisplayed, "Sirkuit Blazor Server tidak boleh crash atau menampilkan error UI setelah Monkey Testing.");

            _output.WriteLine("[SUCCESS] Monkey Test berhasil! Sirkuit Blazor Server tetap stabil dan responsif.");
        }
        finally
        {
            await context.CloseAsync();
        }
    }
}
