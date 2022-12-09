using Ninject;
using Ninject.Modules;

namespace DLQ.Launcher.Kernel
{
    public interface IKernelModuleResolver
    {
        IKernel ResolveKernel(params NinjectModule[] modules);
    }
}