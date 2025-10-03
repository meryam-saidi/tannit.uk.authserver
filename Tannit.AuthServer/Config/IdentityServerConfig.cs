using Duende.IdentityServer;
using Duende.IdentityServer.Models;

namespace Tannit.AuthServer.Config
{
    public static class IdentityServerConfig
    {
        public static IEnumerable<IdentityResource> IdentityResources =>
            new IdentityResource[]
            {
                new IdentityResources.OpenId(), // Obligatoire pour OpenID Connect
                new IdentityResources.Profile(), // Facultatif mais souvent utilisé pour récupérer nom, email, etc.
                new IdentityResources.Email()
            };

        public static IEnumerable<ApiScope> ApiScopes =>
            new ApiScope[]
            {
                new ApiScope("tannitapi.read", "Read access to Tannit API"),
                new ApiScope("tannitapi.write", "Write access to Tannit API"),
                new ApiScope("api1", "Tannit API")
            };

        public static IEnumerable<Client> Clients =>
            new[]
            {
                new Client
                {
                    ClientId = "tannit_web_client",
                    ClientName = "Tannit Web Client",

                    AllowedGrantTypes = GrantTypes.Code,
                    RequireClientSecret = false,
                    RequirePkce = true,

                    RedirectUris = {
                        "http://localhost:3000/callback"
                    },

                    PostLogoutRedirectUris = {
                        "http://localhost:3000/logout"
                    },
            
                    AllowedScopes = {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        IdentityServerConstants.StandardScopes.Email,
                        //"tannitapi.read", 
                        "api1"
                    },
            
                    AllowedCorsOrigins = { 
                        "http://localhost:3000" 
                    },

                    AllowAccessTokensViaBrowser = true,

                    AllowOfflineAccess = true
                }
            };

    }
}
