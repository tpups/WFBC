using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace WFBC.Shared.Models
{
    public static class Policies
    {
        public const string IsCommish = "IsCommish";
        public const string IsManager = "IsManager";

        public static AuthorizationPolicy IsCommishPolicy()
        {
            return new AuthorizationPolicyBuilder().RequireAuthenticatedUser()
                                                   .RequireClaim("Commish", "Commish")
                                                   .Build();
        }

        public static AuthorizationPolicy IsManagerPolicy()
        {
            return new AuthorizationPolicyBuilder().RequireAuthenticatedUser()
                                                   .RequireClaim("Managers", "Managers")
                                                   .Build();
        }
    }
}
