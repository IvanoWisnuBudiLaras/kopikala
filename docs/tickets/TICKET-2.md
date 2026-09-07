# TICKET-2: User Management, Dynamic PBAC, Authentication & Page Route Security

| Metadata | Details |
| :---- | :---- |
| **Ticket ID** | `TICKET-2` |
| **Project** | KopiKala Reservation & Order System |
| **Status** | `OPEN / READY FOR IMPLEMENTATION` |
| **Priority** | High |
| **Prerequisites** | `TICKET-1` completed (Docker PostgreSQL running, Base DbContext connected) **NuGet Packages**: Instal `Microsoft.AspNetCore.Authentication.Google`. *Architectural Note*: **Jangan** instal `Google.Apis` karena tidak diperlukan. |
| **Target Framework** | C\# .NET 10 |
| **Assignee** | Developer (PKL) |

---

## 1\. Objective & Description

Tiket ini bertujuan untuk membangun fondasi keamanan identitas, otentikasi, dan otorisasi hak akses bertingkat:

1. Membuat 5 class entitas database untuk modul akun dan **Dynamic PBAC (Policy-Based Access Control)**.  
2. Menyuntikkan data awal (*Seed Data*) untuk peran bawaan (SuperAdmin, Manager, Kasir, Barista, Customer) dan master izin akses (*Permissions*).  
3. Membangun **`IAuthService`** sebagai jembatan logika login, logout, auto-onboarding Google OAuth 2.1, dan ekstraksi klaim izin ke memori.  
4. Mendaftarkan aturan kebijakan otorisasi (*Authorization Policies*) di `Program.cs`.  
5. Mengamankan rute halaman Blazor menggunakan `<AuthorizeRouteView>` agar halaman `/Staff` dan `/SuperAdmin` terkunci rapat dari akses liar.  
6. Membangun antarmuka form Login MudBlazor dan halaman penolakan akses (*403 Access Denied*).

---

## 2\. Rincian Langkah Kerja (Step-by-Step Tasks)

### Task 1: Pembuatan 5 Class Model Database (Dynamic PBAC)

Buat file class model berikut di dalam folder **`Models/`**:

2. **`Models/ApplicationUser.cs`**: Mewarisi `IdentityUser<Guid>` dengan properti tambahan:  
   * `FullName` (string, max 100\)  
   * `GoogleId` (string?, nullable)  
   * `CreatedAt` (DateTime UTC)  
3. **`Models/Role.cs`**: Mewarisi `IdentityRole<Guid>` atau class mandiri:  
   * `Id` (int / Guid)  
   * `Name` (string)  
   * `Description` (string?)  
   * `IsTemplate` (bool, default false)  
4. **`Models/Permission.cs`**:  
   * `Id` (int, Primary Key)  
   * `Code` (string, misal: `'Booking.VerifyPayment'`)  
   * `GroupName` (string, misal: `'Reservasi'`)  
5. **`Models/RolePermission.cs`** (Tabel Pivot):  
   * `RoleId` (Foreign Key ke `Roles`)  
   * `PermissionId` (Foreign Key ke `Permissions`)  
6. **`Models/UserRole.cs`** (Tabel Pivot):  
   * `UserId` (Foreign Key ke `Users`)  
   * `RoleId` (Foreign Key ke `Roles`)

Perbarui file **`Data/AppDbContext.cs`** untuk mendaftarkan kelima entitas ini (`DbSet<T>`) dan mendefinisikan kunci gabungan (*composite primary key*) untuk tabel pivot pada method `OnModelCreating`. Pastikan menambahkan `options.UseOpenIddict()` agar EF Core membuat 4 tabel OpenIddict (Applications, Authorizations, Scopes, Tokens) bersama tabel Identity.

### Task 1.5: Pembuatan DTOs Autentikasi (Folder DTOs/Auth/)

Buat class kontrak data form di dalam folder **`DTOs/Auth/`**:

1. **`LoginRequestDto.cs`**:  
   * Email (string, required, email format)  
   * Password (string, required)  
   * RememberMe (bool, default false)  
2. **`RegisterRequestDto.cs`**:  
   * FullName (string, required)  
   * Email (string, required, email format)  
   * PhoneNumber (string, required, phone format)  
   * Password (string, required, min 6 chars)  
   * ConfirmPassword (string, compare to Password)  
3. **`UserSessionDto.cs`**:  
   * UserId (Guid)  
   * FullName (string)  
   * Email (string)  
   * Roles (List\<string\>)  
   * Permissions (List\<string\>)  
4. **`ForgotPasswordRequestDto.cs`**:  
   * Email (string, required, email format)  
5. **`ResetPasswordRequestDto.cs`**:  
   * Email (string, required, email format)  
   * Token (string, required)  
   * NewPassword (string, required, min 6 chars)  
   * ConfirmNewPassword (string, compare to NewPassword)

---

### Task 2: Data Seeding Awal (Master Izin & SuperAdmin)

Di dalam folder **`Data/`**, buat file **`DbInitializer.cs`** untuk menyuntikkan data saat pertama kali aplikasi dinyalakan:

* **Master Permissions**:  
  * Meja: `Meja.Kelola` (Grup: Meja \- Kelola meja & tamu walk-in)  
  * Kasir: Pembayaran.Verifikasi (Grup: Kasir \- Verifikasi transfer, check-in, tambah pesanan meja aktif)  
  * Dapur: Dapur.Antrean (Grup: Dapur \- Antrean KDS & saklar 'Tandai Menu Habis')  
  * Laporan: Laporan.Lihat (Grup: Laporan \- Rekapitulasi omzet dan analitik kafe)  
  * Sistem: Sistem.Kelola (Grup: Sistem \- Kelola akun staf & pengaturan peran PBAC)  
* **Master Roles (Template)**:  
  * `SuperAdmin`: Memiliki kelima izin.  
  * Manager: Meja.Kelola, Pembayaran.Verifikasi, Dapur.Antrean, Laporan.Lihat.  
  * Kasir: Meja.Kelola, Pembayaran.Verifikasi.  
  * Barista: Dapur.Antrean.  
  * Customer: Hak akses publik mandiri.  
* **Akun Default SuperAdmin**:  
  * Email: `superadmin@kopikala.com`  
  * Password default terenkripsi (misal: `AdminKopi123!`)

---

### Task 3: Pembuatan Service Otentikasi (`IAuthService`)

Buat kontrak dan implementasi di folder **`Services/`**:

* **`Services/IAuthService.cs`**:  
    
  public interface IAuthService  
    
  {  
      Task\<bool\> LoginAsync(LoginRequestDto dto);  
      Task\<bool\> RegisterCustomerAsync(RegisterRequestDto dto);  
      Task\<string\> GeneratePasswordResetTokenAsync(string email);  
      Task\<bool\> ResetPasswordAsync(ResetPasswordRequestDto dto);  
    
      Task LogoutAsync();  
    
      Task\<bool\> HandleGoogleCallbackAsync(string email, string name, string googleId);  
    
      Task\<List\<string\>\> GetUserPermissionsAsync(Guid userId);  
    
  }  
    
* **`Services/AuthService.cs`**:  
  * Menginjeksi `UserManager<ApplicationUser>`, `SignInManager<ApplicationUser>`, dan `AppDbContext`.  
  * Pada saat login berhasil: Ambil seluruh `Permission.Code` yang dimiliki oleh Role akun tersebut dari tabel `RolePermissions`, lalu sematkan ke dalam **Claims** sesi pengguna (`ClaimTypes.Role` dan custom claim `Permission`).  
  * Catat riwayat login staf ke log sistem (*Audit Log*).  
  * Implementasi reset password menggunakan `UserManager.GeneratePasswordResetTokenAsync` dan `UserManager.ResetPasswordAsync`.

---

### Task 4: Konfigurasi Policy Otorisasi di `Program.cs`

Daftarkan aturan keamanan rute berbasis kebijakan di file **`Program.cs`**:

// Controlled Dynamic Policy Provider pattern  
builder.Services.AddSingleton\<IAuthorizationPolicyProvider, PermissionPolicyProvider\>();  
builder.Services.AddScoped\<IAuthorizationHandler, PermissionAuthorizationHandler\>();  
builder.Services.AddAuthentication().AddGoogle(options \=\> {  
    options.ClientId \= builder.Configuration\["Authentication:Google:ClientId"\];  
    options.ClientSecret \= builder.Configuration\["Authentication:Google:ClientSecret"\];  
});  
builder.Services.AddAuthorization(options \=\>  
{  
    options.AddPolicy("SuperAdminOnly", policy \=\> policy.RequireClaim("Permission", "Sistem.Kelola"));  
    options.AddPolicy("StaffAccess", policy \=\> policy.RequireAssertion(context \=\>  
        context.User.HasClaim(c \=\> c.Type \== "Permission" &&   
        new\[\] { "Meja.Kelola", "Pembayaran.Verifikasi", "Dapur.Antrean", "Laporan.Lihat" }.Contains(c.Value))));  
builder.Services.AddOpenIddict().AddCore(options \=\> options.UseEntityFrameworkCore().UseDbContext\<AppDbContext\>()).AddServer(options \=\> { options.SetAuthorizationEndpointUris("/connect/authorize").SetTokenEndpointUris("/connect/token"); options.AllowAuthorizationCodeFlow().RequireProofKeyForCodeExchange(); options.AddDevelopmentEncryptionCertificate().AddDevelopmentSigningCertificate(); options.UseAspNetCore().EnableAuthorizationEndpointPassthrough().EnableTokenEndpointPassthrough(); }).AddValidation(options \=\> { options.UseLocalServer(); options.UseAspNetCore(); });

});

---

### Task 5: Pengamanan Rute Halaman Blazor & Halaman 403

1. **Perbarui `Components/Routes.razor`**: Bungkus router dengan komponen pengaman bawaan Blazor:  
     
   \<CascadingAuthenticationState\>  
     
       \<Router AppAssembly="@typeof(Program).Assembly"\>  
     
           \<Found Context="routeData"\>  
     
               \<AuthorizeRouteView RouteData="@routeData" DefaultLayout="@typeof(Layout.MainLayout)"\>  
     
                   \<NotAuthorized\>  
     
                       \<KopiKala.Components.Pages.Account.AccessDenied /\>  
     
                   \</NotAuthorized\>  
     
               \</AuthorizeRouteView\>  
     
               \<FocusOnNavigate RouteData="@routeData" Selector="h1" /\>  
     
           \</Found\>  
     
       \</Router\>  
     
   \</CascadingAuthenticationState\>  
     
2. **Buat Halaman 403 (`Components/Pages/Account/AccessDenied.razor`)**: Tampilan kartu MudBlazor ramah yang memberi tahu: *"Akses Ditolak: Anda tidak memiliki izin untuk membuka halaman ini"* disertai tombol kembali ke halaman utama.  
3. **Kunci Halaman dengan Atribut**:  
   * Di atas file halaman staf (`/Staff/...`): Tambahkan `@attribute [Authorize(Policy = "StaffAccess")]`.  
   * Di atas file halaman superadmin (`/SuperAdmin/...`): Tambahkan `@attribute [Authorize(Policy = "SuperAdminOnly")]`.

---

### Task 6: Tampilan Halaman Login MudBlazor (`Components/Pages/Account/Login.razor`)

Buat antarmuka login yang bersih dan nyaman:

* Kotak kartu `MudCard` di tengah layar bertema kopi hangat.  
* Input Email (`MudTextField`), Password (`MudTextField` mode password), dan Checkbox *"Ingat Saya"* (`MudCheckBox`).  
* Link teks *"Lupa Password?"* di bawah field password yang mengarah ke `/Account/ForgotPassword`.  
* Tombol utama `MudButton` *"Masuk"*.  
* Pemisah garis horizontal (*Divider*) dengan tulisan *"atau"*.  
* Tombol sekunder `MudButton` dengan logo Google: *"Masuk dengan Google"* (memicu rute `/Account/PerformExternalLogin?provider=Google`).

### Task 6.5: Halaman Lupa & Reset Password (MudBlazor)

**`Components/Pages/Account/ForgotPassword.razor`**: Form input email untuk meminta link/token reset.  
**`Components/Pages/Account/ResetPassword.razor`**: Form input password baru dan konfirmasi password.

---

## 3\. Kriteria Keberhasilan (Acceptance Criteria)

Tiket ini dinyatakan selesai (*Done*) jika seluruh kondisi berikut teruji:

1. Migrasi database berhasil dijalankan di terminal:  
     
   dotnet ef migrations add AddDynamicPBACAndAuth  
     
   dotnet ef database update  
     
   Tabel `roles`, `permissions`, `role_permissions`, dan data awal SuperAdmin terbentuk di PostgreSQL container Docker.  
     
2. Pengguna dapat login menggunakan akun `superadmin@kopikala.com`.  
3. Pengguna yang login sebagai SuperAdmin dapat membuka halaman `/SuperAdmin` dengan sukses.  
4. Pengguna biasa (Customer) atau pengunjung yang belum login yang mencoba mengetik URL `/Staff` atau `/SuperAdmin` **otomatis dicegat dan diarahkan ke halaman AccessDenied atau Login**.  
5. Tombol Logout berfungsi menghapus sesi Cookie dan mengembalikan pengguna ke halaman publik.  
6. Alur Google OAuth 2.1 teruji: Mengklik tombol login Google menginisiasi *OAuth challenge* dan mengarahkan pengguna ke *Google sign-in endpoint*.  
7. Verifikasi endpoint OpenIddict (`/connect/authorize` dan `/connect/token`) dapat diakses dan mewajibkan penggunaan PKCE (Proof Key for Code Exchange).  
8. Seluruh pengujian E2E Playwright pada modul login, form lupa password, dan pencegatan rute 403 berjalan sukses (Passed / Hijau) di browser Chromium live.

---

## 4\. Standar Pengujian Wajib (Testing Specifications)

### A. Unit Test Coverage (Target Persentase)

* Target Coverage: Minimal 80% Line Coverage pada Services/Auth dan DTOs/Auth.  
* Pustaka: xUnit, Moq, dan Coverlet (coverlet.collector).  
* Pelaporan: Menghasilkan persentase cakupan kode melalui perintah: `dotnet test --collect:"XPlat Code Coverage"` dan menghasilkan laporan ringkasan persentase (Line & Branch Coverage).

### B. API & Integration Test (Logging Detail & Time Traveler)

* Pustaka: `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory`), `Microsoft.Extensions.TimeProvider.Testing` (`FakeTimeProvider`), dan `ITestOutputHelper`.  
* Mekanisme Time Traveler (TimeProvider): Menggunakan `FakeTimeProvider` bawaan .NET 10 untuk memajukan waktu (*Advance time*) secara instan dalam pengujian:  
  * Menguji masa berlaku token login dan token reset password (maju 15-30 menit menggunakan `FakeTimeProvider` untuk memverifikasi token menjadi tidak valid tanpa menunggu waktu nyata).  
  * Menguji penguncian akun (lockout).  
* Standar Logging: Setiap pengujian endpoint wajib mencatat status HTTP, durasi eksekusi (ms), dan payload response secara terstruktur pada log pengujian.

### C. End-to-End (E2E) Browser Test via Microsoft Playwright

* **Mode Eksekusi**: Live Headed Browser (Headless \= false dengan slowMo 50-100ms) sehingga pengembang dapat menyaksikan langsung robot menguji form login dan pencegatan rute.  
* **Skenario 1 (Uji Form Login & Validasi)**:  
  * Robot membuka `/Account/Login`.  
  * Mengetik email salah/password salah \-\> memverifikasi munculnya notifikasi validasi error MudBlazor.  
  * Mengetik kredensial SuperAdmin valid (`superadmin@kopikala.com`) \-\> memverifikasi login berhasil.  
* **Skenario 2 (Uji Pencegatan Akses Ilegal / Route Tampering)**:  
  * Robot membuka browser baru tanpa login (unauthenticated).  
  * Mencoba langsung mengetik URL terlarang: `/Staff` atau `/SuperAdmin`.  
  * Memverifikasi secara visual bahwa sistem seketika mencegat akses dan mengarahkan robot ke halaman `/Account/AccessDenied` (403) atau `/Account/Login`.  
* **Skenario 3 (Uji Alur Lupa Password)**:  
  * Robot membuka `/Account/ForgotPassword`, mengisi email, menekan submit, dan memverifikasi pesan notifikasi sukses.  
* **Skenario 4 (Monkey Testing Form Login)**:  
  * Menjalankan pengetikan cepat string acak dan klik tombol submit bertubi-tubi untuk memastikan form tidak hang atau melempar unhandled exception.

