using KopiKala.Data;
using KopiKala.DTOs.Admin;
using KopiKala.Helpers;
using KopiKala.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KopiKala.Services;

public class SuperAdminService : ISuperAdminService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IWebHostEnvironment _env;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<SuperAdminService> _logger;

    public SuperAdminService(
        IServiceScopeFactory scopeFactory,
        IWebHostEnvironment env,
        TimeProvider timeProvider,
        ILogger<SuperAdminService> logger)
    {
        _scopeFactory = scopeFactory;
        _env = env;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<List<StaffOverviewDto>> GetStaffListAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var staffUsers = await context.Users
            .Include(u => u.Roles)
                .ThenInclude(r => r.Permissions)
            .Where(u => u.Roles.Any(r => r.Name != "Customer"))
            .OrderBy(u => u.FullName)
            .ToListAsync();

        return staffUsers.Select(u => new StaffOverviewDto
        {
            UserId = u.Id,
            FullName = u.FullName,
            Email = u.Email,
            PhoneNumber = u.PhoneNumber,
            RoleName = u.Roles.FirstOrDefault()?.Name ?? "Staf",
            RoleId = u.Roles.FirstOrDefault()?.Id ?? 0,
            Permissions = u.Roles.SelectMany(r => r.Permissions).Select(p => p.Code).Distinct().ToList(),
            CreatedAt = u.CreatedAt
        }).ToList();
    }

    public async Task<bool> CreateStaffAsync(CreateStaffRequestDto dto)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var normalizedEmail = dto.Email.ToLower().Trim();
        if (await context.Users.AnyAsync(u => u.Email.ToLower() == normalizedEmail))
        {
            throw new InvalidOperationException($"Alamat email {dto.Email} sudah terdaftar di sistem.");
        }

        var role = await context.Roles.FirstOrDefaultAsync(r => r.Id == dto.RoleId)
            ?? throw new InvalidOperationException("Peran yang dipilih tidak ditemukan.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = dto.FullName.Trim(),
            Email = normalizedEmail,
            PhoneNumber = dto.PhoneNumber.Trim(),
            PasswordHash = PasswordHelper.HashPassword(dto.Password),
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime
        };
        user.Roles.Add(role);

        context.Users.Add(user);
        await context.SaveChangesAsync();

        _logger.LogInformation("Staff user created: {Email} with Role: {Role}", user.Email, role.Name);
        return true;
    }

    public async Task<bool> ResetStaffPasswordAsync(Guid userId, string temporaryPassword)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return false;

        user.PasswordHash = PasswordHelper.HashPassword(temporaryPassword);
        await context.SaveChangesAsync();

        _logger.LogInformation("Password reset successfully for staff user: {UserId} ({Email})", user.Id, user.Email);
        return true;
    }

    public async Task<List<RoleDto>> GetRolesWithPermissionsAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var roles = await context.Roles
            .Include(r => r.Permissions)
            .OrderBy(r => r.Id)
            .ToListAsync();

        return roles.Select(r => new RoleDto
        {
            Id = r.Id,
            Name = r.Name,
            Description = r.Description,
            IsTemplate = r.IsTemplate,
            Permissions = r.Permissions.Select(p => new PermissionDto
            {
                Id = p.Id,
                Code = p.Code,
                GroupName = p.GroupName
            }).ToList()
        }).ToList();
    }

    public async Task<List<PermissionDto>> GetAllPermissionsAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var permissions = await context.Permissions
            .OrderBy(p => p.GroupName)
            .ThenBy(p => p.Code)
            .ToListAsync();

        return permissions.Select(p => new PermissionDto
        {
            Id = p.Id,
            Code = p.Code,
            GroupName = p.GroupName
        }).ToList();
    }

    public async Task<bool> SaveRolePermissionsAsync(ManageRoleDto dto)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Role? role;
        if (dto.RoleId.HasValue && dto.RoleId.Value > 0)
        {
            role = await context.Roles
                .Include(r => r.Permissions)
                .FirstOrDefaultAsync(r => r.Id == dto.RoleId.Value);

            if (role == null) throw new InvalidOperationException("Peran tidak ditemukan.");

            role.Name = dto.RoleName.Trim();
            role.Description = dto.Description?.Trim();
        }
        else
        {
            role = new Role
            {
                Name = dto.RoleName.Trim(),
                Description = dto.Description?.Trim(),
                IsTemplate = dto.IsTemplate
            };
            context.Roles.Add(role);
            await context.SaveChangesAsync(); // generate ID
        }

        // Sync Many-to-Many permissions
        var selectedPermissions = await context.Permissions
            .Where(p => dto.SelectedPermissionIds.Contains(p.Id))
            .ToListAsync();

        role.Permissions.Clear();
        foreach (var perm in selectedPermissions)
        {
            role.Permissions.Add(perm);
        }

        await context.SaveChangesAsync();
        _logger.LogInformation("Role permissions updated for Role: {RoleName} ({Id}) with {Count} permissions", role.Name, role.Id, role.Permissions.Count);
        return true;
    }

    public async Task<bool> DeleteRoleAsync(int roleId)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var role = await context.Roles
            .Include(r => r.Users)
            .FirstOrDefaultAsync(r => r.Id == roleId);

        if (role == null) return false;

        if (role.IsTemplate)
        {
            throw new InvalidOperationException("Peran template bawaan sistem tidak boleh dihapus.");
        }

        if (role.Users.Any())
        {
            throw new InvalidOperationException("Peran tidak dapat dihapus karena masih digunakan oleh akun pengguna aktif.");
        }

        context.Roles.Remove(role);
        await context.SaveChangesAsync();
        _logger.LogInformation("Custom role deleted: {RoleName} ({Id})", role.Name, roleId);
        return true;
    }

    public async Task<List<ManageTableDto>> GetTablesAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var tables = await context.DiningTables
            .OrderBy(t => t.TableNumber)
            .ToListAsync();

        return tables.Select(t => new ManageTableDto
        {
            TableId = t.Id,
            TableNumber = t.TableNumber,
            Capacity = t.Capacity,
            Area = t.Area,
            IsActive = t.IsActive
        }).ToList();
    }

    public async Task<bool> SaveTableAsync(ManageTableDto dto)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var cleanNumber = dto.TableNumber.Trim().ToUpperInvariant();

        if (dto.TableId.HasValue && dto.TableId.Value != Guid.Empty)
        {
            var table = await context.DiningTables.FirstOrDefaultAsync(t => t.Id == dto.TableId.Value);
            if (table == null) return false;

            // Check number uniqueness
            if (await context.DiningTables.AnyAsync(t => t.TableNumber == cleanNumber && t.Id != dto.TableId.Value))
            {
                throw new InvalidOperationException($"Nomor meja {cleanNumber} sudah digunakan.");
            }

            table.TableNumber = cleanNumber;
            table.Capacity = dto.Capacity;
            table.Area = dto.Area.Trim();
            table.IsActive = dto.IsActive;
        }
        else
        {
            if (await context.DiningTables.AnyAsync(t => t.TableNumber == cleanNumber))
            {
                throw new InvalidOperationException($"Nomor meja {cleanNumber} sudah digunakan.");
            }

            var newTable = new DiningTable
            {
                Id = Guid.NewGuid(),
                TableNumber = cleanNumber,
                Capacity = dto.Capacity,
                Area = dto.Area.Trim(),
                IsActive = dto.IsActive
            };
            context.DiningTables.Add(newTable);
        }

        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteTableAsync(Guid tableId)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var table = await context.DiningTables.FirstOrDefaultAsync(t => t.Id == tableId);
        if (table == null) return false;

        // Check if referenced in bookings
        var isReferenced = await context.Bookings.AnyAsync(b => b.TableId == tableId);
        if (isReferenced)
        {
            // Soft delete by deactivating
            table.IsActive = false;
        }
        else
        {
            context.DiningTables.Remove(table);
        }

        await context.SaveChangesAsync();
        return true;
    }

    public async Task<List<ManageMenuItemDto>> GetMenuItemsAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var items = await context.MenuItems
            .OrderBy(m => m.Category)
            .ThenBy(m => m.Name)
            .ToListAsync();

        return items.Select(m => new ManageMenuItemDto
        {
            MenuItemId = m.Id,
            Name = m.Name,
            Category = m.Category,
            Price = m.Price,
            Stock = m.Stock,
            ImageUrl = m.ImageUrl,
            IsAvailable = m.IsAvailable
        }).ToList();
    }

    public async Task<bool> SaveMenuItemAsync(ManageMenuItemDto dto)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (dto.MenuItemId.HasValue && dto.MenuItemId.Value != Guid.Empty)
        {
            var item = await context.MenuItems.FirstOrDefaultAsync(m => m.Id == dto.MenuItemId.Value);
            if (item == null) return false;

            item.Name = dto.Name.Trim();
            item.Category = dto.Category.Trim();
            item.Price = dto.Price;
            item.Stock = dto.Stock;
            item.IsAvailable = dto.IsAvailable;
            if (!string.IsNullOrEmpty(dto.ImageUrl))
            {
                item.ImageUrl = dto.ImageUrl;
            }
        }
        else
        {
            var newItem = new MenuItem
            {
                Id = Guid.NewGuid(),
                Name = dto.Name.Trim(),
                Category = dto.Category.Trim(),
                Price = dto.Price,
                Stock = dto.Stock,
                ImageUrl = dto.ImageUrl ?? "/images/menu/menu-kopi-susu.jpg",
                IsAvailable = dto.IsAvailable
            };
            context.MenuItems.Add(newItem);
        }

        await context.SaveChangesAsync();
        return true;
    }

    public async Task<string?> UploadMenuImageAsync(Guid menuItemId, Stream fileStream, string fileName)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var item = await context.MenuItems.FirstOrDefaultAsync(m => m.Id == menuItemId);
        if (item == null) return null;

        if (!FileSecurityHelper.IsValidImageExtension(fileName))
        {
            throw new ArgumentException("Format file gambar tidak valid (.jpg, .jpeg, .png).");
        }

        using var memoryStream = new MemoryStream();
        await fileStream.CopyToAsync(memoryStream);
        var bytes = memoryStream.ToArray();

        if (bytes.Length > FileSecurityHelper.MaxFileSizeBytes)
        {
            throw new ArgumentException("Ukuran file gambar maksimal 2 MB.");
        }

        if (!FileSecurityHelper.ValidateMagicBytes(bytes, fileName))
        {
            throw new ArgumentException("File gambar rusak atau bukan format biner yang valid.");
        }

        var safeName = FileSecurityHelper.GenerateSafeFileName(fileName);
        var menuDir = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "images", "menu");
        if (!Directory.Exists(menuDir))
        {
            Directory.CreateDirectory(menuDir);
        }

        var filePath = Path.Combine(menuDir, safeName);
        await File.WriteAllBytesAsync(filePath, bytes);

        var url = $"/images/menu/{safeName}";
        item.ImageUrl = url;
        await context.SaveChangesAsync();

        return url;
    }

    public async Task<bool> DeleteMenuItemAsync(Guid menuItemId)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var item = await context.MenuItems.FirstOrDefaultAsync(m => m.Id == menuItemId);
        if (item == null) return false;

        var isReferenced = await context.BookingDetails.AnyAsync(d => d.MenuItemId == menuItemId);
        if (isReferenced)
        {
            item.IsAvailable = false;
        }
        else
        {
            context.MenuItems.Remove(item);
        }

        await context.SaveChangesAsync();
        return true;
    }

    public async Task<AnalyticsDashboardDto> GetAnalyticsDashboardDataAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var today = DateOnly.FromDateTime(nowUtc);

        var todayBookings = await context.Bookings
            .Include(b => b.BookingDetails)
                .ThenInclude(d => d.MenuItem)
            .Where(b => b.BookingDate == today)
            .ToListAsync();

        var todayCompleted = todayBookings.Where(b => b.Status == "Selesai" || b.Status == "SedangDigunakan").ToList();
        var todayRevenue = todayCompleted.Sum(b => b.TotalAmount);

        // If today has no completed yet, look at the latest daily report for reference revenue
        if (todayRevenue == 0)
        {
            var latestReport = await context.DailyReports
                .OrderByDescending(r => r.ReportDate)
                .FirstOrDefaultAsync();
            if (latestReport != null)
            {
                todayRevenue = latestReport.TotalRevenue;
            }
        }

        var totalTables = await context.DiningTables.CountAsync(t => t.IsActive);
        var occupiedTables = todayBookings.Count(b => b.Status == "SedangDigunakan" || b.Status == "SedangDuduk");
        var availableTables = Math.Max(0, totalTables - occupiedTables);

        var occupancyRate = totalTables > 0 ? (int)Math.Round((double)occupiedTables * 100 / totalTables) : 0;

        var totalBookingsToday = todayBookings.Count;
        var totalCompletedToday = todayBookings.Count(b => b.Status == "Selesai");
        var totalNoShowsToday = todayBookings.Count(b => b.Status == "NoShow" || b.Status == "PeringatanNoShow");
        var totalCancelledToday = todayBookings.Count(b => b.Status == "Batal" || b.Status == "Kedaluwarsa");

        // Top selling items from today's completed bookings or all recent booking details
        var topSellingItems = await context.BookingDetails
            .Include(d => d.MenuItem)
            .GroupBy(d => d.MenuItem != null ? d.MenuItem.Name : "Menu")
            .Select(g => new TopSellingItemDto
            {
                Name = g.Key,
                TotalQuantity = g.Sum(x => x.Quantity),
                TotalRevenue = g.Sum(x => x.SubTotal)
            })
            .OrderByDescending(x => x.TotalQuantity)
            .Take(5)
            .ToListAsync();

        return new AnalyticsDashboardDto
        {
            TodayRevenue = todayRevenue,
            TotalBookingsToday = totalBookingsToday,
            TotalCompletedToday = totalCompletedToday,
            TotalNoShowsToday = totalNoShowsToday,
            TotalCancelledToday = totalCancelledToday,
            TotalTables = totalTables,
            OccupiedTablesNow = occupiedTables,
            AvailableTablesNow = availableTables,
            ActiveOccupancyRate = $"{occupancyRate}%",
            TopSellingItems = topSellingItems,
            OccupancyDonutData = [availableTables, occupiedTables],
            OccupancyDonutLabels = ["Tersedia", "Terisi"]
        };
    }
}
