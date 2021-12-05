using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Serilog;
using App.Data.Sandbox;
using App.SharedKernel.Interfaces;
using App.Infrastructure.Data;
using MediatR;

namespace App.Infrastructure {
    public static class StartupSetup {
        public static void AddSQLServerDbContext(this IServiceCollection services, string connectionString) {
            services.AddDbContext<SandboxContext>(options =>
                options.UseSqlServer(connectionString));
        }

        public static void RegisterInfrastructureDependencies(this IServiceCollection services) {
            services.AddTransient(typeof(IRepository<>), typeof(EfRepository<>));
            services.AddTransient<ISandboxRepository, SandboxRepository>();

            //services.AddTransient<IMediator, Mediator>();

        }
    }
}
