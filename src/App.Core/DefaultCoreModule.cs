using Autofac;

using App.Core.Interfaces;
using App.Core.Services;

namespace App.Core {
    public class DefaultCoreModule : Module {
        protected override void Load(ContainerBuilder builder) {
            builder.RegisterType<ToDoItemSearchService>()
                .As<IToDoItemSearchService>().InstancePerLifetimeScope();
        }
    }
}
