using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using App.Data.Sandbox;
using App.SharedKernel.Interfaces;
using App.Infrastructure.Data;

namespace App.Infrastructure;

public static class StartupSetup {
    public static void AddSQLServerDbContext(this IServiceCollection services, string connectionString) {
        services.AddDbContext<SandboxContext>(options =>
            options.UseSqlServer(connectionString));
    }
}
