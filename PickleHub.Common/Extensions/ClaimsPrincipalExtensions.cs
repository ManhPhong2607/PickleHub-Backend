using System;
using System.Security.Claims;

namespace PickleHub.Common.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var userIdStr = user.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                     ?? user.FindFirst("sub")?.Value;

        return Guid.TryParse(userIdStr, out var userId) ? userId : Guid.Empty;
    }

    public static bool IsAdmin(this ClaimsPrincipal user)
    {
        return user.IsInRole("Admin") || user.HasClaim(ClaimTypes.Role, "Admin");
    }
}
