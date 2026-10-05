using Microsoft.AspNetCore.Authorization;

namespace MKFiloServis.Web.Services;

public sealed record LicenseModuleRequirement(string Module) : IAuthorizationRequirement;

/// <summary>Admin dahil tüm kullanıcılar için lisans hakkı ayrıca zorunludur.</summary>
public sealed class LicenseModuleAuthorizationHandler(LicenseService licenses, LicenseCache cache)
    : AuthorizationHandler<LicenseModuleRequirement>
{
    private readonly System.Runtime.CompilerServices.ConditionalWeakTable<AuthorizationHandlerContext,
        Lazy<Task<LicenseValidationResult>>> checks = new();

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, LicenseModuleRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true) return;
        var validation = await checks.GetValue(context, _ => new Lazy<Task<LicenseValidationResult>>(
            () => licenses.ValidateAsync(persistValidation: false))).Value;
        if (!validation.IsValid) cache.Clear();
        if (validation.IsValid && licenses.HasModulePermission(requirement.Module))
            context.Succeed(requirement);
    }
}
