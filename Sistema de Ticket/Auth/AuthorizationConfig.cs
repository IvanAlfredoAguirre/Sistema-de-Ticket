using Microsoft.AspNetCore.Authorization;

namespace Sistema_de_Ticket.Auth
{
    public static class AuthorizationConfig
    {
        public const string PermissionClaimType = "permission";

        public static void RegisterPolicies(AuthorizationOptions options)
        {
            foreach (var p in Permisos.Todos)
                options.AddPolicy(p, policy => policy.RequireClaim(PermissionClaimType, p));
        }
    }
}
