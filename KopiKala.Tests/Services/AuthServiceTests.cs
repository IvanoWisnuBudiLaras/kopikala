using KopiKala.Data;
using KopiKala.DTOs.Auth;
using KopiKala.Helpers;
using KopiKala.Models;
using KopiKala.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KopiKala.Tests.Services;

public class AuthServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Mock<ILogger<AuthService>> _loggerMock;
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _loggerMock = new Mock<ILogger<AuthService>>();
        _sut = new AuthService(_context, _loggerMock.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsSuccessWithPrincipal()
    {
        var role = new Role { Name = "SuperAdmin", Description = "Admin" };
        var permission = new Permission { Code = "Sistem.Kelola", GroupName = "Sistem" };
        role.Permissions.Add(permission);
        _context.Roles.Add(role);

        var user = new User
        {
            FullName = "Admin Test",
            Email = "admin@kopikala.com",
            PhoneNumber = "08123456789",
            PasswordHash = PasswordHelper.HashPassword("Password123!"),
            CreatedAt = DateTime.UtcNow
        };
        user.Roles.Add(role);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var dto = new LoginRequestDto
        {
            Email = "admin@kopikala.com",
            Password = "Password123!",
            RememberMe = true
        };

        var result = await _sut.LoginAsync(dto);

        Assert.True(result.Success);
        Assert.NotNull(result.Principal);
        Assert.Null(result.ErrorMessage);
        Assert.True(result.Principal.HasClaim("Permission", "Sistem.Kelola"));
    }

    [Fact]
    public async Task LoginAsync_InvalidPassword_ReturnsFailure()
    {
        var user = new User
        {
            FullName = "User Test",
            Email = "user@kopikala.com",
            PhoneNumber = "08123456789",
            PasswordHash = PasswordHelper.HashPassword("CorrectPassword!"),
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var dto = new LoginRequestDto
        {
            Email = "user@kopikala.com",
            Password = "WrongPassword!"
        };

        var result = await _sut.LoginAsync(dto);

        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Null(result.Principal);
    }

    [Fact]
    public async Task LoginAsync_NonExistentEmail_ReturnsFailure()
    {
        var dto = new LoginRequestDto
        {
            Email = "nonexistent@kopikala.com",
            Password = "AnyPassword!"
        };

        var result = await _sut.LoginAsync(dto);

        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Null(result.Principal);
    }

    [Fact]
    public async Task RegisterAndLoginAsync_NewEmail_ReturnsSuccessWithPrincipal()
    {
        var dto = new RegisterRequestDto
        {
            FullName = "Customer Baru",
            Email = "customer@gmail.com",
            PhoneNumber = "08987654321",
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        };

        var result = await _sut.RegisterAndLoginAsync(dto);

        Assert.True(result.Success);
        Assert.NotNull(result.Principal);
        Assert.Null(result.ErrorMessage);

        var createdUser = await _context.Users.Include(u => u.Roles).FirstOrDefaultAsync(u => u.Email == "customer@gmail.com");
        Assert.NotNull(createdUser);
        Assert.Equal("Customer Baru", createdUser.FullName);
        Assert.Contains(createdUser.Roles, r => r.Name == "Customer");
    }

    [Fact]
    public async Task RegisterAndLoginAsync_DuplicateEmail_ReturnsFailure()
    {
        var existingUser = new User
        {
            FullName = "Existing",
            Email = "duplicate@gmail.com",
            PhoneNumber = "081111111",
            PasswordHash = PasswordHelper.HashPassword("Pass123!"),
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(existingUser);
        await _context.SaveChangesAsync();

        var dto = new RegisterRequestDto
        {
            FullName = "Duplicate Test",
            Email = "duplicate@gmail.com",
            PhoneNumber = "082222222",
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        };

        var result = await _sut.RegisterAndLoginAsync(dto);

        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Null(result.Principal);
    }

    [Fact]
    public async Task HandleGoogleCallbackAsync_NewUser_AutoOnboardsWithCustomerRole()
    {
        var result = await _sut.HandleGoogleCallbackAsync("googleuser@gmail.com", "Google User", "google-id-12345");

        Assert.True(result.Success);
        Assert.NotNull(result.Principal);
        Assert.Null(result.ErrorMessage);

        var user = await _context.Users.Include(u => u.Roles).FirstOrDefaultAsync(u => u.Email == "googleuser@gmail.com");
        Assert.NotNull(user);
        Assert.Equal("google-id-12345", user.GoogleId);
        Assert.Contains(user.Roles, r => r.Name == "Customer");
    }

    [Fact]
    public async Task HandleGoogleCallbackAsync_ExistingUserWithoutGoogleId_UpdatesGoogleId()
    {
        var existingUser = new User
        {
            FullName = "Existing User",
            Email = "existing@gmail.com",
            PhoneNumber = "0812345",
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(existingUser);
        await _context.SaveChangesAsync();

        var result = await _sut.HandleGoogleCallbackAsync("existing@gmail.com", "Existing User", "new-google-id");

        Assert.True(result.Success);
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == "existing@gmail.com");
        Assert.NotNull(user);
        Assert.Equal("new-google-id", user.GoogleId);
    }

    [Fact]
    public async Task LogoutAsync_LogsInformationWithoutException()
    {
        var ex = await Record.ExceptionAsync(() => _sut.LogoutAsync());
        Assert.Null(ex);
    }

    [Fact]
    public async Task GetUserPermissionsAsync_ReturnsDistinctPermissions()
    {
        var role1 = new Role { Name = "Kasir" };
        var perm1 = new Permission { Code = "Meja.Kelola", GroupName = "Meja" };
        var perm2 = new Permission { Code = "Pembayaran.Verifikasi", GroupName = "Kasir" };
        role1.Permissions.Add(perm1);
        role1.Permissions.Add(perm2);

        var role2 = new Role { Name = "Barista" };
        var perm3 = new Permission { Code = "Dapur.Antrean", GroupName = "Dapur" };
        role2.Permissions.Add(perm2);
        role2.Permissions.Add(perm3);

        var user = new User
        {
            FullName = "Multi Role Staff",
            Email = "staff@kopikala.com",
            PhoneNumber = "0812345678",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        };
        user.Roles.Add(role1);
        user.Roles.Add(role2);

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var permissions = await _sut.GetUserPermissionsAsync(user.Id);

        Assert.Equal(3, permissions.Count);
        Assert.Contains("Meja.Kelola", permissions);
        Assert.Contains("Pembayaran.Verifikasi", permissions);
        Assert.Contains("Dapur.Antrean", permissions);
    }

    [Fact]
    public async Task GetUserPermissionsAsync_NonExistentUser_ReturnsEmptyList()
    {
        var permissions = await _sut.GetUserPermissionsAsync(Guid.NewGuid());
        Assert.Empty(permissions);
    }

    [Fact]
    public async Task GetUserSessionAsync_ValidUser_ReturnsSessionDto()
    {
        var role = new Role { Name = "Manager" };
        var perm = new Permission { Code = "Laporan.Lihat", GroupName = "Laporan" };
        role.Permissions.Add(perm);

        var user = new User
        {
            FullName = "Manager Kafe",
            Email = "manager@kopikala.com",
            PhoneNumber = "08123456",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        };
        user.Roles.Add(role);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var session = await _sut.GetUserSessionAsync(user.Id);

        Assert.NotNull(session);
        Assert.Equal(user.Id, session.UserId);
        Assert.Equal("Manager Kafe", session.FullName);
        Assert.Contains("Manager", session.Roles);
        Assert.Contains("Laporan.Lihat", session.Permissions);
    }

    [Fact]
    public async Task GetUserSessionAsync_NonExistentUser_ReturnsNull()
    {
        var session = await _sut.GetUserSessionAsync(Guid.NewGuid());
        Assert.Null(session);
    }

    [Fact]
    public async Task GeneratePasswordResetTokenAsync_ValidEmail_ReturnsValidBase64Token()
    {
        var user = new User
        {
            FullName = "Reset User",
            Email = "reset@kopikala.com",
            PhoneNumber = "08123456789",
            PasswordHash = "oldhash",
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var token = await _sut.GeneratePasswordResetTokenAsync("reset@kopikala.com");

        Assert.NotNull(token);
        var decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(token));
        Assert.Contains(user.Id.ToString(), decoded);
    }

    [Fact]
    public async Task GeneratePasswordResetTokenAsync_NonExistentEmail_ReturnsNull()
    {
        var token = await _sut.GeneratePasswordResetTokenAsync("unknown@kopikala.com");
        Assert.Null(token);
    }

    [Fact]
    public async Task ResetPasswordAsync_ValidToken_UpdatesPasswordHash()
    {
        var user = new User
        {
            FullName = "Reset User",
            Email = "reset2@kopikala.com",
            PhoneNumber = "08123456789",
            PasswordHash = PasswordHelper.HashPassword("OldPass123!"),
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var token = await _sut.GeneratePasswordResetTokenAsync("reset2@kopikala.com");
        Assert.NotNull(token);

        var success = await _sut.ResetPasswordAsync("reset2@kopikala.com", token, "NewPass123!");

        Assert.True(success);
        var updatedUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == "reset2@kopikala.com");
        Assert.NotNull(updatedUser);
        Assert.True(PasswordHelper.VerifyPassword("NewPass123!", updatedUser.PasswordHash));
    }

    [Fact]
    public async Task ResetPasswordAsync_ExpiredOrCorruptedToken_ReturnsFalse()
    {
        var user = new User
        {
            FullName = "Reset User",
            Email = "reset3@kopikala.com",
            PhoneNumber = "08123456789",
            PasswordHash = PasswordHelper.HashPassword("OldPass123!"),
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Expired token (in past)
        var pastTicks = DateTime.UtcNow.AddMinutes(-20).Ticks;
        var expiredPayload = $"{user.Id}:{pastTicks}";
        var expiredToken = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(expiredPayload));

        var resultExpired = await _sut.ResetPasswordAsync("reset3@kopikala.com", expiredToken, "NewPass123!");
        Assert.False(resultExpired);

        // Corrupted token
        var resultCorrupt = await _sut.ResetPasswordAsync("reset3@kopikala.com", "not-a-valid-token", "NewPass123!");
        Assert.False(resultCorrupt);

        // Non-existent user
        var resultNonExistent = await _sut.ResetPasswordAsync("ghost@kopikala.com", expiredToken, "NewPass123!");
        Assert.False(resultNonExistent);

        // Mismatched user id token
        var mismatchPayload = $"{Guid.NewGuid()}:{DateTime.UtcNow.AddMinutes(15).Ticks}";
        var mismatchToken = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(mismatchPayload));
        var resultMismatch = await _sut.ResetPasswordAsync("reset3@kopikala.com", mismatchToken, "NewPass123!");
        Assert.False(resultMismatch);
    }

    [Fact]
    public async Task GetUserByEmailAsync_ExistingAndNonExisting_ReturnsExpected()
    {
        var user = new User
        {
            FullName = "Email User",
            Email = "lookup@kopikala.com",
            PhoneNumber = "08123456789",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var found = await _sut.GetUserByEmailAsync("lookup@kopikala.com");
        Assert.NotNull(found);
        Assert.Equal("lookup@kopikala.com", found.Email);

        var notFound = await _sut.GetUserByEmailAsync("notfound@kopikala.com");
        Assert.Null(notFound);
    }
}
