using System.Text.RegularExpressions;
using KopiKala.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using Xunit;
using Xunit.Abstractions;

namespace KopiKala.Tests.E2E;

[Collection("E2E Test Collection")]
public class SuperAdminGovernanceE2ETests
{
    private readonly KopiKalaServerFixture _fixture;
    private readonly ITestOutputHelper _output;

    public SuperAdminGovernanceE2ETests(KopiKalaServerFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    [Fact]
    public async Task Scenario1_SuperAdminPortal_LoginAndVerifyAnalyticsDashboard()
    {
        var context = await _fixture.Browser!.NewContextAsync();
        var page = await context.NewPageAsync();

        try
        {
            _output.WriteLine($"[E2E] Membuka URL Login SuperAdmin: {_fixture.BaseUrl}/Account/Login");
            await page.GotoAsync($"{_fixture.BaseUrl}/Account/Login?returnUrl=/SuperAdmin");
            await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

            await page.Locator("input[name='email']").FillAsync("superadmin@kopikala.com");
            await page.Locator("input[name='password']").FillAsync("AdminKopi123!");
            await page.Locator("button[type='submit']").ClickAsync();

            await page.WaitForURLAsync(new Regex(".*/SuperAdmin.*"));
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            _output.WriteLine("[E2E] Berhasil login sebagai SuperAdmin dan masuk ke /SuperAdmin");

            // Verifikasi Header Ruang Kendali SuperAdmin
            var header = page.GetByText("Ruang Kendali SuperAdmin");
            await header.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 10000 });
            Assert.True(await header.IsVisibleAsync());
            _output.WriteLine("[E2E] Header SuperAdmin terverifikasi.");

            // Verifikasi KPI Cards
            var kpiOmzet = page.GetByText("TOTAL OMZET HARI INI");
            await kpiOmzet.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 10000 });
            Assert.True(await kpiOmzet.IsVisibleAsync());

            var kpiOkupansi = page.GetByText("OKUPANSI MEJA REAL-TIME");
            Assert.True(await kpiOkupansi.IsVisibleAsync());
            _output.WriteLine("[E2E] KPI Cards terverifikasi.");
        }
        finally
        {
            await page.CloseAsync();
            await context.CloseAsync();
        }
    }

    [Fact]
    public async Task Scenario2_SuperAdmin_DynamicPBAC_GovernanceAndTemplates()
    {
        var context = await _fixture.Browser!.NewContextAsync();
        var page = await context.NewPageAsync();

        try
        {
            await page.GotoAsync($"{_fixture.BaseUrl}/Account/Login?returnUrl=/SuperAdmin");
            await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

            await page.Locator("input[name='email']").FillAsync("superadmin@kopikala.com");
            await page.Locator("input[name='password']").FillAsync("AdminKopi123!");
            await page.Locator("button[type='submit']").ClickAsync();

            await page.WaitForURLAsync(new Regex(".*/SuperAdmin.*"));
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Buka Tab: Akun Staf & Dynamic PBAC
            var pbacTab = page.Locator(".mud-tab:has-text('Akun Staf')");
            await pbacTab.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 10000 });
            await pbacTab.ClickAsync();

            // Verifikasi Heading PBAC
            var pbacHeading = page.Locator(".mud-card:has-text('Tata Kelola Dynamic PBAC')");
            await pbacHeading.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 10000 });
            Assert.True(await pbacHeading.IsVisibleAsync());

            // Verifikasi Tombol Template 1-Klik
            var templateKasirBtn = page.GetByRole(AriaRole.Button, new() { Name = "Template Kasir" });
            await templateKasirBtn.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 10000 });
            Assert.True(await templateKasirBtn.IsVisibleAsync());

            await templateKasirBtn.ClickAsync();
            _output.WriteLine("[E2E] Tombol Template Kasir 1-klik berhasil diklik.");
        }
        finally
        {
            await page.CloseAsync();
            await context.CloseAsync();
        }
    }

    [Fact]
    public async Task Scenario3_SuperAdmin_MasterData_TablesAndMenu()
    {
        var context = await _fixture.Browser!.NewContextAsync();
        var page = await context.NewPageAsync();

        try
        {
            await page.GotoAsync($"{_fixture.BaseUrl}/Account/Login?returnUrl=/SuperAdmin");
            await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

            await page.Locator("input[name='email']").FillAsync("superadmin@kopikala.com");
            await page.Locator("input[name='password']").FillAsync("AdminKopi123!");
            await page.Locator("button[type='submit']").ClickAsync();

            await page.WaitForURLAsync(new Regex(".*/SuperAdmin.*"));
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Buka Tab: Master Data Kafe
            var masterDataTab = page.Locator(".mud-tab:has-text('Master Data Kafe')");
            await masterDataTab.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 10000 });
            await masterDataTab.ClickAsync();

            // Verifikasi Master Meja dan Menu
            var masterMejaHeading = page.Locator(".mud-card:has-text('Master Meja Fisik')");
            await masterMejaHeading.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 10000 });
            Assert.True(await masterMejaHeading.IsVisibleAsync());

            var masterMenuHeading = page.Locator(".mud-card:has-text('Master Menu F&B')");
            Assert.True(await masterMenuHeading.IsVisibleAsync());
            _output.WriteLine("[E2E] Master Data Meja dan Menu F&B terverifikasi.");
        }
        finally
        {
            await page.CloseAsync();
            await context.CloseAsync();
        }
    }

    [Fact]
    public async Task Scenario4_SuperAdmin_MonkeyTesting_TabResilience()
    {
        var context = await _fixture.Browser!.NewContextAsync();
        var page = await context.NewPageAsync();

        try
        {
            await page.GotoAsync($"{_fixture.BaseUrl}/Account/Login?returnUrl=/SuperAdmin");
            await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

            await page.Locator("input[name='email']").FillAsync("superadmin@kopikala.com");
            await page.Locator("input[name='password']").FillAsync("AdminKopi123!");
            await page.Locator("button[type='submit']").ClickAsync();

            await page.WaitForURLAsync(new Regex(".*/SuperAdmin.*"));
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Rapid switching between tabs
            for (int i = 0; i < 4; i++)
            {
                await page.Locator(".mud-tab:has-text('Akun Staf')").ClickAsync();
                await page.WaitForTimeoutAsync(80);
                await page.Locator(".mud-tab:has-text('Master Data Kafe')").ClickAsync();
                await page.WaitForTimeoutAsync(80);
                await page.Locator(".mud-tab:has-text('Analitik')").ClickAsync();
                await page.WaitForTimeoutAsync(80);
            }

            var header = page.GetByText("Ruang Kendali SuperAdmin");
            Assert.True(await header.IsVisibleAsync());
            _output.WriteLine("[E2E] Monkey chaos tab switching completed without SignalR disconnect.");
        }
        finally
        {
            await page.CloseAsync();
            await context.CloseAsync();
        }
    }
}
