# TICKET-7: SuperAdmin Portal, Dynamic PBAC Governance, Master Data & Business Analytics Dashboard

| Metadata | Details |
| :--- | :--- |
| **Ticket ID** | `TICKET-7` |
| **Project** | KopiKala Reservation & Order System |
| **Status** | `OPEN / READY FOR IMPLEMENTATION` |
| **Priority** | High / Governance & Analytics |
| **Prerequisites** | `TICKET-1` s/d `TICKET-6` completed |
| **Target Framework** | C# .NET 10 |
| **Assignee** | Developer (PKL) |

---

## 1. Objective & Description

Tiket ini berfokus pada pembangunan **ruang kendali pemilik sistem (SuperAdmin Portal)** pada rute terproteksi `/SuperAdmin`:

1. **Manajemen Akun Staf**: Penambahan staf baru, reset password staf, dan pencatatan jejak audit login.
2. **Tata Kelola Dynamic PBAC**: Pembuatan peran kustom, antarmuka centang 5 izin operasional inti (`Meja.Kelola`, `Pembayaran.Verifikasi`, `Dapur.Antrean`, `Laporan.Lihat`, `Sistem.Kelola`), serta tombol 1-klik penerapan template peran bawaan (Kasir, Barista, Manajer).
3. **Pengelolaan Master Data Kafe**: Pengaturan meja fisik (nomor meja, kapasitas kursi, area Indoor/Outdoor, saklar aktif) dan master katalog F&B (nama menu, kategori, harga, stok, ketersediaan, foto).
4. **Mini Analytics Dashboard**: Kartu metrik KPI omzet harian (terintegrasi dengan Cron Job `daily_reports`), rasio okupansi meja real-time, dan visualisasi `MudChart` (grafik donat status meja dan grafik batang menu terlaris).
5. **Rangkaian Pengujian Menyeluruh**: Unit Test Coverage minimal 80%, API Test dengan `FakeTimeProvider`, dan E2E Live Browser Playwright + Monkey Testing.

---

## 2. Rincian Langkah Kerja (Step-by-Step Tasks)

### Task 1: DTOs Tata Kelola SuperAdmin (`DTOs/Admin/`)
- `CreateStaffRequestDto.cs`: Form pendaftaran staf baru (`FullName`, `Email`, `PhoneNumber`, `Password`, `RoleId`).
- `StaffOverviewDto.cs`: Ringkasan akun staf (`UserId`, `FullName`, `Email`, `RoleName`, `IsActive`, `LastLoginAt`).
- `ManageRoleDto.cs`: Form kelola peran & izin (`RoleId`, `RoleName`, `Description`, `SelectedPermissionIds`).
- `ManageTableDto.cs`: Form CRUD meja (`TableId`, `TableNumber`, `Capacity`, `Area`, `IsActive`).
- `ManageMenuItemDto.cs`: Form CRUD menu F&B (`MenuItemId`, `Name`, `Category`, `Price`, `Stock`, `IsAvailable`, `ImageFile`).
- `AnalyticsDashboardDto.cs`: Kontrak data analitik (`TodayRevenue`, `ActiveOccupancyRate`, `TotalBookingsToday`, `TotalNoShowsToday`, `TopSellingItems`, `OccupancyDonutData`).

### Task 2: Service Tata Kelola SuperAdmin (`ISuperAdminService` & `SuperAdminService.cs`)
- `GetStaffListAsync()`: Mengambil daftar akun staf beserta perannya.
- `CreateStaffAsync(CreateStaffRequestDto dto)`: Membuat akun staf baru dengan hash PBKDF2.
- `ResetStaffPasswordAsync(Guid userId, string temporaryPassword)`: Mereset password akun staf.
- `GetRolesWithPermissionsAsync()`: Mengambil seluruh peran beserta izin PBAC yang terasosiasi.
- `SaveRolePermissionsAsync(ManageRoleDto dto)`: Menyimpan / memperbarui peran dan relasi many-to-many `role_permissions`.
- `SaveTableAsync(ManageTableDto dto)`: Menambah atau mengedit meja fisik.
- `SaveMenuItemAsync(ManageMenuItemDto dto)`: Menambah atau mengedit item menu F&B.
- `GetAnalyticsDashboardDataAsync()`: Mengompilasi KPI transaksi hari ini dan rekapan harian `daily_reports`.

### Task 3: Antarmuka Dasbor Analitik & Master Data (`Components/Pages/Admin/`)
1. **`Dashboard.razor` (Mini Analytics & KPI Kafe)**:
   - 4 Kartu Metrik Angka Tebal: Total Omzet Hari Ini, Okupansi Meja Real-Time, Total Booking Selesai vs No-Show.
   - `MudChart` Donut: Rasio Okupansi Meja (Tersedia vs Terisi).
   - `MudChart` Bar: 5 Menu Terlaris.
2. **`StaffManagement.razor` (Kelola Akun & Dynamic PBAC)**:
   - Tabel daftar staf (`MudTable`) dengan aksi Reset Password.
   - Tab Kelola Peran: Centang 5 izin operasional dan 1-klik template.
3. **`MasterData.razor` (Kelola Meja & Menu F&B)**:
   - Tabel & Form Meja: Nomor, kapasitas, area, saklar aktif.
   - Tabel & Form Menu F&B: Nama, kategori, harga, stok, ketersediaan, upload foto.

---

## 3. Standar Pengujian Wajib (Testing Specifications)

### A. Unit Test Coverage
- Minimal **80% Line Coverage** pada `Services/SuperAdminService.cs` dan `DTOs/Admin/`.
- Skenario: Uji pembuatan staf, validasi PBAC pivot table, CRUD meja/menu, dan kompilasi analitik.

### B. Integration / API Test dengan Time Traveler (`FakeTimeProvider`)
- Menguji penarikan metrik analitik omzet lintas hari melintasi pergantian tanggal `daily_reports`.

### C. End-to-End (E2E) Browser Test via Microsoft Playwright
- Login SuperAdmin -> navigasi ke `/SuperAdmin`.
- Pembuatan peran baru dan penugasan izin PBAC dinamis.
- Pengujian interaksi visual dashboard analitik dan Monkey Chaos Testing.

---

## 4. Kriteria Keberhasilan (Acceptance Criteria)

1. Rute `/SuperAdmin` hanya dapat diakses oleh pemegang izin `Sistem.Kelola` (role lain dialihkan ke 403 / Access Denied).
2. SuperAdmin dapat membuat peran baru dan mengatur izin PBAC yang langsung tersimpan di PostgreSQL.
3. Master data meja dan menu dapat diperbarui tanpa error runtime.
4. Dashboard analitik membaca dan menampilkan omzet serta diagram `MudChart` secara akurat.
5. Unit tests lulus dengan coverage >= 80% dan Playwright E2E berjalan sukses.
6. Berkas `docs/walkthrough/TICKET-7-WALKTHROUGH.md` tersusun lengkap.
