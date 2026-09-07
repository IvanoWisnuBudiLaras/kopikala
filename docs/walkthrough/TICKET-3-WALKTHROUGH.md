# WALKTHROUGH TICKET-3: Landing Page, Dynamic Role-Based Navbar, Visual Assets & Playwright E2E Testing

**Status**: COMPLETED & FULLY VERIFIED (E2E PLAYWRIGHT + MONKEY TESTING)  
**Tanggal**: 2026-09-07  
**Assignee**: Developer (PKL)  

---

## 1. Ringkasan Eksekusi
Mengimplementasikan gerbang utama aplikasi KopiKala:
1. **Landing Page Publik**: Bertema *Modern Classic Warm Coffee* dengan tipografi Playfair Display & Plus Jakarta Sans, Hero Banner dengan dark gradient overlay, 3 Feature Cards, Galeri 6 Menu Populer, Galeri Suasana Kafe, dan CTA Bottom Banner.
2. **Dynamic Role-Based Navbar**: Menggunakan `<AuthorizeView>` dan policy PBAC untuk merender tombol secara reaktif sesuai peran (Customer, Staff/Kasir/Barista/Manager, SuperAdmin, dan Guest).
3. **Fitur Lupa & Reset Password**: DTOs, AuthService token generator (15 menit masa berlaku), halaman `/Account/ForgotPassword` dan `/Account/ResetPassword`.
4. **OpenIddict OAuth 2.1 Server + PKCE**: Dedicated `AuthorizationController` untuk endpoint `/connect/authorize`, `/connect/token`, dan `/connect/userinfo`, serta migrasi 4 tabel OpenIddict di PostgreSQL.
5. **Aset Visual Lengkap**: 10 gambar proporsional di `wwwroot/images/` yang terverifikasi tampil tanpa broken images (HTTP 200).
6. **Rangkaian Pengujian Lengkap**: 42 Unit/Integration Tests (`KopiKala.Tests`) + 4 E2E/Monkey Chaos Tests Playwright (`KopiKala.Tests.E2E`), total **46/46 tests Passed**.

---

## 2. Rincian Arsitektur & Komponen

### A. Landing Page Publik (`Components/Pages/Home.razor`)
- **Hero Banner**: Judul *"Nikmati Kopi Terbaik Tanpa Antre Meja"* berlatar `hero-indoor.jpg` dengan gradien overlay gelap, sub-judul konsep kafe, dan tombol CTA adaptif.
- **Section Fitur Singkat Kafe**: 3 kartu fitur (*Pilih Meja & Durasi*, *Pre-Order F&B*, *Pembayaran Fleksibel*).
- **Galeri Menu Populer**: 6 kartu menu unggulan (*Kopi Susu, Americano, Latte, Matcha Latte, Croissant, French Fries*).
- **Galeri Suasana & Barista**: Menampilkan `hero-outdoor.jpg` dan `barista-working.jpg`.
- **CTA Bottom Banner**: Banner penutup responsif.

### B. Dynamic Role-Based Navbar (`Components/Layout/MainLayout.razor`)
- **Guest / Tamu (NotAuthorized)**: Tombol *"Beranda"*, Toggle Dark/Light Mode, tombol *"Masuk"*, dan *"Daftar"*.
- **Customer (Policy: CustomerOnly)**: Tombol *"Pesan Meja"* (`/Booking`), *"Pesanan Saya"* (`/Customer/Orders`), dan User Profile Menu.
- **Staff (Policy: StaffAccess)**: Tombol oranye *"Portal Staf"* (`/Staff`).
- **SuperAdmin (Policy: SuperAdminOnly)**: Tombol merah *"SuperAdmin"* (`/SuperAdmin`).
- **Toggle Tema Reaktif**: Beralih seketika antara Light Ivory Canvas (`#FAF9F6`) dan Dark Espresso Surface (`#201C1A`).

### C. Fitur Lupa & Reset Password
- **DTOs (`DTOs/Auth/`)**: `ForgotPasswordRequestDto.cs` dan `ResetPasswordRequestDto.cs`.
- **`AuthService.cs`**:
  - `GeneratePasswordResetTokenAsync(email)`: Menghasilkan token URL-safe Base64 (valid 15 menit).
  - `ResetPasswordAsync(email, token, newPassword)`: Validasi masa aktif token dan hashing PBKDF2 salt 100.000 iterasi.
- **Halaman Razor**: `/Account/ForgotPassword` dan `/Account/ResetPassword`.

### D. OpenIddict OAuth 2.1 Server & Controller
- **`Controllers/AuthorizationController.cs`**:
  - Endpoint `/connect/authorize`: Menangani *Authorization Code Flow* + PKCE (`code_challenge` / `S256`) dan validasi sesi cookie.
  - Endpoint `/connect/token`: Penukaran kode otorisasi menjadi Access Token, ID Token, dan Refresh Token.
  - Endpoint `/connect/userinfo`: Pengambilan profil klaim user via Bearer token.
- **Migrasi Database (`20260907085410_AddOpenIddictTables`)**: Mendaftarkan 4 tabel OpenIddict (`OpenIddictApplications`, `OpenIddictAuthorizations`, `OpenIddictScopes`, `OpenIddictTokens`) di database PostgreSQL `kopikala_db`.

### E. Pengujian E2E Microsoft Playwright (`KopiKala.Tests.E2E`)
- **`Infrastructure/KopiKalaServerFixture.cs`**: Menjalankan web host KopiKala secara otomatis pada port TCP bebas dinamis dan mengontrol browser Chromium (mendukung mode *Headed* maupun *Headless*).
- **Skenario 1 (Tampilan Publik & Integritas Gambar)**: Memvalidasi teks hero, ketersediaan tombol, dan memastikan semua elemen gambar memiliki `naturalWidth > 0`.
- **Skenario 2 (Alur Navigasi Lengkap & Dynamic Navbar)**: Menguji login SuperAdmin dan Staff Kasir, memeriksa kemunculan tombol navigasi dinamis, dan memverifikasi alur logout.
- **Skenario 3 (Monkey Testing / Chaos UI Resilience Testing)**: Menjalankan 25 simulasi klik dan interaksi acak berkecepatan tinggi untuk membuktikan kestabilan sirkuit Blazor Server SignalR.

---

## 3. Hasil Pengujian (Test Results)

### A. Unit & Integration Tests (`KopiKala.Tests`)
```text
Passed!  - Failed: 0, Passed: 42, Skipped: 0, Total: 42, Duration: 5 s - KopiKala.Tests.dll
Coverage: AuthService.cs (100% Line), PasswordHelper.cs (100% Line)
```

### B. Microsoft Playwright E2E & Chaos Tests (`KopiKala.Tests.E2E`)
```text
Passed!  - Failed: 0, Passed: 4, Skipped: 0, Total: 4, Duration: 7 s - KopiKala.Tests.E2E.dll
- Scenario1_PublicLandingPage_RendersProperlyWithoutBrokenImages: PASSED
- Scenario2_FullUserJourney_LoginSuperAdmin_DynamicNavbar_Logout: PASSED
- Scenario2_FullUserJourney_LoginStaffKasir_DynamicNavbar: PASSED
- Scenario3_MonkeyChaosTesting_RapidRandomClicks_ResilienceVerified: PASSED
```

### C. Total Keseluruhan
- **Total Tests**: **46/46 PASSED** (0 failed).
- **Solusi Visual Studio 2026 (`KopiKala.slnx`)**: Terintegrasi penuh dan lolos `dotnet build KopiKala.sln` (0 error).
