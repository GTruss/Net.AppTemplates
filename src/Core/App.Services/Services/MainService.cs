using System;
using System.IO;
using System.Linq;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog.Context;

using Serilog;
using App.SharedKernel.Interfaces;
using Models = App.Data.Models;
using System.Threading.Tasks;
using App.Services.Events;
using MediatR;

namespace App.Services {
    public class MainService {
        private readonly ILogger<MainService> _log;
        private readonly IConfiguration _config;
        private readonly IRepository<Models.Log> _logRepo;
        private readonly IMediator _mediator;

        public MainService(ILogger<MainService> log, IConfiguration config, 
            IRepository<Models.Log> logRepo, IMediator mediator) {
            _log = log;
            _config = config;
            _logRepo = logRepo;
            _mediator = mediator;
        }

        public async Task Run() {
            
            // Main driver for this application..
            _log.LogDebug("MainService.Run()");

            int FirstValue = 1;

            // Read from appsettings
            int SecondValue = _config.GetValue<int>("SecondValue");

            // Do some logging...
            _log.LogInformation("Logging first value {FirstValue}", FirstValue);


            // Logging within a "context" with MS Extensions.Logging
            using (LogContext.PushProperty("FirstValue", FirstValue)) 
            using (LogContext.PushProperty("AnotherProperty", "SomeValue")) {
                _log.LogInformation("Logging second value {SecondValue}", SecondValue);
            }

            // Logging within a "context" with Serilog
            Log.ForContext<MainService>() // Sets SourceContext property
               .ForContext("FirstValue", FirstValue) // Creates custom property
               .Information("Logging with Context with Serilog");

            try {
                int i = 0;
                int x = 5 / i;
            }
            catch (Exception ex) {
                _log.LogError(ex, ex.Message);
            }

            // Same EventType (hash)
            int iCount = 10;
            for (int i = 0; i <= 10; i++) {
                _log.LogInformation("Processing {i} of {iCount}", i, iCount);
            }

            
            var count = _logRepo.GetCountAsync().GetAwaiter().GetResult();

            _log.LogInformation("There are {cnt} log entries in the database.", count);

            _log.LogInformation("Sending Mediatr Event...");
            var serviceEvent = new SomeServiceEvent("Test message from the Main Service.");
            _mediator.Publish(serviceEvent).GetAwaiter().GetResult();

            _log.LogWarning("Danger. A fatal exception is about to occur.");

            throw new Exception("A critical error has occured.");

        }
    }
}
