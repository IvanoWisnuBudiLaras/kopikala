# TICKET-1: Initial Project Scaffolding, Package Installation, Docker PostgreSQL & DB Connection

| Metadata | Details |
| :---- | :---- |
| **Ticket ID** | `TICKET-1` |
| **Project** | KopiKala Reservation & Order System |
| **Status** | `OPEN / READY FOR IMPLEMENTATION` |
| **Priority** | High / Blocker |
| **Target Framework** | C\# .NET 10 |
| **Dependencies** | MudBlazor, Npgsql PostgreSQL, Docker, Docker Compose |
| **Assignee** | Developer (PKL) |

---

## 1\. Objective & Description

Tiket ini adalah instruksi kerja tahap pertama untuk membangun fondasi fisik proyek **KopiKala**:

1. Menyiapkan struktur folder kerja Monolith yang bersih di Visual Studio 2026\.  
2. Menginstal paket dependensi NuGet resmi (MudBlazor dan Npgsql PostgreSQL).  
3. Membuat konfigurasi `docker-compose.yml` untuk menjalankan mesin database PostgreSQL di dalam Docker container lokal.  
4. Mengonfigurasi connection string di `appsettings.json` dan mendaftarkan DbContext serta MudBlazor di `Program.cs`.  
5. Memverifikasi koneksi database dan menjalankan migrasi awal.

---

## 2\. Rincian Langkah Kerja (Step-by-Step Tasks)

### Task 1: Pembuatan Struktur Folder Proyek

Pastikan proyek `KopiKala` (Blazor Web App .NET 10\) memiliki struktur folder sebagai berikut di panel *Solution Explorer*:

KopiKala/

├── Components/

│   ├── Layout/                \# MainLayout.razor, NavMenu.razor

│   ├── Pages/

│   │   ├── Customer/          \# Booking, Pre-order F\&B, Invoice

│   │   ├── Staff/             \# Denah Meja Kasir, Antrean Barista (KDS)

│   │   └── Admin/             \# Manajemen Akun & Dynamic PBAC

│   └── \_Imports.razor         \# Global Razor namespaces

├── Data/                      \# AppDbContext.cs & Seeding logic

├── DTOs/                      \# Form request & response contracts

├── Helpers/                   \# Static extension methods (Rupiah, sanitasi file)

├── Models/                    \# 10 Class Entity Database

├── Services/                  \# Business logic interfaces & implementations

├── Workers/                   \# BackgroundService implementations

├── wwwroot/

│   └── uploads/               \# Folder penyimpanan bukti bayar

├── appsettings.json

├── docker-compose.yml

└── Program.cs

---

### Task 2: Instalasi Paket NuGet Resmi

Buka **Package Manager Console** di Visual Studio atau terminal proyek, lalu jalankan perintah instalasi paket berikut:

\# 1\. Pustaka Komponen UI Blazor (MudBlazor)

dotnet add package MudBlazor

\# 2\. Driver & Entity Framework Core Provider untuk PostgreSQL

dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL

\# 3\. Alat Migrasi Entity Framework Core Tools

dotnet add package Microsoft.EntityFrameworkCore.Tools

dotnet add package Microsoft.EntityFrameworkCore.Design  
\# 4\. Paket OpenIddict untuk Keamanan & OAuth2

dotnet add package OpenIddict.AspNetCore

dotnet add package OpenIddict.EntityFrameworkCore

---

### Task 3: Setup Docker (Dockerfile & Docker Compose)

#### Task 3.1: Pembuatan Dockerfile multi-stage build

Buat file bernama Dockerfile di root proyek untuk build .NET 10 Blazor Web App:

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base  
WORKDIR /app  
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build  
WORKDIR /src  
COPY \["KopiKala/KopiKala.csproj", "KopiKala/"\]  
RUN dotnet restore "KopiKala/KopiKala.csproj"  
COPY . .  
WORKDIR "/src/KopiKala"  
RUN dotnet build "KopiKala.csproj" \-c Release \-o /app/build

FROM build AS publish  
RUN dotnet publish "KopiKala.csproj" \-c Release \-o /app/publish

FROM base AS final  
WORKDIR /app  
COPY \--from=publish /app/publish .  
ENTRYPOINT \["dotnet", "KopiKala.dll"\]

#### Task 3.2: Update docker-compose.yml Unified

Perbarui docker-compose.yml untuk mencakup layanan database dan aplikasi:

version: '3.8'

services:

  kopikala-db:

    image: postgres:16-alpine

    container\_name: kopikala-postgres

    restart: always

    environment:

      POSTGRES\_USER: kopikala\_user

      POSTGRES\_PASSWORD: kopikala\_password123

      POSTGRES\_DB: kopikala\_db

    ports:

      \- "5432:5432"

    volumes:

      \- kopikala\_pgdata:/var/lib/postgresql/data

  kopikala-web:  
    build:  
      context: .  
      dockerfile: Dockerfile  
    ports:  
      \- "8080:8080"  
    environment:  
      \- ConnectionStrings\_\_DefaultConnection=Host=kopikala-db;Port=5432;Database=kopikala\_db;Username=kopikala\_user;Password=kopikala\_password123  
    depends\_on:  
      kopikala-db:  
        condition: service\_healthy  
    volumes:  
      \- kopikala\_uploads:/app/wwwroot/uploads

    healthcheck:

      test: \["CMD-SHELL", "pg\_isready \-U kopikala\_user \-d kopikala\_db"\]

      interval: 10s

      timeout: 5s

      retries: 5

volumes:

  kopikala\_pgdata:

    driver: local  
  kopikala\_uploads:  
    driver: local

*Catatan: Hal ini memastikan semua foto bukti pembayaran pelanggan yang tersimpan di wwwroot/uploads/payments/ tetap bertahan secara permanen meskipun container dijalankan ulang.*

#### Cara Menjalankan Docker:

Buka terminal di folder proyek dan jalankan:

docker compose up \-d

*Pastikan container `kopikala-postgres` berstatus `Up / Healthy`.*

---

### Task 4: Konfigurasi Koneksi Database di C\#

#### 1\. Atur `appsettings.json` / `appsettings.Development.json`

Tambahkan string koneksi PostgreSQL yang mengarah ke container Docker lokal:

{

  "ConnectionStrings": {

    "DefaultConnection": "Host=localhost;Port=5432;Database=kopikala\_db;Username=kopikala\_user;Password=kopikala\_password123"

  },

  "Logging": {

    "LogLevel": {

      "Default": "Information",

      "Microsoft.AspNetCore": "Warning"

    }

  },

  "AllowedHosts": "\*"

}

#### 2\. Buat File `Data/AppDbContext.cs`

Siapkan kerangka DbContext dasar yang mewarisi `DbContext`:

using Microsoft.EntityFrameworkCore;

namespace KopiKala.Data;

public class AppDbContext : DbContext

{

    public AppDbContext(DbContextOptions\<AppDbContext\> options) : base(options)

    {

    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)

    {

        base.OnModelCreating(modelBuilder);

        // Konfigurasi relasi dan tabel akan ditambahkan di TICKET-2

    }

}

#### 3\. Konfigurasi `Program.cs`

Daftarkan servis Npgsql PostgreSQL dan MudBlazor:

using KopiKala.Data;

using Microsoft.EntityFrameworkCore;

using MudBlazor.Services;

var builder \= WebApplication.CreateBuilder(args);

// 1\. Daftarkan Koneksi Database PostgreSQL Docker

builder.Services.AddDbContext\<AppDbContext\>(options \=\>

    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2\. Daftarkan Servis MudBlazor

builder.Services.AddMudServices();

// 3\. Daftarkan Komponen Blazor Server Global

builder.Services.AddRazorComponents()

    .AddInteractiveServerComponents();

var app \= builder.Build();

if (\!app.Environment.IsDevelopment())

{

    app.UseExceptionHandler("/Error", createScopeForErrors: true);

    app.UseHsts();

}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseAntiforgery();

app.MapRazorComponents\<KopiKala.Components.App\>()

    .AddInteractiveServerRenderMode();

app.Run();

#### 4\. Daftarkan MudBlazor di `_Imports.razor`

Buka file `Components/_Imports.razor` dan tambahkan baris impor berikut:

@using MudBlazor

@using MudBlazor.Services

#### 5\. Tambahkan Komponen MudBlazor di `App.razor`

Pastikan di file `Components/App.razor` sudah memuat file CSS dan JS bawaan MudBlazor:

* Di dalam `<head>`:  
  `<link href="_content/MudBlazor/MudBlazor.min.css" rel="stylesheet" />`  
* Di dalam `<body>` (paling bawah):  
  `<script src="_content/MudBlazor/MudBlazor.min.js"></script>`

Serta di `Components/Layout/MainLayout.razor`, tambahkan provider bawaan:

\<MudThemeProvider /\>

\<MudDialogProvider /\>

\<MudSnackbarProvider /\>

---

## 3\. Kriteria Keberhasilan (Acceptance Criteria)

Tiket ini dinyatakan selesai (*Done*) jika seluruh kondisi berikut terpenuhi:

1. Seluruh container (`kopikala-postgres` dan `kopikala-web`) berhasil dibangun dan berjalan normal via `docker compose up -d`.  
2. Seluruh paket NuGet (`MudBlazor`, `Npgsql.EntityFrameworkCore.PostgreSQL`, `Microsoft.EntityFrameworkCore.Tools`) terpasang tanpa bentrok versi.  
3. Struktur folder proyek telah dibuat lengkap sesuai Task 1\.  
4. Perintah migrasi awal Entity Framework berhasil dijalankan di terminal:  
     
   dotnet ef migrations add InitialDatabaseSetup  
     
   dotnet ef database update  
     
5. Saat proyek dijalankan (F5 di Visual Studio), halaman web terbuka di browser dengan tema MudBlazor yang menyala, tanpa ada pesan error koneksi database.

