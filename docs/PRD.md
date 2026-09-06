# Product Requirement Document (PRD) — Official Release

**Project Name**: KopiKala Reservation & Order System  
**Target Scope**: Mini Project PKL (Praktek Kerja Lapangan) — Enterprise Standard  
**Tech Stack**: C\# .NET 10 | Blazor Web App (Interactive Server, Global) | MudBlazor | PostgreSQL | .NET Aspire  
**Document Version**: 1.0-Release  
**Status**: APPROVED & READY FOR CODING IMPLEMENTATION  
**Terakhir Diperbarui**: 2026-09-05

---

## 1\. Ringkasan Eksekutif & Visi Produk

KopiKala Reservation & Order System adalah platform web reservasi meja terpadu dengan pemesanan makanan dan minuman (F\&B) untuk kedai kopi modern. Sistem dirancang untuk mengatasi masalah antrean fisik, mengoptimalkan perputaran meja kafe (*table turnover*), mengeliminasi risiko penjualan ganda (*double-booking*), serta mewujudkan pembukuan kasir dan dapur yang 100% transparan dan dapat diaudit secara bersih.

Proyek ini dibangun menggunakan arsitektur **Monolith Klasik 1-Project** berbasis **Blazor Web App (.NET 10\)** dengan dukungan pemantauan visual **.NET Aspire**, didukung oleh komponen antarmuka **MudBlazor (100% C\#)**, dan menggunakan database **PostgreSQL** yang 100% gratis dan siap jalan di tahap produksi tanpa biaya lisensi.

---

## 2\. Pilihan Teknologi Utama (Technology Stack)

* **Bahasa & Runtime**: C\# 14 / .NET 10 (Long Term Support).  
* **Framework Antarmuka**: Blazor Web App (Interactive Server Mode, Interactivity Location: Global).  
* **Komponen UI**: MudBlazor Component Library (Material Design, 100% C\#, bebas dari kompilasi JavaScript/Node.js).  
* **Database Mesin**: PostgreSQL (Npgsql Entity Framework Core) — Bebas biaya lisensi selamanya untuk produksi.  
* **Orkestrasi & Observabilitas**: .NET Aspire (Dashboard pemantauan performa, kueri database, dan jejak Worker).  
* **Otentikasi & Otorisasi**: Google OAuth 2.1 (PKCE) \+ Kredensial Lokal Staf \+ Dynamic PBAC (Policy-Based Access Control).  
* **Pemrosesan Latar Belakang**: Tri-Tier Engine (`IHostedService` / `BackgroundService` bawaan C\#).

---

## 3\. Alur Bisnis 6 Tahap & 5 Aturan Operasional Kafe

### Alur Utama (The 6-Step Linear Flow)

\[Pelanggan / Tamu Walk-in\]

           │

           ▼

1\. Pilih Meja & Tentukan Durasi Duduk (1 Jam, 2 Jam, atau 3 Jam)

           │

           ▼

2\. Pilih Menu F\&B (Pre-Order Kopi & Makanan)

           │

           ▼

3\. Halaman Detail Invoice (Tampil No Rekening Kafe & Nama Perwakilan Rombongan)

           │

           ├── Jalur A: Transfer Bank ──\> Unggah Foto Bukti di Web ──\> Kasir Verifikasi Lunas

           └── Jalur B: Bayar di Tempat ──\> Bayar Tunai / QRIS saat tiba di kasir

           │

           ▼

4\. Tamu Tiba di Kafe (Kasir Check-in via Nama Perwakilan / Kode Invoice \-\> Status: SedangDigunakan / Seated)

           │

           ▼

5\. Sesi Nongkrong Aktif (Kasir dapat: Tambah Pesanan Menu & Perpanjang Waktu jika slot kosong)

           │

           ▼

6\. Timer Durasi Habis \-\> Worker Ubah Status Selesai \-\> Meja Otomatis Aktif Kembali (Tersedia)

### Lima Aturan Operasional Terpadu:

1. **Batas Waktu Duduk Dinamis**: Pelanggan wajib memilih durasi nongkrong saat pemesanan untuk menjaga sirkulasi kapasitas kafe.  
2. **Perpanjangan Waktu oleh Kasir**: Kasir dapat menambah waktu duduk tamu di sistem jika sesi berikutnya pada meja tersebut belum dipesan orang lain.  
3. **Pencatatan Tamu Offline (*Walk-In*)**: Kasir dapat membuka meja secara langsung bagi tamu yang datang fisik tanpa reservasi web.  
4. **Akuntabilitas Nama Perwakilan**: Setiap transaksi wajib mencatat nama perwakilan pemesan meja untuk kejelasan pelayanan di lantai toko.  
5. **Tambah Pesanan di Tempat (*Add-On Order*) & Audit Terpadu**: Menu tambahan langsung dicatat ke nota meja aktif, menghasilkan struk akhir yang menggabungkan pesanan awal, pesanan tambahan, dan potongan DP tanpa selisih uang atau inventaris.  
6. **Fitur Ganti Menu di Kasir (Item Substitution / Switch Menu)**: Kasir memiliki wewenang untuk menukar item pre-order pelanggan saat check-in jika terjadi kendala stok mendadak di dapur atau atas permintaan pelanggan. Sistem secara otomatis menghitung selisih harga (tambah tagihan jika upgrade, potong tagihan jika downgrade) dan memperbarui rincian `BookingDetails` tanpa membatalkan reservasi meja utama.  
7. **Catatan Batasan Cakupan (Scope Boundary Note)**: Peran Barista/Dapur dibatasi secara ketat pada Kitchen Display System (pelacakan status tiket) dan saklar ketersediaan menu jadi. Logistik inventaris gudang bahan mentah (seperti pelacakan gramasi daging/sayuran) secara eksplisit dikecualikan agar proyek tetap fokus dan realistis dalam batasan PKL.

---

## 4\. Topologi Tampilan Antarmuka (3 Portal Terpadu)

Sistem membagi antarmuka web ke dalam **3 Area Utama**:

1. **Area Customer (`/`)**:  
   * Komponen `MudStepper` untuk pemesanan bertahap tanpa reload halaman.  
   * Denah meja interaktif dengan visualisasi kursi dan kode warna status meja.  
   * Pilihan durasi duduk berbasis chip tombol empuk (`MudChip`).  
   * Katalog F\&B interaktif dengan penambah porsi dan bar mengambang (*Sticky Bottom Bar*).  
   * Halaman invoice dengan tombol satu klik *"Salin No. Rekening"* dan pratinjau upload bukti transfer.  
2. **Area Pekerja (`/Staff`)**:  
   * Satu portal terpadu untuk Pelayan, Barista, Kasir, dan Manajer.  
   * Tampilan menu dinamis berbasis hak akses klaim PBAC:  
     * *Kasir*: Denah meja 2D *real-time*, pencarian Nama Perwakilan / Kode Invoice, verifikasi bukti transfer, check-in, dan tambah pesanan.  
     * *Barista / Dapur*: Layar antrean pesanan dapur (*Kitchen Display System*) dengan tombol aksi cepat.  
     * *Manajer (Admin Kafe)*: Manajemen stok menu, perubahan harga, dan laporan keuangan harian.  
3. **Area SuperAdmin (`/SuperAdmin`)**:  
   * Ruang kendali pemilik sistem untuk manajemen akun staf, pembuatan peran kustom, serta pengaturan template izin akses.

---

## 5\. Sistem Hak Akses: Controlled Dynamic PBAC & Fitur Login

* **Struktur 4 Tabel Otorisasi**: `Users`, `Roles`, `Permissions`, `RolePermissions`, dan `UserRoles`.  
* **5 Controlled Core Permissions**:  
  * `Meja.Kelola`: Pengaturan meja fisik, kapasitas, dan tamu walk-in offline.  
  * `Pembayaran.Verifikasi`: Validasi bukti transfer bank, check-in tamu, dan tambah pesanan di meja aktif.  
  * `Dapur.Antrean`: Papan antrean masak KDS (Mulai/Selesai) dan saklar darurat 'Tandai Menu Habis'.  
  * `Laporan.Lihat`: Tinjauan laporan omzet harian dan rasio okupansi meja.  
  * `Sistem.Kelola`: Manajemen akun staf, pembuatan peran kustom, dan pengaturan template PBAC.  
* **Hirarki Peran (Role Hierarchy)**: Pemisahan antara Pelanggan Eksternal (tersegregasi) vs Hirarki Staf Internal (SuperAdmin \-\> Manager \-\> Kasir & Barista), dengan dukungan penugasan peran ganda (multi-tasking role assignment).  
* **Fitur SuperAdmin**: Bebas membuat peran baru atau menggunakan **Template Bawaan**:  
  * *Template Kasir*, *Template Barista*, dan *Template Manajer*.  
* **Fitur Operasional Login**:  
  * **Auto-Onboarding Google OAuth 2.1 (PKCE)**: Pendaftaran instan otomatis bagi pelanggan baru saat login Google pertama kali.  
  * **Remember Me**: Pilihan Cookie awet 30 hari untuk HP pribadi pelanggan vs Transient Cookie sementara untuk komputer kasir bersama.  
  * **Remote Force Logout**: SuperAdmin dapat memutus sesi login kasir dari jarak jauh melalui pembaruan *Security Stamp*.  
  * **Audit Log Login**: Pencatatan riwayat siapa yang login, jam masuk, dan alamat IP staf.

---

## 6\. Skema Database Fisik PostgreSQL & Kontrol Konkurensi (10 Tabel)

1. `Users`: Akun pengguna, peran, provider OAuth, dan nomor WhatsApp.  
2. `Roles`: Daftar peran buatan SuperAdmin dan status template.  
3. `Permissions`: Master daftar izin akses granular sistem.  
4. `RolePermissions`: Relasi Many-to-Many peran dengan kumpulan izin.  
5. `UserRoles`: Relasi Many-to-Many akun pengguna dengan peran.  
6. `DiningTables`: Nomor meja, kapasitas kursi, area lokasi, status aktif, dan kolom konkurensi `xmin`.  
7. `Timeslots`: Sesi jam buka kafe (waktu mulai dan selesai).  
8. `MenuItems`: Nama menu, kategori, harga, stok dapur, dan foto menu.  
9. `Bookings`: Induk transaksi, kode invoice, durasi jam, nama perwakilan, status bayar, dan batas kedaluwarsa.  
10. `BookingDetails`: Rincian item menu F\&B (pre-order maupun tambahan di tempat) beserta harga saat transaksi.  
* **Optimistic Concurrency Control (OCC)**:  
  * Menggunakan kolom bawaan PostgreSQL **`xmin`** (`.IsRowVersion()`) yang otomatis berubah setiap ada operasi update meja.  
  * Dilindungi aturan mesin database **`UNIQUE CONSTRAINT (TableId, BookingDate, TimeslotId)`** untuk transaksi aktif, menjamin secara mutlak mustahil terjadi *double-booking* di detik yang sama.

---

## 7\. Sistem Worker Latar Belakang (Tri-Tier Engine)

1. **Background Job (Event-Driven Queue via `System.Threading.Channels`)**:  
   * Menangani tugas asinkron instan (render file PDF invoice dan kirim notifikasi pesan) agar web pelanggan tidak mengalami *freeze* atau *loading* lama.  
2. **Scheduler (Dynamic Future Triggers)**:  
   * Mengirimkan pengingat jadwal ke WhatsApp pelanggan tepat H-1 jam sebelum kedatangan.  
   * Mengeksekusi pembatalan otomatis bagi tamu bayar di tempat yang tidak hadir setelah toleransi 20 menit (*grace period*).  
3. **Cron Job (Calendar-Bound Tasks)**:  
   * **Tengah Malam (`0 0 * * *`)**: Rekapitulasi omzet harian otomatis, rasio okupansi meja, dan pencatatan ke tabel laporan kasir.  
   * **Mingguan (`0 23 * * 0`)**: Pembersihan otomatis file foto bukti transfer lama (\> 30 hari) dari direktori server.

---

## 8\. Paket Keamanan Sistem (5-Pillar Security Suite)

1. **Anti-CSRF**: Otomatis melalui generator token Antiforgery bawaan form Blazor/Razor.  
2. **Anti-SQLi & Anti-XSS**: Otomatis melalui parameterized queries Entity Framework Core dan HTML encoding Blazor.  
3. **Sanitasi File Bukti Bayar**: Whitelist ekstensi `.jpg`/`.png`, validasi biner *Magic Bytes*, batas ukuran maks 2 MB, dan penamaan ulang dengan GUID acak.  
4. **Proteksi Kepemilikan Nota (Anti-IDOR)**: Validasi User ID pada Service Layer sebelum membuka nota tagihan.  
5. **CORS Dinonaktifkan**: Menutup celah akses luar karena sistem bersifat Monolith satu domain (*Same-Origin*).

---

## 9\. Lingkup Hasil Jadi Proyek (*Final Deliverables Scope*)

Paket hasil jadi yang diserahkan untuk pengujian sidang PKL:

1. **Solusi Visual Studio 2026 (`KopiKala.sln`)**: 1 Proyek Monolith Blazor Web App C\# .NET 10 yang siap dijalankan dengan tombol **F5**.  
2. **Database PostgreSQL Otomatis & Seed Data Siap Uji**: Auto-migration dengan 10 meja kafe, 12 menu F\&B, serta akun demo bawaan (`superadmin@kopikala.com`, `kasir@kopikala.com`, `barista@kopikala.com`).  
3. **Dashboard Pemantauan Visual (.NET Aspire Dashboard)**: Layar pemantau kesehatan aplikasi, log, dan aktivitas Worker secara visual.  
4. **Paket Dokumentasi Lengkap**: Berkas resmi `PRD.md`, `DISCUSSION.md`, `AGENT.md`, dan `README.md` panduan instalasi.

