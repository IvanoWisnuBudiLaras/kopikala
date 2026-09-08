# TICKET-8: End-to-End System Integration, .NET Aspire Observability & PKL Defense Demonstration Suite

| Metadata | Details |
| :--- | :--- |
| **Ticket ID** | `TICKET-8` |
| **Project** | KopiKala Reservation & Order System |
| **Status** | `COMPLETED` |
| **Priority** | Critical / Final Delivery Milestone |
| **Prerequisites** | `TICKET-1` s/d `TICKET-7` completed |
| **Target Framework** | C# .NET 10 |
| **Assignee** | Developer (PKL) |

---

## 1. Objective & Description

Tiket ini adalah **puncak penyelesaian seluruh proyek KopiKala**, bertugas menyatukan seluruh modul menjadi satu kesatuan sistem komersial yang siap dideploy ke server nyata dan siap dipresentasikan di depan dosen/penguji sidang PKL:

1. **Integrasi Hulu-ke-Hilir (*End-to-End Integration*)**: Menghubungkan seluruh 3 portal: Customer (`/`), Staff (`/Staff`), dan SuperAdmin (`/SuperAdmin`) dalam satu rantai transaksi utuh.
2. **Observabilitas Visual & Health Check**: Mengonfigurasi endpoint kesehatan `/healthz` dan pemantauan server visual (*Traces, Metrics, Logs*) saat demonstrasi sidang.
3. **Validasi 7 Standar Mutlak Kesiapan Produksi**: Memverifikasi Docker multi-stage build, docker-compose, pengujian solusi penuh (128 tests passed, > 80% coverage), 4 peran utama, 5-pilar security suite, reporting bisnis, dan zero-downtime E2E flow.
4. **Dokumentasi Repositori Resmi (`README.md`)**: Panduan instalasi 1-klik, prasyarat, kredensial akun demo penguji, dan arsitektur sistem.
5. **Naskah Skenario Demo Sidang PKL (7 Skenario Demonstrasi)**: Panduan langkah demi langkah saat presentasi di depan dewan penguji.

---

## 2. Rincian Langkah Kerja (Step-by-Step Tasks)

### Task 1: Konfigurasi Observabilitas Visual & Health Endpoints
1. Mengonfigurasi `builder.Services.AddHealthChecks()` dan `app.MapHealthChecks("/healthz")` di `Program.cs`.
2. Verifikasi pemantauan container, latensi kueri EF Core, dan log background worker.

### Task 2: Verifikasi Kontainerisasi Penuh (Docker Production Test)
1. `Dockerfile` multi-stage build .NET 10 (base, build, publish, final) dan `docker-compose.yml` terintegrasi.
2. Memverifikasi volume `kopikala_uploads` mempertahankan foto bukti transfer secara permanen.

### Task 3: Penyusunan Panduan Repositori (`README.md`)
1. Deskripsi proyek KopiKala dan nilai bisnisnya.
2. Prasyarat sistem: .NET 10 SDK, Docker Desktop, Visual Studio 2026.
3. Panduan jalankan 1-klik (`docker compose up -d` & `dotnet run`).
4. Tabel kredensial demo lengkap (SuperAdmin, Manager, Kasir, Barista, Customer).

### Task 4: Skenario Demo Sidang PKL (Naskah Pengujian 7 Langkah)
- Skenario 1 (Pemesanan Pelanggan): Wizard 4-langkah, kalkulasi sticky bar, nota invoice dengan countdown 15m.
- Skenario 2 (Verifikasi Kasir & Check-In): Inspeksi foto bukti transfer, verifikasi lunas, check-in tamu ke meja.
- Skenario 3 (Fleksibilitas Kasir): Ganti menu (*item substitution*) & pesanan tambahan (*add-on order*).
- Skenario 4 (Tamu Walk-In): Pembukaan meja langsung di lantai kafe secara offline.
- Skenario 5 (Etika Hospitality): Peringatan waktu habis & pengecekan master schedule sebelum interaksi fisik.
- Skenario 6 (Tata Kelola Dynamic PBAC): Pembuatan peran kustom & centang 5 izin operasional secara live.
- Skenario 7 (Mini Analytics & Otomasi Cron): Demonstrasi omzet, okupansi, dan rekonsiliasi harian `daily_reports`.

---

## 3. Standar Pengujian Regresi Terpadu (Regression Testing Suite)

### A. Full Solution Unit & Integration Test Coverage
```shell
dotnet test KopiKala.Tests/KopiKala.Tests.csproj
```
- **115 Tests Passed**, 0 Failed.
- **100% Line Coverage** pada `AuthService`, `BookingService`, `CashierService`, `SuperAdminService`, dan `BookingMaintenanceWorker`.

### B. End-to-End Master Journey Playwright Test
```shell
dotnet test KopiKala.Tests.E2E/KopiKala.Tests.E2E.csproj
```
- **13 Tests Passed**, 0 Failed (100% Success Rate).

**Total Pengujian Solusi**: **128 / 128 TESTS PASSED**.

---

## 4. Kriteria Keberhasilan (Acceptance Criteria)

1. Seluruh 3 portal web berfungsi 100% tanpa tautan rusak atau galat konsol browser.
2. Perintah `docker compose up -d` menyalakan web dan database secara mandiri.
3. Health check `/healthz` dan container status siap beroperasi.
4. Laporan Coverlet membuktikan solusi terlindungi oleh cakupan pengujian unit minimal 80% (100% pada service utama).
5. Seluruh 7 skenario demo sidang berhasil dipraktikkan tanpa kendala runtime.
6. Berkas `docs/walkthrough/TICKET-8-WALKTHROUGH.md` dan `README.md` tersusun lengkap.
