using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Extensions.Logging;

namespace App.Api.Web.Controllers; 

[ApiController]
[Route("api/v{version:apiVersion}/weatherforecast")]
[ApiVersion("1.0", Deprecated = true)]
[ApiVersion("1.1-beta", Deprecated = true)]
//[Produces("application/vnd.test+json")]
public partial class WeatherForecastController : ControllerBase {
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    private readonly ILogger<WeatherForecastController> _logger;

    public WeatherForecastController(ILogger<WeatherForecastController> logger) {
        _logger = logger;
    }

    [HttpGet]
    [MapToApiVersion("1.0")]
    public IEnumerable<WeatherForecast> Get() {
        var rng = new Random();
        _logger.LogInformation("Get Forecast called {dbl}", rng.NextDouble());
        return Enumerable.Range(1, 5).Select(index => new WeatherForecast {
            Date = DateTime.Now.AddDays(index),
            TemperatureC = rng.Next(-20, 55),
            Summary = Summaries[rng.Next(Summaries.Length)]
        })
        .ToArray();
    }

    [HttpGet]
    // Example of Versioning specific HTTP Requests
    [MapToApiVersion("1.1-beta")]
    public IEnumerable<WeatherForecast> Get_V1_1() {
        var rng = new Random();
        _logger.LogInformation("Get Forecast called {dbl}", rng.NextDouble());
        return Enumerable.Range(1, 5).Select(index => new WeatherForecast {
            Date = DateTime.Now.AddDays(index),
            TemperatureC = rng.Next(60, 80),
            Summary = Summaries[rng.Next(Summaries.Length)]
        })
        .ToArray();
    }
}
