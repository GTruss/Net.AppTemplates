using Ardalis.ListStartupServices;

using Autofac;

using App.Core;
using App.Infrastructure;
using App.SharedKernel.Logging.Serilog;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

using System.Collections.Generic;
using Serilog.Formatting.Json;
using Serilog.Sinks.MSSqlServer;
using Serilog;
using System.Configuration;
using System;
using Microsoft.Extensions.Logging;
using System.IO;
using Serilog.Context;

namespace App.Web {
    public class Startup {
        private readonly IWebHostEnvironment _env;
        private ILogger<Startup> _logger;
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration config, IWebHostEnvironment env) {
            Configuration = config;
            _env = env;

            _ = new ConfigurationBuilder()
                 .SetBasePath(env.ContentRootPath)
                 .AddJsonFile("appSettings.json", optional: false, reloadOnChange: true)
                 .AddJsonFile($"appSettings.{env.EnvironmentName}.json", optional: false, reloadOnChange: true)
                 .AddEnvironmentVariables()
                 .Build();

            string logFileName = AppDomain.CurrentDomain.BaseDirectory + @$"\logs\LogFile_{ DateTime.Now:yyyyMMdd_hhmmss}.log";
            StartupSetup.ConfigureLogger(Configuration, logFileName);

        }

        public void ConfigureServices(IServiceCollection services) {
            services.Configure<CookiePolicyOptions>(options => {
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.AddSQLServerDbContext(Configuration.GetConnectionString("SQLServerConnection"));
            //services.AddSQLiteDbContext(Configuration.GetConnectionString("SqliteConnection"));

            services.AddControllersWithViews().AddNewtonsoftJson();
            services.AddRazorPages();

            services.AddSwaggerGen(c => {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });
                c.EnableAnnotations();
            });

            // add list services for diagnostic purposes - see https://github.com/ardalis/AspNetCoreStartupServices
            services.Configure<ServiceConfig>(config => {
                config.Services = new List<ServiceDescriptor>(services);

                // optional - default path to view services is /listservices - recommended to choose your own path
                config.Path = "/listservices";
            });
        }

        public void ConfigureContainer(ContainerBuilder builder) {
            builder.RegisterModule(new DefaultCoreModule());
            builder.RegisterModule(new DefaultInfrastructureModule(_env.EnvironmentName == "Development"));
        }


        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger) {
            _logger = logger;

            string envstr = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            Log.ForContext<Startup>()
               .ForContext("Environment", envstr)
               .Information("Environment: {env}", envstr);

            CleanupExpiredLogs();

            if (string.IsNullOrEmpty(envstr) ||
                !envstr.ToLower().Contains("development") &&
                !envstr.ToLower().Contains("staging") &&
                !envstr.ToLower().Contains("production")) {
                _logger.LogError("Missing/Unexpected environment variable: ASPNETCORE_ENVIRONMENT = {env} (expected 'Development', 'Staging' or 'Production').", envstr);
                // Microsoft's default is to "fallback" to Production if the ASPNETCORE_ENVIRONMENT environment
                // variable doesn't exist. This is bad. If it's a manual deployment to a new Development or Staging
                // environment and they forget to add the Environment Variable, it will run the Production
                // configuration BY DEFAULT! Bad Microsoft. Bad. People make mistakes. Err on the side of caution.
                // So, require that the Environment Variable to be set AND it must be one of the three.               
                return;
            }

            if (env.EnvironmentName == "Development") {
                app.UseDeveloperExceptionPage();
                app.UseShowAllServicesMiddleware();
            }
            else {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }
            app.UseRouting();

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();

            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger();

            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.), specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1"));

            app.UseEndpoints(endpoints => {
                endpoints.MapDefaultControllerRoute();
                endpoints.MapRazorPages();
            });
        }

        private void CleanupExpiredLogs() {
            try {
                LogArchive logSettings = new();
                Configuration.Bind("LogArchive", logSettings);

                Log.ForContext<Startup>() 
                   .ForContext("LogArchive", logSettings) 
                   .Information("CleanupExpiredLogs()");

                string path = AppDomain.CurrentDomain.BaseDirectory;
                path += @"\logs";

                var files = Directory.GetFiles(path);
                foreach (var fileName in files) {
                    var file = new FileInfo(fileName);
                    DateTime threshold = DateTime.Now;

                    threshold = logSettings.Expiration.Interval.ToLower() switch {
                        "minutes" => DateTime.Now.AddMinutes(-logSettings.Expiration.IntervalCount),
                        "days" => DateTime.Now.AddDays(-logSettings.Expiration.IntervalCount),
                        _ => throw new NotImplementedException()
                    };

                    if (file.CreationTime < threshold) {
                        file.Delete();
                    }
                }
            }
            catch (Exception ex) {
                _logger.LogError(ex, ex.Message);                
                return;
            }
        }
    }

    public record LogArchive {
        public ExpirationSettings Expiration { get; set; }

        public record ExpirationSettings {
            public string Interval { get; set; }
            public int IntervalCount { get; set; }
        }
    }
}
