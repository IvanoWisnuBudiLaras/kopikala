using KopiKala.Helpers;
using KopiKala.Models;
using Microsoft.EntityFrameworkCore;

namespace KopiKala.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(AppDbContext context)
    {
        // 1. Pastikan 5 Controlled Core Permissions ada
        var corePermissions = new List<(string Code, string GroupName)>
        {
            ("Meja.Kelola", "Meja"),
            ("Pembayaran.Verifikasi", "Kasir"),
            ("Dapur.Antrean", "Dapur"),
            ("Laporan.Lihat", "Laporan"),
            ("Sistem.Kelola", "Sistem")
        };

        foreach (var (code, group) in corePermissions)
        {
            if (!await context.Permissions.AnyAsync(p => p.Code == code))
            {
                context.Permissions.Add(new Permission { Code = code, GroupName = group });
            }
        }
        await context.SaveChangesAsync();

        // 2. Setup Role Template
        var allPerms = await context.Permissions.ToListAsync();
        var permMap = allPerms.ToDictionary(p => p.Code, p => p);

        var superAdminRole = await context.Roles.Include(r => r.Permissions).FirstOrDefaultAsync(r => r.Name == "SuperAdmin");
        if (superAdminRole == null)
        {
            superAdminRole = new Role { Name = "SuperAdmin", Description = "Pemilik sistem dengan hak akses tak terbatas", IsTemplate = true };
            context.Roles.Add(superAdminRole);
            await context.SaveChangesAsync();
        }

        // Pastikan SuperAdmin memiliki semua izin
        foreach (var p in allPerms)
        {
            if (!superAdminRole.Permissions.Any(rp => rp.Id == p.Id))
            {
                superAdminRole.Permissions.Add(p);
            }
        }
        await context.SaveChangesAsync();

        // Role Manager
        var managerRole = await context.Roles.Include(r => r.Permissions).FirstOrDefaultAsync(r => r.Name == "Manager");
        if (managerRole != null)
        {
            var managerCodes = new[] { "Meja.Kelola", "Pembayaran.Verifikasi", "Dapur.Antrean", "Laporan.Lihat" };
            foreach (var code in managerCodes)
            {
                if (permMap.TryGetValue(code, out var p) && !managerRole.Permissions.Any(rp => rp.Id == p.Id))
                {
                    managerRole.Permissions.Add(p);
                }
            }
        }

        // Role Kasir
        var kasirRole = await context.Roles.Include(r => r.Permissions).FirstOrDefaultAsync(r => r.Name == "Kasir");
        if (kasirRole != null)
        {
            var kasirCodes = new[] { "Meja.Kelola", "Pembayaran.Verifikasi" };
            foreach (var code in kasirCodes)
            {
                if (permMap.TryGetValue(code, out var p) && !kasirRole.Permissions.Any(rp => rp.Id == p.Id))
                {
                    kasirRole.Permissions.Add(p);
                }
            }
        }

        // Role Barista
        var baristaRole = await context.Roles.Include(r => r.Permissions).FirstOrDefaultAsync(r => r.Name == "Barista");
        if (baristaRole != null)
        {
            if (permMap.TryGetValue("Dapur.Antrean", out var p) && !baristaRole.Permissions.Any(rp => rp.Id == p.Id))
            {
                baristaRole.Permissions.Add(p);
            }
        }
        await context.SaveChangesAsync();

        // 3. Akun Default SuperAdmin
        var superAdminUser = await context.Users.Include(u => u.Roles).FirstOrDefaultAsync(u => u.Email == "superadmin@kopikala.com");
        if (superAdminUser == null)
        {
            superAdminUser = new User
            {
                FullName = "Super Administrator",
                Email = "superadmin@kopikala.com",
                PhoneNumber = "081234567890",
                PasswordHash = PasswordHelper.HashPassword("AdminKopi123!"),
                CreatedAt = DateTime.UtcNow
            };
            superAdminUser.Roles.Add(superAdminRole);
            context.Users.Add(superAdminUser);
            await context.SaveChangesAsync();
        }
    }
}
