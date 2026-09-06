using KopiKala.Data;
using KopiKala.DTOs.Auth;
using KopiKala.Helpers;
using KopiKala.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace KopiKala.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly ILogger<AuthService> _logger;

    public AuthService(AppDbContext context, ILogger<AuthService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<AuthResult> LoginAsync(LoginRequestDto dto)
    {
        var email = dto.Email.ToLower().Trim();
        var user = await _context.Users
            .Include(u => u.Roles)
                .ThenInclude(r => r.Permissions)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email);

        if (user == null || string.IsNullOrEmpty(user.PasswordHash))
        {
            _logger.LogWarning("Gagal login: Email {Email} tidak ditemukan.", dto.Email);
            return new AuthResult(false, "Email atau kata sandi tidak valid.", null);
        }

        if (!PasswordHelper.VerifyPassword(dto.Password, user.PasswordHash))
        {
            _logger.LogWarning("Gagal login: Kata sandi salah untuk email {Email}.", dto.Email);
            return new AuthResult(false, "Email atau kata sandi tidak valid.", null);
        }

        var principal = CreateClaimsPrincipal(user);
        _logger.LogInformation("Berhasil login: User {Email} [{Id}] masuk ke sistem.", user.Email, user.Id);
        return new AuthResult(true, null, principal);
    }

    public async Task<AuthResult> RegisterAndLoginAsync(RegisterRequestDto dto)
    {
        var email = dto.Email.ToLower().Trim();
        if (await _context.Users.AnyAsync(u => u.Email.ToLower() == email))
        {
            return new AuthResult(false, "Alamat email sudah terdaftar.", null);
        }

        var customerRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Customer");
        if (customerRole == null)
        {
            customerRole = new Role { Name = "Customer", Description = "Pelanggan kafe", IsTemplate = true };
            _context.Roles.Add(customerRole);
            await _context.SaveChangesAsync();
        }

        var user = new User
        {
            FullName = dto.FullName.Trim(),
            Email = email,
            PhoneNumber = dto.PhoneNumber.Trim(),
            PasswordHash = PasswordHelper.HashPassword(dto.Password),
            CreatedAt = DateTime.UtcNow
        };
        user.Roles.Add(customerRole);

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Registrasi pelanggan baru sukses: {Email} [{Id}]", user.Email, user.Id);
        var principal = CreateClaimsPrincipal(user);
        return new AuthResult(true, null, principal);
    }

    public async Task LogoutAsync()
    {
        _logger.LogInformation("User berhasil logout.");
    }

    public async Task<AuthResult> HandleGoogleCallbackAsync(string email, string name, string googleId)
    {
        var normalizedEmail = email.ToLower().Trim();
        var user = await _context.Users
            .Include(u => u.Roles)
                .ThenInclude(r => r.Permissions)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

        if (user == null)
        {
            var customerRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Customer");
            if (customerRole == null)
            {
                customerRole = new Role { Name = "Customer", Description = "Pelanggan kafe", IsTemplate = true };
                _context.Roles.Add(customerRole);
                await _context.SaveChangesAsync();
            }

            user = new User
            {
                FullName = name,
                Email = normalizedEmail,
                PhoneNumber = "",
                GoogleId = googleId,
                CreatedAt = DateTime.UtcNow
            };
            user.Roles.Add(customerRole);
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Auto-onboarding Google OAuth berhasil: User {Email} terdaftar.", normalizedEmail);
        }
        else if (string.IsNullOrEmpty(user.GoogleId))
        {
            user.GoogleId = googleId;
            await _context.SaveChangesAsync();
        }

        var principal = CreateClaimsPrincipal(user);
        return new AuthResult(true, null, principal);
    }

    public async Task<List<string>> GetUserPermissionsAsync(Guid userId)
    {
        var user = await _context.Users
            .Include(u => u.Roles)
                .ThenInclude(r => r.Permissions)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null) return [];

        return user.Roles
            .SelectMany(r => r.Permissions)
            .Select(p => p.Code)
            .Distinct()
            .ToList();
    }

    public async Task<UserSessionDto?> GetUserSessionAsync(Guid userId)
    {
        var user = await _context.Users
            .Include(u => u.Roles)
                .ThenInclude(r => r.Permissions)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null) return null;

        return new UserSessionDto
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Roles = user.Roles.Select(r => r.Name).ToList(),
            Permissions = user.Roles
                .SelectMany(r => r.Permissions)
                .Select(p => p.Code)
                .Distinct()
                .ToList()
        };
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await _context.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower().Trim());
    }

    private static ClaimsPrincipal CreateClaimsPrincipal(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email)
        };

        foreach (var role in user.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role.Name));
        }

        var permissions = user.Roles
            .SelectMany(r => r.Permissions)
            .Select(p => p.Code)
            .Distinct();

        foreach (var perm in permissions)
        {
            claims.Add(new Claim("Permission", perm));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return new ClaimsPrincipal(identity);
    }
}
