using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace KopiKala.Helpers;

public class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    public DefaultAuthorizationPolicyProvider FallbackPolicyProvider { get; }

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        FallbackPolicyProvider = new DefaultAuthorizationPolicyProvider(options);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() =>
        FallbackPolicyProvider.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() =>
        FallbackPolicyProvider.GetFallbackPolicyAsync();

    public async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        // 1. Cek apakah ada policy bawaan yang terdaftar eksplisit di Program.cs
        var explicitPolicy = await FallbackPolicyProvider.GetPolicyAsync(policyName);
        if (explicitPolicy != null)
        {
            return explicitPolicy;
        }

        // 2. Generate policy dinamis untuk setiap kode permission yang diminta via [Authorize(Policy = "...")]
        var policy = new AuthorizationPolicyBuilder();
        policy.AddRequirements(new PermissionRequirement(policyName));
        return policy.Build();
    }
}
