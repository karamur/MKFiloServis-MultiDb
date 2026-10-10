using Microsoft.EntityFrameworkCore;
using MKFiloServis.Web.Data;

namespace MKFiloServis.Web.Services;

/// <summary>Atomically counts failed password and TOTP attempts for one account.</summary>
internal static class AuthenticationLockoutPolicy
{
    internal static Task<int> RegisterFailedAttemptAsync(
        ApplicationDbContext context,
        int userId,
        DateTime nowUtc,
        TimeSpan lockoutDuration)
        => context.Kullanicilar
            .Where(user => user.Id == userId && user.Aktif && !user.IsDeleted &&
                (!user.Kilitli || (user.KilitlenmeBitisUtc.HasValue && user.KilitlenmeBitisUtc <= nowUtc)))
            .ExecuteUpdateAsync(update => update
                .SetProperty(user => user.BasarisizGirisSayisi, user => user.BasarisizGirisSayisi + 1)
                .SetProperty(user => user.Kilitli, user => user.BasarisizGirisSayisi + 1 >= 5)
                .SetProperty(user => user.KilitlenmeBitisUtc,
                    user => user.BasarisizGirisSayisi + 1 >= 5
                        ? nowUtc.Add(lockoutDuration)
                        : user.KilitlenmeBitisUtc));
}
