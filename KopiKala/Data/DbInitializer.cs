using KopiKala.Helpers;
using KopiKala.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace KopiKala.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(AppDbContext context, IServiceProvider serviceProvider)
    {
        // 1. Pastikan 5 Controlled Core Permissions ada
        var corePermissions = new List<(string Code, string GroupName)>
        {
            ("Meja.Kelola",            "Meja"),
            ("Pembayaran.Verifikasi",  "Kasir"),
            ("Dapur.Antrean",          "Dapur"),
            ("Laporan.Lihat",          "Laporan"),
            ("Sistem.Kelola",          "Sistem")
        };

        foreach (var (code, group) in corePermissions)
        {
            if (!await context.Permissions.AnyAsync(p => p.Code == code))
                context.Permissions.Add(new Permission { Code = code, GroupName = group });
        }
        await context.SaveChangesAsync();

        var allPerms = await context.Permissions.ToListAsync();
        var permMap  = allPerms.ToDictionary(p => p.Code, p => p);

        // 2. Helper untuk upsert role dengan izin
        async Task<Role> UpsertRoleAsync(string name, string description, bool isTemplate, string[] permCodes)
        {
            var role = await context.Roles.Include(r => r.Permissions)
                                   .FirstOrDefaultAsync(r => r.Name == name);
            if (role == null)
            {
                role = new Role { Name = name, Description = description, IsTemplate = isTemplate };
                context.Roles.Add(role);
                await context.SaveChangesAsync();
            }

            foreach (var code in permCodes)
            {
                if (permMap.TryGetValue(code, out var perm) &&
                    !role.Permissions.Any(rp => rp.Id == perm.Id))
                {
                    role.Permissions.Add(perm);
                }
            }
            return role;
        }

        var superAdminRole = await UpsertRoleAsync("SuperAdmin", "Pemilik sistem dengan hak akses tak terbatas", true,
            ["Meja.Kelola", "Pembayaran.Verifikasi", "Dapur.Antrean", "Laporan.Lihat", "Sistem.Kelola"]);

        var managerRole = await UpsertRoleAsync("Manager", "Manajer / Admin kafe", true,
            ["Meja.Kelola", "Pembayaran.Verifikasi", "Dapur.Antrean", "Laporan.Lihat"]);

        var kasirRole = await UpsertRoleAsync("Kasir", "Kasir operasional kafe", true,
            ["Meja.Kelola", "Pembayaran.Verifikasi"]);

        var baristaRole = await UpsertRoleAsync("Barista", "Barista / dapur kafe", true,
            ["Dapur.Antrean"]);

        // Customer role – no permissions (public access)
        var customerRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "Customer");
        if (customerRole == null)
        {
            customerRole = new Role { Name = "Customer", Description = "Pelanggan kafe", IsTemplate = true };
            context.Roles.Add(customerRole);
        }

        await context.SaveChangesAsync();

        // 3. Seed akun demo
        await UpsertUserAsync(context, superAdminRole,
            "Super Administrator", "superadmin@kopikala.com", "081234567890", "AdminKopi123!");

        await UpsertUserAsync(context, kasirRole,
            "Kasir Demo", "kasir@kopikala.com", "081234567891", "KasirKopi123!");

        await UpsertUserAsync(context, baristaRole,
            "Barista Demo", "barista@kopikala.com", "081234567892", "BaristaKopi123!");

        // 4. Seed Timeslots
        if (!await context.Timeslots.AnyAsync())
        {
            context.Timeslots.AddRange(
                new Timeslot { Id = 1, SessionName = "Sesi Pagi (09:00 - 11:00)", StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(11, 0) },
                new Timeslot { Id = 2, SessionName = "Sesi Siang (11:00 - 13:00)", StartTime = new TimeOnly(11, 0), EndTime = new TimeOnly(13, 0) },
                new Timeslot { Id = 3, SessionName = "Sesi Siang-Sore (13:00 - 15:00)", StartTime = new TimeOnly(13, 0), EndTime = new TimeOnly(15, 0) },
                new Timeslot { Id = 4, SessionName = "Sesi Sore (15:00 - 17:00)", StartTime = new TimeOnly(15, 0), EndTime = new TimeOnly(17, 0) },
                new Timeslot { Id = 5, SessionName = "Sesi Senja (17:00 - 19:00)", StartTime = new TimeOnly(17, 0), EndTime = new TimeOnly(19, 0) },
                new Timeslot { Id = 6, SessionName = "Sesi Malam (19:00 - 21:00)", StartTime = new TimeOnly(19, 0), EndTime = new TimeOnly(21, 0) },
                new Timeslot { Id = 7, SessionName = "Sesi Larut Malam (21:00 - 23:00)", StartTime = new TimeOnly(21, 0), EndTime = new TimeOnly(23, 0) }
            );
            await context.SaveChangesAsync();
        }

        // 5. Seed 10 Dining Tables (Indoor AC & Outdoor Smoking)
        if (!await context.DiningTables.AnyAsync())
        {
            context.DiningTables.AddRange(
                new DiningTable { TableNumber = "IN-01", Capacity = 2, Area = "Indoor AC", IsActive = true },
                new DiningTable { TableNumber = "IN-02", Capacity = 2, Area = "Indoor AC", IsActive = true },
                new DiningTable { TableNumber = "IN-03", Capacity = 4, Area = "Indoor AC", IsActive = true },
                new DiningTable { TableNumber = "IN-04", Capacity = 4, Area = "Indoor AC", IsActive = true },
                new DiningTable { TableNumber = "IN-05", Capacity = 6, Area = "Indoor AC", IsActive = true },
                new DiningTable { TableNumber = "IN-06", Capacity = 8, Area = "Indoor AC", IsActive = true },
                new DiningTable { TableNumber = "OUT-01", Capacity = 2, Area = "Outdoor Smoking", IsActive = true },
                new DiningTable { TableNumber = "OUT-02", Capacity = 4, Area = "Outdoor Smoking", IsActive = true },
                new DiningTable { TableNumber = "OUT-03", Capacity = 4, Area = "Outdoor Smoking", IsActive = true },
                new DiningTable { TableNumber = "OUT-04", Capacity = 6, Area = "Outdoor Smoking", IsActive = true }
            );
            await context.SaveChangesAsync();
        }

        // 6. Seed 12 Menu Items
        if (!await context.MenuItems.AnyAsync())
        {
            context.MenuItems.AddRange(
                new MenuItem { Name = "Kopi Susu Gula Aren", Category = "Coffee", Price = 22000, Stock = 50, ImageUrl = "/images/menu/menu-kopi-susu.jpg", IsAvailable = true },
                new MenuItem { Name = "Americano Klasik", Category = "Coffee", Price = 18000, Stock = 50, ImageUrl = "/images/menu/menu-americano.jpg", IsAvailable = true },
                new MenuItem { Name = "Cafe Latte Velvet", Category = "Coffee", Price = 26000, Stock = 40, ImageUrl = "/images/menu/menu-latte.jpg", IsAvailable = true },
                new MenuItem { Name = "Caramel Macchiato", Category = "Coffee", Price = 28000, Stock = 35, ImageUrl = "/images/menu/menu-latte.jpg", IsAvailable = true },
                new MenuItem { Name = "Matcha Latte Uji", Category = "Non-Coffee", Price = 28000, Stock = 30, ImageUrl = "/images/menu/menu-matcha.jpg", IsAvailable = true },
                new MenuItem { Name = "Signature Chocolate", Category = "Non-Coffee", Price = 26000, Stock = 35, ImageUrl = "/images/menu/menu-matcha.jpg", IsAvailable = true },
                new MenuItem { Name = "Earl Grey Milk Tea", Category = "Non-Coffee", Price = 24000, Stock = 30, ImageUrl = "/images/menu/menu-kopi-susu.jpg", IsAvailable = true },
                new MenuItem { Name = "Butter Croissant", Category = "Pastry & Bakery", Price = 22000, Stock = 25, ImageUrl = "/images/menu/menu-croissant.jpg", IsAvailable = true },
                new MenuItem { Name = "Pain Au Chocolat", Category = "Pastry & Bakery", Price = 25000, Stock = 20, ImageUrl = "/images/menu/menu-croissant.jpg", IsAvailable = true },
                new MenuItem { Name = "Crispy French Fries", Category = "Snacks", Price = 20000, Stock = 40, ImageUrl = "/images/menu/menu-fries.jpg", IsAvailable = true },
                new MenuItem { Name = "Truffle Fries", Category = "Snacks", Price = 28000, Stock = 30, ImageUrl = "/images/menu/menu-fries.jpg", IsAvailable = true },
                new MenuItem { Name = "Fried Platter Mix", Category = "Snacks", Price = 35000, Stock = 20, ImageUrl = "/images/menu/menu-fries.jpg", IsAvailable = true }
            );
            await context.SaveChangesAsync();
        }

        // 7. Seed OpenIddict OAuth 2.1 Client (PKCE Required)
        var appManager = serviceProvider.GetService<IOpenIddictApplicationManager>();
        if (appManager != null && await appManager.FindByClientIdAsync("kopikala-client") == null)
        {
            await appManager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = "kopikala-client",
                DisplayName = "KopiKala Web & Mobile OAuth 2.1 Client",
                ClientType = ClientTypes.Public,
                ConsentType = ConsentTypes.Implicit,
                RedirectUris =
                {
                    new Uri("https://localhost:5001/oauth/callback"),
                    new Uri("http://localhost:5080/oauth/callback"),
                    new Uri("http://localhost:5081/oauth/callback")
                },
                Permissions =
                {
                    Permissions.Endpoints.Authorization,
                    Permissions.Endpoints.Token,
                    Permissions.GrantTypes.AuthorizationCode,
                    Permissions.GrantTypes.RefreshToken,
                    Permissions.ResponseTypes.Code,
                    Permissions.Scopes.Email,
                    Permissions.Scopes.Profile,
                    Permissions.Scopes.Roles,
                    Permissions.Prefixes.Scope + "permissions"
                },
                Requirements =
                {
                    Requirements.Features.ProofKeyForCodeExchange // PKCE Wajib
                }
            });
        }
    }

    private static async Task UpsertUserAsync(
        AppDbContext context, Role role,
        string fullName, string email, string phone, string password)
    {
        var user = await context.Users.Include(u => u.Roles).FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
        {
            user = new User
            {
                FullName     = fullName,
                Email        = email,
                PhoneNumber  = phone,
                PasswordHash = PasswordHelper.HashPassword(password),
                CreatedAt    = DateTime.UtcNow
            };
            user.Roles.Add(role);
            context.Users.Add(user);
        }
        else
        {
            user.FullName = fullName;
            user.PhoneNumber = phone;
            user.PasswordHash = PasswordHelper.HashPassword(password);
            if (!user.Roles.Any(r => r.Id == role.Id || r.Name == role.Name))
            {
                user.Roles.Add(role);
            }
        }
        await context.SaveChangesAsync();
    }
}
