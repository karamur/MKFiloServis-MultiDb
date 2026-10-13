using Microsoft.AspNetCore.Authorization;

namespace MKFiloServis.Web.Services;

public sealed record LicenseModuleRequirement(string Module) : IAuthorizationRequirement;

/// <summary>Admin dahil tüm kullanıcılar için lisans hakkı ayrıca zorunludur.</summary>
public sealed class LicenseModuleAuthorizationHandler(LicenseService licenses)
    : AuthorizationHandler<LicenseModuleRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, LicenseModuleRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated == true && licenses.HasModulePermission(requirement.Module))
            context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
