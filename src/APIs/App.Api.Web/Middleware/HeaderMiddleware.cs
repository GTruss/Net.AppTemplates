using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace App.Api.Web.Middleware;

/// <summary>
/// Logs the x-api-version Request Header
/// </summary>
public class HeaderMiddleware {
    private readonly RequestDelegate _next;
    private readonly ILogger _logger;

    public HeaderMiddleware(RequestDelegate next, ILogger<HeaderMiddleware> logger) {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context) {

        if (!context.Request.Path.Value.ToLower().Contains("swagger")) {
            context.Request.Headers.TryGetValue("x-api-version", out var versionNumber);

            string ver = !string.IsNullOrEmpty(versionNumber) ? versionNumber : "Not sent; using DefaultApiVersion from Startup.";
            _logger.LogInformation("Incoming Request called with API Version: {version}", ver);
        }

        await _next.Invoke(context);
    }
}
