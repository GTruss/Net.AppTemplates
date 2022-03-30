using System;
using System.Windows.Forms;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using My.Shared.Logging.Serilog;
using App.Services;
using App.SharedKernel.Interfaces;
using App.Data.Models;
using App.Data.Sandbox;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using App.Infrastructure.Data;

namespace App.Win; 

public partial class MainForm : Form {
    private readonly IServiceScope _scope;
    private readonly ILogger<MainForm> _logger;
    private readonly IConfiguration _configuration;
    private readonly LogEventQueue _logEvents;
    private readonly LogEventQueue _logEventsFlat;

    private string _errorCode = string.Empty;

    public MainForm(IServiceScope scope, ILogger<MainForm> logger, IConfiguration configuration, 
                    LogEventQueue logEvents, LogEventQueue logEventsFlat) {
        _scope = scope;
        _logger = logger;
        _configuration = configuration;
        _logEvents = logEvents;
        _logEventsFlat = logEventsFlat;
    
        InitializeComponent();
    }

    private void MainForm_Load(object sender, EventArgs e) {
        _logEvents.Enqueued += _logEvents_Enqueued;
        _logEventsFlat.Enqueued += _logEventsFlat_Enqueued;
    }

    delegate void SetTextCallback(object? sender, LogEventQueueArgs e);

    private void _logEvents_Enqueued(object? sender, LogEventQueueArgs e) {
        if (txtLog.InvokeRequired) {
            SetTextCallback d = new SetTextCallback(_logEvents_Enqueued);
            this.BeginInvoke(d, new object[] { sender, e });
            return;
        }
        txtLog.AppendText(e.Message);
    }

    private void _logEventsFlat_Enqueued(object? sender, LogEventQueueArgs e) {
        var items = e.Message.Split('|');
        if (items[2] == "ERR") {
            _errorCode = items[1];
        }
        else {
            _errorCode = string.Empty;
        }
    }

    private void btnRun_Click(object sender, EventArgs e) {
        try {
            using (var scope = _scope.ServiceProvider.CreateScope()) {
                var svc = scope.ServiceProvider.GetRequiredService<MainService>();
                svc.Run().GetAwaiter().GetResult();                   
            };
        }
        catch (Exception ex) {
            _logger.LogError(ex, ex.Message);
            if (!string.IsNullOrEmpty(_errorCode)) {
                MessageBox.Show($"An error has occured. Please contact the Help Desk with reference to the following error code: {_errorCode}.");
            }
        }
    }
}
