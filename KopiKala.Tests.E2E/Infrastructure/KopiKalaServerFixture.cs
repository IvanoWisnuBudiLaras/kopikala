using System.Diagnostics;
using Microsoft.Playwright;
using Xunit;

namespace KopiKala.Tests.E2E.Infrastructure;

public class KopiKalaServerFixture : IAsyncLifetime
{
    private Process? _serverProcess;
    public string BaseUrl { get; private set; } = "http://localhost:5099";
    public IPlaywright? PlaywrightInstance { get; private set; }
    public IBrowser? Browser { get; private set; }

    // Mode Headless dapat diatur via environment variable (default: false / Headed live browser)
    public bool Headless { get; } =
        !string.Equals(Environment.GetEnvironmentVariable("PLAYWRIGHT_HEADED"), "true", StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase)
        ? false // Default to headed for live developer view as requested in PRD/TICKET-3
        : true;

    public async Task InitializeAsync()
    {
        // 1. Jalankan web server KopiKala di port 5099
        var projectPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "KopiKala"));

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --no-build --no-restore --urls \"{BaseUrl}\"",
            WorkingDirectory = projectPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        _serverProcess = Process.Start(startInfo);

        // 2. Tunggu server siap merespons HTTP (maks 30 detik)
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        var isReady = false;
        for (var i = 0; i < 30; i++)
        {
            try
            {
                var response = await httpClient.GetAsync(BaseUrl);
                if (response.IsSuccessStatusCode)
                {
                    isReady = true;
                    break;
                }
            }
            catch
            {
                // Server belum siap
            }
            await Task.Delay(1000);
        }

        if (!isReady)
        {
            throw new InvalidOperationException($"Server KopiKala gagal siap di {BaseUrl} dalam 30 detik.");
        }

        // 3. Inisialisasi Microsoft Playwright & Chromium
        PlaywrightInstance = await Playwright.CreateAsync();
        Browser = await PlaywrightInstance.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = Headless,
            SlowMo = Headless ? 0 : 75 // 75ms slowMo agar pergerakan terlihat live
        });
    }

    public async Task DisposeAsync()
    {
        if (Browser != null)
        {
            await Browser.CloseAsync();
        }

        PlaywrightInstance?.Dispose();

        if (_serverProcess != null && !_serverProcess.HasExited)
        {
            try
            {
                _serverProcess.Kill(entireProcessTree: true);
                _serverProcess.Dispose();
            }
            catch
            {
                // Process cleanup fallback
            }
        }
    }
}
