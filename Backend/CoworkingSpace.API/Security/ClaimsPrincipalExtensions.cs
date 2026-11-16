using System.Security.Claims;

namespace CoworkingSpace.API.Security
{
    public static class Roles
    {
        public const string Admin = "Admin";
    }

    public static class ClaimsPrincipalExtensions
    {
        public static int? GetUserId(this ClaimsPrincipal user)
        {
            var value = user.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(value, out int id) ? id : null;
        }

        public static bool IsAdmin(this ClaimsPrincipal user) => user.IsInRole(Roles.Admin);

        // True when the caller is the given user, or is an admin.
        public static bool CanAccessUser(this ClaimsPrincipal user, int userId)
            => user.IsAdmin() || user.GetUserId() == userId;
    }
}
