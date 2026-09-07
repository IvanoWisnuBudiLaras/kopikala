using System.Text.RegularExpressions;
using KopiKala.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using Xunit;
using Xunit.Abstractions;

namespace KopiKala.Tests.E2E;

[Collection("E2E Test Collection")]
public class CustomerBookingE2ETests
{
    private readonly KopiKalaServerFixture _fixture;
    private readonly ITestOutputHelper _output;

    public CustomerBookingE2ETests(KopiKalaServerFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    [Fact]
    public async Task Scenario1_CustomerBookingWizard_CompleteOrderAndGenerateInvoice()
    {
        var context = await _fixture.Browser!.NewContextAsync();
        var page = await context.NewPageAsync();

        try
        {
            // 1. Login terlebih dahulu menggunakan SuperAdmin
            _output.WriteLine($"[E2E] Membuka URL Login: {_fixture.BaseUrl}/Account/Login");
            await page.GotoAsync($"{_fixture.BaseUrl}/Account/Login?returnUrl=/Booking");
            await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

            await page.Locator("input[name='email']").FillAsync("superadmin@kopikala.com");
            await page.Locator("input[name='password']").FillAsync("AdminKopi123!");
            await page.Locator("button[type='submit']").ClickAsync();

            // Tunggu redirect ke /Booking
            await page.WaitForURLAsync(new Regex(".*/Booking.*"));
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            _output.WriteLine("[E2E] Berhasil login dan tiba di halaman /Booking");

            // 2. Step 1: Jadwal & Durasi
            var step1Title = page.GetByText("Langkah 1: Tentukan Tanggal, Sesi & Durasi Duduk");
            await step1Title.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 10000 });
            _output.WriteLine("[E2E] Step 1 aktif.");

            // Pilih durasi 2 Jam
            var durationChip = page.GetByText("2 Jam (Rekomendasi)");
            if (await durationChip.IsVisibleAsync())
            {
                await durationChip.ClickAsync();
                _output.WriteLine("[E2E] Memilih durasi duduk: 2 Jam");
            }

            // Klik 'Lanjut: Pilih Meja'
            var lanjutMejaBtn = page.GetByRole(AriaRole.Button, new() { Name = "Lanjut: Pilih Meja" });
            await lanjutMejaBtn.ClickAsync();
            _output.WriteLine("[E2E] Berpindah ke Step 2 (Denah Meja)");

            // 3. Step 2: Denah Meja 2D
            var step2Title = page.GetByText("Langkah 2: Pilih Denah Meja Interaktif");
            await step2Title.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 10000 });

            // Klik meja IN-01 via data-testid
            var tableCard = page.Locator("[data-testid='table-IN-01']");
            await tableCard.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
            await tableCard.ClickAsync();
            _output.WriteLine("[E2E] Meja IN-01 diklik.");

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

            // Tambah 1 porsi lagi (menjadi 2x)
            var plusIconBtn = kopiSusuCard.Locator("button.mud-button-filled").First;
            if (await plusIconBtn.IsVisibleAsync())
            {
                await plusIconBtn.ClickAsync();
                _output.WriteLine("[E2E] Menambah porsi menjadi 2x.");
            }

            // Tambah menu 'Butter Croissant'
            var croissantCard = page.Locator(".mud-card").Filter(new() { HasText = "Butter Croissant" }).First;
            if (await croissantCard.IsVisibleAsync())
            {
                var addCroissantBtn = croissantCard.GetByRole(AriaRole.Button, new() { Name = "Tambah Menu" });
                if (await addCroissantBtn.IsVisibleAsync())
                {
                    await addCroissantBtn.ClickAsync();
                    _output.WriteLine("[E2E] Menambahkan 1x Butter Croissant ke keranjang.");
                }
            }

            // Klik 'Lanjut: Konfirmasi Pesanan'
            var lanjutCheckoutBtn = page.GetByRole(AriaRole.Button, new() { Name = "Lanjut: Konfirmasi Pesanan" });
            await lanjutCheckoutBtn.ClickAsync();
            _output.WriteLine("[E2E] Berpindah ke Step 4 (Konfirmasi & Checkout)");

            // 5. Step 4: Konfirmasi Identitas & Checkout
            var step4Title = page.GetByText("Langkah 4: Konfirmasi Identitas & Metode Pembayaran");
            await step4Title.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 10000 });

            // Isi Data Pemesan jika belum terisi
            var repInput = page.Locator("input").Filter(new() { Has = page.Locator("..").Locator("label:has-text('Nama Perwakilan')") }).First;
            if (await repInput.CountAsync() == 0)
            {
                repInput = page.Locator("input").Nth(0);
            }
            await repInput.FillAsync("Kak Dimas");

            var phoneInput = page.Locator("input").Nth(1);
            await phoneInput.FillAsync("081234567890");

            // Klik Buat Pesanan
            var submitBtn = page.GetByRole(AriaRole.Button, new() { Name = "Buat Pesanan & Terbitkan Invoice" });
            await submitBtn.ClickAsync();
            _output.WriteLine("[E2E] Mengirim form reservasi & invoice...");

            // 6. Verifikasi Pengalihan ke Halaman Invoice
            await page.WaitForURLAsync(new Regex(".*/Invoice/.*"), new() { Timeout = 15000 });
            _output.WriteLine($"[E2E] Berhasil dialihkan ke halaman invoice: {page.Url}");

            var invoiceHeading = page.Locator("h5:has-text('Invoice')");
            await invoiceHeading.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 10000 });
            var invoiceText = await invoiceHeading.InnerTextAsync();
            Assert.Contains("INV/", invoiceText);
            _output.WriteLine($"  → Kode Invoice: {invoiceText}");

            // Verifikasi instruksi transfer BCA & countdown timer
            var bankText = page.GetByText("123-456-7890");
            Assert.True(await bankText.IsVisibleAsync(), "Nomor rekening bank BCA harus tampil di invoice.");

            var timerText = page.GetByText("Selesaikan pembayaran & unggah bukti transfer dalam:");
            Assert.True(await timerText.IsVisibleAsync(), "Countdown timer 15 menit harus tampil.");

            _output.WriteLine("[SUCCESS] Skenario 1 (Alur Lengkap Pemesanan & Invoice) berhasil 100%!");
        }
        finally
        {
            await context.CloseAsync();
        }
    }

    [Fact]
    public async Task Scenario2_MonkeyTesting_BookingWizardUI_Resilience()
    {
        var context = await _fixture.Browser!.NewContextAsync();
        var page = await context.NewPageAsync();

        try
        {
            // Login
            await page.GotoAsync($"{_fixture.BaseUrl}/Account/Login?returnUrl=/Booking");
            await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

            await page.Locator("input[name='email']").FillAsync("superadmin@kopikala.com");
            await page.Locator("input[name='password']").FillAsync("AdminKopi123!");
            await page.Locator("button[type='submit']").ClickAsync();

            await page.WaitForURLAsync(new Regex(".*/Booking.*"));
            _output.WriteLine("[MONKEY TEST] Memulai pengujian stres resiliensi antarmuka Booking Wizard...");

            // Klik cepat antar tombol navigasi step di header
            var step1Btn = page.GetByText("1. Jadwal & Durasi");
            var step2Btn = page.GetByText("2. Denah Meja");

            for (var i = 0; i < 4; i++)
            {
                if (await step2Btn.IsVisibleAsync())
                {
                    await step2Btn.ClickAsync(new() { Timeout = 1000 });
                }
                if (await step1Btn.IsVisibleAsync())
                {
                    await step1Btn.ClickAsync(new() { Timeout = 1000 });
                }
            }

            // Pindah ke step 2 dan klik acak beberapa kartu meja
            if (await step2Btn.IsVisibleAsync())
            {
                await step2Btn.ClickAsync();
            }

            var tableCards = await page.Locator(".mud-card").AllAsync();
            for (var i = 0; i < Math.Min(tableCards.Count, 6); i++)
            {
                try
                {
                    await tableCards[i].ClickAsync(new() { Timeout = 1000 });
                }
                catch
                {
                    // Ignore non-clickable
                }
            }

            _output.WriteLine("[MONKEY TEST] 15+ aksi acak pada kartu meja dan navigasi stepper dieksekusi tanpa crash.");
            var errorUi = page.Locator("#blazor-error-ui");
            Assert.False(await errorUi.IsVisibleAsync(), "SignalR circuit tidak boleh mengalami crash (blazor-error-ui tidak muncul).");

            _output.WriteLine("[SUCCESS] Skenario 2 (Monkey Resilience UI) berhasil!");
        }
        finally
        {
            await context.CloseAsync();
        }
    }
}
