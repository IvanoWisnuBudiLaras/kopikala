using KopiKala.DTOs.Auth;
using KopiKala.Models;
using System.Security.Claims;

namespace KopiKala.Services;

public interface IAuthService
{
    Task<AuthResult> LoginAsync(LoginRequestDto dto);
    Task<AuthResult> RegisterAndLoginAsync(RegisterRequestDto dto);
    Task LogoutAsync();
    Task<AuthResult> HandleGoogleCallbackAsync(string email, string name, string googleId);
    Task<List<string>> GetUserPermissionsAsync(Guid userId);
    Task<UserSessionDto?> GetUserSessionAsync(Guid userId);
    Task<User?> GetUserByEmailAsync(string email);
    Task<string?> GeneratePasswordResetTokenAsync(string email);
    Task<bool> ResetPasswordAsync(string email, string token, string newPassword);
}

public record AuthResult(bool Success, string? ErrorMessage, ClaimsPrincipal? Principal);
