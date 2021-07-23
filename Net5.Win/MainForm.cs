using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Net5.Cli;
using Net5.Common.Serilog;

using static System.Formats.Asn1.AsnWriter;

namespace Net5.Win {
    public partial class MainForm : Form {
        private readonly IServiceScope _scope;
        private readonly ILogger<MainForm> _logger;
        private readonly IConfiguration _configuration;
        private readonly LogEventQueue<string> _logEvents;

        public MainForm(IServiceScope scope, ILogger<MainForm> logger, IConfiguration configuration, LogEventQueue<string> logEvents) {
            _scope = scope;
            _logger = logger;
            _configuration = configuration;
            _logEvents = logEvents;
        
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e) {
            _logEvents.Enqueued += _logEvents_Enqueued;
        }

        private void _logEvents_Enqueued(object? sender, LogEventQueueArgs e) {
            txtLog.AppendText(e.Message);
        }

        private void btnRun_Click(object sender, EventArgs e) {
            try {
                using (var scope = _scope.ServiceProvider.CreateScope()) {
                    var svc = scope.ServiceProvider.GetRequiredService<MainService>();
                    svc.Run();                   
                };
            }
            catch (Exception ex) {
                _logger.LogError(ex, ex.Message);
            }
        }
    }
}
