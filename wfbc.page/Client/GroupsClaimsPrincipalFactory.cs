using System.Linq;
using System.Text.Json;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication.Internal;

namespace WFBC.Client
{
    // Parses Zitadel project role claims into flat claims for policy evaluation.
    // Zitadel embeds the project ID in the claim name, so the actual claim looks like:
    //   "urn:zitadel:iam:org:project:366760786435015572:roles": { "Commish": {...}, "Managers": {...} }
    // This factory finds claims matching the pattern and extracts role keys as individual claims.
    public class GroupsClaimsPrincipalFactory : AccountClaimsPrincipalFactory<RemoteUserAccount>
    {
        public GroupsClaimsPrincipalFactory(IAccessTokenProviderAccessor accessor) : base(accessor) { }

        public override async ValueTask<ClaimsPrincipal> CreateUserAsync(
         RemoteUserAccount account,
         RemoteAuthenticationUserOptions options)
        {
            ClaimsPrincipal user = await base.CreateUserAsync(account, options);

            if (user.Identity.IsAuthenticated)
            {
                var identity = (ClaimsIdentity)user.Identity;

                // Remove existing role claims to avoid duplicates
                Claim[] roleClaims = identity.FindAll(identity.RoleClaimType).ToArray();
                if (roleClaims != null && roleClaims.Any())
                {
                    foreach (Claim existingClaim in roleClaims)
                    {
                        identity.RemoveClaim(existingClaim);
                    }
                }

                try
                {
                    // Find Zitadel role claim by pattern match (project ID is embedded in claim name)
                    // Pattern: urn:zitadel:iam:org:project:{projectId}:roles
                    var zitadelRolesClaim = identity.Claims
                        .FirstOrDefault(c => c.Type.StartsWith("urn:zitadel:iam:org:project:") && c.Type.EndsWith(":roles"));

                    if (zitadelRolesClaim != null && !string.IsNullOrEmpty(zitadelRolesClaim.Value))
                    {
                        // Parse the JSON object: { "Commish": { "orgId": "orgDomain" }, "Managers": { ... } }
                        using var doc = JsonDocument.Parse(zitadelRolesClaim.Value);
                        foreach (var property in doc.RootElement.EnumerateObject())
                        {
                            // Add each role key as its own claim: e.g., Claim("Commish", "Commish")
                            identity.AddClaim(new Claim(property.Name, property.Name));
                        }
                    }
                }
                catch
                {
                    // Silently handle parsing errors
                }
            }
            return user;
        }
    }
}
