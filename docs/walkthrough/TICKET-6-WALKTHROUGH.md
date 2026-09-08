# WALKTHROUGH TICKET-6: Tri-Tier Background Worker Engine (Background Jobs, Schedulers & Cron Jobs)

**Status**: COMPLETED & FULLY VERIFIED (COVERAGE >= 85% ON WORKERS)  
**Tanggal**: 2026-09-08  
**Assignee**: Developer (PKL)  

---

## 1. Ringkasan Eksekusi
Mengimplementasikan arsitektur otomasi latar belakang kafe (**Tri-Tier Background Engine**) pada C# .NET 10 tanpa dependensi pihak ketiga:
1. **Tier 1 — Event-Driven In-Memory Queue (`IBackgroundTaskQueue`, `BackgroundTaskQueue`, `QueuedHostedService`)**:
   - Menggunakan `System.Threading.Channels` berbasis bounded channel (kapasitas 100 antrean) untuk tugas-tugas instan asinkron tanpa memblokir thread web pelanggan.
   - Diproses secara sekuensial FIFO dengan penanganan pembatalan token dan isolasi error per work item.
2. **Tier 2 — Scheduler & Maintenance Worker (`BookingMaintenanceWorker`)**:
   - Berbasis `PeriodicTimer` (default 30 detik) yang terintegrasi dengan `TimeProvider` resmi .NET 10.
   - **Auto-Cancel 15 Menit**: Membatalkan reservasi transfer bank (`MenungguBayar`) yang melewati batas `ExpiresAt` menjadi `Batal`.
   - **Hospitality Alert No-Show 20 Menit**: Menandai tamu `BayarDiTempat` yang terlambat > 20 menit dari jam sesi reservasi dengan status `PeringatanNoShow` untuk konfirmasi kasir.
   - **Hospitality Alert Waktu Habis**: Menandai meja aktif (`SedangDigunakan` / `SedangDuduk`) yang durasi duduknya habis dengan status `WaktuHabis` agar kasir/pelayan dapat mengecek ketersediaan jadwal slot berikutnya sebelum menawarkan perpanjangan waktu atau mengakhiri sesi.
   - **Scheduler Pengingat H-1 Jam**: Menghasilkan log notifikasi pengingat kedatangan tepat 1 jam sebelum jadwal sesi reservasi.
3. **Tier 3 — Midnight Reconciliation Cron Worker (`MidnightReconciliationWorker`)**:
   - Berjalan pada pergantian hari untuk rekapitulasi data akuntansi harian ke tabel `daily_reports`.
   - Menghitung total omzet (`TotalRevenue`) dari transaksi berstatus `Selesai`.
   - Menghitung total reservasi berhasil, total no-show, dan total pembatalan.
   - Menentukan menu terlaris (`TopSellingItem`) berdasarkan akumulasi porsi di tabel `booking_details`.
   - Pembersihan mingguan (Minggu malam) berkas foto bukti bayar lama (> 30 hari) di folder `wwwroot/uploads/payments/`.
4. **Registrasi Layanan di `Program.cs`**:
   - Seluruh worker dan queue terdaftar sebagai hosted services `AddHostedService<T>()` dengan `TimeProvider.System`.
5. **Database Model & Migrasi**:
   - Entitas `Models/DailyReport.cs` dan migrasi EF Core `AddDailyReportTable`.

---

## 2. Rincian File & Komponen

### A. Model & Database (`Models/` & `Data/`)
- `Models/DailyReport.cs`: Kolom `Id`, `ReportDate`, `TotalRevenue`, `TotalBookings`, `TotalNoShows`, `TotalCancelled`, `TopSellingItem`, `GeneratedAt`.
- `Data/AppDbContext.cs`: `DbSet<DailyReport> DailyReports` + pemetaan relasi dan index unik `ReportDate`.
- `Migrations/20260908xxxxxx_AddDailyReportTable.cs`: Diterapkan ke PostgreSQL.

### B. Background Queue & Workers (`Workers/`)
- `Workers/IBackgroundTaskQueue.cs`: Kontrak enqueue/dequeue berbasis `ValueTask`.
- `Workers/BackgroundTaskQueue.cs`: Implementasi non-blocking bounded channel (`Channel.CreateBounded`).
- `Workers/QueuedHostedService.cs`: BackgroundService pemroses queue dengan error handling.
- `Workers/BookingMaintenanceWorker.cs`: Dynamic scheduler 30s untuk auto-cancel 15m, alert no-show 20m, dan alert durasi duduk habis.
- `Workers/MidnightReconciliationWorker.cs`: Cron job tutup buku 00:00 dan pembersihan foto bukti transfer > 30 hari.

### C. Unit & Integration Tests (`KopiKala.Tests/`)
- `Workers/BackgroundTaskQueueTests.cs`:
  - `QueueBackgroundWorkItemAsync_EnqueuesAndDequeues_InFifoOrder`: ✅ Passed
  - `QueueBackgroundWorkItemAsync_NullWorkItem_ThrowsArgumentNullException`: ✅ Passed
  - `QueuedHostedService_ExecutesWorkItems_UntilCancelled`: ✅ Passed
  - `QueuedHostedService_WhenWorkItemThrows_LogsErrorAndContinues`: ✅ Passed
- `Workers/BookingMaintenanceWorkerTests.cs`:
  - `PerformMaintenanceCycleAsync_AutoCancels_ExpiredTransferBookings`: ✅ Passed
  - `PerformMaintenanceCycleAsync_TriggersNoShowAlert_WhenLateOver20Min`: ✅ Passed
  - `PerformMaintenanceCycleAsync_TriggersWaktuHabis_WhenSeatedDurationExpires`: ✅ Passed
  - `BookingMaintenanceWorker_ExecuteAsync_RunsPeriodicLoopAndStops`: ✅ Passed
- `Workers/MidnightReconciliationWorkerTests.cs`:
  - `PerformReconciliationAsync_CalculatesDailyMetrics_Accurately`: ✅ Passed
  - `PerformReconciliationAsync_ExistingReport_UpdatesData`: ✅ Passed
  - `PerformFileCleanup_DeletesFilesOlderThan30Days`: ✅ Passed
  - `MidnightReconciliationWorker_ExecuteAsync_RunsLoopAndReconciles`: ✅ Passed
- `Integration/TimeTravelerWorkerTests.cs`:
  - `TimeTraveler_FullTriTierCycle_SimulatesAllTimeWindows`: Simulasi mesin waktu `FakeTimeProvider` melintasi 4 jendela waktu kritis (+16m transfer timeout -> +21m no-show alert -> +2h 1m waktu habis -> 00:05 tutup buku tengah malam): ✅ Passed

---

## 3. Hasil Pengujian (Test Results)

```shell
dotnet test KopiKala.Tests/KopiKala.Tests.csproj -p:CollectCoverage=true -p:Include="[KopiKala]KopiKala.Workers.*"
```

**Hasil**:
- **Total Tests**: 92 Unit/Integration Tests Passed (0 Failed, 0 Skipped)
- **Cakupan BookingMaintenanceWorker**: **100% Line Coverage**, **100% Method Coverage**
- **Cakupan Total Workers Namespace**: **85.91% Line Coverage**, **100% Method Coverage**

```text
+----------+--------+--------+--------+
| Module   | Line   | Branch | Method |
+----------+--------+--------+--------+
| KopiKala | 85.91% | 83.33% | 100%   |
+----------+--------+--------+--------+
```

### Full Solution Test Suite
```shell
dotnet test KopiKala.sln
```
- **KopiKala.Tests.dll**: 92 Passed, 0 Failed.
- **KopiKala.Tests.E2E.dll**: 9 Passed, 0 Failed.
- **Total**: **101/101 PASSED (100% Success Rate)**.
