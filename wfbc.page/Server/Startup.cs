using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Linq;
using WFBC.Server.Interface;
using WFBC.Server.DataAccess;
using WFBC.Server.Models;
using WFBC.Server.Services;
using WFBC.Shared.Models;
using WFBC.Shared;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json;

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

            // CORS Configuration for Blazor WebAssembly with Authorization
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(builder =>
                {
                    builder
                        .WithOrigins("https://localhost:5003", "http://localhost:5003") // Your client URLs
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials(); // Critical for authorization tokens
                });
            });

            // Zitadel Authentication (JWT Bearer)
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = Configuration["Zitadel:Authority"];
                options.Audience = Configuration["Zitadel:ClientId"];
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = Configuration["Zitadel:Authority"],
                    ValidateAudience = true,
                    ValidAudience = Configuration["Zitadel:ClientId"],
                    ValidateLifetime = true
                };
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        // Map Zitadel project roles to flat claims for policy evaluation
                        // Zitadel embeds the project ID in the claim name:
                        //   urn:zitadel:iam:org:project:{projectId}:roles
                        var identity = context.Principal?.Identity as System.Security.Claims.ClaimsIdentity;
                        if (identity == null) return System.Threading.Tasks.Task.CompletedTask;

                        var rolesClaim = identity.Claims
                            .FirstOrDefault(c => c.Type.StartsWith("urn:zitadel:iam:org:project:") && c.Type.EndsWith(":roles"));
                        if (rolesClaim != null)
                        {
                            try
                            {
                                using var doc = JsonDocument.Parse(rolesClaim.Value);
                                foreach (var property in doc.RootElement.EnumerateObject())
                                {
                                    // Add each role key as its own claim: e.g., Claim("Commish", "Commish")
                                    identity.AddClaim(new System.Security.Claims.Claim(property.Name, property.Name));
                                }
                            }
                            catch { }
                        }

                        return System.Threading.Tasks.Task.CompletedTask;
                    }
                };
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
            // Interfaces
            services.AddTransient<ITeam, TeamDataAccessLayer>();
            services.AddTransient<IManager, ManagerDataAccessLayer>();
            services.AddTransient<IDraft, DraftDataAccessLayer>();
            services.AddTransient<IPick, PickDataAccessLayer>();
            services.AddTransient<IStandings, StandingsDataAccessLayer>();
            services.AddTransient<ISeasonSettings, SeasonSettingsDataAccessLayer>();
            // Memory Cache
            services.AddMemoryCache();
            
            // Services
            services.AddTransient<RotisserieStandingsService>();
            services.AddTransient<ServerSideStandingsCache>();

            // SignalR
            services.AddSignalR();

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
            
            app.UseCors(); // Enable CORS middleware

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapControllers();
                endpoints.MapHub<WFBC.Server.Hubs.ProgressHub>("/progressHub");
                endpoints.MapFallbackToFile("index.html");
            });
        }
    }
}
