using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

// DI, Serilog, Settings

namespace Net5.ConsoleAppBase {
    public class MainService {
        private readonly ILogger<MainService> _log;
        private readonly IConfiguration _config;

        public MainService(ILogger<MainService> log, IConfiguration config) {
            _log = log;
            _config = config;
        }

        public void Run() {
            // Main driver for this application..
            _log.LogInformation("Cleaning up expired logs");

            int FirstValue = 1;

            // Read from appsettings
            int SecondValue = _config.GetValue<int>("SecondValue");

            // Do some logging...
            _log.LogInformation("Logging first value {FirstValue}", FirstValue);
            _log.LogInformation("Logging second value {SecondValue}", SecondValue);

        }
    }
}
