# WALKTHROUGH TICKET-2: User Management, Dynamic PBAC, OpenIddict OAuth 2.1 Server & Page Route Security

**Status**: COMPLETED & FULLY VERIFIED (100% AUTH COVERAGE)  
**Tanggal**: 2026-09-06 (Diperbarui 2026-09-07)  
**Assignee**: Developer (PKL)  

---

## 1. Ringkasan Eksekusi
Mengimplementasikan modul identitas, otentikasi, otorisasi hak akses granular (**Dynamic PBAC**), serta server otorisasi internal **OpenIddict OAuth 2.1 (PKCE)**:
- **Routing & State**: Berbasis komponen Razor (`@page`) dengan layout adaptif `<AuthorizeRouteView>`.
- **Autentikasi**: Cookie Authentication + Google OAuth 2.1 dengan auto-onboarding pelanggan baru.
- **Dynamic PBAC**: 5 controlled core permissions (`Meja.Kelola`, `Pembayaran.Verifikasi`, `Dapur.Antrean`, `Laporan.Lihat`, `Sistem.Kelola`) yang dievaluasi dinamis via `PermissionPolicyProvider` dan `PermissionAuthorizationHandler`.
- **OpenIddict OAuth 2.1 Server**: Mendukung *Authorization Code Flow + PKCE wajib* (`/connect/authorize`, `/connect/token`, `/connect/userinfo`).
- **Fitur Lupa & Reset Password**: Validasi DTOs, token 15 menit, dan hashing PBKDF2.
- **Pengujian**: 42 Unit & Integration Tests (xUnit + Moq + FakeTimeProvider) dengan Line Coverage `AuthService.cs` mencapai **100%**.

---

## 2. Rincian Arsitektur & Implementasi

1. **Password Hasher & Kriptografi Helper (`Helpers/PasswordHelper.cs`)**:
   - Algoritma `PBKDF2` (HMAC-SHA256, 100.000 iterasi, 128-bit cryptographic random salt).
   - Verifikasi aman anti-timing attack menggunakan `CryptographicOperations.FixedTimeEquals`.

2. **Controlled Dynamic PBAC Provider Pattern**:
   - `Helpers/PermissionRequirement.cs`: Menyimpan kode izin.
   - `Helpers/PermissionAuthorizationHandler.cs`: Evaluasi claims `Permission` & bypass otomatis SuperAdmin (`Sistem.Kelola`).
   - `Helpers/PermissionPolicyProvider.cs`: Resolusi asynchronous policy dinamis tanpa deadlocks (`await GetPolicyAsync`).

3. **DTOs Autentikasi (`DTOs/Auth/`)**:
   - `LoginRequestDto.cs`: Email, Password, RememberMe.
   - `RegisterRequestDto.cs`: FullName, Email, PhoneNumber, Password, ConfirmPassword.
   - `ForgotPasswordRequestDto.cs`: Email.
   - `ResetPasswordRequestDto.cs`: Email, Token, NewPassword, ConfirmNewPassword.
   - `UserSessionDto.cs`: UserId, FullName, Email, Roles, Permissions.

4. **Layanan Bisnis Otentikasi (`Services/IAuthService.cs` & `Services/AuthService.cs`)**:
   - `LoginAsync(LoginRequestDto)`: Validasi kredensial, ekstraksi izin role dari database ke Claims principal.
   - `RegisterAndLoginAsync(RegisterRequestDto)`: Registrasi customer mandiri + assign role Customer.
   - `HandleGoogleCallbackAsync()`: Auto-onboarding Google OAuth 2.1 (JIT user creation).
   - `GeneratePasswordResetTokenAsync(email)`: Token URL-safe Base64 bertanda waktu 15 menit.
   - `ResetPasswordAsync(email, token, newPassword)`: Validasi masa aktif token dan perbarui password hash.
   - `GetUserPermissionsAsync()` & `GetUserSessionAsync()`.

5. **OpenIddict OAuth 2.1 Server & Controller (`Controllers/AuthorizationController.cs`)**:
   - `modelBuilder.UseOpenIddict()` mendaftarkan 4 tabel resmi OpenIddict.
   - Migrasi `20260907085410_AddOpenIddictTables` diterapkan ke PostgreSQL container.
   - Endpoint `/connect/authorize`: Menangani otorisasi code flow dengan **PKCE wajib** (`code_challenge` / `S256`).
   - Endpoint `/connect/token`: Penukaran authorization code menjadi Access Token, ID Token, dan Refresh Token.
   - Endpoint `/connect/userinfo`: Pengembalian klaim pengguna terautentikasi.

6. **Data Seeding Awal (`Data/DbInitializer.cs`)**:
   - 5 Core Permissions & 5 Role Templates (`SuperAdmin`, `Manager`, `Kasir`, `Barista`, `Customer`).
   - Akun demo bawaan: `superadmin@kopikala.com` (`AdminKopi123!`), `kasir@kopikala.com` (`KasirKopi123!`), `barista@kopikala.com` (`BaristaKopi123!`).
   - Seed client OAuth 2.1 `kopikala-client` dengan izin `AuthorizationCode`, `RefreshToken`, dan aturan wajib PKCE.

7. **Pengamanan Rute Halaman & Form MudBlazor**:
   - `Components/Routes.razor` dilindungi `<AuthorizeRouteView>`.
   - `Components/Pages/Account/AccessDenied.razor` (403 Access Denied).
   - `Components/Pages/Account/Login.razor`, `Register.razor`, `ForgotPassword.razor`, `ResetPassword.razor`.
   - `/Staff` dilindungi `[Authorize(Policy = "StaffAccess")]`.
   - `/SuperAdmin` dilindungi `[Authorize(Policy = "SuperAdminOnly")]`.

---

## 3. Hasil Pengujian (xUnit Test Suite)

```text
Passed!  - Failed: 0, Passed: 42, Skipped: 0, Total: 42, Duration: 5 s - KopiKala.Tests.dll
Coverage: AuthService.cs (100% Line, 27/27 Lines), PasswordHelper.cs (100% Line, 25/25 Lines)
```

- **OpenIddict Integration Tests (`OpenIddictOAuthServerTests.cs`)**:
  - `AuthorizeEndpoint_WithoutPkce_ReturnsErrorOrRejection`: ✅ Passed
  - `AuthorizeEndpoint_WithPkce_RedirectsToLoginWhenUnauthenticated`: ✅ Passed
  - `TokenEndpoint_InvalidOrMissingGrant_ReturnsBadRequest`: ✅ Passed
- **Time Traveler Test (`TimeTravelerAuthTests.cs`)**:
  - Transient session (15-30m), Persistent Remember Me (30 hari), dan Token Reset (15m): ✅ Passed
- **Build Status**: **0 Warning, 0 Error**.
- **Test Status**: **100% Passed**.
