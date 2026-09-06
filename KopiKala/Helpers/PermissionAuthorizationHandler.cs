using Microsoft.AspNetCore.Authorization;

namespace KopiKala.Helpers;

public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        // 1. SuperAdmin otomatis lolos semua policy (Sistem.Kelola)
        if (context.User.HasClaim("Permission", "Sistem.Kelola"))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // 2. Cek apakah user memiliki klaim permission spesifik
        if (context.User.HasClaim(c => c.Type == "Permission" && c.Value == requirement.Permission))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
