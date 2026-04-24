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
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
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
                // Zitadel puts the project ID in the 'aud' claim, not the client ID
                var projectId = Configuration["Zitadel:ProjectId"];
                var clientId = Configuration["Zitadel:ClientId"];
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = Configuration["Zitadel:Authority"],
                    ValidateAudience = true,
                    ValidAudiences = new[] { projectId, clientId },
                    ValidateLifetime = true
                };
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        if (!string.IsNullOrEmpty(accessToken) &&
                            context.HttpContext.Request.Path.StartsWithSegments("/progressHub"))
                        {
                            context.Token = accessToken;
                        }
                        return System.Threading.Tasks.Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("ZitadelAuth");
                        logger.LogError("JWT authentication failed: {Error}", context.Exception?.Message);
                        return System.Threading.Tasks.Task.CompletedTask;
                    },
                    OnTokenValidated = async context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("ZitadelAuth");
                        var identity = context.Principal?.Identity as System.Security.Claims.ClaimsIdentity;
                        if (identity == null) return;

                        // Check for role claims directly in the access token
                        var rolesClaim = identity.Claims
                            .FirstOrDefault(c => c.Type.StartsWith("urn:zitadel:iam:org:project:") && c.Type.EndsWith(":roles"));

                        // Zitadel may not include role claims in JWT access tokens.
                        // Fall back to the userinfo endpoint with caching to avoid repeated calls.
                        if (rolesClaim == null)
                        {
                            var cache = context.HttpContext.RequestServices.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>();
                            var sub = identity.Claims.FirstOrDefault(c => 
                                c.Type == "sub" || c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                            var cacheKey = $"zitadel_roles_{sub}";

                            // Try cache first
                            if (sub != null && cache.TryGetValue(cacheKey, out object cachedObj) && cachedObj is string cachedRolesJson)
                            {
                                rolesClaim = new System.Security.Claims.Claim("cached_roles", cachedRolesJson);
                            }
                            else
                            {
                                // Fetch from userinfo endpoint
                                try
                                {
                                    var authority = context.Options.Authority?.TrimEnd('/');
                                    var accessToken = context.SecurityToken is Microsoft.IdentityModel.JsonWebTokens.JsonWebToken jwt 
                                        ? jwt.EncodedToken 
                                        : (context.SecurityToken as System.IdentityModel.Tokens.Jwt.JwtSecurityToken)?.RawData;
                                    
                                    if (!string.IsNullOrEmpty(accessToken))
                                    {
                                        var httpClientFactory = context.HttpContext.RequestServices.GetRequiredService<System.Net.Http.IHttpClientFactory>();
                                        var httpClient = httpClientFactory.CreateClient("zitadel");
                                        httpClient.DefaultRequestHeaders.Authorization = 
                                            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                                        var userInfoResponse = await httpClient.GetAsync($"{authority}/oidc/v1/userinfo");
                                        
                                        if (userInfoResponse.IsSuccessStatusCode)
                                        {
                                            var userInfoJson = await userInfoResponse.Content.ReadAsStringAsync();
                                            using var userInfoDoc = JsonDocument.Parse(userInfoJson);
                                            
                                            foreach (var prop in userInfoDoc.RootElement.EnumerateObject())
                                            {
                                                if (prop.Name.StartsWith("urn:zitadel:iam:org:project:") && prop.Name.EndsWith(":roles"))
                                                {
                                                    var rolesJson = prop.Value.GetRawText();
                                                    rolesClaim = new System.Security.Claims.Claim(prop.Name, rolesJson);
                                                    // Cache roles for 30 minutes to avoid repeated userinfo calls
                                                    if (sub != null)
                                                    {
                                                        cache.Set(cacheKey, rolesJson, System.TimeSpan.FromMinutes(30));
                                                    }
                                                    break;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            logger.LogWarning("Zitadel userinfo request failed: {Status}", userInfoResponse.StatusCode);
                                        }
                                    }
                                }
                                catch (System.Exception ex)
                                {
                                    logger.LogError("Error fetching Zitadel userinfo: {Error}", ex.Message);
                                }
                            }
                        }

                        // Parse role claims (from token, userinfo, or cache) into flat claims
                        if (rolesClaim != null)
                        {
                            try
                            {
                                using var doc = JsonDocument.Parse(rolesClaim.Value);
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
                                                    identity.AddClaim(new System.Security.Claims.Claim(property.Name, property.Name));
                                            }
                                        }
                                    }
                                }
                                else if (doc.RootElement.ValueKind == JsonValueKind.Object)
                                {
                                    foreach (var property in doc.RootElement.EnumerateObject())
                                    {
                                        if (addedRoles.Add(property.Name))
                                            identity.AddClaim(new System.Security.Claims.Claim(property.Name, property.Name));
                                    }
                                }
                            }
                            catch (System.Exception ex)
                            {
                                logger.LogError("Error parsing Zitadel role claims: {Error}", ex.Message);
                            }
                        }
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
            services.AddTransient<ISeasonTeam, SeasonTeamDataAccessLayer>();
            services.AddTransient<ICommishSettings, CommishSettingsDataAccessLayer>();
            services.AddTransient<IBoxScore, BoxScoreDataAccessLayer>();
            services.AddTransient<RotowireFetchService>();
            services.AddHttpClient("rotowire").ConfigurePrimaryHttpMessageHandler(() => new System.Net.Http.HttpClientHandler
            {
                AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate | System.Net.DecompressionMethods.Brotli
            });
            // Named HttpClient for Zitadel userinfo calls (reuses connections)
            services.AddHttpClient("zitadel");
            // Memory Cache
            services.AddMemoryCache();
            
            // Services
            services.AddTransient<WagerService>();
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
