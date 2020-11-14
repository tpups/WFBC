using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication.Internal;
using Microsoft.AspNetCore.Identity;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using System.Configuration;
using Microsoft.AspNetCore.Authentication;
using Okta.AspNetCore;
using Okta.Sdk;
using Okta.Sdk.Configuration;
using wfbc.page.Server.Models;

namespace wfbc.page.Server
{
    public class GroupsToRolesTransformer : IClaimsTransformation
    {
        private OktaClient client;

        public GroupsToRolesTransformer(IOktaSettings settings)
        {
            client = new OktaClient(new OktaClientConfiguration
            {
                OktaDomain = settings.OktaDomain,
                Token = settings.Token
            });
        }

        public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal IPrincipal)
        {
            var idClaim = IPrincipal.FindFirst(x => x.Type == ClaimTypes.NameIdentifier);
            if (idClaim != null)
            {
                var user = await client.Users.GetUserAsync(idClaim.Value);
                if (user != null)
                {
                    var groups = user.Groups.ToEnumerable();
                    foreach (var group in groups)
                    {
                        ((ClaimsIdentity)IPrincipal.Identity).AddClaim(new Claim(ClaimTypes.Role, group.Profile.Name));
                    }
                }
            }
            return IPrincipal;
        }
    }
}
