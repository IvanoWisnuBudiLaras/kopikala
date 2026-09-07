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

        // 3. Daftarkan Database PostgreSQL (Npgsql) + OpenIddict
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        builder.Services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
            options.UseOpenIddict();
        });

        // 4. Layanan Bisnis, Otentikasi & API Controllers
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddScoped<IAuthService, AuthService>();
        builder.Services.AddScoped<IBookingService, BookingService>();
        builder.Services.AddHttpClient();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddControllers();

        // 5. Konfigurasi Otentikasi Berbasis Cookie & Google OAuth
        builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                // DefaultChallenge = Cookie agar unauthenticated user di-redirect ke Login
                // bukan langsung ke Google. Google hanya dipicu eksplisit via /auth/google
                options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
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

            // Policy Khusus Pelanggan (Customer)
            options.AddPolicy("CustomerOnly", policy =>
                policy.RequireAssertion(context =>
                    context.User.IsInRole("Customer") ||
                    context.User.HasClaim(System.Security.Claims.ClaimTypes.Role, "Customer") ||
                    (!context.User.HasClaim("Permission", "Sistem.Kelola") &&
                     !context.User.HasClaim(c => c.Type == "Permission" &&
                        new[] { "Meja.Kelola", "Pembayaran.Verifikasi", "Dapur.Antrean", "Laporan.Lihat" }.Contains(c.Value)))));
        });

        // 7. Konfigurasi OpenIddict (Internal OAuth 2.1 Server + PKCE)
        builder.Services.AddOpenIddict()
            .AddCore(options =>
            {
                options.UseEntityFrameworkCore()
                       .UseDbContext<AppDbContext>();
            })
            .AddServer(options =>
            {
                options.SetAuthorizationEndpointUris("/connect/authorize")
                       .SetTokenEndpointUris("/connect/token")
                       .SetUserInfoEndpointUris("/connect/userinfo");
                options.AllowAuthorizationCodeFlow()
                       .RequireProofKeyForCodeExchange(); // PKCE wajib
                options.RegisterScopes("openid", "email", "profile", "roles", "permissions");
                options.AddDevelopmentEncryptionCertificate()
                       .AddDevelopmentSigningCertificate();
                options.UseAspNetCore()
                       .EnableAuthorizationEndpointPassthrough()
                       .EnableTokenEndpointPassthrough()
                       .EnableUserInfoEndpointPassthrough()
                       .DisableTransportSecurityRequirement();
            })
            .AddValidation(options =>
            {
                options.UseLocalServer();
                options.UseAspNetCore();
            });

        builder.Services.AddCascadingAuthenticationState();

        var app = builder.Build();

        // Jalankan Database Seeder otomatis saat startup
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await DbInitializer.SeedAsync(db, scope.ServiceProvider);
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

        // Endpoint Login Form POST resmi
        app.MapPost("/auth/login-submit", async (HttpContext context, IAuthService authService) =>
        {
            var form = await context.Request.ReadFormAsync();
            var email = form["email"].ToString();
            var password = form["password"].ToString();
            var rememberMe = form["rememberMe"].ToString().Equals("true", StringComparison.OrdinalIgnoreCase);
            var returnUrl = form["returnUrl"].ToString();

            var result = await authService.LoginAsync(new DTOs.Auth.LoginRequestDto
            {
                Email = email,
                Password = password,
                RememberMe = rememberMe
            });

            if (!result.Success || result.Principal == null)
            {
                var error = Uri.EscapeDataString(result.ErrorMessage ?? "Login gagal.");
                var redirect = string.IsNullOrEmpty(returnUrl)
                    ? $"/Account/Login?error={error}"
                    : $"/Account/Login?error={error}&returnUrl={Uri.EscapeDataString(returnUrl)}";
                return Results.Redirect(redirect);
            }

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = rememberMe,
                ExpiresUtc = rememberMe ? DateTimeOffset.UtcNow.AddDays(30) : null
            };

            await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, result.Principal, authProperties);

            if (!string.IsNullOrWhiteSpace(returnUrl) && returnUrl.StartsWith("/") && !returnUrl.StartsWith("//") && !returnUrl.StartsWith("/#"))
            {
                return Results.Redirect(returnUrl);
            }

            if (result.Principal.HasClaim("Permission", "Sistem.Kelola"))
            {
                return Results.Redirect("/SuperAdmin");
            }
            if (result.Principal.HasClaim(c => c.Type == "Permission" && new[] { "Meja.Kelola", "Pembayaran.Verifikasi", "Dapur.Antrean", "Laporan.Lihat" }.Contains(c.Value)))
            {
                return Results.Redirect("/Staff");
            }

            return Results.Redirect("/");
        }).DisableAntiforgery();

        // Endpoint Register Form POST resmi
        app.MapPost("/auth/register-submit", async (HttpContext context, IAuthService authService) =>
        {
            var form = await context.Request.ReadFormAsync();
            var fullName = form["fullName"].ToString();
            var email = form["email"].ToString();
            var phoneNumber = form["phoneNumber"].ToString();
            var password = form["password"].ToString();
            var confirmPassword = form["confirmPassword"].ToString();

            if (password != confirmPassword)
            {
                var err = Uri.EscapeDataString("Konfirmasi kata sandi tidak cocok.");
                return Results.Redirect($"/Account/Register?error={err}");
            }

            var result = await authService.RegisterAndLoginAsync(new DTOs.Auth.RegisterRequestDto
            {
                FullName = fullName,
                Email = email,
                PhoneNumber = phoneNumber,
                Password = password,
                ConfirmPassword = confirmPassword
            });

            if (!result.Success || result.Principal == null)
            {
                var err = Uri.EscapeDataString(result.ErrorMessage ?? "Registrasi gagal.");
                return Results.Redirect($"/Account/Register?error={err}");
            }

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
            };

            await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, result.Principal, authProperties);
            return Results.Redirect("/");
        }).DisableAntiforgery();

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

        app.MapControllers();
        app.MapStaticAssets();
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        app.Run();
    }
}
