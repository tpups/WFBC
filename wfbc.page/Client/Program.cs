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

            builder.Services.AddHttpClient<AuthorizedClient>(client => 
                {
                    client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
                    client.Timeout = TimeSpan.FromMinutes(15); // Extended timeout for long-running operations
                })
                .AddHttpMessageHandler(sp => sp.GetRequiredService<AuthorizationMessageHandler>()
                    .ConfigureHandler(
                        authorizedUrls: new[] { builder.HostEnvironment.BaseAddress }));

            // Supply HttpClient instances that include access tokens when making requests to the server project
            builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("WfbcServerAPI"));

            builder.Services.AddOidcAuthentication(options =>
            {
                builder.Configuration.Bind("Okta", options.ProviderOptions);
                options.ProviderOptions.ResponseType = "code";
                options.UserOptions.RoleClaim = "role";
            }).AddAccountClaimsPrincipalFactory<GroupsClaimsPrincipalFactory>();

            builder.Services.AddAuthorizationCore(options =>
            {
                options.AddPolicy(Policies.IsCommish, Policies.IsCommishPolicy());
                options.AddPolicy(Policies.IsManager, Policies.IsManagerPolicy());
            });

            // Add a separate HttpClient for public requests
            builder.Services.AddHttpClient<PublicClient>(client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress));
            
            builder.Services.AddApiAuthorization();

            builder.Services.AddSingleton<AppState>();

            builder.Services.AddMediaQueryService();

            await builder.Build().RunAsync();
        }
    }
}
