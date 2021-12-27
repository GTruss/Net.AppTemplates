using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using App.Services;
using My.Shared.Logging.Serilog;

namespace App.Api.Web.Controllers {
    [Route("api/[controller]")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "v3.0")]
    [ApiVersion("3.0")]
    [ApiVersion("3.1")]
    public partial class MainServiceController : ControllerBase {
        private readonly ILogger<MainServiceController> _logger;
        private readonly MainService _mainService;
        private readonly InMemorySink _memSink;

        private List<string> _logEntries = new List<string>();

        public MainServiceController(ILogger<MainServiceController> logger, 
                                     InMemorySink memSink, 
                                     MainService mainService) {
            _logger = logger;
            _mainService = mainService;
            _memSink = memSink;

            _memSink.Events.Enqueued += LogEvents_Enqueued;
        }

        [HttpGet]
        public async Task<ActionResult> Get() {
            _logger.LogInformation("Get() called");
            var response = new {
                Status = "OK"
            };
            _logger.LogInformation("Get() complete");

            var detail = new ResponseModel(_logEntries);
            return Ok(JsonConvert.SerializeObject(detail));
        }

        [HttpPost]
        [MapToApiVersion("3.1")]
        public async Task<ActionResult> Post() {
            try {
                _logger.LogInformation("Running MainService...");
                await _mainService.Run().ConfigureAwait(false);
                _logger.LogInformation("MainService finished.");

                var detail = new ResponseModel(_logEntries);
                return Ok(JsonConvert.SerializeObject(detail));
            }
            catch (Exception ex) {
                _logger.LogCritical(ex, "There was an unexpected error while running the main service.");

                var detail = new ResponseModel(_logEntries, ex);
                return Problem(JsonConvert.SerializeObject(detail), 
                    "MainService", (int)HttpStatusCode.InternalServerError,
                    "There was an unexpected error while running the main service.", ex.GetType().ToString());
            }
        }

        #region Events
        private void LogEvents_Enqueued(object sender, LogEventQueueArgs e) {
            _logEntries.Add(e.Message.Replace("\r\n", ""));
            //_logEntries.Add(JsonConvert.SerializeObject(e.LogEvent));
        }
        #endregion
    }

    public record ResponseModel(List<string> log, Exception error = null);
}
