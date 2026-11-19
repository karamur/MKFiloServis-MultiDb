using Microsoft.AspNetCore.Authorization;

namespace MKFiloServis.Web.Services;

public sealed record LicenseModuleRequirement(string Module) : IAuthorizationRequirement;

/// <summary>Admin dahil tüm kullanıcılar için lisans hakkı ayrıca zorunludur.</summary>
public sealed class LicenseModuleAuthorizationHandler(LicenseService licenses, Microsoft.AspNetCore.Hosting.IWebHostEnvironment env)
    : AuthorizationHandler<LicenseModuleRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, LicenseModuleRequirement requirement)
    {
        // Development ortamında lisans kontrolü atlanır (tüm modüller erişilebilir)
        if (env.IsDevelopment() && context.User.Identity?.IsAuthenticated == true)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        if (context.User.Identity?.IsAuthenticated == true && licenses.HasModulePermission(requirement.Module))
            context.Succeed(requirement);
        return Task.CompletedTask;
    }
}

