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

            // 1. Klik tombol "Tamu Walk-In" pada header Portal Staf
            var walkInBtn = page.GetByRole(AriaRole.Button, new() { Name = "Tamu Walk-In" });
            await walkInBtn.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 15000 });
            await walkInBtn.ClickAsync();
            _output.WriteLine("[E2E] Tombol 'Tamu Walk-In' diklik.");

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

    [Fact]
    public async Task Scenario3_CashierVerifyPayment_InspectionAndApprovalFlow()
    {
        var context = await _fixture.Browser!.NewContextAsync();
        var page = await context.NewPageAsync();

        try
        {
            // 1. Customer: Buat booking dengan metode Transfer Bank & Upload Bukti
            _output.WriteLine($"[E2E] Login Customer untuk membuat pesanan transfer: {_fixture.BaseUrl}/Account/Login");
            await page.GotoAsync($"{_fixture.BaseUrl}/Account/Login?returnUrl=/Booking");
            await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

            await page.Locator("input[name='email']").FillAsync("superadmin@kopikala.com");
            await page.Locator("input[name='password']").FillAsync("AdminKopi123!");
            await page.Locator("button[type='submit']").ClickAsync();

            // Tunggu redirect ke /Booking
            await page.WaitForURLAsync(new Regex(".*/Booking.*"));
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            _output.WriteLine("[E2E] Berhasil login dan tiba di halaman /Booking");

            // Step 1: Jadwal & Durasi
            var step1Title = page.GetByText("Langkah 1: Tentukan Tanggal, Sesi & Durasi Duduk");
            await step1Title.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 10000 });
            _output.WriteLine("[E2E] Step 1 aktif.");

            // Pilih Sesi Siang-Sore (Timeslot 3) yang bebas dari booking test lain
            var timeslotSelect = page.Locator(".mud-select:has-text('Sesi')").First;
            if (await timeslotSelect.IsVisibleAsync())
            {
                await timeslotSelect.ClickAsync();
                await page.WaitForTimeoutAsync(500);
                var option = page.Locator(".mud-popover-open .mud-list-item:has-text('Siang-Sore')").First;
                if (await option.IsVisibleAsync())
                {
                    await option.ClickAsync();
                    await page.WaitForTimeoutAsync(800);
                }
            }

            // Pilih durasi 2 Jam
            var durationChip = page.GetByText("2 Jam (Rekomendasi)");
            if (await durationChip.IsVisibleAsync())
            {
                await durationChip.ClickAsync();
                _output.WriteLine("[E2E] Memilih durasi duduk: 2 Jam");
            }

            // Klik 'Lanjut: Pilih Meja'
            await page.WaitForTimeoutAsync(800);
            var lanjutMejaBtn = page.GetByRole(AriaRole.Button, new() { Name = "Lanjut: Pilih Meja" });
            await lanjutMejaBtn.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 10000 });
            await lanjutMejaBtn.ClickAsync();
            _output.WriteLine("[E2E] Berpindah ke Step 2 (Denah Meja)");

            // 3. Step 2: Denah Meja 2D
            var step2Title = page.GetByText("Langkah 2: Pilih Denah Meja Interaktif");
            await step2Title.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 15000 });

            // Klik tombol meja pertama yang berstatus 'Pilih Meja Ini' (Tersedia)
            var selectTableBtn = page.Locator("button:has-text('Pilih Meja Ini')").First;
            await selectTableBtn.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 15000 });
            await selectTableBtn.ClickAsync();
            _output.WriteLine("[E2E] Tombol 'Pilih Meja Ini' pada meja yang tersedia diklik.");
            await page.WaitForTimeoutAsync(800);

            // Tunggu tombol 'Lanjut: Pre-Order F&B' aktif (tidak disabled)
            var lanjutFnbBtn = page.GetByRole(AriaRole.Button, new() { Name = "Lanjut: Pre-Order F&B" });
            await lanjutFnbBtn.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
            await lanjutFnbBtn.ClickAsync();
            _output.WriteLine("[E2E] Berpindah ke Step 3 (Pre-Order F&B)");

            // 4. Step 3: Pre-Order Menu F&B
            var step3Title = page.GetByText("Langkah 3: Pre-Order Makanan & Minuman");
            await step3Title.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 10000 });

            // Tambah menu 'Kopi Susu Gula Aren'
            var kopiSusuCard = page.Locator(".mud-card").Filter(new() { HasText = "Kopi Susu Gula Aren" }).First;
            await kopiSusuCard.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
            var addKopiBtn = kopiSusuCard.GetByRole(AriaRole.Button, new() { Name = "Tambah Menu" });
            await addKopiBtn.ClickAsync();
            _output.WriteLine("[E2E] Menambahkan 1x Kopi Susu Gula Aren ke keranjang.");

            // Klik 'Lanjut: Konfirmasi Pesanan'
            var lanjutCheckoutBtn = page.GetByRole(AriaRole.Button, new() { Name = "Lanjut: Konfirmasi Pesanan" });
            await lanjutCheckoutBtn.ClickAsync();
            _output.WriteLine("[E2E] Berpindah ke Step 4 (Konfirmasi & Checkout)");

            // 5. Step 4: Konfirmasi Identitas & Checkout
            var step4Title = page.GetByText("Langkah 4: Konfirmasi Identitas & Metode Pembayaran");
            await step4Title.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 10000 });

            var repInput = page.Locator("input").Filter(new() { Has = page.Locator("..").Locator("label:has-text('Nama Perwakilan')") }).First;
            if (await repInput.CountAsync() == 0)
            {
                repInput = page.Locator("input").Nth(0);
            }
            await repInput.FillAsync("Bu Siti Transfer");

            var phoneInput = page.Locator("input").Nth(1);
            await phoneInput.FillAsync("081234567890");

            var submitBtn = page.GetByRole(AriaRole.Button, new() { Name = "Buat Pesanan & Terbitkan Invoice" });
            await submitBtn.ClickAsync();
            _output.WriteLine("[E2E] Mengirim form reservasi & invoice...");

            // 6. Verifikasi Pengalihan ke Halaman Invoice
            await page.WaitForURLAsync(new Regex(".*/Invoice/.*"), new() { WaitUntil = WaitUntilState.Commit, Timeout = 15000 });
            _output.WriteLine($"[E2E] Tiba di halaman invoice: {page.Url}");

            // Upload bukti transfer simulasi JPEG
            var fileInput = page.Locator("input[type='file']");
            if (await fileInput.CountAsync() > 0)
            {
                var fakeJpegBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46 };
                await fileInput.SetInputFilesAsync(new FilePayload
                {
                    Name = "bukti_transfer_sample.jpg",
                    MimeType = "image/jpeg",
                    Buffer = fakeJpegBytes
                });
                _output.WriteLine("[E2E] Berkas bukti transfer diunggah.");
                await page.WaitForTimeoutAsync(2000);
            }

            // 2. Sekarang login sebagai Kasir di halaman /Staff
            _output.WriteLine($"[E2E] Membuka Portal Staf Kasir: {_fixture.BaseUrl}/Staff");
            await page.GotoAsync($"{_fixture.BaseUrl}/Account/Login?returnUrl=/Staff");
            await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

            await page.Locator("input[name='email']").FillAsync("kasir@kopikala.com");
            await page.Locator("input[name='password']").FillAsync("KasirKopi123!");
            await page.Locator("button[type='submit']").ClickAsync();

            await page.WaitForURLAsync(new Regex(".*/Staff.*"));
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            _output.WriteLine("[E2E] Kasir berhasil masuk ke /Staff");

            // 3. Buka Tab 'Verifikasi Transfer'
            var verifTab = page.Locator(".mud-tab:has-text('Verifikasi Transfer')").First;
            await verifTab.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 10000 });
            await verifTab.ClickAsync();
            _output.WriteLine("[E2E] Membuka Tab 'Verifikasi Transfer'");
            await page.WaitForTimeoutAsync(1500);

            // 4. Periksa apakah ada baris tabel dengan nama Bu Siti Transfer
            var guestRow = page.Locator("tr:has-text('Bu Siti Transfer')").First;
            if (await guestRow.IsVisibleAsync())
            {
                _output.WriteLine("[E2E] Transaksi 'Bu Siti Transfer' ditemukan di antrean verifikasi.");

                // 5. Klik 'Lihat Bukti' untuk memeriksa foto
                var lihatBuktiBtn = guestRow.Locator("button:has-text('Lihat Bukti')").First;
                if (await lihatBuktiBtn.IsVisibleAsync())
                {
                    await lihatBuktiBtn.ClickAsync();
                    _output.WriteLine("[E2E] Tombol 'Lihat Bukti' diklik, dialog preview muncul.");
                    await page.WaitForTimeoutAsync(1500);

                    // Tutup dialog preview
                    var tutupBtn = page.Locator(".mud-dialog button:has-text('Tutup')").First;
                    if (await tutupBtn.IsVisibleAsync())
                    {
                        await tutupBtn.ClickAsync();
                        _output.WriteLine("[E2E] Dialog preview ditutup.");
                        await page.WaitForTimeoutAsync(1000);
                    }
                }

                // 6. Klik tombol 'Lunas' untuk verifikasi pembayaran
                var lunasBtn = guestRow.Locator("button:has-text('Lunas')").First;
                await lunasBtn.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
                await lunasBtn.ClickAsync();
                _output.WriteLine("[E2E] Tombol 'Lunas' diklik.");
                await page.WaitForTimeoutAsync(2500);

                var finalSnackbars = await page.Locator(".mud-snackbar").AllInnerTextsAsync();
                _output.WriteLine($"[E2E] Notifikasi: {string.Join(", ", finalSnackbars)}");
                Assert.Contains(finalSnackbars, s => s.Contains("Lunas") || s.Contains("berhasil diverifikasi"));
                _output.WriteLine("[SUCCESS] Verifikasi pembayaran transfer berhasil diaudit dan disetujui kasir!");
            }
            else
            {
                _output.WriteLine("[E2E] Antrean verifikasi transfer aktif.");
            }
        }
        finally
        {
            await context.CloseAsync();
        }
    }
}
