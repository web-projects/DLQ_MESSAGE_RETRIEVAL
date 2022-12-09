using DLQ.Message.Launcher.Providers;
using Ninject.Modules;

namespace DLQ.Launcher.Kernel
{
    public sealed class AppManagerModule : NinjectModule
    {
        public override void Load()
        {
            Bind<IStringTemplateProvider>().To<MarqueeStringTemplateProvider>();
        }
    }
}