using System.Security.Claims;

namespace GatumsGourmet.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sub")
            ?? throw new InvalidOperationException("UserId claim not found."));

    public static bool IsAdmin(this ClaimsPrincipal user) =>
        user.IsInRole("Admin");
}
