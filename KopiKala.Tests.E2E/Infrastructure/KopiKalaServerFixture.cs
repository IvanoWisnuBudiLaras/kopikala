using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Microsoft.Playwright;
using Xunit;

namespace KopiKala.Tests.E2E.Infrastructure;

public class KopiKalaServerFixture : IAsyncLifetime
{
    private Process? _serverProcess;
    public string BaseUrl { get; private set; } = "";
    public IPlaywright? PlaywrightInstance { get; private set; }
    public IBrowser? Browser { get; private set; }

    // Mode Headless dapat diatur via environment variable (default: false / Headed live browser)
    public bool Headless { get; } =
        !string.Equals(Environment.GetEnvironmentVariable("PLAYWRIGHT_HEADED"), "true", StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase)
        ? false
        : true;

    public async Task InitializeAsync()
    {
        var port = GetFreePort();
        BaseUrl = $"http://127.0.0.1:{port}";

        var projectPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "KopiKala"));

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --no-build --urls \"{BaseUrl}\"",
            WorkingDirectory = projectPath,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        startInfo.EnvironmentVariables["ASPNETCORE_ENVIRONMENT"] = "Development";
        startInfo.EnvironmentVariables["DOTNET_ENVIRONMENT"] = "Development";

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
                // Server warming up
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
            SlowMo = Headless ? 0 : 75
        });
    }

    private static int GetFreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
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
                // Process cleanup
            }
        }
    }
}
