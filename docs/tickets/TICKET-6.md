# TICKET-6: Tri-Tier Background Worker Engine (Background Jobs, Schedulers & Cron Jobs)

| Metadata | Details |
| :--- | :--- |
| **Ticket ID** | `TICKET-6` |
| **Project** | KopiKala Reservation & Order System |
| **Status** | `COMPLETED` |
| **Priority** | High / Automation & Reliability |
| **Prerequisites** | `TICKET-1` s/d `TICKET-5` completed |
| **Target Framework** | C# .NET 10 |
| **Assignee** | Developer (PKL) |

---

## 1. Objective & Description

Tiket ini berfokus pada pembangunan **otak otomasi latar belakang kafe (Tri-Tier Background Engine)** yang berjalan mandiri tanpa membebani antarmuka web:

1. Membangun **Background Job (Event-Driven Queue)** menggunakan `System.Threading.Channels` untuk memproses tugas instan asinkron (render invoice PDF & antrean notifikasi) tanpa membuat browser pelanggan mengalami *freeze*.  
2. Membangun **Scheduler (Dynamic Future Triggers)** berbasis `PeriodicTimer`:  
   * Auto-cancel pemesanan transfer bank yang melebihi batas waktu bayar 15 menit.  
   * Memicu notifikasi aktif *'WaktuHabis'* pada portal Kasir & Waiter saat durasi duduk (1–3 jam) habis. Kasir memeriksa jadwal master di sistem terlebih dahulu untuk menentukan apakah slot berikutnya kosong (Skenario A: tawarkan perpanjangan atau pesanan tambahan) atau sudah dipesan orang lain (Skenario B: info waktu habis dengan sopan).  
   * Memicu alert urgensi 'PerluKonfirmasiNoShow' ke kasir untuk tamu bayar di tempat yang telat > 20 menit, memberikan kesempatan kasir memverifikasi secara manual sebelum membatalkan reservasi.  
   * Pengiriman pengingat jadwal reservasi tepat H-1 jam sebelum kedatangan.  
3. Membangun **Cron Job (Calendar-Bound Tasks)**:  
   * Tutup buku & rekapitulasi omzet harian otomatis setiap pukul 00:00 malam (`0 0 * * *`) ke tabel `daily_reports`.  
   * Pembersihan otomatis berkas foto bukti bayar lama (> 30 hari) di folder `wwwroot/uploads/payments/` setiap minggu malam (`0 23 * * 0`).  
4. Menyiapkan rangkaian pengujian otomatis menggunakan **Time Traveler (`FakeTimeProvider`)** untuk memvalidasi seluruh siklus waktu tanpa menunggu waktu nyata.

---

## 2. Rincian Langkah Kerja (Step-by-Step Tasks)

### Task 1: Pemodelan Tabel Laporan Harian (`Models/DailyReport.cs`)
- Entitas `DailyReport` dengan kolom: `Id`, `ReportDate`, `TotalRevenue`, `TotalBookings`, `TotalNoShows`, `TotalCancelled`, `TopSellingItem`, `GeneratedAt`.
- Registrasi `DbSet<DailyReport>` dan migrasi EF Core `AddDailyReportTable`.

### Task 2: Pembangunan Background Job Queue (`Workers/BackgroundTaskQueue.cs`)
- `IBackgroundTaskQueue`: Interface berbasis `ValueTask`.
- `BackgroundTaskQueue`: In-memory bounded channel (`System.Threading.Channels`).
- `QueuedHostedService`: `BackgroundService` pemroses antrean dengan error handling.

### Task 3: Pembangunan Scheduler & Maintenance Worker (`Workers/BookingMaintenanceWorker.cs`)
- Dynamic scheduler 30s (`PeriodicTimer`):
  1. Auto-cancel transfer bank 15 menit.
  2. Peringatan No-Show 20 menit untuk bayar di tempat.
  3. Peringatan Waktu Habis durasi duduk.
  4. Log pengingat H-1 jam.

### Task 4: Pembangunan Cron Job Tutup Buku Tengah Malam (`Workers/MidnightReconciliationWorker.cs`)
- Rekonsiliasi harian 00:00 ke tabel `daily_reports`.
- Pembersihan mingguan foto bukti pembayaran > 30 hari.

### Task 5: Pendaftaran Service di `Program.cs`
- Registrasi `IBackgroundTaskQueue`, `QueuedHostedService`, `BookingMaintenanceWorker`, `MidnightReconciliationWorker`.

---

## 3. Standar Pengujian Wajib (Testing Specifications)

### A. Unit Test Coverage
- Minimal **80% Line Coverage** pada folder `Workers/`.
- Hasil uji: **85.91% Line Coverage**, **100% Method Coverage**, **100% Line Coverage pada `BookingMaintenanceWorker`**.

### B. API / Integration Test dengan Time Traveler (`FakeTimeProvider`)
- `TimeTravelerWorkerTests.cs` memvalidasi siklus penuh 15m transfer timeout, 20m no-show alert, 2h waktu habis, dan midnight reconciliation.

---

## 4. Kriteria Keberhasilan (Acceptance Criteria)

1. Seluruh worker (`BookingMaintenanceWorker`, `MidnightReconciliationWorker`, `QueuedHostedService`) menyala otomatis di latar belakang saat aplikasi di-run (F5).  
2. Simulasi waktu 15 menit berhasil membatalkan booking transfer bank yang tidak dibayar tanpa campur tangan staf.  
3. Simulasi waktu 20 menit berhasil mengirimkan alert konfirmasi no-show ke kasir alih-alih pembatalan sepihak.  
4. Meja yang durasi duduknya habis memicu status WaktuHabis agar staf dapat melakukan pendekatan layanan tamu sebelum meja di-reset manual.  
5. Tabel `daily_reports` di PostgreSQL berhasil terisi data omzet otomatis saat jam 00:00 tiba.  
6. Laporan Coverlet membuktikan Line Coverage folder `Workers/` mencapai minimal 80%.  
