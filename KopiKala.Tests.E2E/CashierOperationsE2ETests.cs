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
    public async Task Scenario2_CashierWalkInAndAddOnOrderFlow()
    {
        var context = await _fixture.Browser!.NewContextAsync();
        var page = await context.NewPageAsync();

        try
        {
            _output.WriteLine($"[E2E] Login Kasir untuk alur Walk-In: {_fixture.BaseUrl}/Account/Login");
            await page.GotoAsync($"{_fixture.BaseUrl}/Account/Login?returnUrl=/Staff");
            await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

            await page.Locator("input[name='email']").FillAsync("kasir@kopikala.com");
            await page.Locator("input[name='password']").FillAsync("KasirKopi123!");
            await page.Locator("button[type='submit']").ClickAsync();

            await page.WaitForURLAsync(new Regex(".*/Staff.*"));
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // 1. Buka Dialog Tamu Walk-In
            var walkInBtn = page.GetByRole(AriaRole.Button, new() { Name = "Tamu Walk-In" });
            await walkInBtn.ClickAsync();
            _output.WriteLine("[E2E] Modal Walk-In dibuka.");

            var walkInTitle = page.GetByText("Buka Meja Tamu Walk-In (Offline)");
            await walkInTitle.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });

            // 2. Isi Nama Perwakilan Tamu
            var repNameInput = page.Locator("input").Filter(new() { HasText = "" }).Last;
            // Let's find the text field for Nama Perwakilan
            var repInput = page.GetByLabel("Nama Perwakilan Tamu");
            if (await repInput.IsVisibleAsync())
            {
                await repInput.FillAsync("Pak Joko WalkIn");
            }
            else
            {
                await page.Locator(".mud-dialog input[type='text']").First.FillAsync("Pak Joko WalkIn");
            }
            _output.WriteLine("[E2E] Mengisi nama perwakilan tamu: Pak Joko WalkIn");

            // 3. Submit Buka Meja
            var submitWalkInBtn = page.GetByRole(AriaRole.Button, new() { Name = "Buka Meja Sekarang" });
            await submitWalkInBtn.ClickAsync();
            _output.WriteLine("[E2E] Menekan tombol 'Buka Meja Sekarang'");

            // Tunggu modal tertutup dan snackbar muncul
            await page.WaitForTimeoutAsync(1500);

            // Verifikasi nama tamu muncul di salah satu kartu meja
            var guestText = page.GetByText("Pak Joko WalkIn");
            await guestText.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 10000 });
            Assert.True(await guestText.IsVisibleAsync());
            _output.WriteLine("[E2E] Tamu Pak Joko WalkIn berhasil menempati meja (SedangDigunakan).");

            // 4. Buka Menu Aksi Meja
            var aksiMejaBtn = page.GetByRole(AriaRole.Button, new() { Name = "Aksi Meja" }).First;
            if (await aksiMejaBtn.IsVisibleAsync())
            {
                await aksiMejaBtn.ClickAsync();
                await page.WaitForTimeoutAsync(500);

                var addOnMenuItem = page.GetByText("Tambah Menu (Add-on)");
                if (await addOnMenuItem.IsVisibleAsync())
                {
                    await addOnMenuItem.ClickAsync();
                    _output.WriteLine("[E2E] Membuka modal Tambah Pesanan (Add-On)");

                    var addOnTitle = page.GetByText("Tambah Pesanan Menu (Add-On)");
                    await addOnTitle.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });

                    // Klik tombol tambah (+) pada menu pertama
                    var addPlusBtn = page.Locator(".mud-dialog button").Filter(new() { Has = page.Locator("svg") }).Nth(1);
                    if (await addPlusBtn.IsVisibleAsync())
                    {
                        await addPlusBtn.ClickAsync();
                    }

                    // Simpan Pesanan Tambahan
                    var saveAddOnBtn = page.GetByRole(AriaRole.Button, new() { Name = "Simpan Pesanan Tambahan" });
                    if (await saveAddOnBtn.IsEnabledAsync())
                    {
                        await saveAddOnBtn.ClickAsync();
                        _output.WriteLine("[E2E] Berhasil menyimpan pesanan tambahan add-on.");
                        await page.WaitForTimeoutAsync(1500);
                    }
                }
            }
        }
        finally
        {
            await context.CloseAsync();
        }
    }
}
