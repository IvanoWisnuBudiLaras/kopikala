using System.ComponentModel.DataAnnotations;
using KopiKala.DTOs.Auth;
using Xunit;

namespace KopiKala.Tests.DTOs;

public class AuthDtoTests
{
    [Fact]
    public void LoginRequestDto_PropertiesAndValidation_Works()
    {
        var dto = new LoginRequestDto
        {
            Email = "test@example.com",
            Password = "Password123!",
            RememberMe = true
        };

        Assert.Equal("test@example.com", dto.Email);
        Assert.Equal("Password123!", dto.Password);
        Assert.True(dto.RememberMe);

        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, context, results, true);
        Assert.True(isValid);
    }

    [Fact]
    public void RegisterRequestDto_Validation_Works()
    {
        var dto = new RegisterRequestDto
        {
            FullName = "Test User",
            Email = "test@example.com",
            PhoneNumber = "08123456789",
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        };

        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, context, results, true);
        Assert.True(isValid);
    }

    [Fact]
    public void ForgotPasswordRequestDto_Validation_Works()
    {
        var dto = new ForgotPasswordRequestDto
        {
            Email = "user@example.com"
        };

        Assert.Equal("user@example.com", dto.Email);

        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, context, results, true);
        Assert.True(isValid);
    }

    [Fact]
    public void ResetPasswordRequestDto_Validation_Works()
    {
        var dto = new ResetPasswordRequestDto
        {
            Email = "user@example.com",
            Token = "valid-token-base64",
            NewPassword = "NewPassword123!",
            ConfirmNewPassword = "NewPassword123!"
        };

        Assert.Equal("user@example.com", dto.Email);
        Assert.Equal("valid-token-base64", dto.Token);
        Assert.Equal("NewPassword123!", dto.NewPassword);
        Assert.Equal("NewPassword123!", dto.ConfirmNewPassword);

        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, context, results, true);
        Assert.True(isValid);
    }

    [Fact]
    public void UserSessionDto_Properties_Work()
    {
        var id = Guid.NewGuid();
        var dto = new UserSessionDto
        {
            UserId = id,
            FullName = "Admin",
            Email = "admin@example.com",
            Roles = ["SuperAdmin"],
            Permissions = ["Sistem.Kelola", "Meja.Kelola"]
        };

        Assert.Equal(id, dto.UserId);
        Assert.Equal("Admin", dto.FullName);
        Assert.Equal("admin@example.com", dto.Email);
        Assert.Contains("SuperAdmin", dto.Roles);
        Assert.Contains("Sistem.Kelola", dto.Permissions);
    }
}
