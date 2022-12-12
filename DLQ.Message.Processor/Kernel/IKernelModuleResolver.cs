using Ninject;
using Ninject.Modules;

namespace DLQ.Message.Processor.Kernel
{
    public interface IKernelModuleResolver
    {
        IKernel ResolveKernel(params NinjectModule[] modules);
    }
}