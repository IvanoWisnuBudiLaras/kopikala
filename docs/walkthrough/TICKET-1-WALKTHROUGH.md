# WALKTHROUGH TICKET-1: Initial Project Scaffolding & Database Connection

**Status**: COMPLETED  
**Tanggal**: 2026-09-06  
**Assignee**: Developer (PKL)  

---

## 1. Ringkasan Eksekusi
Membangun pondasi fisik proyek KopiKala berbasis Monolith Blazor Web App C# .NET 10 yang terhubung ke database PostgreSQL di dalam Docker container lokal.

---

## 2. Langkah-Langkah yang Telah Dikerjakan

1. **Setup Docker Compose & PostgreSQL 17 Alpine**:
   - Dibuat file `docker-compose.yml` dengan service `kopikala-db` (Port: 5432).
   - Dibuat file `init.sql` berisi skema fisik 10 tabel relasional + ekstensi `pgcrypto` + seed data awal.
   - Container PostgreSQL berhasil dijalankan dan berstatus *Up / Healthy*.

2. **Pembersihan Konflik & Setup Solution**:
   - Dibuat file `KopiKala.sln` untuk mengikat proyek tunggal `KopiKala.csproj`.
   - File `docker-compose.override.yml` dikosongkan agar Visual Studio tidak mencoba menjalankan Blazor di container (aplikasi jalan native via dotnet CLI / F5).

3. **Instalasi Paket Dependensi NuGet**:
   - `MudBlazor` (v8.2.0) - Pustaka komponen antarmuka Material 100% C#.
   - `Npgsql.EntityFrameworkCore.PostgreSQL` (v10.0.0) - Driver database PostgreSQL.
   - `Microsoft.EntityFrameworkCore.Tools` & `Microsoft.EntityFrameworkCore.Design`.

4. **Scaffolding Database Otomatis (Reverse Engineering)**:
   - Dijalankan perintah resmi: `dotnet ef dbcontext scaffold` membaca skema `init.sql` di PostgreSQL.
   - Dihasilkan 8 class Model di folder `Models/` (`User`, `Role`, `Permission`, `DiningTable`, `Timeslot`, `MenuItem`, `Booking`, `BookingDetail`).
   - Dihasilkan `Data/AppDbContext.cs` lengkap dengan relasi Many-to-Many (`role_permissions`, `user_roles`) via `UsingEntity`.

5. **Konfigurasi Program.cs & MudBlazor UI Provider**:
   - Mendaftarkan `builder.Services.AddMudServices()`.
   - Mendaftarkan `builder.Services.AddDbContext<AppDbContext>(UseNpgsql)`.
   - Menambahkan `@using MudBlazor` di `Components/_Imports.razor`.
   - Memasang stylesheet MudBlazor, Google Roboto font, dan script JS di `Components/App.razor`.
   - Memasang `<MudThemeProvider />`, `<MudPopoverProvider />`, `<MudDialogProvider />`, dan `<MudSnackbarProvider />` di `Components/Layout/MainLayout.razor`.

6. **Verifikasi Build**:
   - Dijalankan `dotnet build KopiKala.sln`: **0 Warning, 0 Error**.

---

## 3. Hasil Akhir
Pondasi sistem siap untuk pengembangan modul otorisasi akun dan Dynamic PBAC pada TICKET-2.
