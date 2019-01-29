using AutoMapper;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using MyCodeCamp.Data;
using MyCodeCamp.Data.Entities;
using Newtonsoft.Json;
using System.Text;
using System.Threading.Tasks;

namespace MyCodeCamp
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();

            _env = env;
            _config = builder.Build();
        }

        private IConfigurationRoot _config;
        private IHostingEnvironment _env;

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(_config);                             
            services.AddDbContext<CampContext>(ServiceLifetime.Scoped); 
            services.AddScoped<ICampRepository, CampRepository>();
            services.AddTransient<CampDbInitializer>();
            services.AddTransient<CampIdentityInitializer>();

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddAutoMapper();


            // Add aspNetCore Identity to the services collection
            services.AddIdentity<CampUser, IdentityRole>()
                .AddEntityFrameworkStores<CampContext>();

            // configure Identity options
            services.Configure<IdentityOptions>(config =>
            {
                config.Cookies.ApplicationCookie.Events = new CookieAuthenticationEvents()
                {
                    OnRedirectToLogin = (ctx) =>
                    {
                        if (ctx.Request.Path.StartsWithSegments("/api") && ctx.Response.StatusCode == 200)
                        {
                            ctx.Response.StatusCode = 401;
                        }

                        return Task.CompletedTask;
                    },
                    OnRedirectToAccessDenied = (ctx) =>
                    {
                        if (ctx.Request.Path.StartsWithSegments("/api") && ctx.Response.StatusCode == 200)
                        {
                            ctx.Response.StatusCode = 403;
                        }

                        return Task.CompletedTask;
                    }
                };
            });


            // 
            //services.AddApiVersioning(cfg => 
            //{
            //    cfg.DefaultApiVersion = new ApiVersion(1, 1);
            //    cfg.AssumeDefaultVersionWhenUnspecified = true;
            //    cfg.ReportApiVersions = true;
            //});


            // add CORS to the services collection. CORS Policies
            services.AddCors(cfg =>
            {
                // This policy only allow wildermuth policy
                cfg.AddPolicy("Wildermuth", bldr =>
                {
                    bldr.AllowAnyHeader()
                    .AllowAnyMethod()
                    .WithOrigins("http://wildermuth.com");
                });
                // allow anyone to read our api but will not be ble to perform Put, Post, Delete
                cfg.AddPolicy("AnyGET", bldr =>
                {
                    bldr.AllowAnyHeader()
                    .WithMethods("GET")
                    .AllowAnyOrigin();
                });

            });


            // Add Authorization policies to services collection. This is about authorizing certain users not authenticating them
            services.AddAuthorization(cfg =>
            {
                cfg.AddPolicy("SuperUsers", p => p.RequireClaim("SuperUser", "True"));
            });


            // Add framework services.
            services.AddMvc(opt => 
            {
                if (!_env.IsProduction())
                {
                    opt.SslPort = 44318;
                }
                opt.Filters.Add(new RequireHttpsAttribute());  // add support for Https using global filters
            })
                .AddJsonOptions(opt =>
                {
                    // AddJsonOptions fluent syntax let us control how serialization works for generated Json data from the API call
                    opt.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                }); 
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, CampDbInitializer seeder,
                              CampIdentityInitializer identitySeeder)
        {
            loggerFactory.AddConsole(_config.GetSection("Logging"));
            loggerFactory.AddDebug();

            //app.UseCors(cfg => 
            //{  // This will allow anyone to access your api
            //    cfg.AllowAnyHeader()
            //       .AllowAnyMethod()
            //       .AllowAnyOrigin();
            //     //.WithOrigins("http://wildermuth.com"); will only allow this url
            //});


            
            app.UseIdentity();    // here we are using the Identity service. note: this must be before UseMvc

            // use the middleware for security tokens            
            app.UseJwtBearerAuthentication(new JwtBearerOptions()
            {
                AutomaticAuthenticate = true,
                AutomaticChallenge = true,
                TokenValidationParameters = new TokenValidationParameters()
                {
                    ValidIssuer = _config["Tokens:Issuer"],
                    ValidAudience = _config["Tokens:Audience"],
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Tokens:Key"])),
                    ValidateLifetime = true
                }
            });


            app.UseMvc();

            seeder.Seed().Wait();
            identitySeeder.Seed().Wait();
        }

        // 


    }
}
