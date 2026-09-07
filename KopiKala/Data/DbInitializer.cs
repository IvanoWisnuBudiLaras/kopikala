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

        // 4. Seed OpenIddict OAuth 2.1 Client (PKCE Required)
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
