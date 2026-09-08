using KopiKala.Data;
using KopiKala.DTOs.Admin;
using KopiKala.Helpers;
using KopiKala.Models;
using KopiKala.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Xunit;

namespace KopiKala.Tests.Services;

public class SuperAdminServiceTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly Mock<IWebHostEnvironment> _envMock;
    private readonly FakeTimeProvider _timeProvider;
    private readonly Mock<ILogger<SuperAdminService>> _loggerMock;
    private readonly SuperAdminService _sut;
    private readonly string _tempWebRoot;

    private readonly Guid _superAdminId = Guid.NewGuid();
    private readonly Guid _kasirId = Guid.NewGuid();
    private readonly Guid _customerId = Guid.NewGuid();
    private readonly Guid _table1Id = Guid.NewGuid();
    private readonly Guid _menuItem1Id = Guid.NewGuid();

    public SuperAdminServiceTests()
    {
        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: dbName));

        _serviceProvider = services.BuildServiceProvider();
        _scopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();

        _envMock = new Mock<IWebHostEnvironment>();
        _tempWebRoot = Path.Combine(Path.GetTempPath(), "kopikala_admin_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempWebRoot);
        _envMock.Setup(e => e.WebRootPath).Returns(_tempWebRoot);

        _timeProvider = new FakeTimeProvider();
        _timeProvider.SetUtcNow(new DateTimeOffset(2026, 9, 8, 10, 0, 0, TimeSpan.Zero));

        _loggerMock = new Mock<ILogger<SuperAdminService>>();
        _sut = new SuperAdminService(_scopeFactory, _envMock.Object, _timeProvider, _loggerMock.Object);

        SeedData();
    }

    private AppDbContext GetContext() => _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<AppDbContext>();

    private void SeedData()
    {
        using var context = GetContext();

        // Permissions
        var perm1 = new Permission { Id = 1, Code = "Meja.Kelola", GroupName = "Meja" };
        var perm2 = new Permission { Id = 2, Code = "Pembayaran.Verifikasi", GroupName = "Kasir" };
        var perm3 = new Permission { Id = 3, Code = "Dapur.Antrean", GroupName = "Dapur" };
        var perm4 = new Permission { Id = 4, Code = "Laporan.Lihat", GroupName = "Laporan" };
        var perm5 = new Permission { Id = 5, Code = "Sistem.Kelola", GroupName = "Sistem" };
        context.Permissions.AddRange(perm1, perm2, perm3, perm4, perm5);

        // Roles
        var superAdminRole = new Role { Id = 1, Name = "SuperAdmin", Description = "Owner", IsTemplate = true };
        superAdminRole.Permissions.Add(perm1);
        superAdminRole.Permissions.Add(perm2);
        superAdminRole.Permissions.Add(perm3);
        superAdminRole.Permissions.Add(perm4);
        superAdminRole.Permissions.Add(perm5);

        var kasirRole = new Role { Id = 2, Name = "Kasir", Description = "Cashier", IsTemplate = true };
        kasirRole.Permissions.Add(perm1);
        kasirRole.Permissions.Add(perm2);

        var customerRole = new Role { Id = 3, Name = "Customer", Description = "Guest", IsTemplate = true };

        context.Roles.AddRange(superAdminRole, kasirRole, customerRole);

        // Users
        var superAdmin = new User
        {
            Id = _superAdminId,
            FullName = "Super Admin User",
            Email = "superadmin@kopikala.com",
            PhoneNumber = "081234567890",
            PasswordHash = PasswordHelper.HashPassword("AdminKopi123!"),
            CreatedAt = DateTime.UtcNow
        };
        superAdmin.Roles.Add(superAdminRole);

        var kasir = new User
        {
            Id = _kasirId,
            FullName = "Kasir Demo",
            Email = "kasir@kopikala.com",
            PhoneNumber = "081234567891",
            PasswordHash = PasswordHelper.HashPassword("KasirKopi123!"),
            CreatedAt = DateTime.UtcNow
        };
        kasir.Roles.Add(kasirRole);

        var customer = new User
        {
            Id = _customerId,
            FullName = "Customer Regular",
            Email = "customer@gmail.com",
            PhoneNumber = "081234567892",
            PasswordHash = PasswordHelper.HashPassword("CustKopi123!"),
            CreatedAt = DateTime.UtcNow
        };
        customer.Roles.Add(customerRole);

        context.Users.AddRange(superAdmin, kasir, customer);

        // Tables
        var table1 = new DiningTable
        {
            Id = _table1Id,
            TableNumber = "IN-01",
            Capacity = 4,
            Area = "Indoor AC",
            IsActive = true
        };
        context.DiningTables.Add(table1);

        // Menu items
        var menu1 = new MenuItem
        {
            Id = _menuItem1Id,
            Name = "Signature Kopi Susu",
            Category = "Coffee",
            Price = 22000,
            Stock = 50,
            IsAvailable = true
        };
        context.MenuItems.Add(menu1);

        context.SaveChanges();
    }

    public void Dispose()
    {
        using (var context = GetContext())
        {
            context.Database.EnsureDeleted();
        }
        _serviceProvider.Dispose();
        if (Directory.Exists(_tempWebRoot))
        {
            try { Directory.Delete(_tempWebRoot, true); } catch { }
        }
    }

    [Fact]
    public async Task GetStaffListAsync_ReturnsOnlyStaffUsers_ExcludesCustomers()
    {
        var result = await _sut.GetStaffListAsync();
        Assert.Equal(2, result.Count);
        Assert.Contains(result, s => s.Email == "superadmin@kopikala.com");
        Assert.Contains(result, s => s.Email == "kasir@kopikala.com");
        Assert.DoesNotContain(result, s => s.Email == "customer@gmail.com");
    }

    [Fact]
    public async Task CreateStaffAsync_ValidRequest_CreatesUserWithRole()
    {
        var dto = new CreateStaffRequestDto
        {
            FullName = "Barista Baru",
            Email = "barista.baru@kopikala.com",
            PhoneNumber = "081999888777",
            Password = "BaristaPassword123!",
            RoleId = 2 // Kasir
        };

        var success = await _sut.CreateStaffAsync(dto);
        Assert.True(success);

        using var context = GetContext();
        var user = await context.Users.Include(u => u.Roles).FirstOrDefaultAsync(u => u.Email == "barista.baru@kopikala.com");
        Assert.NotNull(user);
        Assert.Equal("Barista Baru", user.FullName);
        Assert.True(PasswordHelper.VerifyPassword("BaristaPassword123!", user.PasswordHash!));
        Assert.Single(user.Roles);
        Assert.Equal(2, user.Roles.First().Id);
    }

    [Fact]
    public async Task CreateStaffAsync_DuplicateEmail_ThrowsInvalidOperationException()
    {
        var dto = new CreateStaffRequestDto
        {
            FullName = "Duplicate User",
            Email = "kasir@kopikala.com", // existing
            PhoneNumber = "081234567890",
            Password = "Password123!",
            RoleId = 2
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CreateStaffAsync(dto));
    }

    [Fact]
    public async Task ResetStaffPasswordAsync_UpdatesPasswordHash()
    {
        var success = await _sut.ResetStaffPasswordAsync(_kasirId, "NewTempPassword123!");
        Assert.True(success);

        using var context = GetContext();
        var user = await context.Users.FindAsync(_kasirId);
        Assert.NotNull(user);
        Assert.True(PasswordHelper.VerifyPassword("NewTempPassword123!", user.PasswordHash!));
    }

    [Fact]
    public async Task ResetStaffPasswordAsync_NonExistentUser_ReturnsFalse()
    {
        var success = await _sut.ResetStaffPasswordAsync(Guid.NewGuid(), "Pass123!");
        Assert.False(success);
    }

    [Fact]
    public async Task GetRolesWithPermissionsAsync_ReturnsAllRolesAndMappedPermissions()
    {
        var roles = await _sut.GetRolesWithPermissionsAsync();
        Assert.Equal(3, roles.Count);

        var superAdmin = roles.First(r => r.Name == "SuperAdmin");
        Assert.Equal(5, superAdmin.Permissions.Count);

        var kasir = roles.First(r => r.Name == "Kasir");
        Assert.Equal(2, kasir.Permissions.Count);
    }

    [Fact]
    public async Task GetAllPermissionsAsync_ReturnsAll5CorePermissions()
    {
        var perms = await _sut.GetAllPermissionsAsync();
        Assert.Equal(5, perms.Count);
        Assert.Contains(perms, p => p.Code == "Meja.Kelola");
        Assert.Contains(perms, p => p.Code == "Sistem.Kelola");
    }

    [Fact]
    public async Task SaveRolePermissionsAsync_CreatesNewCustomRole_WithSelectedPermissions()
    {
        var dto = new ManageRoleDto
        {
            RoleId = null,
            RoleName = "Kasir Sore",
            Description = "Shift sore",
            IsTemplate = false,
            SelectedPermissionIds = new List<int> { 1, 2 } // Meja.Kelola, Pembayaran.Verifikasi
        };

        var success = await _sut.SaveRolePermissionsAsync(dto);
        Assert.True(success);

        using var context = GetContext();
        var role = await context.Roles.Include(r => r.Permissions).FirstOrDefaultAsync(r => r.Name == "Kasir Sore");
        Assert.NotNull(role);
        Assert.Equal(2, role.Permissions.Count);
        Assert.False(role.IsTemplate);
    }

    [Fact]
    public async Task SaveRolePermissionsAsync_UpdatesExistingRolePermissions()
    {
        var dto = new ManageRoleDto
        {
            RoleId = 2, // Kasir
            RoleName = "Kasir",
            Description = "Updated description",
            IsTemplate = true,
            SelectedPermissionIds = new List<int> { 1, 2, 3 } // Add Dapur.Antrean
        };

        var success = await _sut.SaveRolePermissionsAsync(dto);
        Assert.True(success);

        using var context = GetContext();
        var role = await context.Roles.Include(r => r.Permissions).FirstOrDefaultAsync(r => r.Id == 2);
        Assert.NotNull(role);
        Assert.Equal(3, role.Permissions.Count);
        Assert.Equal("Updated description", role.Description);
    }

    [Fact]
    public async Task DeleteRoleAsync_TemplateRole_ThrowsInvalidOperationException()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.DeleteRoleAsync(1)); // SuperAdmin template
    }

    [Fact]
    public async Task DeleteRoleAsync_CustomUnusedRole_DeletesSuccessfully()
    {
        int customRoleId;
        using (var context = GetContext())
        {
            var customRole = new Role { Name = "Unused Role", Description = "Temp", IsTemplate = false };
            context.Roles.Add(customRole);
            await context.SaveChangesAsync();
            customRoleId = customRole.Id;
        }

        var success = await _sut.DeleteRoleAsync(customRoleId);
        Assert.True(success);

        using (var context = GetContext())
        {
            var exists = await context.Roles.AnyAsync(r => r.Id == customRoleId);
            Assert.False(exists);
        }
    }

    [Fact]
    public async Task GetTablesAsync_And_SaveTableAsync_CreateAndUpdate()
    {
        // 1. Create table
        var newTableDto = new ManageTableDto
        {
            TableNumber = "OUT-01",
            Capacity = 2,
            Area = "Outdoor Smoking",
            IsActive = true
        };
        var createSuccess = await _sut.SaveTableAsync(newTableDto);
        Assert.True(createSuccess);

        var tables = await _sut.GetTablesAsync();
        Assert.Equal(2, tables.Count);

        // 2. Update table
        var created = tables.First(t => t.TableNumber == "OUT-01");
        created.Capacity = 6;
        var updateSuccess = await _sut.SaveTableAsync(created);
        Assert.True(updateSuccess);

        var updatedTables = await _sut.GetTablesAsync();
        Assert.Equal(6, updatedTables.First(t => t.TableNumber == "OUT-01").Capacity);
    }

    [Fact]
    public async Task DeleteTableAsync_RemovesUnreferencedTable()
    {
        var tables = await _sut.GetTablesAsync();
        var tableId = tables.First().TableId!.Value;

        var success = await _sut.DeleteTableAsync(tableId);
        Assert.True(success);

        var remaining = await _sut.GetTablesAsync();
        Assert.Empty(remaining);
    }

    [Fact]
    public async Task GetMenuItemsAsync_And_SaveMenuItemAsync_CreateAndUpdate()
    {
        var newDto = new ManageMenuItemDto
        {
            Name = "Matcha Latte",
            Category = "Non-Coffee",
            Price = 28000,
            Stock = 30,
            IsAvailable = true
        };
        var success = await _sut.SaveMenuItemAsync(newDto);
        Assert.True(success);

        var items = await _sut.GetMenuItemsAsync();
        Assert.Equal(2, items.Count);

        var matcha = items.First(m => m.Name == "Matcha Latte");
        matcha.Price = 30000;
        await _sut.SaveMenuItemAsync(matcha);

        var updated = await _sut.GetMenuItemsAsync();
        Assert.Equal(30000, updated.First(m => m.Name == "Matcha Latte").Price);
    }

    [Fact]
    public async Task UploadMenuImageAsync_ValidImage_SavesAndUpdatesUrl()
    {
        var validJpegBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46 };
        using var stream = new MemoryStream(validJpegBytes);

        var url = await _sut.UploadMenuImageAsync(_menuItem1Id, stream, "menu_photo.jpg");
        Assert.NotNull(url);
        Assert.StartsWith("/images/menu/", url);

        using var context = GetContext();
        var item = await context.MenuItems.FindAsync(_menuItem1Id);
        Assert.Equal(url, item!.ImageUrl);
    }

    [Fact]
    public async Task UploadMenuImageAsync_InvalidExtension_ThrowsArgumentException()
    {
        var invalidBytes = new byte[] { 0x01, 0x02 };
        using var stream = new MemoryStream(invalidBytes);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.UploadMenuImageAsync(_menuItem1Id, stream, "bad.txt"));
    }

    [Fact]
    public async Task DeleteMenuItemAsync_RemovesItem()
    {
        var success = await _sut.DeleteMenuItemAsync(_menuItem1Id);
        Assert.True(success);

        var items = await _sut.GetMenuItemsAsync();
        Assert.Empty(items);
    }

    [Fact]
    public async Task GetAnalyticsDashboardDataAsync_CalculatesMetricsCorrectly()
    {
        var today = new DateOnly(2026, 9, 8);
        using (var context = GetContext())
        {
            var booking1 = new Booking
            {
                Id = Guid.NewGuid(),
                InvoiceCode = "INV/20260908/S1",
                UserId = _customerId,
                TableId = _table1Id,
                TimeslotId = 1,
                BookingDate = today,
                DurationHours = 2,
                RepresentativeName = "Guest 1",
                Status = "Selesai",
                PaymentMethod = "TransferBank",
                TotalAmount = 50000,
                BookingDetails = new List<BookingDetail>
                {
                    new() { Id = Guid.NewGuid(), MenuItemId = _menuItem1Id, Quantity = 2, UnitPrice = 25000, SubTotal = 50000, OrderType = "PreOrder" }
                }
            };

            var booking2 = new Booking
            {
                Id = Guid.NewGuid(),
                InvoiceCode = "INV/20260908/S2",
                UserId = _customerId,
                TableId = _table1Id,
                TimeslotId = 2,
                BookingDate = today,
                DurationHours = 2,
                RepresentativeName = "Guest 2",
                Status = "SedangDigunakan",
                PaymentMethod = "BayarDiTempat",
                TotalAmount = 30000
            };

            context.Bookings.AddRange(booking1, booking2);
            await context.SaveChangesAsync();
        }

        var analytics = await _sut.GetAnalyticsDashboardDataAsync();
        Assert.NotNull(analytics);
        Assert.Equal(80000, analytics.TodayRevenue); // 50.000 + 30.000
        Assert.Equal(2, analytics.TotalBookingsToday);
        Assert.Equal(1, analytics.TotalCompletedToday);
        Assert.Equal(1, analytics.TotalTables);
        Assert.Equal(1, analytics.OccupiedTablesNow);
        Assert.Equal(0, analytics.AvailableTablesNow);
        Assert.Equal("100%", analytics.ActiveOccupancyRate);
        Assert.NotEmpty(analytics.TopSellingItems);
    }
}
