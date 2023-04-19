using System.Linq;
using System.Text.Json;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication.Internal;

namespace WFBC.Client
{
    // This is required because multiple roles arrive in json string array format ["",""]
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
                Claim[] roleClaims = identity.FindAll(identity.RoleClaimType).ToArray();
                var userClaims = user.Claims;

                if (roleClaims != null && roleClaims.Any())
                {
                    foreach (Claim existingClaim in roleClaims)
                    {
                        identity.RemoveClaim(existingClaim);
                    }
                }
                try
                {
                    if (userClaims != null && userClaims.Any())
                    {
                        foreach (Claim userClaim in userClaims)
                        {
                            if (userClaim.Type == "groups" && userClaim.Value != null)
                            {
                                string groups = userClaim.Value;
                                if (!string.IsNullOrEmpty(groups))
                                {
                                    string[] userGroups = JsonSerializer.Deserialize<string[]>(userClaim.Value);
                                    foreach (string userGroup in userGroups)
                                    {
                                        identity.AddClaim(claim: new Claim(userGroup, userGroup));
                                    }
                                }
                            }
                        }
                    }
                }
                catch
                {

                }

            }
            return user;
        }
    }
}
