using System.Text;
using System.Text.Json;

namespace MoviesApp.Api.Functions;

/// <summary>
/// Reads the authenticated user from the SWA `X-MS-CLIENT-PRINCIPAL` header. Used by admin functions to
/// assert the `admin` role (defense-in-depth; SWA already enforces roles via staticwebapp.config.json).
/// </summary>
public static class ClientPrincipalParser
{
    private sealed class ClientPrincipal
    {
        public string? UserId { get; set; }
        public string? UserDetails { get; set; }
        public string? IdentityProvider { get; set; }
        public IEnumerable<string> UserRoles { get; set; } = Array.Empty<string>();
    }

    public static (string? userDetails, bool isAdmin) Parse(string? headerValue)
    {
        if (string.IsNullOrWhiteSpace(headerValue))
        {
            return (null, false);
        }

        try
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(headerValue));
            var principal = JsonSerializer.Deserialize<ClientPrincipal>(
                decoded, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            if (principal is null)
            {
                return (null, false);
            }

            var isAdmin = principal.UserRoles.Contains("admin", StringComparer.OrdinalIgnoreCase);
            return (principal.UserDetails, isAdmin);
        }
        catch (Exception ex) when (ex is FormatException or JsonException)
        {
            return (null, false);
        }
    }
}
