# TICKET-5: Staff Portal — Cashier Operations (Check-In, Payment Verification, Walk-In, Add-On Orders & Menu Substitution)

| Metadata | Details |
| :--- | :--- |
| **Ticket ID** | `TICKET-5` |
| **Project** | KopiKala Reservation & Order System |
| **Status** | `COMPLETED` |
| **Priority** | High / Core Operational Flow |
| **Prerequisites** | `TICKET-1`, `TICKET-2`, `TICKET-3`, & `TICKET-4` completed |
| **Target Framework** | C# .NET 10 |
| **Assignee** | Developer (PKL) |

---

## 1. Objective & Description

Tiket ini berfokus pada pembangunan **Portal Operasional Kasir (`/Staff` / `Meja.Kelola` & `Pembayaran.Verifikasi`)**:

1. **Dashboard Operasional Kasir**: Denah meja 2D real-time kasir dengan status meja (Tersedia, Terisi/Seated, Menunggu Pembayaran, Kedaluwarsa).
2. **Pencarian Cepat & Check-In**: Pencarian transaksi berdasarkan Nama Perwakilan atau Kode Invoice -> Eksekusi Check-In (`SedangDigunakan` / `Seated`).
3. **Verifikasi Bukti Transfer**: Dialog inspeksi foto bukti transfer bank pelanggan, tombol *Verifikasi Lunas* atau *Tolak Pembayaran*.
4. **Pencatatan Tamu Walk-In Offline**: Kasir dapat membuka meja langsung untuk tamu fisik tanpa reservasi web sebelumnya.
5. **Tambah Pesanan di Tempat (*Add-On Order*)**: Kasir dapat menambahkan item menu F&B ke meja yang sedang aktif nongkrong (`BookingDetails` tambahan dengan audit nota gabungan).
6. **Fitur Ganti Menu (*Item Substitution*)**: Kasir dapat menukar item menu pre-order pelanggan saat check-in jika stok habis/permintaan pelanggan, dengan kalkulasi otomatis selisih harga (upgrade/downgrade).
7. **Perpanjang Waktu Duduk (*Timeslot Extension*)**: Kasir dapat menambah durasi duduk (+1 jam) jika slot sesi berikutnya pada meja tersebut belum dipesan orang lain.
8. **Service Layer & Pengujian Otomatis**: Membangun `ICashierService` / `CashierService` dengan pengujian unit minimal 80% coverage dan Playwright E2E test.

---

## 2. Rincian Langkah Kerja (Step-by-Step Tasks)

### Task 1: DTOs Operasional Kasir (`DTOs/Staff/`)
- `CashierBookingSummaryDto.cs`: Ringkasan transaksi kasir untuk tabel antrean dan pencarian.
- `WalkInBookingRequestDto.cs`: Form input tamu walk-in (Meja, Durasi, Nama Perwakilan, WhatsApp, Metode Bayar, Menu).
- `AddOnOrderRequestDto.cs`: Tambah menu ke booking aktif (`BookingId`, `Items`).
- `SubstituteItemRequestDto.cs`: Tukar menu (`BookingId`, `OldDetailId`, `NewMenuItemId`, `Quantity`).
- `ExtendDurationRequestDto.cs`: Perpanjang durasi duduk (`BookingId`, `AdditionalHours`).

### Task 2: Service Layer (`Services/ICashierService.cs` & `CashierService.cs`)
- `GetActiveTableStatusesAsync(DateOnly date, int timeslotId)`
- `SearchBookingsAsync(string query, DateOnly? date)`
- `GetPendingVerificationsAsync()`
- `VerifyPaymentAsync(Guid bookingId, bool approved, string? notes)`
- `CheckInGuestAsync(Guid bookingId)`
- `CreateWalkInBookingAsync(WalkInBookingRequestDto dto, Guid cashierId)`
- `AddOrderToActiveBookingAsync(AddOnOrderRequestDto dto)`
- `SubstituteMenuItemAsync(SubstituteItemRequestDto dto)`
- `ExtendBookingDurationAsync(ExtendDurationRequestDto dto)`

### Task 3: Komponen Antarmuka Kasir MudBlazor (`Components/Pages/Staff/Dashboard.razor`)
- Tab 1: **Denah Meja Kasir**: Tampilan visual meja 2D dengan badge status dan aksi cepat (Check-in, Add-on, Extend, Detail).
- Tab 2: **Verifikasi Transfer**: Tabel daftar booking yang menunggu verifikasi bukti bayar, modal pratinjau foto bukti, dan tombol Setujui/Tolak.
- Tab 3: **Pencarian & Riwayat Transaksi Hari Ini**: Filter cepat berdasarkan nama perwakilan, kode invoice, atau nomor meja.
- Tab 4: **Buka Meja Walk-In**: Dialog / form cepat untuk tamu fisik langsung.

---

## 3. Standar Pengujian Wajib (Testing Specifications)

### A. Unit Test Coverage
- Minimal **80% Line Coverage** pada `Services/CashierService.cs` dan `DTOs/Staff/`.
- Skenario Uji:
  1. Uji verifikasi bukti bayar mengubah status booking ke `Dikonfirmasi` / `Lunas`.
  2. Uji tolak verifikasi mengubah status ke `Batal`.
  3. Uji check-in mengubah status ke `SedangDigunakan`.
  4. Uji penambahan pesanan add-on menghitung penambahan `TotalAmount`.
  5. Uji ganti menu menghitung selisih harga (upgrade menambah tagihan, downgrade mengurangi).
  6. Uji perpanjangan waktu gagal jika sesi berikutnya bentrok dengan booking lain.
  7. Uji pembuatan walk-in booking berhasil mengunci meja.

### B. End-to-End (E2E) Browser Test via Microsoft Playwright
- Login sebagai Kasir (`kasir@kopikala.com`).
- Buka `/Staff`.
- Verifikasi bukti transfer pending.
- Lakukan check-in tamu via pencarian nama perwakilan.
- Lakukan add-on menu ke meja aktif.

---

## 4. Kriteria Keberhasilan (Acceptance Criteria)

1. Kasir dapat memverifikasi bukti bayar transfer dan check-in tamu tanpa reload halaman.
2. Tamu walk-in offline dapat langsung menempati meja dan tercatat di database.
3. Add-on menu dan pergantian menu terhitung akurat pada total tagihan akhir.
4. Perpanjangan waktu duduk divalidasi ketat terhadap ketersediaan slot berikutnya.
5. Unit tests lulus dengan coverage >= 80%.
