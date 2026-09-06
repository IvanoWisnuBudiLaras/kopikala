-- =============================================================================
-- PROYEK: KOPIKALA RESERVATION & ORDER SYSTEM
-- ENGINE: PostgreSQL 15+ / .NET 10 Npgsql
-- SKEMA: 10 Tabel Relasional Terpadu + Seed Data Awal
-- =============================================================================

-- Aktifkan ekstensi UUID generator
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- =============================================================================
-- 1. GRUP TABEL OTORISASI & AKUN (DYNAMIC PBAC)
-- =============================================================================

-- Tabel Master Pengguna
CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    full_name VARCHAR(100) NOT NULL,
    email VARCHAR(150) NOT NULL UNIQUE,
    phone_number VARCHAR(20) NOT NULL,
    password_hash VARCHAR(255) NULL, -- Nullable jika menggunakan Google OAuth 2.1
    google_id VARCHAR(100) NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Tabel Master Peran (Dikelola SuperAdmin)
CREATE TABLE roles (
    id SERIAL PRIMARY KEY,
    name VARCHAR(50) NOT NULL UNIQUE,
    description VARCHAR(200) NULL,
    is_template BOOLEAN NOT NULL DEFAULT FALSE
);

-- Tabel Master Izin Akses Granular
CREATE TABLE permissions (
    id SERIAL PRIMARY KEY,
    code VARCHAR(100) NOT NULL UNIQUE, -- Contoh: 'Booking.VerifyPayment'
    group_name VARCHAR(50) NOT NULL
);

-- Tabel Pivot: Peran <-> Izin (Many-to-Many)
CREATE TABLE role_permissions (
    role_id INT NOT NULL REFERENCES roles(id) ON DELETE CASCADE,
    permission_id INT NOT NULL REFERENCES permissions(id) ON DELETE CASCADE,
    PRIMARY KEY (role_id, permission_id)
);

-- Tabel Pivot: Pengguna <-> Peran (Many-to-Many)
CREATE TABLE user_roles (
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    role_id INT NOT NULL REFERENCES roles(id) ON DELETE RESTRICT,
    PRIMARY KEY (user_id, role_id)
);

-- =============================================================================
-- 2. GRUP TABEL MASTER OPERASIONAL KAFE & MENU
-- =============================================================================

-- Tabel Master Meja Kafe
CREATE TABLE dining_tables (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    table_number VARCHAR(10) NOT NULL UNIQUE, -- 'T-01', 'VIP-01'
    capacity INT NOT NULL CHECK (capacity > 0),
    area VARCHAR(20) NOT NULL CHECK (area IN ('Indoor_AC', 'Outdoor_Smoking')),
    is_active BOOLEAN NOT NULL DEFAULT TRUE
);

-- Tabel Master Sesi Jam Buka Kafe
CREATE TABLE timeslots (
    id SERIAL PRIMARY KEY,
    session_name VARCHAR(50) NOT NULL,
    start_time TIME NOT NULL,
    end_time TIME NOT NULL,
    CONSTRAINT chk_timeslot_valid CHECK (end_time > start_time)
);

-- Tabel Master Menu Makanan & Minuman
CREATE TABLE menu_items (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(100) NOT NULL,
    category VARCHAR(20) NOT NULL CHECK (category IN ('Coffee', 'NonCoffee', 'Meal', 'Snack')),
    price NUMERIC(12, 2) NOT NULL CHECK (price >= 0),
    stock INT NOT NULL DEFAULT 0 CHECK (stock >= 0),
    image_url VARCHAR(255) NULL,
    is_available BOOLEAN NOT NULL DEFAULT TRUE
);

-- =============================================================================
-- 3. GRUP TABEL TRANSAKSI RESERVASI & PRE-ORDER
-- =============================================================================

-- Tabel Induk Transaksi Reservasi
CREATE TABLE bookings (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    invoice_code VARCHAR(50) NOT NULL UNIQUE,
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
    table_id UUID NOT NULL REFERENCES dining_tables(id) ON DELETE RESTRICT,
    timeslot_id INT NOT NULL REFERENCES timeslots(id) ON DELETE RESTRICT,
    booking_date DATE NOT NULL,
    duration_hours INT NOT NULL DEFAULT 2 CHECK (duration_hours > 0),
    representative_name VARCHAR(100) NOT NULL,
    status VARCHAR(25) NOT NULL DEFAULT 'MenungguBayar'
        CHECK (status IN ('MenungguBayar', 'Dikonfirmasi', 'SedangDuduk', 'Selesai', 'Batal', 'NoShow')),
    payment_method VARCHAR(20) NOT NULL
        CHECK (payment_method IN ('TransferBank', 'BayarDiTempat')),
    payment_proof_url VARCHAR(255) NULL,
    total_amount NUMERIC(12, 2) NOT NULL DEFAULT 0 CHECK (total_amount >= 0),
    seated_at TIMESTAMPTZ NULL,
    expires_at TIMESTAMPTZ NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- INDEKS UNIK PENCEGAH DOUBLE-BOOKING (Hanya berlaku untuk yang tidak dibatalkan)
CREATE UNIQUE INDEX uq_active_table_booking
ON bookings (table_id, booking_date, timeslot_id)
WHERE status NOT IN ('Batal', 'NoShow');

-- Tabel Rincian Menu F&B (Komposisi: Induk & Anak)
CREATE TABLE booking_details (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    booking_id UUID NOT NULL REFERENCES bookings(id) ON DELETE CASCADE,
    menu_item_id UUID NOT NULL REFERENCES menu_items(id) ON DELETE RESTRICT,
    quantity INT NOT NULL CHECK (quantity > 0),
    unit_price NUMERIC(12, 2) NOT NULL CHECK (unit_price >= 0),
    sub_total NUMERIC(12, 2) NOT NULL CHECK (sub_total >= 0),
    order_type VARCHAR(20) NOT NULL DEFAULT 'PreOrder'
        CHECK (order_type IN ('PreOrder', 'AdditionalOrder'))
);

-- =============================================================================
-- 4. DATA AWAL SIAP DEMO SIDANG (SEED DATA)
-- =============================================================================

-- Master Sesi Waktu
INSERT INTO timeslots (session_name, start_time, end_time) VALUES
('Sesi Pagi',   '09:00:00', '12:00:00'),
('Sesi Siang',  '13:00:00', '16:00:00'),
('Sesi Sore',   '16:30:00', '19:30:00'),
('Sesi Malam',  '20:00:00', '23:00:00');

-- Master Meja Fisik Kafe
INSERT INTO dining_tables (table_number, capacity, area) VALUES
('T-01', 2, 'Indoor_AC'),
('T-02', 2, 'Indoor_AC'),
('T-03', 4, 'Indoor_AC'),
('T-04', 4, 'Indoor_AC'),
('T-05', 6, 'Indoor_AC'),
('T-06', 2, 'Outdoor_Smoking'),
('T-07', 4, 'Outdoor_Smoking'),
('T-08', 4, 'Outdoor_Smoking'),
('T-09', 6, 'Outdoor_Smoking'),
('T-10', 8, 'Outdoor_Smoking');

-- Master Menu Kopi & Makanan
INSERT INTO menu_items (name, category, price, stock) VALUES
('Es Kopi Susu Gula Aren', 'Coffee',    20000, 100),
('Americano Double Shot',  'Coffee',    18000, 100),
('Caffe Latte Vanilla',    'Coffee',    24000, 80),
('Matcha Green Tea Latte', 'NonCoffee', 25000, 60),
('Earl Grey Milk Tea',     'NonCoffee', 22000, 60),
('Butter Croissant',       'Snack',     18000, 40),
('French Fries Truffle',   'Snack',     22000, 50),
('Spaghetti Aglio Olio',   'Meal',      35000, 30);

-- Master Peran Bawaan (Template SuperAdmin)
INSERT INTO roles (name, description, is_template) VALUES
('SuperAdmin', 'Pemilik sistem dengan hak akses tak terbatas', TRUE),
('Manager',    'Manajer operasional kafe dan laporan keuangan', TRUE),
('Kasir',      'Staf kasir operasional meja dan pembayaran',   TRUE),
('Barista',    'Staf bar dapur dan antrean penyajian F&B',     TRUE),
('Customer',   'Pelanggan kafe untuk reservasi dan pesanan',  TRUE);

-- Master Izin Akses Atomik (Permissions)
INSERT INTO permissions (code, group_name) VALUES
('Table.View',          'Meja'),
('Table.Manage',        'Meja'),
('Booking.View',        'Reservasi'),
('Booking.Create',      'Reservasi'),
('Booking.VerifyPayment','Reservasi'),
('Booking.CheckIn',     'Reservasi'),
('Booking.AddOnOrder',  'Reservasi'),
('Kitchen.ViewQueue',   'Dapur'),
('Kitchen.UpdateStatus','Dapur'),
('Report.ViewFinancial','Laporan'),
('Role.Manage',         'Sistem');
