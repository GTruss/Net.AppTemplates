using Autofac;
using App.Services;

namespace App.Core;

public class DefaultServicesModule : Module {
    protected override void Load(ContainerBuilder builder) {
        builder.RegisterType<MainService>()
            .As<MainService>()
            .InstancePerLifetimeScope();
    }
}
