# WALKTHROUGH TICKET-1: Initial Project Scaffolding, Package Installation, Docker PostgreSQL & DB Connection

**Status**: COMPLETED & FULLY VERIFIED  
**Tanggal**: 2026-09-06 (Diperbarui 2026-09-07)  
**Assignee**: Developer (PKL)  

---

## 1. Ringkasan Eksekusi
Membangun fondasi fisik proyek KopiKala berbasis Monolith Blazor Web App C# .NET 10 yang terhubung ke database PostgreSQL di dalam Docker container lokal:
1. Menyiapkan struktur folder kerja Monolith yang bersih di Visual Studio 2026.
2. Menginstal paket dependensi NuGet resmi (MudBlazor, Npgsql PostgreSQL, EF Core Tools/Design, OpenIddict).
3. Mengonfigurasi `docker-compose.yml` terpadu dengan image `postgres:16-alpine` dan service web app.
4. Mengonfigurasi connection string di `appsettings.json` (`kopikala_db`/`kopikala_user`).
5. Mendaftarkan DbContext serta MudBlazor di `Program.cs`.
6. Menjalankan migrasi awal EF Core `InitialDatabaseSetup` (10 tabel relasional + `__EFMigrationsHistory`).

---

## 2. Langkah-Langkah yang Telah Dikerjakan

1. **Setup Struktur Folder Proyek**:
   - `Components/Layout/`: `MainLayout.razor`, `NavMenu.razor`
   - `Components/Pages/`: Area Customer, Staff, Admin, Account
   - `Data/`: `AppDbContext.cs`, `DbInitializer.cs`
   - `DTOs/`: `DTOs/Auth/`, `DTOs/Booking/`
   - `Helpers/`: `PasswordHelper.cs`, `CurrencyHelper.cs`, Policy Providers
   - `Models/`: 10 Class Entity Database
   - `Services/`: `IAuthService.cs`, `AuthService.cs`
   - `Workers/`: Background Service Engine
   - `wwwroot/uploads/`: Folder penyimpanan bukti bayar (`uploads/payments/`)

2. **Instalasi Paket Dependensi NuGet Resmi**:
   - `MudBlazor` (v8.2.0) - Pustaka komponen antarmuka Material 100% C#.
   - `Npgsql.EntityFrameworkCore.PostgreSQL` (v10.0.0) - Driver database PostgreSQL.
   - `Microsoft.EntityFrameworkCore.Tools` & `Microsoft.EntityFrameworkCore.Design` (v10.0.11).
   - `Microsoft.AspNetCore.Authentication.Google` (v10.0.11).
   - `OpenIddict.AspNetCore` & `OpenIddict.EntityFrameworkCore` (v6.1.1).

3. **Setup Docker & Database PostgreSQL**:
   - `docker-compose.yml`: Service `kopikala-db` (`postgres:16-alpine`, container `kopikala-postgres`, port 5432, healthcheck `pg_isready -U kopikala_user -d kopikala_db`) dan service `kopikala-web` dengan volume `kopikala_uploads:/app/wwwroot/uploads` untuk persistensi foto bukti transfer pembayaran.
   - `Dockerfile`: Multi-stage build .NET 10 (base, build, publish, final) untuk deployment container Blazor Web App.

4. **Konfigurasi Database & Program.cs**:
   - `appsettings.json`: Connection string `Host=localhost;Port=5432;Database=kopikala_db;Username=kopikala_user;Password=kopikala_password123`.
   - `Program.cs`: Mendaftarkan `AddMudServices()`, `AddDbContext<AppDbContext>(options.UseNpgsql)`, `AddRazorComponents().AddInteractiveServerComponents()`.
   - `Components/_Imports.razor`: Mengimpor namespace MudBlazor global.
   - `Components/App.razor`: Memuat CSS, font Plus Jakarta Sans & Playfair Display, dan script JS MudBlazor.
   - `Components/Layout/MainLayout.razor`: Memasang `<MudThemeProvider>`, `<MudPopoverProvider>`, `<MudDialogProvider>`, dan `<MudSnackbarProvider>`.

5. **Migrasi Awal Entity Framework Core**:
   - Dijalankan: `dotnet ef migrations add InitialDatabaseSetup --context AppDbContext`
   - Diterapkan ke PostgreSQL: `dotnet ef database update --context AppDbContext`
   - Terbentuk 11 tabel (10 entity + `__EFMigrationsHistory`): `users`, `roles`, `permissions`, `user_roles`, `role_permissions`, `dining_tables`, `timeslots`, `menu_items`, `bookings`, `booking_details`.

6. **Verifikasi Runtime & Build**:
   - `dotnet build KopiKala.sln`: **0 Warning, 0 Error**.
   - Homepage `GET /`: **HTTP 200 OK**.

---

## 3. Hasil Akhir
Pondasi sistem, database PostgreSQL Docker container, dan koneksi EF Core siap 100% untuk pengembangan otentikasi akun dan Dynamic PBAC pada TICKET-2.
