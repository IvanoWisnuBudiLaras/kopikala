using KopiKala.DTOs.Admin;

namespace KopiKala.Services;

public interface ISuperAdminService
{
    Task<List<StaffOverviewDto>> GetStaffListAsync();

    Task<bool> CreateStaffAsync(CreateStaffRequestDto dto);

    Task<bool> ResetStaffPasswordAsync(Guid userId, string temporaryPassword);

    Task<List<RoleDto>> GetRolesWithPermissionsAsync();

    Task<List<PermissionDto>> GetAllPermissionsAsync();

    Task<bool> SaveRolePermissionsAsync(ManageRoleDto dto);

    Task<bool> DeleteRoleAsync(int roleId);

    Task<List<ManageTableDto>> GetTablesAsync();

    Task<bool> SaveTableAsync(ManageTableDto dto);

    Task<bool> DeleteTableAsync(Guid tableId);

    Task<List<ManageMenuItemDto>> GetMenuItemsAsync();

    Task<bool> SaveMenuItemAsync(ManageMenuItemDto dto);

    Task<string?> UploadMenuImageAsync(Guid menuItemId, Stream fileStream, string fileName);

    Task<bool> DeleteMenuItemAsync(Guid menuItemId);

    Task<AnalyticsDashboardDto> GetAnalyticsDashboardDataAsync();
}
