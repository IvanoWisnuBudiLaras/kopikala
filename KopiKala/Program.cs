using KopiKala.Components;
using KopiKala.Data;
using KopiKala.Helpers;
using KopiKala.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;

namespace KopiKala;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // 1. Daftarkan UI MudBlazor
        builder.Services.AddMudServices();

        // 2. Daftarkan Komponen Blazor Server (Interactive Server Mode, Global)
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        // 3. Daftarkan Database PostgreSQL (Npgsql)
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        // 4. Layanan Bisnis & Otentikasi
        builder.Services.AddScoped<IAuthService, AuthService>();
        builder.Services.AddHttpClient();
        builder.Services.AddHttpContextAccessor();

        // 5. Konfigurasi Otentikasi Berbasis Cookie & Google OAuth
        builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
            })
            .AddCookie(options =>
            {
                options.Cookie.Name = "KopiKala.Auth";
                options.LoginPath = "/Account/Login";
                options.AccessDeniedPath = "/Account/AccessDenied";
                options.ExpireTimeSpan = TimeSpan.FromDays(30);
                options.SlidingExpiration = true;
            })
            .AddGoogle(options =>
            {
                options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? "";
                options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "";
                options.CallbackPath = "/signin-google";
                options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                // Authorized redirect URI yang harus cocok persis dengan Google Cloud Console
                // Pastikan https://localhost:5001/signin-google terdaftar di Google Cloud Console
            });

        // 6. Konfigurasi Controlled Dynamic PBAC (Custom Policy Provider & Handler)
        builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

        builder.Services.AddAuthorization(options =>
        {
            // Policy Khusus SuperAdmin
            options.AddPolicy("SuperAdminOnly", policy =>
                policy.RequireClaim("Permission", "Sistem.Kelola"));

            // Policy Agregat untuk Akses Staf Umum
            options.AddPolicy("StaffAccess", policy =>
                policy.RequireAssertion(context =>
                    context.User.HasClaim("Permission", "Sistem.Kelola") ||
                    context.User.HasClaim(c => c.Type == "Permission" &&
                        new[] { "Meja.Kelola", "Pembayaran.Verifikasi", "Dapur.Antrean", "Laporan.Lihat" }.Contains(c.Value))));
        });

        builder.Services.AddCascadingAuthenticationState();

        var app = builder.Build();

        // Jalankan Database Seeder otomatis saat startup
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await DbInitializer.SeedAsync(db);
        }

        // Endpoint pemicu Google Login resmi (Challenge)
        app.MapGet("/auth/google", (string? returnUrl) =>
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = returnUrl ?? "/"
            };
            return Results.Challenge(properties, [GoogleDefaults.AuthenticationScheme]);
        });

        // Endpoint Logout resmi
        app.MapGet("/auth/logout", async (HttpContext context) =>
        {
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.Redirect("/Account/Login");
        });

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseAuthentication();
        app.UseAuthorization();
        app.UseAntiforgery();

        app.MapStaticAssets();
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        app.Run();
    }
}
