using System;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WFBC.Shared.Models;
using WFBC.Client.Services;
using BlazorPro.BlazorSize;
using Microsoft.AspNetCore.Components.Authorization;

namespace WFBC.Client
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");

            builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

            // Transient handler that catches 401s after a silent-refresh failure
            // and redirects to the login endpoint for clean re-authentication.
            builder.Services.AddTransient<UnauthorizedRedirectHandler>();

            builder.Services.AddHttpClient<AuthorizedClient>(client => 
                {
                    client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
                    client.Timeout = TimeSpan.FromMinutes(15); // Extended timeout for long-running operations
                })
                .AddHttpMessageHandler(sp => sp.GetRequiredService<AuthorizationMessageHandler>()
                    .ConfigureHandler(
                        authorizedUrls: new[] { builder.HostEnvironment.BaseAddress }))
                .AddHttpMessageHandler<UnauthorizedRedirectHandler>();

            // Supply HttpClient instances that include access tokens when making requests to the server project
            builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("WfbcServerAPI"));

            builder.Services.AddOidcAuthentication(options =>
            {
                builder.Configuration.Bind("Zitadel", options.ProviderOptions);
                options.ProviderOptions.ResponseType = "code";
                options.ProviderOptions.DefaultScopes.Add("openid");
                options.ProviderOptions.DefaultScopes.Add("profile");
                options.ProviderOptions.DefaultScopes.Add("email");
                // offline_access requests a refresh token so the access token can be
                // silently renewed without bouncing the user through Zitadel's iframe.
                // Requires "Refresh Token" enabled on the Zitadel application.
                options.ProviderOptions.DefaultScopes.Add("offline_access");
                options.ProviderOptions.DefaultScopes.Add("urn:zitadel:iam:org:project:roles");
                // Request project-specific audience in the access token so the server can validate it
                options.ProviderOptions.DefaultScopes.Add("urn:zitadel:iam:org:project:id:zitadel:aud");
                options.UserOptions.RoleClaim = "urn:zitadel:iam:org:project:roles";
            }).AddAccountClaimsPrincipalFactory<GroupsClaimsPrincipalFactory>();

            builder.Services.AddAuthorizationCore(options =>
            {
                options.AddPolicy(Policies.IsCommish, Policies.IsCommishPolicy());
                options.AddPolicy(Policies.IsManager, Policies.IsManagerPolicy());
            });

            // Add a separate HttpClient for public requests
            builder.Services.AddHttpClient<PublicClient>(client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress));
            

            builder.Services.AddSingleton<AppState>();

            // Register StandingsCacheService with PublicClient for standings data
            // Use Singleton for performance - cache is now properly isolated by year
            builder.Services.AddSingleton<StandingsCacheService>(sp => 
                new StandingsCacheService(sp.GetRequiredService<PublicClient>().Client));

            builder.Services.AddMediaQueryService();

            await builder.Build().RunAsync();
        }
    }
}
