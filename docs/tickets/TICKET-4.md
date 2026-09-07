# TICKET-4: Customer Booking Wizard, Stay Duration, F\&B Pre-Order & Invoice Generation

| Metadata | Details |
| :---- | :---- |
| **Ticket ID** | `TICKET-4` |
| **Project** | KopiKala Reservation & Order System |
| **Status** | `OPEN / READY FOR IMPLEMENTATION` |
| **Priority** | Critical / Core Business Flow |
| **Prerequisites** | `TICKET-1`, `TICKET-2`, & `TICKET-3` completed |
| **Target Framework** | C\# .NET 10 |
| **Assignee** | Developer (PKL) |

---

## 1\. Objective & Description

Tiket ini berfokus pada pembangunan **jantung transaksi pelanggan (Customer Portal)**:

1. Membuat antarmuka pemesanan bertahap (*Step-by-Step Wizard*) menggunakan komponen **`MudStepper`** di halaman `Components/Pages/Customer/Booking.razor`.  
2. Menerapkan pemilihan durasi duduk (1, 2, atau 3 jam) dan denah meja 2D interaktif berkode warna.  
3. Menerapkan mekanisme penahanan meja sementara (*Temporary Hold 15 Menit*) di dalam `Session` serta penguncian *Optimistic Concurrency Control* (`xmin`) di database PostgreSQL untuk mencegah *double-booking*.  
4. Membangun katalog F\&B pre-order dengan penambah porsi dan *Sticky Bottom Summary Bar*.  
5. Menghasilkan halaman invoice lengkap: kode invoice unik, input Nama Perwakilan, info nomor rekening kafe, form upload bukti transfer (`MudFileUpload`), serta opsi *Bayar di Tempat*.  
6. Menyiapkan rangkaian tes otomatis (Unit Test Coverage 80% & API Test dengan simulasi mesin waktu `FakeTimeProvider`).  
7. Menambahkan pengujian End-to-End (E2E) browser nyata menggunakan Microsoft Playwright (Live Headed Mode & Monkey Testing) untuk memvalidasi alur pemesanan 4 langkah MudStepper, kalkulasi tagihan sticky bar, dan penerbitan nota invoice secara visual dan otomatis.

---

## 2\. Rincian Langkah Kerja (Step-by-Step Tasks)

### Task 1: Pembuatan DTOs Pemesanan (Folder `DTOs/Booking/`)

Buat class kontrak data di folder **`DTOs/Booking/`**:

1. **`DTOs/Booking/CreateBookingRequestDto.cs`**:  
   * `TableId` (Guid, required)  
   * `BookingDate` (DateOnly, required)  
   * `TimeslotId` (int, required)  
   * `DurationHours` (int, default 2, range 1-3)  
   * `RepresentativeName` (string, required, nama perwakilan pemesan)  
   * `CustomerPhone` (string, required, nomor WhatsApp)  
   * `PaymentMethod` (string: `'TransferBank'` atau `'BayarDiTempat'`)  
   * `SelectedItems` (List)  
2. **`DTOs/Booking/OrderItemDto.cs`**:  
   * `MenuItemId` (Guid)  
   * `Quantity` (int, min 1\)  
   * `UnitPrice` (decimal)  
3. **`DTOs/Booking/InvoiceResponseDto.cs`**:  
   * `BookingId` (Guid), `InvoiceCode` (string), `TableNumber` (string), `RepresentativeName` (string)  
   * `BookingDate` (DateOnly), `StartTime` (TimeSpan), `EndTime` (TimeSpan), `DurationHours` (int)  
   * `Items` (List), \`TotalAmount\` (decimal), \`Status\` (string), \`PaymentMethod\` (string)  
   * `PaymentProofUrl` (string?), `ExpiresAt` (DateTime)

---

### Task 2: Service Layer Pemesanan (`IBookingService` & `BookingService`)

Buat kontrak dan implementasi logika bisnis di folder **`Services/`**:

* **`Services/IBookingService.cs`**:  
    
  public interface IBookingService  
    
  {  
    
      Task\<List\<DiningTableDto\>\> GetAvailableTablesAsync(DateOnly date, int timeslotId);  
    
      Task\<bool\> HoldTableSessionAsync(Guid tableId, DateOnly date, int timeslotId);  
    
      Task\<Guid\> CreateBookingAsync(CreateBookingRequestDto dto, Guid userId);  
    
      Task\<InvoiceResponseDto?\> GetInvoiceAsync(Guid bookingId);  
    
      Task\<bool\> UploadPaymentProofAsync(Guid bookingId, IBrowserFile file);  
    
  }  
    
* **Logika Inti di `Services/BookingService.cs`**:  
  1. **Validasi Anti-Bentrok (OCC & SQL Unique)**: Memeriksa apakah meja sudah dipesan di tanggal dan sesi tersebut. Jika ada bentrokan, tangkap `DbUpdateConcurrencyException` atau constraint violation dan lempar pesan ramah.  
  2. **Perhitungan Waktu Selesai**: `EndTime = Timeslot.StartTime + TimeSpan.FromHours(dto.DurationHours)`.  
  3. **Penyimpanan Transaksi**:  
     * Buat kode invoice unik via `InvoiceCodeHelper` (misal: `INV/20260906/001`).  
     * Simpan entitas `Booking` (status `MenungguBayar`) dan entitas `BookingDetails` (mencatat `UnitPrice` saat transaksi).  
     * Set `ExpiresAt = DateTime.UtcNow.AddMinutes(15)` untuk batas waktu transfer.

---

### Task 3: Wizard Pemesanan 4 Langkah MudBlazor (`Components/Pages/Customer/Booking.razor`)

Bangun alur interaktif menggunakan **`MudStepper`**:

* **Langkah 1: Tanggal & Durasi Duduk**:  
  * Pemilih tanggal: `MudDatePicker` (hanya bisa memilih hari ini ke depan).  
  * Pemilih sesi waktu: `MudSelect` daftar sesi jam kafe.  
  * Pemilih durasi duduk: `MudChipSet` pilihan pil empuk: `[1 Jam]`, `[2 Jam]`, `[3 Jam]`.  
* **Langkah 2: Denah Meja Visual 2D**:  
  * Grid responsif (`MudGrid`) memisahkan Area Indoor AC dan Outdoor Smoking.  
  * Kartu meja (`MudCard`) dengan indikator kursi dan warna status:  
    * *Hijau*: Kosong / Siap Dipilih.  
    * *Kuning*: Sedang Dipilih oleh Anda.  
    * *Abu-abu*: Sudah Terisi / Tidak Tersedia.  
* **Langkah 3: Pre-Order Menu F\&B**:  
  * Grid kartu menu (`MudCardMedia`) menampilkan foto, nama, harga rupiah, dan tombol porsi `[-] 1 [+]`.  
  * Saklar *"Tandai Habis"* dihormati: menu yang `IsAvailable = false` otomatis berwarna abu-abu redup dan tidak bisa ditambah porsinya.  
* **Sticky Bottom Bar Mengambang**:  
  * Menampilkan ringkasan langsung di bawah layar:  
    `Meja Terpilih: T-02 (2 Jam) | Total F&B: Rp 50.000` \-\> Tombol `[Lanjut ke Checkout]`.

---

### Task 4: Halaman Detail Invoice & Pembayaran (`Components/Pages/Customer/Invoice.razor`)

Halaman nota tagihan terpadu:

* Ringkasan: Nomor Meja, Nama Perwakilan Rombongan, Waktu Kunjungan, Rincian Menu.  
* **Countdown Timer 15 Menit**: Komponen pengingat hitung mundur sisa waktu pembayaran.  
* **Pilihan Jalur Pembayaran**:  
  * **Jalur A (Transfer Bank)**:  
    * Kotak info rekening kafe: *Bank BCA 123-456-7890 a.n. KopiKala Official* \+ Tombol 1-klik **"Salin Rekening"**.  
    * Komponen `MudFileUpload` untuk mengunggah foto struk transfer (divalidasi oleh `FileSecurityHelper` maks 2MB, ekstensi `.jpg`/`.png`).  
    * Setelah diunggah, status berubah menjadi `MenungguVerifikasiKasir`.  
  * **Jalur B (Bayar di Tempat)**:  
    * Pelanggan memilih bayar tunai/QRIS saat tiba di lokasi. Status langsung berubah menjadi `Dikonfirmasi` dengan catatan batas toleransi keterlambatan 20 menit.

---

## 3\. Standar Pengujian Wajib (Testing Specifications)

### A. Unit Test Coverage (Target Persentase)

* **Target Coverage**: Minimal **80% Line Coverage** pada `Services/BookingService.cs` dan `DTOs/Booking/`.  
* **Pustaka**: xUnit, Moq, dan Coverlet (`coverlet.collector`).  
* **Skenario Uji Unit**:  
  1. Uji kalkulasi `EndTime` berdasarkan `DurationHours` (1, 2, dan 3 jam).  
  2. Uji perhitungan total tagihan F\&B (`Quantity * UnitPrice`).  
  3. Uji penolakan booking jika meja sudah berstatus terisi di tanggal dan sesi yang sama.

### B. API / Integration Test (Pencatatan Log & Time Traveler)

* **Pustaka**: `Microsoft.AspNetCore.Mvc.Testing`, `Microsoft.Extensions.TimeProvider.Testing` (`FakeTimeProvider`), dan `ITestOutputHelper`.  
* **Skenario Time Traveler (`FakeTimeProvider`)**:  
  1. Buat booking dengan status `MenungguBayar` (batas waktu 15 menit).  
  2. Gunakan `fakeTimeProvider.Advance(TimeSpan.FromMinutes(16))` untuk memajukan waktu simulasi 16 menit ke depan.  
  3. Verifikasi melalui endpoint/service bahwa status booking telah dinyatakan kedaluwarsa (*Expired/Cancelled*) dan meja otomatis berstatus kembali tersedia (*Available*).  
* **Standar Logging**: Mencatat rincian kode status HTTP, ID booking yang terbentuk, latensi eksekusi kueri, dan payload respons pada output tes.

### C. End-to-End (E2E) Browser Test via Microsoft Playwright

* **Mode Eksekusi**: Live Headed Browser (Headless \= false dengan slowMo 50-100ms) sehingga pengembang dapat menyaksikan langsung robot memilih tanggal, durasi, meja, dan menu di monitor.  
* **Skenario 1 (Alur Lengkap Pemesanan Pelanggan)**:  
  1. Robot membuka halaman `/Booking`.  
  2. Memilih tanggal hari ini, memilih sesi jam, dan memilih durasi duduk (misal 2 Jam via MudChip).  
  3. Mengklik Meja T-02 di denah meja 2D dan memverifikasi perubahan warna dari hijau ke kuning.  
  4. Beralih ke langkah F\&B: Menambah 2x Es Kopi Susu dan 1x Croissant. Memverifikasi teks total tagihan di Sticky Bottom Bar terhitung otomatis pas (Rp 58.000).  
  5. Melanjutkan ke langkah Checkout: Mengisi nama perwakilan (misal 'Kak Dimas') dan nomor WhatsApp, memilih opsi 'Transfer Bank', lalu menekan tombol 'Konfirmasi Booking'.  
  6. Memverifikasi pengalihan ke halaman `/Invoice/{id}` dan memeriksa munculnya kode invoice, countdown timer 15 menit, serta info rekening BCA kafe.  
* **Skenario 2 (Monkey Test / Chaos Resiliensi Meja)**:  
  7. Robot melakukan klik bertubi-tubi secara cepat pada beberapa kartu meja dan tombol penambah porsi menu untuk menguji bahwa sirkuit SignalR Blazor tidak putus dan Optimistic Concurrency Control (xmin) tidak memunculkan unhandled exception.

---

## 4\. Kriteria Keberhasilan (Acceptance Criteria)

Tiket ini dinyatakan selesai (*Done*) jika seluruh kondisi berikut terbukti lolos uji:

1. Pelanggan dapat menyelesaikan wizard 4 langkah (Tanggal & Durasi \-\> Pilih Meja \-\> Pre-order Menu \-\> Checkout) tanpa error runtime.  
2. Data tersimpan sah di PostgreSQL: 1 baris di tabel `bookings` dan baris menu di `booking_details`.  
3. Foto bukti transfer berhasil tersimpan di folder `wwwroot/uploads/payments/` dengan nama acak GUID yang aman.  
4. Dua pengguna di browser berbeda yang mencoba mengklik meja yang sama di tanggal & sesi yang sama: orang pertama berhasil, orang kedua menerima notifikasi penolakan yang ramah (*Optimistic Concurrency Control* bekerja).  
5. Laporan Coverlet menunjukkan Line Coverage `BookingService` mencapai minimal 80%.  
6. Seluruh pengujian E2E Playwright berjalan sukses (Passed / Hijau) di browser Chromium live tanpa crash koneksi SignalR.

