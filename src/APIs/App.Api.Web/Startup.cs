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
using Microsoft.Extensions.Options;
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
using App.Api.Web.Helpers;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using System.Reflection;
using Serilog.Exceptions;

namespace App.Api.Web;

public class Startup {
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _env;
    private ILogger<Startup> _logger;
    private InMemorySink _memSink;

    public Startup(IConfiguration configuration, IHostEnvironment env) {
        _configuration = configuration;
        _env = env;

        _ = new ConfigurationBuilder().SetBasePath(env.ContentRootPath)
            .AddJsonFile("appSettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appSettings.{env.EnvironmentName}.json", optional: false, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        string logFileName = AppDomain.CurrentDomain.BaseDirectory + @$"\logs\LogFile_{ DateTime.Now:yyyyMMdd_hhmmss}.log";
        var name = Assembly.GetExecutingAssembly().GetName();
        _memSink = new InMemorySink("[{Timestamp:HH:mm:ss.fff} ({EventType}) {Level:u3}] ({SourceContext}) {Message:lj}{NewLine}{Exception}");

        Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithExceptionDetails()
                .Enrich.WithProperty("Assembly", name.Name)
                .Enrich.WithProperty("Version", name.Version.ToString())
                .Enrich.With<EventTypeEnricher>()
                .Enrich.With<SourceContextClassEnricher>()
                .Enrich.With<ApplicationNameColumnEnricher>()
                .WriteTo.Sink(_memSink)
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
            c.OperationFilter<SwaggerDefaultValues>();
        });
        services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
        
        services.AddApiVersioning(o => {
            o.DefaultApiVersion = new ApiVersion(3, 1);
            o.AssumeDefaultVersionWhenUnspecified = true;
            o.ReportApiVersions = true;
        });

        services.AddVersionedApiExplorer(options => {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        // Use envstr here if DI instancing is needed per environment
        string envstr = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

        // Define Dependency Injected Services
        services.AddSQLServerDbContext(_configuration.GetConnectionString("Sandbox"));

        services.AddHealthChecksUI(setupSettings: setup => {
                    setup.AddHealthCheckEndpoint("App.Api.Web", "http://localhost:31381/healthui");
                    setup.SetEvaluationTimeInSeconds(120);
                })
                .AddInMemoryStorage();

        services.AddHealthChecks()
            .AddUrlGroup(new Uri("http://localhost:31381/api/v3.1/MainService"),
                         name: "Main Service",                             
                         tags: new string[] { "api", "controller" })
            .AddUrlGroup(new Uri("http://localhost:31381/api/v3.1/WeatherForecast"),
                         name: "Weather Forecast",
                         tags: new string[] { "api", "controller"})
            .AddSqlServer(_configuration.GetConnectionString("Sandbox"),
                         name: "Sandbox",
                         tags: new string[] { "db", "sql", "sqlserver" });

        services.AddCors(options => {
            options.AddPolicy(name: "MyAllowAllOrigins", 
                builder => {
                    builder.AllowAnyOrigin();
                    builder.AllowAnyMethod();
                    builder.AllowAnyHeader();
                });
        });
    }

    public void ConfigureContainer(ContainerBuilder builder) {
        if (_memSink is not null) {
            builder.RegisterInstance<InMemorySink>(_memSink);
        }

        builder.RegisterModule(new DefaultServicesModule());
        builder.RegisterModule(new DefaultInfrastructureModule(_env.EnvironmentName == "Development"));
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger,
                          IApiVersionDescriptionProvider provider) {
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
            app.UseCors("MyAllowAllOrigins");
            app.UseDeveloperExceptionPage();
            app.UseSwagger();
            app.UseSwaggerUI(c => {
                foreach (var description in provider.ApiVersionDescriptions) {
                    c.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json", description.GroupName);
                }
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
