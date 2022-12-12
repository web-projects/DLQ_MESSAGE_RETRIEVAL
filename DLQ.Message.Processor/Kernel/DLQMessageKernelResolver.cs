using DLQ.Message.Processor.Modules;
using Ninject.Modules;

namespace DLQ.Message.Processor.Kernel
{
    public class DLQMessageKernelResolver : KernelResolverBase
    {
        public override NinjectModule[] NinjectModules => new NinjectModule[]
        {
            new DLQMessageModule()
        };
    }
}
