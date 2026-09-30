using System.Security.Claims;

namespace Ride_HailingApi.Helpers;

public static class ClaimsPrincipalExtension
{
 
    public static int GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var id) ? id : 0;
    }

    public static string GetRole(this ClaimsPrincipal principal) =>
        principal.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

}