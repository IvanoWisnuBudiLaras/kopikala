# TICKET-3: Landing Page, Dynamic Role-Based Navbar & Post-Login Redirection

| Metadata | Details |
| :---- | :---- |
| **Ticket ID** | `TICKET-3` |
| **Project** | KopiKala Reservation & Order System |
| **Status** | `OPEN / READY FOR IMPLEMENTATION` |
| **Priority** | High |
| **Prerequisites** | `TICKET-1` & `TICKET-2` completed (Auth, Dynamic PBAC & Routes ready) |
| **Target Framework** | C\# .NET 10 |
| **Assignee** | Developer (PKL) |

---

## 1\. Objective & Description

Tiket ini berfokus pada pembangunan antarmuka gerbang utama (Landing Page) dan sistem navigasi adaptif (*Dynamic Role-Based Navbar*):

1. Membangun halaman beranda publik (**Landing Page**) bertema kopi hangat menggunakan komponen MudBlazor.  
2. Mengimplementasikan **Navbar Dinamis** berbasis `<AuthorizeView>`:  
   * **Sebelum Login (*Unauthenticated*)**: Menampilkan tombol *"Masuk / Login"*, tombol *"Daftar"*, serta informasi hero section kafe.  
   * **Setelah Login Berhasil (*Authenticated*)**: Navbar otomatis berubah secara reaktif memunculkan identitas pengguna, tombol Logout, serta menu jalan pintas sesuai hak akses peran:  
     * *Customer*: Tombol *"Pesan Meja Sekarang"*, *"Riwayat Reservasi Saya"*.  
     * *Staff (Kasir, Barista, Manajer)*: Tombol jalan pintas ke *"Portal Operasional Staf"* (`/Staff`).  
     * *SuperAdmin*: Tombol jalan pintas ke *"Panel SuperAdmin"* (`/SuperAdmin`).  
3. Mengatur alur kembalinya pengguna pasca-login: `Landing Page -> Login -> Berhasil -> Kembali ke Landing Page dengan Navbar Lengkap Sesuai Peran`.  
4. Menyiapkan struktur folder aset visual di `wwwroot/images/` (banners, menu) dan mengintegrasikan 10 aset gambar realistis ke dalam komponen MudBlazor di Landing Page.

---

## 2\. Rincian Langkah Kerja (Step-by-Step Tasks)

### Task 1: Desain Landing Page Publik (`Components/Pages/Home.razor`)

* **Penempatan Aset Gambar di `wwwroot/`:**  
  * `wwwroot/images/banners/hero-indoor.jpg` (Hero background/banner)  
  * `wwwroot/images/banners/hero-outdoor.jpg` (Section suasana outdoor)  
  * `wwwroot/images/menu/` (menu-kopi-susu.jpg, menu-americano.jpg, menu-latte.jpg, menu-matcha.jpg, menu-croissant.jpg, menu-fries.jpg)  
  * `wwwroot/images/staff/barista-working.jpg` (Section tentang kafe/barista)

Ganti file `Home.razor` bawaan dengan tampilan beranda KopiKala bertema *Warm Coffee Aesthetic*:

* **Hero Banner**:  
  * Judul besar: *"Nikmati Kopi Terbaik Tanpa Antre Meja"* menggunakan `hero-indoor.jpg` dengan dark overlay gradient.  
  * Sub-judul: Penjelasan singkat konsep kafe reservasi meja & pre-order F\&B KopiKala.  
  * Tombol Utama (CTA): `MudButton` *"Pesan Meja Sekarang"* (mengarahkan ke login jika belum login, atau langsung ke wizard booking jika sudah login).  
* **Section Fitur Singkat Kafe**:  
  * 3 Kartu MudBlazor (`MudCard`) menampilkan `MudCardMedia` Image="/images/menu/menu-kopi-susu.jpg", dll:  
    1. *Pilih Meja & Durasi*: Atur waktu nongkrong santai Anda.  
    2. *Pre-Order F\&B*: Minuman diracik segar tepat saat Anda tiba.  
    3. *Pembayaran Fleksibel*: Transfer bank atau bayar di tempat saat datang.  
* **Section Galeri Suasana & Menu Populer**:  
  * Menampilkan pratinjau foto menu unggulan dan tata letak area menggunakan `hero-outdoor.jpg` dan `barista-working.jpg`.

---

### Task 2: Implementasi Dynamic Navbar (`Components/Layout/MainLayout.razor` & `NavMenu.razor`)

Manfaatkan komponen bawaan Blazor `<AuthorizeView>` di dalam `MudAppBar`:

\<MudAppBar Elevation="1" Class="px-4"\>

    \<\!-- Brand Logo / Nama Kafe \--\>

    \<MudText Typo="Typo.h6" Class="font-weight-bold"\>☕ KopiKala\</MudText\>

    

    \<MudSpacer /\>

    \<\!-- BLOK DINAMIS BERBASIS STATUS OTENTIKASI \--\>

    \<AuthorizeView\>

        \<\!-- KONDISI 1: SUDAH LOGIN (AUTHENTICATED) \--\>

        \<Authorized\>

            \<\!-- Menu Khusus Pelanggan (Customer) \--\>

            \<AuthorizeView Policy="CustomerOnly" Context="custContext"\>

                \<MudButton Href="/Booking" Variant="Variant.Filled" Color="Color.Primary" Class="mr-2"\>

                    Booking Meja

                \</MudButton\>

                \<MudButton Href="/Customer/Orders" Variant="Variant.Text" Color="Color.Inherit" Class="mr-2"\>

                    Pesanan Saya

                \</MudButton\>

            \</AuthorizeView\>

            \<\!-- Menu Khusus Staf (Kasir / Barista / Manager) \--\>

            \<AuthorizeView Policy="StaffAccess" Context="staffContext"\>

                \<MudButton Href="/Staff" Variant="Variant.Outlined" Color="Color.Warning" Class="mr-2"\>

                    Portal Staf

                \</MudButton\>

            \</AuthorizeView\>

            \<\!-- Menu Khusus SuperAdmin \--\>

            \<AuthorizeView Policy="SuperAdminOnly" Context="adminContext"\>

                \<MudButton Href="/SuperAdmin" Variant="Variant.Outlined" Color="Color.Error" Class="mr-2"\>

                    SuperAdmin

                \</MudButton\>

            \</AuthorizeView\>

            \<\!-- Profil User & Tombol Logout \--\>

            \<MudMenu Label="@context.User.Identity?.Name" EndIcon="@Icons.Material.Filled.AccountCircle" Color="Color.Inherit"\>

                \<MudMenuItem Href="/Account/Manage"\>Profil Akun\</MudMenuItem\>

                \<MudDivider /\>

                \<form action="/Account/Logout" method="post"\>

                    \<AntiforgeryToken /\>

                    \<input type="hidden" name="ReturnUrl" value="" /\>

                    \<button type="submit" class="mud-menu-item mud-list-item-clickable"\>

                        Keluar (Logout)

                    \</button\>

                \</form\>

            \</MudMenu\>

        \</Authorized\>

        \<\!-- KONDISI 2: BELUM LOGIN (GUEST / PENGUNJUNG PUBLIK) \--\>

        \<NotAuthorized\>

            \<MudButton Href="/Account/Login" Variant="Variant.Text" Color="Color.Inherit" Class="mr-2"\>

                Masuk

            \</MudButton\>

            \<MudButton Href="/Account/Register" Variant="Variant.Filled" Color="Color.Primary"\>

                Daftar

            \</MudButton\>

        \</NotAuthorized\>

    \</AuthorizeView\>

\</MudAppBar\>

---

### Task 3: Penanganan Redirection Pasca-Login

Pastikan alur login memulangkan pengguna ke halaman yang tepat:

1. Saat pengguna belum login mengklik tombol *"Pesan Meja"* di Landing Page, sistem mengarahkannya ke `/Account/Login?returnUrl=/Booking`.  
2. Setelah login berhasil (baik lewat Email & Password maupun Google OAuth 2.1), sistem membaca parameter `returnUrl`:  
   * Jika ada `returnUrl`: Arahkan kembali ke URL tujuan tersebut.  
   * Jika tidak ada: Kembalikan ke halaman beranda (`/`), dan biarkan Navbar dinamis menampilkan tombol-tombol hak aksesnya.

---

## 3\. Kriteria Keberhasilan (Acceptance Criteria)

Tiket ini dinyatakan selesai (*Done*) jika seluruh skenario berikut lolos pengujian:

1. Saat web dibuka pertama kali di browser baru/incognito:  
   * Landing page terbuka dengan banner hero KopiKala.  
   * Navbar hanya menampilkan tombol *"Masuk"* dan *"Daftar"*.  
2. Pengguna mengklik tombol *"Masuk"* dan login menggunakan akun Customer:  
   * Layar kembali ke Landing Page.  
   * Navbar otomatis berubah: Tombol "Masuk/Daftar" hilang, digantikan oleh tombol *"Booking Meja"*, *"Pesanan Saya"*, nama akun, dan tombol *"Keluar"*.  
3. Pengguna login menggunakan akun Staf (Kasir/Barista):  
   * Navbar memunculkan tombol oranye *"Portal Staf"*.  
4. Pengguna login menggunakan akun SuperAdmin:  
   * Navbar memunculkan tombol merah *"SuperAdmin"*.  
5. Menekan tombol Logout menghapus sesi dan Navbar seketika kembali ke tampilan publik (belum login).  
6. Seluruh 10 aset gambar tampil tajam dan proporsional di Landing Page tanpa tautan gambar rusak (broken image), responsif di layar mobile maupun desktop.

## 4\. Standar Pengujian Wajib (Testing Specifications)

### A. Unit Test Coverage (Target Persentase)

* Target Coverage: Minimal 80% Line Coverage pada komponen navigasi dan logika redirection.  
* Pustaka: bUnit (pustaka resmi pengujian komponen Blazor), xUnit, dan Coverlet.  
* Pelaporan: Menghasilkan ringkasan persentase cakupan kode via terminal:

```shell
dotnet test --collect:"XPlat Code Coverage"
```

* Skenario Uji: Memverifikasi perubahan state navbar saat status otentikasi berubah dari NotAuthorized ke Authorized.

### B. API & Endpoint Test (Logging Detail & Time Traveler)

* Pustaka: Microsoft.AspNetCore.Mvc.Testing (WebApplicationFactory), Microsoft.Extensions.TimeProvider.Testing (FakeTimeProvider), dan ITestOutputHelper.  
* Mekanisme Time Traveler:  
  * Menggunakan FakeTimeProvider untuk memajukan waktu simulasi:  
    * Menguji apakah sesi login di navbar otomatis kedaluwarsa setelah masa aktif cookie habis (simulasi maju 30 hari untuk remember-me atau 15 menit untuk transient).  
    * Menguji apakah rute halaman publik tetap merespons dengan cepat.  
* Standar Logging: Setiap pengetesan endpoint mencatat kode status HTTP, waktu respons (latency ms), dan redirect header secara terperinci.

