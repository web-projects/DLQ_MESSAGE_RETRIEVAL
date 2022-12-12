using DLQ.Launcher.Modules;
using Ninject.Modules;

namespace DLQ.Launcher.Kernel
{
    public class AppManagerKernelResolver : KernelResolverBase
    {
        public override NinjectModule[] NinjectModules => new NinjectModule[]
        {
            new AppManagerModule(),
        };
    }
}
