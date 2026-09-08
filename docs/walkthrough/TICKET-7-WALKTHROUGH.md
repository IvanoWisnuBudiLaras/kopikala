# WALKTHROUGH TICKET-7: SuperAdmin Portal, Dynamic PBAC Governance, Master Data & Business Analytics Dashboard

**Status**: COMPLETED & FULLY VERIFIED (100% COVERAGE ON SUPERADMIN SERVICE)  
**Tanggal**: 2026-09-08  
**Assignee**: Developer (PKL)  

---

## 1. Ringkasan Eksekusi
Mengimplementasikan seluruh modul tata kelola dan observabilitas bisnis pemilik sistem (**SuperAdmin Portal** di rute terproteksi `/SuperAdmin` dengan policy `SuperAdminOnly`):
1. **Manajemen Akun Staf**:
   - Pembuatan akun staf baru dengan enkripsi kata sandi PBKDF2 (HMAC-SHA256, 100.000 iterasi).
   - Penugasan peran staf dinamis (Kasir, Barista, Manager, dll.).
   - Reset kata sandi staf secara mandiri oleh SuperAdmin.
2. **Tata Kelola Dynamic PBAC (Policy-Based Access Control)**:
   - Manajemen relasi Many-to-Many pada tabel pivot `role_permissions`.
   - Antarmuka centang 5 izin operasional inti (`Meja.Kelola`, `Pembayaran.Verifikasi`, `Dapur.Antrean`, `Laporan.Lihat`, `Sistem.Kelola`).
   - Tombol 1-klik template peran bawaan: *Template Kasir*, *Template Barista*, *Template Manager*, dan *Template SuperAdmin*.
   - Pembuatan peran kustom baru dan penghapusan peran kustom tanpa mengorbankan template bawaan.
3. **Pengelolaan Master Data Kafe**:
   - **Master Meja Fisik**: Penambahan meja, pengaturan kapasitas kursi (1–20 orang), pengelompokan area (*Indoor AC* vs *Outdoor Smoking*), serta saklar aktif/rusak.
   - **Master Katalog Menu F&B**: Penambahan menu baru, pengaturan kategori (*Coffee*, *Non-Coffee*, *Pastry & Bakery*, *Snacks*), penetapan harga rupiah & stok porsi, saklar ketersediaan (*IsAvailable*), dan upload foto menu tervalidasi magic bytes ke `wwwroot/images/menu/`.
4. **Mini Analytics Dashboard & Visualisasi Bisnis**:
   - 4 Kartu Metrik Angka Tebal: *Total Omzet Hari Ini* (Rp), *Okupansi Meja Real-Time* (% dan rasio terisi), *Total Reservasi Selesai*, dan *Total No-Show & Pembatalan*.
   - `MudChart` Tipe Donut: Rasio Okupansi Meja Real-time (Tersedia vs Terisi).
   - `MudChart` Tipe Bar / Tabel Metrik: 5 Menu Kopi & Makanan Terlaris berdasarkan kuantitas dan total akumulasi pendapatan.
5. **Pengujian Menyeluruh (Unit, Integration & Playwright E2E)**:
   - 115 Unit & Integration Tests di `KopiKala.Tests` lulus 100% (**100% Line Coverage pada `SuperAdminService`**).
   - 13 Playwright E2E Tests di `KopiKala.Tests.E2E` lulus 100% di browser Chromium live.

---

## 2. Rincian Arsitektur & Komponen

### A. DTOs Tata Kelola SuperAdmin (`DTOs/Admin/`)
- `CreateStaffRequestDto.cs`: Form input pembuatan akun staf baru (`FullName`, `Email`, `PhoneNumber`, `Password`, `RoleId`).
- `StaffOverviewDto.cs`: Kontrak data ringkasan akun staf beserta daftar izin aktifnya.
- `ManageRoleDto.cs`: Kontrak data editor peran dan daftar `SelectedPermissionIds`.
- `RoleDto.cs` & `PermissionDto.cs`: Kontrak data pembacaan relasi peran dan izin.
- `ManageTableDto.cs`: Form input CRUD master meja fisik.
- `ManageMenuItemDto.cs`: Form input CRUD master katalog menu F&B.
- `AnalyticsDashboardDto.cs`: Kontrak data KPI analitik omzet, rasio okupansi meja, dan 5 menu terlaris.

### B. Business Service Layer (`Services/ISuperAdminService.cs` & `SuperAdminService.cs`)
- `GetStaffListAsync()`: Mengambil seluruh user bertaraf staf (non-customer) beserta perannya.
- `CreateStaffAsync(dto)`: Validasi keunikan email, hashing password PBKDF2, dan penugasan role.
- `ResetStaffPasswordAsync(userId, temporaryPassword)`: Reset kredensial akun staf.
- `GetRolesWithPermissionsAsync()` & `GetAllPermissionsAsync()`: Membaca seluruh data master peran dan izin PBAC.
- `SaveRolePermissionsAsync(dto)`: Menyimpan/memperbarui peran dan sinkronisasi Many-to-Many pada `role_permissions`.
- `DeleteRoleAsync(roleId)`: Validasi anti-hapus pada template bawaan sistem dan peran yang sedang terpakai.
- `GetTablesAsync()`, `SaveTableAsync(dto)`, `DeleteTableAsync(tableId)`: Manajemen master meja fisik (dukungan soft delete jika meja memiliki riwayat booking).
- `GetMenuItemsAsync()`, `SaveMenuItemAsync(dto)`, `UploadMenuImageAsync(menuItemId, stream, fileName)`, `DeleteMenuItemAsync(menuItemId)`: Manajemen master katalog menu dan upload foto aman.
- `GetAnalyticsDashboardDataAsync()`: Kompilasi data transaksi hari ini dan integrasi data rekapan malam dari tabel `daily_reports`.

### C. Antarmuka SuperAdmin MudBlazor (`Components/Pages/Admin/Dashboard.razor`)
- Tab 1: **Analitik & KPI Bisnis**: 4 Kartu KPI omzet/okupansi, diagram donat okupansi meja, dan tabel 5 menu terlaris.
- Tab 2: **Akun Staf & Dynamic PBAC**: Tabel daftar staf, modal dialog Tambah Staf & Reset Password, dan panel editor Dynamic PBAC dengan 1-klik template.
- Tab 3: **Master Data Kafe**: Sub-panel Master Meja Fisik dan Sub-panel Master Menu F&B dengan modal dialog interaktif.

---

## 3. Hasil Pengujian (Test Results)

### A. Unit & Integration Tests (`KopiKala.Tests`)
```shell
dotnet test KopiKala.Tests/KopiKala.Tests.csproj -p:CollectCoverage=true -p:Include="[KopiKala]KopiKala.Services.SuperAdminService"
```

**Hasil**:
- **Total Tests**: 115 Passed, 0 Failed, 0 Skipped (100% Success Rate)
- **Cakupan SuperAdminService**: **100% Line Coverage**, **100% Method Coverage**, **100% Branch Coverage**

```text
+----------+------+--------+--------+
| Module   | Line | Branch | Method |
+----------+------+--------+--------+
| KopiKala | 100% | 100%   | 100%   |
+----------+------+--------+--------+
```

### B. End-to-End (E2E) Browser Tests via Microsoft Playwright
```shell
dotnet test KopiKala.Tests.E2E/KopiKala.Tests.E2E.csproj --filter "FullyQualifiedName~SuperAdminGovernanceE2ETests"
```

**Hasil E2E**:
1. `Scenario1_SuperAdminPortal_LoginAndVerifyAnalyticsDashboard` (Passed): Login SuperAdmin, verifikasi KPI cards (Omzet, Okupansi), dan render MudChart.
2. `Scenario2_SuperAdmin_DynamicPBAC_GovernanceAndTemplates` (Passed): Navigasi ke tab Akun Staf & PBAC, verifikasi tombol template 1-klik, dan checklist izin.
3. `Scenario3_SuperAdmin_MasterData_TablesAndMenu` (Passed): Navigasi ke tab Master Data, verifikasi tabel Meja Fisik dan tabel Menu F&B.
4. `Scenario4_SuperAdmin_MonkeyTesting_TabResilience` (Passed): Stres uji perpindahan tab cepat bertubi-tubi tanpa gangguan SignalR circuit.

### C. Total Keseluruhan Solusi
- **KopiKala.Tests.dll**: 115 Passed
- **KopiKala.Tests.E2E.dll**: 13 Passed
- **Total**: **128/128 Tests PASSED (100% Success Rate)**
