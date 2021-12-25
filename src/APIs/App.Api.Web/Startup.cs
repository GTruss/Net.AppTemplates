using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;

using Newtonsoft.Json;

using Serilog;
using Serilog.Formatting.Json;
using Serilog.Sinks.MSSqlServer;
using My.Shared.Logging.Serilog;
using App.Services;
using App.Api.Web.Middleware;
using App.Infrastructure;
using System.Configuration;
using Autofac;
using App.Core;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using HealthChecks.UI.Client;

namespace App.Api.Web {
    public class Startup {
        private readonly IConfiguration _configuration;
        private readonly IHostEnvironment _env;
        private ILogger<Startup> _logger;

        public Startup(IConfiguration configuration, IHostEnvironment env) {
            _configuration = configuration;
            _env = env;

            _ = new ConfigurationBuilder().SetBasePath(env.ContentRootPath)
                .AddJsonFile("appSettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appSettings.{env.EnvironmentName}.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            string logFileName = AppDomain.CurrentDomain.BaseDirectory + @$"\logs\LogFile_{ DateTime.Now:yyyyMMdd_hhmmss}.log";
            Log.Logger = new LoggerConfiguration()
                    .ReadFrom.Configuration(configuration)
                    .Enrich.FromLogContext()
                    .Enrich.WithMachineName()
                    .Enrich.With<EventTypeEnricher>()
                    .Enrich.With<SourceContextClassEnricher>()
                    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss.fff} ({EventType}) {Level:u3}] ({SourceContext}) {Message:lj}{NewLine}{Exception}")
                    .WriteTo.File(logFileName, outputTemplate: "[{Timestamp:HH:mm:ss.fff} ({EventType}) {Level:u3}] ({SourceContext}) {Message:lj}{NewLine}{Exception}")
                    .WriteTo.File(new JsonFormatter(), logFileName + ".json")
                    .WriteTo.MSSqlServer(
                        configuration.GetConnectionString("Sandbox"),
                        sinkOptions: new MSSqlServerSinkOptions() {
                            AutoCreateSqlTable = true,
                            TableName = "Log",
                            SchemaName = "dbo"
                        },
                        appConfiguration: configuration,
                        columnOptionsSection: configuration.GetSection("Serilog:ColumnOptions"),
                        logEventFormatter: new JsonFormatter()
                    )
                    .CreateLogger();
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services) {
            services.Configure<CookiePolicyOptions>(options => {
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.AddControllers().AddNewtonsoftJson();
            services.AddSwaggerGen(c => {
                // Added Meta controller
                c.SwaggerDoc("v3.1", new OpenApiInfo { Title = "App.Api.Web", Version = "v3.1", Description = "A sample Web API project." });
                // Added MainService controller
                c.SwaggerDoc("v3.0", new OpenApiInfo { Title = "App.Api.Web", Version = "v3.0", Description = "Now supports the MainService." });
                // Weather Forecaster v2
                c.SwaggerDoc("v2.0", new OpenApiInfo { Title = "App.Api.Web", Version = "v2.0", Description = "Returns 10 forecasts" });
                // Bug fixes
                c.SwaggerDoc("v1.1", new OpenApiInfo { Title = "App.Api.Web", Version = "v1.1", Description = "Returns higher range of 5 forecasts." });
                // Initial release
                c.SwaggerDoc("v1.0", new OpenApiInfo { Title = "App.Api.Web", Version = "v1.0", Description = "Returns 5 forecasts. Deprecated." });
                c.EnableAnnotations();
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

            /////////////////////
            // The following block implements Request Header Versioning.
            //
            // Send on the request header:
            //      x-api-version = {version}
            //
            // To use version 3.0 of the API, for example:
            //      x-api-version = 3.0
            // 
            // This directly corresponds to the Controller Attribute:
            //      [ApiVersion("3.0")]
            //      
            // And Method Attribute:
            //      [MapToApiVersion("3.0")]
            //
            // This allows each Controller and Method to be wired up independently for versioning.
            services.AddApiVersioning(o => {
                o.DefaultApiVersion = new ApiVersion(3, 1);
                o.AssumeDefaultVersionWhenUnspecified = true;
                o.ApiVersionReader = new HeaderApiVersionReader("x-api-version"); // Use Request Header versioning
            });
            // Alternatively, can use Media Type versioning instead of Header versioning: 
            // eg, 
            //  Decorate the Controller with the following attribute:
            //     [Produces("application/vnd.test+json")]
            //
            //  All of the requests that return JSON can indicate a specific version number in the Request Header:
            //      Accept = application/json;v=2.0
            //
            // Can also be used with multiple media types, including custom.
            //
            // To implement, replace:
            //      o.ApiVersionReader = new HeaderApiVersionReader("x-api-version");
            //
            // with:
            //      o.ApiVersionReader = new MediaTypeApiVersionReader();
            //
            // 
            // Another alternative is to use Query String Versioning, if preferred:
            //
            //      o.ApiVersionReader = new QueryStringApiVersionReader("v")
            //
            // eg,
            //      mysite.com/api/customers?v=3.0
            //
            //
            // Note that URL versioning is NOT recommended as it breaks the "Cool URLs don't change" guideline by Tim Berners-Lee and
            // can be very difficult to maintain and support with many releases.
            // 
            /////////////////////


            // Use envstr here if DI instancing is needed per environment
            string envstr = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            // Define Dependency Injected Services
            //services.AddTransient<MainService>();
            services.AddSQLServerDbContext(_configuration.GetConnectionString("Sandbox"));

            services.AddHealthChecksUI(setupSettings: setup => {
                        setup.AddHealthCheckEndpoint("App.Api.Web", "http://localhost:31381/healthui");
                        setup.SetEvaluationTimeInSeconds(120);
                    })
                    .AddInMemoryStorage();

            services.AddHealthChecks()
                .AddUrlGroup(new Uri("http://localhost:31381/api/MainService"),
                             name: "Main Service",                             
                             tags: new string[] { "api", "controller" })
                .AddUrlGroup(new Uri("http://localhost:31381/api/WeatherForecast"),
                             name: "Weather Forecast",
                             tags: new string[] { "api", "controller"})
                .AddSqlServer(_configuration.GetConnectionString("Sandbox"),
                             name: "Sandbox",
                             tags: new string[] { "db", "sql", "sqlserver" });
        }

        public void ConfigureContainer(ContainerBuilder builder) {
            builder.RegisterModule(new DefaultServicesModule());
            builder.RegisterModule(new DefaultInfrastructureModule(_env.EnvironmentName == "Development"));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
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

            if (env.IsDevelopment() || envstr == "Staging") {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => {
                    c.SwaggerEndpoint("/swagger/v3.1/swagger.json", "App.Api.Web v3.1");
                    c.SwaggerEndpoint("/swagger/v3.0/swagger.json", "App.Api.Web v3.0");
                    c.SwaggerEndpoint("/swagger/v2.0/swagger.json", "App.Api.Web v2.0");
                    c.SwaggerEndpoint("/swagger/v1.1/swagger.json", "App.Api.Web v1.1");
                    c.SwaggerEndpoint("/swagger/v1.0/swagger.json", "App.Api.Web v1.0");
                });
            }
            // Custom Middleware Injection
            // Logs the x-api-version Request Header
            app.UseHeaderMiddleware();

            app.UseSerilogRequestLogging();
            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthorization();
            app.UseStaticFiles();
            app.UseCookiePolicy();

            app.UseEndpoints(endpoints => {
                endpoints.MapControllers();
                endpoints.MapHealthChecksUI();
                endpoints.MapHealthChecks("/healthui", new HealthCheckOptions {
                     Predicate = _ => true,
                     ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                 });
                endpoints.MapHealthChecks("/health", new() {
                    ResponseWriter = async (context, report) => {
                        context.Response.ContentType = "application/json";
                        var response = new {
                            Status = report.Status.ToString(),
                            HealthChecks = report.Entries.Select(x => new {
                                Component = x.Key,
                                Status = x.Value.Status.ToString(),
                                Description = x.Value.Description
                            }),
                            HealthCheckDuration = report.TotalDuration,
                        };
                        await context.Response.WriteAsync(JsonConvert.SerializeObject(response)).ConfigureAwait(false);
                    }
                });
            });
        }

        private void CleanupExpiredLogs() {
            try {
                LogArchive logSettings = new();
                _configuration.Bind("LogArchive", logSettings);

                Log.ForContext<Startup>()
                   .ForContext("LogArchive", logSettings)
                   .Information("CleanupExpiredLogs()");

                _logger.LogInformation("LogArchive={logSettings}", logSettings);

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
                Log.Error(ex, ex.Message);
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
