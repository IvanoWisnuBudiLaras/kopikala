using System.Text.RegularExpressions;
using KopiKala.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using Xunit;
using Xunit.Abstractions;

namespace KopiKala.Tests.E2E;

[Collection("E2E Test Collection")]
public class CashierOperationsE2ETests
{
    private readonly KopiKalaServerFixture _fixture;
    private readonly ITestOutputHelper _output;

    public CashierOperationsE2ETests(KopiKalaServerFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    [Fact]
    public async Task Scenario1_CashierPortal_LoginAndVerifyTableFloorPlan()
    {
        var context = await _fixture.Browser!.NewContextAsync();
        var page = await context.NewPageAsync();

        try
        {
            _output.WriteLine($"[E2E] Membuka URL Login Kasir: {_fixture.BaseUrl}/Account/Login");
            await page.GotoAsync($"{_fixture.BaseUrl}/Account/Login?returnUrl=/Staff");
            await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

            await page.Locator("input[name='email']").FillAsync("kasir@kopikala.com");
            await page.Locator("input[name='password']").FillAsync("KasirKopi123!");
            await page.Locator("button[type='submit']").ClickAsync();

            await page.WaitForURLAsync(new Regex(".*/Staff.*"));
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            _output.WriteLine("[E2E] Berhasil login sebagai Kasir dan masuk ke /Staff");

            // Verifikasi Header Portal Staf
            var portalHeader = page.GetByText("Portal Operasional Staf");
            await portalHeader.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 10000 });
            Assert.True(await portalHeader.IsVisibleAsync());
            _output.WriteLine("[E2E] Header Portal Operasional Staf terlihat.");

            // Verifikasi Tab Denah Meja & Area
            var indoorHeader = page.GetByText("Area Indoor (AC)");
            await indoorHeader.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 10000 });
            Assert.True(await indoorHeader.IsVisibleAsync());

            var outdoorHeader = page.GetByText("Area Outdoor (Smoking)");
            Assert.True(await outdoorHeader.IsVisibleAsync());
            _output.WriteLine("[E2E] Area Indoor dan Outdoor denah meja 2D terverifikasi.");

            // Verifikasi Tombol Tamu Walk-In
            var walkInBtn = page.GetByRole(AriaRole.Button, new() { Name = "Tamu Walk-In" });
            Assert.True(await walkInBtn.IsVisibleAsync());
            _output.WriteLine("[E2E] Tombol Tamu Walk-In siap digunakan.");
        }
        finally
        {
            await context.CloseAsync();
        }
    }

    [Fact]
    public async Task Scenario2_CashierWalkIn_SimplifiedFlow()
    {
        var context = await _fixture.Browser!.NewContextAsync();
        var page = await context.NewPageAsync();

        try
        {
            _output.WriteLine($"[E2E] Login Kasir: {_fixture.BaseUrl}/Account/Login");
            await page.GotoAsync($"{_fixture.BaseUrl}/Account/Login?returnUrl=/Staff");
            await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

            await page.Locator("input[name='email']").FillAsync("kasir@kopikala.com");
            await page.Locator("input[name='password']").FillAsync("KasirKopi123!");
            await page.Locator("button[type='submit']").ClickAsync();

            await page.WaitForURLAsync(new Regex(".*/Staff.*"));
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            _output.WriteLine("[E2E] Login Kasir berhasil.");

            // 1. Klik tombol "Buka Meja" pada kartu meja OUT-03 yang pasti kosong
            var bukaMejaBtn = page.Locator(".mud-card:has-text('OUT-03') button:has-text('Buka Meja')").First;
            if (await bukaMejaBtn.CountAsync() == 0)
            {
                bukaMejaBtn = page.Locator(".mud-card:has-text('OUT-') button:has-text('Buka Meja')").First;
            }
            await bukaMejaBtn.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 10000 });
            await bukaMejaBtn.ClickAsync();
            _output.WriteLine("[E2E] Tombol 'Buka Meja' pada meja outdoor diklik.");

            // 2. Tunggu Blazor SignalR merender MudDialog
            await page.WaitForTimeoutAsync(2000);

            // 3. Cari MudDialog yang muncul
            var dialog = page.Locator(".mud-dialog").First;
            var dialogVisible = await dialog.IsVisibleAsync();
            _output.WriteLine($"[E2E] MudDialog visible: {dialogVisible}");

            Assert.True(dialogVisible);

            // 4. Isi form di dalam dialog - target input nama perwakilan secara spesifik
            var repInput = dialog.Locator(".mud-input-control:has-text('Nama Perwakilan') input").First;
            await repInput.FillAsync("Pak Joko WalkIn");
            await repInput.PressAsync("Tab");
            _output.WriteLine("[E2E] Nama perwakilan diisi ke field yang benar.");

            // 5. Submit - klik span tombol "Buka Meja Sekarang"
            var submitSpan = page.Locator("span:has-text('Buka Meja Sekarang')").Last;
            await submitSpan.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
            await submitSpan.ClickAsync();
            _output.WriteLine("[E2E] Tombol Buka Meja Sekarang diklik.");
            await page.WaitForTimeoutAsync(3000);

            var snackbarText = await page.Locator(".mud-snackbar").AllInnerTextsAsync();
            _output.WriteLine($"[E2E] Snackbars: {string.Join(", ", snackbarText)}");

            var cardsText = await page.Locator(".mud-card").AllInnerTextsAsync();
            _output.WriteLine($"[E2E] Card Texts: {string.Join(" | ", cardsText)}");

            // 6. Verifikasi tamu muncul di denah meja
            var guestText = page.Locator("text=Pak Joko WalkIn").First;
            await guestText.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 10000 });
            Assert.True(await guestText.IsVisibleAsync());
            _output.WriteLine("[E2E] Tamu Pak Joko WalkIn berhasil masuk ke denah meja.");
        }
        finally
        {
            await context.CloseAsync();
        }
    }
}
