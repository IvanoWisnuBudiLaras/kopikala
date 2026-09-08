# ☕ KopiKala Reservation & Order System

> **Enterprise-Grade Table Reservation & F&B Pre-Order Platform for Modern Coffee Shops**  
> *Proyek PKL (Praktek Kerja Lapangan) — Standar Produksi Enterprise 2026*

---

## 📌 Ringkasan Eksekutif & Visi Produk

**KopiKala** adalah platform web reservasi meja terpadu dengan pemesanan makanan dan minuman (*Food & Beverage Pre-Order*) untuk kedai kopi modern. Sistem dirancang secara presisi untuk:
- Mengeliminasi antrean fisik dan risiko penjualan ganda (*double-booking*) melalui **Optimistic Concurrency Control (OCC)** di database PostgreSQL.
- Memberikan fleksibilitas operasional kafe: durasi duduk dinamis (1–3 jam), walk-in offline tamu fisik, pesanan tambahan di tempat (*Add-On Order*), dan penukaran menu (*Item Substitution*).
- Menjunjung tinggi etika **Hospitality**: peringatan waktu habis dan peringatan no-show yang mewajibkan pengecekan jadwal master di sistem mendahului interaksi staf ke tamu.
- Menyediakan transparansi pembukuan kasir, pelacakan antrean dapur, dan tata kelola hak akses **Controlled Dynamic PBAC (Policy-Based Access Control)**.

---

## 🛠️ Pilihan Teknologi (Technology Stack)

| Komponen | Teknologi | Deskripsi |
|---|---|---|
| **Bahasa & Runtime** | C# 14 / .NET 10 (LTS) | Performa tinggi, native async/await, dan TimeProvider abstraction. |
| **Frontend Framework** | Blazor Web App (.NET 10) | Interactive Server Mode (Global), C# Full-Stack, SignalR real-time state. |
| **Komponen UI** | MudBlazor (v8.2.0) | Pustaka antarmuka Material Design 100% C# (bebas dependensi Node.js). |
| **Database Engine** | PostgreSQL 16 (Npgsql EF Core) | Open-source bebas biaya lisensi produksi selamanya dengan relasi OCC `xmin`. |
| **Otentikasi & OAuth 2.1** | Cookie Auth + Google OAuth + OpenIddict | Mendukung PKCE wajib (`/connect/authorize`, `/connect/token`). |
| **Otorisasi Hak Akses** | Controlled Dynamic PBAC | 5 Core Permissions (`Meja.Kelola`, `Pembayaran.Verifikasi`, `Dapur.Antrean`, `Laporan.Lihat`, `Sistem.Kelola`). |
| **Background Processing** | Tri-Tier Engine | In-memory queue (`System.Threading.Channels`), 30s scheduler, 00:00 midnight reconciliation. |
| **Testing Suite** | xUnit, Moq, Coverlet & Playwright | 128 tests (115 Unit/Integration + 13 E2E Live Headed Browser & Monkey Chaos). |
| **Kontainerisasi** | Docker & Docker Compose | Multi-stage build .NET 10 dan persistensi volume `kopikala_uploads`. |

---

## 🏢 Topologi 3 Portal Terpadu

1. **Area Customer (`/`)**:
   - Beranda publik (*Warm Coffee Aesthetic*), katalog 6 menu populer, dan galeri kafe.
   - **Wizard Pemesanan 4 Langkah (`/Booking`)**: Pemilih jadwal & durasi duduk -> Denah meja 2D -> Pre-Order F&B + Sticky Bottom Summary Bar -> Konfirmasi & Checkout.
   - **Nota Tagihan Invoice (`/Invoice/{id}`)**: Countdown timer 15 menit, opsi Bank BCA (tombol salin rekening & upload bukti bayar) atau Bayar di Tempat.
   - **Riwayat Pesanan (`/Customer/Orders`)**.
2. **Area Staf Operasional (`/Staff`)**:
   - **Denah Meja Kasir 2D**: Visualisasi status meja Indoor AC & Outdoor Smoking (Tersedia, Dikonfirmasi, Menunggu Verifikasi, Sedang Digunakan).
   - **Verifikasi Transfer**: Inspeksi foto bukti transfer bank pelanggan, tombol *Verifikasi Lunas* atau *Tolak*.
   - **Pencarian Cepat & Check-In**: Pencarian multi-kriteria (Nama Perwakilan, Kode Invoice, No Meja, WhatsApp).
   - **Tamu Walk-In Offline**: Pembukaan meja langsung di lantai kafe.
   - **Aksi Meja Aktif**: Tambah menu add-on, ganti item menu pre-order, dan perpanjang waktu duduk (+1 jam).
3. **Area SuperAdmin (`/SuperAdmin`)**:
   - **Mini Analytics Dashboard**: 4 KPI Cards omzet/okupansi, diagram donat okupansi meja, dan 5 menu terlaris.
   - **Manajemen Akun Staf**: Pembuatan akun staf dengan hash PBKDF2, penugasan role, dan reset password.
   - **Tata Kelola Dynamic PBAC**: Centang 5 izin operasional dan 4 tombol template 1-klik (*Kasir, Barista, Manager, SuperAdmin*).
   - **Master Data Kafe**: CRUD Meja Fisik & CRUD Menu F&B dengan upload foto tervalidasi magic bytes.

---

## 👥 Tabel Akun Demo Penguji Sidang PKL

| Peran | Email Akun | Password Default | Izin Akses PBAC | Portal Utama |
|---|---|---|---|---|
| **SuperAdmin** | `superadmin@kopikala.com` | `AdminKopi123!` | 5 Izin Lengkap (`Sistem.Kelola` bypass) | `/SuperAdmin` |
| **Manager** | `manager@kopikala.com` | `ManagerKopi123!` | `Meja.Kelola`, `Pembayaran.Verifikasi`, `Dapur.Antrean`, `Laporan.Lihat` | `/Staff` |
| **Kasir** | `kasir@kopikala.com` | `KasirKopi123!` | `Meja.Kelola`, `Pembayaran.Verifikasi` | `/Staff` |
| **Barista** | `barista@kopikala.com` | `BaristaKopi123!` | `Dapur.Antrean` | `/Staff` |
| **Customer** | `customer@gmail.com` | `CustKopi123!` / Google Login | Hak Akses Mandiri | `/` & `/Booking` |

---

## 🚀 Panduan Menjalankan Sistem (1-Klik Startup)

### Prasyarat Sistem:
- .NET 10 SDK
- Docker Desktop
- Visual Studio 2026 / VS Code

### 1. Menjalankan Database PostgreSQL via Docker:
```shell
docker compose up -d
```
*Container `kopikala-postgres` akan menyala di port `5432` dengan database `kopikala_db`.*

### 2. Menjalankan Aplikasi Web Blazor:
```shell
dotnet run --project KopiKala/KopiKala.csproj
```
Buka browser di `http://localhost:5000` atau URL yang tampil di konsol terminal.

### 3. Menjalankan Full Production Build via Docker Compose:
```shell
docker compose up -d --build
```
Aplikasi web berjalan di port `8080` dan database di port `5432`.

---

## 🧪 Eksekusi Pengujian (Testing Suite)

### A. Unit & Integration Tests (115 Tests):
```shell
dotnet test KopiKala.Tests/KopiKala.Tests.csproj
```

### B. Unit Test Code Coverage:
```shell
dotnet test KopiKala.Tests/KopiKala.Tests.csproj -p:CollectCoverage=true -p:Include="[KopiKala]KopiKala.Services.*"
```

### C. End-to-End (E2E) Live Headed Browser Playwright Tests (13 Tests):
```shell
dotnet test KopiKala.Tests.E2E/KopiKala.Tests.E2E.csproj
```

**Hasil Pengujian Terpadu**: **128 / 128 Tests Passed (100% Hijau)**.

---

## 🏛️ Struktur Folder Repositori

```text
KopiKala/
├── KopiKala/                      # Monolith Blazor Web App (.NET 10)
│   ├── Components/
│   │   ├── Layout/                # MainLayout.razor (Dynamic Navbar), NavMenu.razor
│   │   └── Pages/
│   │       ├── Customer/          # Booking.razor (MudStepper), Invoice.razor, Orders.razor
│   │       ├── Staff/             # Dashboard.razor (Denah Meja 2D, Verifikasi, Walk-In)
│   │       ├── Admin/             # Dashboard.razor (Analytics, PBAC, Master Data)
│   │       └── Account/           # Login.razor, Register.razor, AccessDenied.razor
│   ├── Controllers/               # AuthorizationController.cs (OpenIddict PKCE)
│   ├── Data/                      # AppDbContext.cs, DbInitializer.cs
│   ├── DTOs/                      # DTOs/Auth, DTOs/Booking, DTOs/Staff, DTOs/Admin
│   ├── Helpers/                   # PasswordHelper, CurrencyHelper, FileSecurityHelper, PBAC
│   ├── Models/                    # 10 Class Entities Database + DailyReport
│   ├── Services/                  # AuthService, BookingService, CashierService, SuperAdminService
│   ├── Workers/                   # Tri-Tier Background Engine (Queue, Maintenance, Reconciliation)
│   ├── wwwroot/                   # uploads/payments/, images/menu/, banners/
│   ├── Dockerfile
│   └── Program.cs
├── KopiKala.Tests/                # 115 Unit & Time Traveler Integration Tests
├── KopiKala.Tests.E2E/            # 13 Microsoft Playwright Live Browser Tests
├── docs/                          # PRD.md, DISCUSSION.md, walkthroughs, tickets T1-T8
└── docker-compose.yml
```

---

## 📜 Lisensi & Pengembang

Dikembangkan sebagai pemenuhan **Proyek Praktek Kerja Lapangan (PKL) 2026** berstandar arsitektur industri enterprise.
