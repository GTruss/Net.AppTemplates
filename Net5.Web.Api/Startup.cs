using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;

using Serilog;

namespace Net5.Web.Api {
    public class Startup {
        private ILogger<Startup> _logger;

        public Startup(IConfiguration configuration, IHostEnvironment env) {

            var builder = new ConfigurationBuilder().SetBasePath(env.ContentRootPath)
                .AddJsonFile("appSettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appSettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();

            this.Configuration = builder.Build();
            Configuration = configuration;
            CleanupExpiredLogs();

        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services) {

            services.AddControllers();
            services.AddSwaggerGen(c => {
                c.SwaggerDoc("v2.0", new OpenApiInfo { Title = "Net5.Web.Api v2.0", Version = "v2.0" });
                c.SwaggerDoc("v1.1", new OpenApiInfo { Title = "Net5.Web.Api v1.1", Version = "v1.1" });
                c.SwaggerDoc("v1.0", new OpenApiInfo { Title = "Net5.Web.Api v1.0", Version = "v1.0", Description = "Deprecated" });
                c.DocInclusionPredicate((docName, apiDesc) => {
                    var actionApiVersionModel = apiDesc.ActionDescriptor?.GetApiVersion();
                    // would mean this action is unversioned and should be included everywhere
                    if (actionApiVersionModel == null) {
                        return true;
                    }
                    if (actionApiVersionModel.DeclaredApiVersions.Any()) {
                        return actionApiVersionModel.DeclaredApiVersions.Any(v => $"v{v}" == docName);
                    }
                    return actionApiVersionModel.ImplementedApiVersions.Any(v => $"v{v}" == docName);
                });
            });

            services.AddApiVersioning(o => {
                o.DefaultApiVersion = new ApiVersion(2, 0);
                o.AssumeDefaultVersionWhenUnspecified = true;
                o.ApiVersionReader = new MediaTypeApiVersionReader();
            });
            //services.AddApiVersioning(o => o.ApiVersionReader = new HeaderApiVersionReader("x-api-version"));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger) {
            _logger = logger;
            string envstr = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

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


            if (env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => {
                    c.SwaggerEndpoint("/swagger/v2.0/swagger.json", "Net5.Web.Api v2.0");
                    c.SwaggerEndpoint("/swagger/v1.1/swagger.json", "Net5.Web.Api v1.1");
                    c.SwaggerEndpoint("/swagger/v1.0/swagger.json", "Net5.Web.Api v1.0");
                });
            }
            app.UseSerilogRequestLogging();

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints => {
                endpoints.MapControllers();
            });
        }

        private void CleanupExpiredLogs() {
            try {
                LogArchive logSettings = new();
                Configuration.Bind("LogArchive", logSettings);

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
            catch {
                //The log file directory doesn't exist yet, ignore
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
