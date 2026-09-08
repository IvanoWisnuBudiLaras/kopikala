using System.ComponentModel.DataAnnotations;
using KopiKala.DTOs.Admin;
using Xunit;

namespace KopiKala.Tests.DTOs;

public class AdminDtoTests
{
    private static List<ValidationResult> ValidateModel(object model)
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(model, null, null);
        Validator.TryValidateObject(model, context, results, true);
        return results;
    }

    [Fact]
    public void CreateStaffRequestDto_Validation_WorksCorrectly()
    {
        var validDto = new CreateStaffRequestDto
        {
            FullName = "Barista Baru",
            Email = "barista.baru@kopikala.com",
            PhoneNumber = "081234567890",
            Password = "Password123!",
            RoleId = 2
        };
        Assert.Empty(ValidateModel(validDto));

        var invalidDto = new CreateStaffRequestDto
        {
            FullName = "",
            Email = "invalid-email",
            PhoneNumber = "",
            Password = "123", // too short < 6
            RoleId = 0
        };
        var errors = ValidateModel(invalidDto);
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(CreateStaffRequestDto.FullName)));
        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(CreateStaffRequestDto.Email)));
        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(CreateStaffRequestDto.Password)));
    }

    [Fact]
    public void ManageRoleDto_Validation_WorksCorrectly()
    {
        var validDto = new ManageRoleDto
        {
            RoleName = "Kasir Sore",
            Description = "Shift sore",
            SelectedPermissionIds = new List<int> { 1, 2 }
        };
        Assert.Empty(ValidateModel(validDto));

        var invalidDto = new ManageRoleDto
        {
            RoleName = ""
        };
        var errors = ValidateModel(invalidDto);
        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(ManageRoleDto.RoleName)));
    }

    [Fact]
    public void ManageTableDto_Validation_WorksCorrectly()
    {
        var validDto = new ManageTableDto
        {
            TableNumber = "IN-09",
            Capacity = 4,
            Area = "Indoor AC",
            IsActive = true
        };
        Assert.Empty(ValidateModel(validDto));

        var invalidDto = new ManageTableDto
        {
            TableNumber = "",
            Capacity = 0, // < 1
            Area = ""
        };
        var errors = ValidateModel(invalidDto);
        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(ManageTableDto.TableNumber)));
        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(ManageTableDto.Capacity)));
    }

    [Fact]
    public void ManageMenuItemDto_Validation_WorksCorrectly()
    {
        var validDto = new ManageMenuItemDto
        {
            Name = "Espresso Con Panna",
            Category = "Coffee",
            Price = 25000,
            Stock = 40,
            IsAvailable = true
        };
        Assert.Empty(ValidateModel(validDto));

        var invalidDto = new ManageMenuItemDto
        {
            Name = "",
            Price = -1000,
            Stock = -5
        };
        var errors = ValidateModel(invalidDto);
        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(ManageMenuItemDto.Name)));
        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(ManageMenuItemDto.Price)));
        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(ManageMenuItemDto.Stock)));
    }
}
