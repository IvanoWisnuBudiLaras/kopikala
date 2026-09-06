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
    private readonly AuthService _sut; // System Under Test

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
        // Arrange
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

        // Act
        var result = await _sut.LoginAsync(dto);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Principal);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task LoginAsync_InvalidPassword_ReturnsFailure()
    {
        // Arrange
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

        // Act
        var result = await _sut.LoginAsync(dto);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Null(result.Principal);
    }

    [Fact]
    public async Task LoginAsync_NonExistentEmail_ReturnsFailure()
    {
        // Arrange
        var dto = new LoginRequestDto
        {
            Email = "nonexistent@kopikala.com",
            Password = "AnyPassword!"
        };

        // Act
        var result = await _sut.LoginAsync(dto);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Null(result.Principal);
    }

    [Fact]
    public async Task RegisterAndLoginAsync_NewEmail_ReturnsSuccessWithPrincipal()
    {
        // Arrange
        var dto = new RegisterRequestDto
        {
            FullName = "Customer Baru",
            Email = "customer@gmail.com",
            PhoneNumber = "08987654321",
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        };

        // Act
        var result = await _sut.RegisterAndLoginAsync(dto);

        // Assert
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
        // Arrange
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

        // Act
        var result = await _sut.RegisterAndLoginAsync(dto);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Null(result.Principal);
    }

    [Fact]
    public async Task HandleGoogleCallbackAsync_NewUser_AutoOnboardsWithCustomerRole()
    {
        // Act
        var result = await _sut.HandleGoogleCallbackAsync("googleuser@gmail.com", "Google User", "google-id-12345");

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Principal);
        Assert.Null(result.ErrorMessage);

        var user = await _context.Users.Include(u => u.Roles).FirstOrDefaultAsync(u => u.Email == "googleuser@gmail.com");
        Assert.NotNull(user);
        Assert.Equal("google-id-12345", user.GoogleId);
        Assert.Contains(user.Roles, r => r.Name == "Customer");
    }

    [Fact]
    public async Task GetUserPermissionsAsync_ReturnsDistinctPermissions()
    {
        // Arrange
        var role1 = new Role { Name = "Kasir" };
        var perm1 = new Permission { Code = "Meja.Kelola", GroupName = "Meja" };
        var perm2 = new Permission { Code = "Pembayaran.Verifikasi", GroupName = "Kasir" };
        role1.Permissions.Add(perm1);
        role1.Permissions.Add(perm2);

        var role2 = new Role { Name = "Barista" };
        var perm3 = new Permission { Code = "Dapur.Antrean", GroupName = "Dapur" };
        role2.Permissions.Add(perm2); // Duplikat permission across roles
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

        // Act
        var permissions = await _sut.GetUserPermissionsAsync(user.Id);

        // Assert
        Assert.Equal(3, permissions.Count);
        Assert.Contains("Meja.Kelola", permissions);
        Assert.Contains("Pembayaran.Verifikasi", permissions);
        Assert.Contains("Dapur.Antrean", permissions);
    }

    [Fact]
    public async Task GetUserSessionAsync_ValidUser_ReturnsSessionDto()
    {
        // Arrange
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

        // Act
        var session = await _sut.GetUserSessionAsync(user.Id);

        // Assert
        Assert.NotNull(session);
        Assert.Equal(user.Id, session.UserId);
        Assert.Equal("Manager Kafe", session.FullName);
        Assert.Contains("Manager", session.Roles);
        Assert.Contains("Laporan.Lihat", session.Permissions);
    }
}
