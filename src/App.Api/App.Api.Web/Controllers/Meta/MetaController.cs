using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

using Swashbuckle.AspNetCore.Annotations;

using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace App.Api.Web {
    [ApiController]
    [ApiExplorerSettings(GroupName = "v3.1")]
    [ApiVersion("3.1")]
    public class MetaController : ControllerBase {
        private readonly HealthCheckService _healthCheckService;

        public MetaController(HealthCheckService healthCheckService) {
            _healthCheckService = healthCheckService;
        }

        [SwaggerOperation(
            Summary = "Gets information about the API",
            Description = "Gets information about the API.",
            OperationId = "Meta.Info",
            Tags = new[] { "Meta" })
        ]
        [HttpGet("/info")]
        public ActionResult<string> Info() {
            var assembly = typeof(Startup).Assembly;

            var creationDate = System.IO.File.GetCreationTime(assembly.Location);
            var version = FileVersionInfo.GetVersionInfo(assembly.Location).ProductVersion;

            return Ok($"Version: {version}, Last Updated: {creationDate}");
        }

        [SwaggerOperation(
            Summary = "Returns the health of the API & Services.",
            Description = "Returns the health of the API & Services.",
            OperationId = "Meta.Health",
            Tags = new[] { "Meta" })
        ]
        [HttpGet("/health")]
        public async Task<IActionResult> Health() {
            var report = await _healthCheckService.CheckHealthAsync();

            var response = new {
                Status = report.Status.ToString(),
                HealthChecks = report.Entries.Select(x => new {
                    Component = x.Key,
                    Status = x.Value.Status.ToString(),
                    Description = x.Value.Description
                }),
                HealthCheckDuration = report.TotalDuration
            };

            return report.Status == HealthStatus.Healthy ? Ok(response) : StatusCode((int)HttpStatusCode.ServiceUnavailable, report);
        }
    }
}
