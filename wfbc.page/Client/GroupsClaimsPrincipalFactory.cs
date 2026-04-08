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
                        // Zitadel may return roles as a JSON object OR a JSON array of objects.
                        // Object: { "Commish": { "orgId": "orgDomain" }, "Managers": { ... } }
                        // Array:  [{ "Commish": { ... }, "Managers": { ... } }, { ... }]
                        using var doc = JsonDocument.Parse(zitadelRolesClaim.Value);
                        var addedRoles = new System.Collections.Generic.HashSet<string>();

                        if (doc.RootElement.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var element in doc.RootElement.EnumerateArray())
                            {
                                if (element.ValueKind == JsonValueKind.Object)
                                {
                                    foreach (var property in element.EnumerateObject())
                                    {
                                        if (addedRoles.Add(property.Name))
                                            identity.AddClaim(new Claim(property.Name, property.Name));
                                    }
                                }
                            }
                        }
                        else if (doc.RootElement.ValueKind == JsonValueKind.Object)
                        {
                            foreach (var property in doc.RootElement.EnumerateObject())
                            {
                                if (addedRoles.Add(property.Name))
                                    identity.AddClaim(new Claim(property.Name, property.Name));
                            }
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
