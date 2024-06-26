using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Linq;
using WFBC.Server.Interface;
using WFBC.Server.DataAccess;
using WFBC.Server.Models;
using WFBC.Shared.Models;
using WFBC.Shared;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;
using MongoDB.Driver.Core.Configuration;
using Okta.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace WFBC.Server
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
            services.AddControllers();

            // Okta Authentication
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = OktaDefaults.ApiAuthenticationScheme;
                options.DefaultChallengeScheme = OktaDefaults.ApiAuthenticationScheme;
                options.DefaultSignInScheme = OktaDefaults.ApiAuthenticationScheme;
            })
            .AddOktaWebApi(new OktaWebApiOptions()
            {
                OktaDomain = Configuration["Okta:OktaDomain"],
                AuthorizationServerId = Configuration["Okta:AuthorizationServerId"],
                Audience = Configuration["Okta:Audience"]
            });
            // Authorization Policies
            services.AddAuthorization(options =>
            {
                options.AddPolicy(Policies.IsCommish, Policies.IsCommishPolicy());
                options.AddPolicy(Policies.IsManager, Policies.IsManagerPolicy());
            });
            // Swagger
            services.AddSwaggerGen();
            // DB
            services.Configure<DatabaseSettings>(Configuration.GetSection(nameof(DatabaseSettings)));
            services.AddSingleton<IDatabaseSettings>(x => x.GetRequiredService<IOptions<DatabaseSettings>>().Value);
            services.AddSingleton<WfbcDBContext>();
            // Okta
            services.Configure<OktaSettings>(Configuration.GetSection(nameof(OktaSettings)));
            services.AddSingleton<IOktaSettings>(x => x.GetRequiredService<IOptions<OktaSettings>>().Value);
            // Interfaces
            services.AddTransient<ITeam, TeamDataAccessLayer>();
            services.AddTransient<IManager, ManagerDataAccessLayer>();
            services.AddTransient<IDraft, DraftDataAccessLayer>();
            services.AddTransient<IPick, PickDataAccessLayer>();
            services.AddTransient<IStandings, StandingsDataAccessLayer>();

            services.AddResponseCompression(opts =>
            {
                opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
                    new[] { "application/octet-stream" });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseWebAssemblyDebugging();
                // Swagger
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseBlazorFrameworkFiles();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapControllers();
                endpoints.MapFallbackToFile("index.html");
            });
        }
    }
}
