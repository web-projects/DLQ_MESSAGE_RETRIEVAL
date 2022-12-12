using Ninject;
using Ninject.Modules;
using System.Collections.Generic;

namespace DLQ.Message.Processor.Kernel
{
    public abstract class KernelResolverBase : IKernelModuleResolver
    {
        public abstract NinjectModule[] NinjectModules { get; }

        public virtual IKernel ResolveKernel(params NinjectModule[] modules)
        {
            List<NinjectModule> moduleList = new List<NinjectModule>();
            moduleList.AddRange(NinjectModules);
            moduleList.AddRange(modules);

            IKernel kernel = new StandardKernel(moduleList.ToArray());
            kernel.Settings.InjectNonPublic = true;
            kernel.Settings.InjectParentPrivateProperties = true;

            return kernel;
        }
    }

}