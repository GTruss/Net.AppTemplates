using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Net5.Web.Api.Controllers {
    [ApiVersion("2.0")]
    [ApiVersion("3.0")]
    public partial class WeatherForecastController : ControllerBase {

        [HttpGet]
        [MapToApiVersion("2.0")]
        [MapToApiVersion("3.0")]
        public IEnumerable<WeatherForecast> Get_V2_0() {
            var rng = new Random();
            _logger.LogInformation("Get Forecast called {dbl}", rng.NextDouble());
            return Enumerable.Range(1, 10).Select(index => new WeatherForecast {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = rng.Next(100, 110),
                Summary = Summaries[rng.Next(Summaries.Length)]
            })
            .ToArray();
        }

    }
}
