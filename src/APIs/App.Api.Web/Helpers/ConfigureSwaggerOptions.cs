using System;

using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;

using Swashbuckle.AspNetCore.SwaggerGen;


namespace App.Api.Web.Helpers;

public class ConfigureSwaggerOptions : IConfigureOptions<SwaggerGenOptions> {
    private readonly IApiVersionDescriptionProvider _provider;

    public ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider) => _provider = provider;

    public void Configure(SwaggerGenOptions options) {
        foreach (var description in _provider.ApiVersionDescriptions)
            options.SwaggerDoc(description.GroupName, CreateInfoForApiVersion(description));
    }

    private static OpenApiInfo CreateInfoForApiVersion(ApiVersionDescription description) {
        var info = new OpenApiInfo {
            Title = "App.Api.Web",
            Version = description.ApiVersion.ToString(),
            Description = "A sample Web API project.",
            Contact = new OpenApiContact {
                Name = "IT Department",
                Email = "developer@noone.xyz",
                Url = new Uri("https://github.com")
            }
        };

        if (description.IsDeprecated)
            info.Description += "<div><strong>This API version has been deprecated.</strong></div>";

        return info;
    }

}
