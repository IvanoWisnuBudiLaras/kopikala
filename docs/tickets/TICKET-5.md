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

1. **Dashboard Operasional Kasir**: Denah meja 2D real-time kasir dengan status meja (Tersedia, Terisi/Seated, Menunggu Pembayaran, Menunggu Verifikasi).
2. **Pencarian Cepat & Check-In**: Pencarian transaksi berdasarkan Nama Perwakilan, Kode Invoice, No Meja, atau No WhatsApp -> Eksekusi Check-In (`SedangDigunakan` / `SeatedAt`).
3. **Verifikasi Bukti Transfer**: Dialog inspeksi foto bukti transfer bank pelanggan, tombol *Verifikasi Lunas* (`Dikonfirmasi`) atau *Tolak Pembayaran* (`Batal`).
4. **Pencatatan Tamu Walk-In Offline**: Kasir dapat membuka meja langsung untuk tamu fisik tanpa reservasi web sebelumnya.
5. **Tambah Pesanan di Tempat (*Add-On Order*)**: Kasir dapat menambahkan item menu F&B ke meja yang sedang aktif nongkrong (`BookingDetails` bertipe `AddOn` dengan audit nota gabungan).
6. **Fitur Ganti Menu (*Item Substitution*)**: Kasir dapat menukar item menu pre-order pelanggan saat check-in jika stok habis atau atas permintaan pelanggan, dengan kalkulasi otomatis selisih harga (upgrade/downgrade).
7. **Perpanjang Waktu Duduk (*Timeslot Extension*)**: Kasir dapat menambah durasi duduk (+1 jam) jika slot sesi berikutnya pada meja tersebut belum dipesan orang lain.
8. **Service Layer & Pengujian Otomatis**: Membangun `ICashierService` / `CashierService` dengan pengujian unit 100% line coverage dan 3 skenario Playwright E2E test live headed browser.

---

## 2. Rincian Langkah Kerja (Step-by-Step Tasks)

### Task 1: DTOs Operasional Kasir (`DTOs/Staff/`)
- `CashierBookingDto.cs`: Kontrak data detail transaksi kasir lengkap beserta rincian meja, jam, tamu, dan item menu.
- `CashierTableStatusDto.cs`: Kontrak data status denah meja 2D.
- `CashierOrderItemDto.cs`: Kontrak data rincian item nota dengan penanda `OrderType` (`PreOrder` / `AddOn`).
- `WalkInBookingRequestDto.cs`: Form input tamu walk-in (Meja, Sesi, Durasi, Nama Perwakilan, WhatsApp, Metode Bayar, Menu).
- `AddOnOrderRequestDto.cs`: Tambah menu ke booking aktif (`BookingId`, `Items`).
- `SubstituteItemRequestDto.cs`: Tukar menu (`BookingId`, `OldDetailId`, `NewMenuItemId`, `NewQuantity`).
- `ExtendDurationRequestDto.cs`: Perpanjang durasi duduk (`BookingId`, `AdditionalHours`).

### Task 2: Service Layer (`Services/ICashierService.cs` & `CashierService.cs`)
- `GetTableStatusesAsync(DateOnly date, int timeslotId)`
- `GetPendingVerificationsAsync()`
- `SearchBookingsAsync(string? query, DateOnly? date)`
- `GetBookingDetailsAsync(Guid bookingId)`
- `VerifyPaymentAsync(Guid bookingId, bool approved, string? notes)`
- `CheckInGuestAsync(Guid bookingId)`
- `CreateWalkInBookingAsync(WalkInBookingRequestDto dto, Guid cashierId)`
- `AddOrderToActiveBookingAsync(AddOnOrderRequestDto dto)`
- `SubstituteMenuItemAsync(SubstituteItemRequestDto dto)`
- `ExtendBookingDurationAsync(ExtendDurationRequestDto dto)`
- `CompleteBookingSessionAsync(Guid bookingId)`

### Task 3: Komponen Antarmuka Kasir MudBlazor (`Components/Pages/Staff/Dashboard.razor`)
- Tab 1: **Denah Meja Kasir**: Tampilan visual meja 2D Indoor AC & Outdoor Smoking dengan badge status dan aksi cepat (Check-in, Add-on, Extend, Detail).
- Tab 2: **Verifikasi Transfer**: Tabel daftar booking yang menunggu verifikasi bukti bayar, modal pratinjau foto bukti, dan tombol Setujui/Tolak.
- Tab 3: **Pencarian & Riwayat Transaksi Hari Ini**: Filter cepat berdasarkan nama perwakilan, kode invoice, atau nomor meja.
- Tab 4: **Buka Meja Walk-In**: Dialog / form cepat untuk tamu fisik langsung.

---

## 3. Standar Pengujian Wajib (Testing Specifications)

### A. Unit Test Coverage
- Minimal **80% Line Coverage** pada `Services/CashierService.cs` dan `DTOs/Staff/`.
- **Hasil Aktual**: **100% Line Coverage**, **100% Method Coverage** pada `CashierService.cs` (83 tests passed).

### B. End-to-End (E2E) Browser Test via Microsoft Playwright
- `Scenario1_CashierPortal_LoginAndVerifyTableFloorPlan` (Passed): Login kasir, verifikasi denah meja 2D, layout badge status.
- `Scenario2_CashierWalkIn_SimplifiedFlow` (Passed): Alur pembukaan meja offline walk-in langsung dan perubahan status ke `SedangDigunakan`.
- `Scenario3_CashierVerifyPayment_InspectionAndApprovalFlow` (Passed): Inspeksi foto bukti transfer bank, approval lunas, dan pembaruan instan status meja.

---

## 4. Kriteria Keberhasilan (Acceptance Criteria)

1. Kasir dapat memverifikasi bukti bayar transfer dan check-in tamu tanpa reload halaman.
2. Tamu walk-in offline dapat langsung menempati meja dan tercatat di database.
3. Add-on menu dan pergantian menu terhitung akurat pada total tagihan akhir.
4. Perpanjangan waktu duduk divalidasi ketat terhadap ketersediaan slot berikutnya.
5. Unit tests lulus dengan coverage 100% dan E2E test berjalan hijau di Chromium.
