# TICKET-8: End-to-End System Integration, .NET Aspire Observability & PKL Defense Demonstration Suite

| Metadata | Details |
| :--- | :--- |
| **Ticket ID** | `TICKET-8` |
| **Project** | KopiKala Reservation & Order System |
| **Status** | `OPEN / READY FOR IMPLEMENTATION` |
| **Priority** | Critical / Final Delivery Milestone |
| **Prerequisites** | `TICKET-1` s/d `TICKET-7` completed |
| **Target Framework** | C# .NET 10 |
| **Assignee** | Developer (PKL) |

---

## 1. Objective & Description

Tiket ini adalah **puncak penyelesaian seluruh proyek KopiKala**, bertugas menyatukan seluruh modul menjadi satu kesatuan sistem komersial yang siap dideploy ke server nyata dan siap dipresentasikan di depan dosen/penguji sidang PKL:

1. **Integrasi Hulu-ke-Hilir (*End-to-End Integration*)**: Menghubungkan seluruh 3 portal: Customer (`/`), Staff (`/Staff`), dan SuperAdmin (`/SuperAdmin`) dalam satu rantai transaksi utuh.
2. **Observabilitas Visual (.NET Aspire Dashboard)**: Mengonfigurasi dan memverifikasi dashboard pemantauan kesehatan server visual (*Traces, Metrics, Logs*) saat demonstrasi sidang.
3. **Validasi 7 Standar Mutlak Kesiapan Produksi**: Memverifikasi Docker multi-stage build, docker-compose, pengujian solusi penuh (> 80% coverage), 4 peran utama, 5-pilar security suite, reporting bisnis, dan zero-downtime E2E flow.
4. **Dokumentasi Repositori Resmi (`README.md`)**: Panduan instalasi 1-klik, prasyarat, kredensial akun demo penguji, dan arsitektur sistem.
5. **Naskah Skenario Demo Sidang PKL (7 Skenario Demonstrasi)**: Panduan langkah demi langkah saat presentasi di depan dewan penguji.

---

## 2. Rincian Langkah Kerja (Step-by-Step Tasks)

### Task 1: Konfigurasi Observabilitas Visual (.NET Aspire Dashboard)
1. Memastikan integrasi ServiceDefaults dan Aspire Dashboard aktif di `Program.cs`.
2. Verifikasi pemantauan:
   - **Tab Resources**: Status container database `kopikala-postgres` dan web app `kopikala-web`.
   - **Tab Traces**: Jejak waktu eksekusi kueri Entity Framework Core dan latensi request HTTP.
   - **Tab Logs**: Catatan aktivitas Tri-Tier Worker dan jejak audit login staf secara real-time.

### Task 2: Verifikasi Kontainerisasi Penuh (Docker Production Test)
1. Eksekusi `docker compose down -v && docker compose up -d --build`.
2. Memverifikasi web dan database berjalan mandiri, sehat (*healthy*), dan volume `kopikala_uploads` mempertahankan foto bukti transfer secara persisten.

### Task 3: Penyusunan Panduan Repositori (`README.md`)
1. Deskripsi proyek KopiKala dan nilai bisnisnya.
2. Prasyarat sistem: .NET 10 SDK, Docker Desktop, Visual Studio 2026.
3. Panduan jalankan 1-klik (`docker compose up -d` & `dotnet run`).
4. Tabel kredensial demo lengkap (SuperAdmin, Manager, Kasir, Barista, Customer).

### Task 4: Skenario Demo Sidang PKL (Naskah Pengujian 7 Langkah)
- Skenario 1 (Pemesanan Pelanggan): Wizard 4-langkah, kalkulasi sticky bar, nota invoice dengan countdown 15m.
- Skenario 2 (Verifikasi Kasir & Check-In): Inspeksi foto bukti transfer, verifikasi lunas, check-in tamu ke meja.
- Skenario 3 (Layar Dapur Barista KDS): Pelacakan status tiket dapur Mulai/Selesai.
- Skenario 4 (Fleksibilitas Kasir): Ganti menu (*item substitution*) & pesanan tambahan (*add-on order*).
- Skenario 5 (Etika Hospitality): Peringatan waktu habis & pengecekan master schedule sebelum interaksi fisik.
- Skenario 6 (Tata Kelola Dynamic PBAC): Pembuatan peran kustom & centang 5 izin operasional secara live.
- Skenario 7 (Pemantauan Kinerja Aspire): Demonstrasi live tracing & database latency di Aspire Dashboard.

---

## 3. Standar Pengujian Regresi Terpadu (Regression Testing Suite)

### A. Full Solution Unit Test Coverage
```shell
dotnet test KopiKala.sln --collect:"XPlat Code Coverage"
```
- Seluruh modul (Models, Services, Workers, DTOs, Helpers, Controllers) terverifikasi dengan Line Coverage >= 80%.

### B. End-to-End Master Journey Playwright Test
- Menjalankan pengujian otomatis E2E penuh dalam mode Live Headed Browser melintasi seluruh peran.

---

## 4. Kriteria Keberhasilan (Acceptance Criteria)

1. Seluruh 3 portal web berfungsi 100% tanpa tautan rusak atau galat konsol browser.
2. Perintah `docker compose up -d` menyalakan web dan database secara mandiri.
3. Aspire Dashboard menampilkan metrik kesehatan aplikasi dan log transaksi secara real-time.
4. Laporan Coverlet membuktikan solusi terlindungi oleh cakupan pengujian unit minimal 80%.
5. Seluruh 7 skenario demo sidang berhasil dipraktikkan tanpa kendala runtime.
6. Berkas `docs/walkthrough/TICKET-8-WALKTHROUGH.md` tersusun lengkap.
