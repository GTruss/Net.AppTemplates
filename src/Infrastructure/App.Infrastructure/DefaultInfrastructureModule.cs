using Autofac;



using System.Collections.Generic;
using System.Reflection;

using Module = Autofac.Module;
using MediatR;
using MediatR.Pipeline;
using App.Services;
using App.Data.Models;
using App.Infrastructure.Data;
using App.SharedKernel.Interfaces;
using App.Services.Interfaces;
using My.Shared.Logging.Serilog;

namespace App.Infrastructure {
    public class DefaultInfrastructureModule : Module {
        private readonly bool _isDevelopment = false;
        private readonly List<Assembly> _assemblies = new List<Assembly>();
        private readonly InMemorySink _memSink;

        public DefaultInfrastructureModule(bool isDevelopment, InMemorySink memsink = null, Assembly callingAssembly = null) {
            _isDevelopment = isDevelopment;
            _memSink = memsink;
            var coreAssembly = Assembly.GetAssembly(typeof(Log)); // Any type from your Core project
            var servicesAssembly = Assembly.GetAssembly(typeof(MainService)); // Any type from your Services project
            var infrastructureAssembly = Assembly.GetAssembly(typeof(StartupSetup)); // Any type from your Infrastructure project
            _assemblies.Add(coreAssembly);
            _assemblies.Add(servicesAssembly);
            _assemblies.Add(infrastructureAssembly);
            if (callingAssembly != null) {
                _assemblies.Add(callingAssembly);
            }
        }

        protected override void Load(ContainerBuilder builder) {
            if (_isDevelopment) {
                RegisterDevelopmentOnlyDependencies(builder);
            }
            else {
                RegisterProductionOnlyDependencies(builder);
            }
            RegisterCommonDependencies(builder);
        }

        private void RegisterCommonDependencies(ContainerBuilder builder ) {
            builder.RegisterGeneric(typeof(EfRepository<>))
                .As(typeof(IRepository<>))
                .As(typeof(IReadRepository<>))
                .InstancePerLifetimeScope();

            builder
                .RegisterType<Mediator>()
                .As<IMediator>()
                .InstancePerLifetimeScope();

            builder.Register<ServiceFactory>(context => {
                var c = context.Resolve<IComponentContext>();
                return t => c.Resolve(t);
            });

            var mediatrOpenTypes = new[]
            {
                typeof(IRequestHandler<,>),
                typeof(IRequestExceptionHandler<,,>),
                typeof(IRequestExceptionAction<,>),
                typeof(INotificationHandler<>),
            };

            foreach (var mediatrOpenType in mediatrOpenTypes) {
                builder
                .RegisterAssemblyTypes(_assemblies.ToArray())
                .AsClosedTypesOf(mediatrOpenType)
                .AsImplementedInterfaces();
            }

            builder.RegisterType<EmailSender>()
                .As<IEmailSender>()
                .InstancePerLifetimeScope();
        }

        private void RegisterDevelopmentOnlyDependencies(ContainerBuilder builder) {
            // TODO: Add development only services
        }

        private void RegisterProductionOnlyDependencies(ContainerBuilder builder) {
            // TODO: Add production only services
        }

    }
}
