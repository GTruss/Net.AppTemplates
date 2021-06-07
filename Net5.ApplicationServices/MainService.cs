using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Serilog.Context;

namespace Net5.Cli {
    public class MainService {
        private readonly ILogger<MainService> _log;
        private readonly IConfiguration _config;
        // private readonly YourDataContext _db;

        //public MainService(ILogger<MainService> log, IConfiguration config, YourDataContext db) {
        public MainService(ILogger<MainService> log, IConfiguration config) {
            _log = log;
            _config = config;
            //_db = db;
        }

        public void Run() {
            // Main driver for this application..
            _log.LogDebug("MainService.Run()");

            int FirstValue = 1;

            // Read from appsettings
            int SecondValue = _config.GetValue<int>("SecondValue");

            // Do some logging...
            _log.LogInformation("Logging first value {FirstValue}", FirstValue);

            // Logging within a "context"
            using (LogContext.PushProperty("FirstValue", FirstValue)) {
                _log.LogInformation("Logging second value {SecondValue}", SecondValue);
            }

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

            _log.LogWarning("Danger. A fatal exception is about to occur.");

            throw new Exception("A critical error has occured.");

        }
    }
}
