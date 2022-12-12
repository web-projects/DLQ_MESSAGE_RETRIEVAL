using DLQ.Message.Processor.Kernel;
using DLQ.Message.Processor.Providers;
using Ninject;

namespace DLQ.Message.Server
{
    internal class ServerProcessorLoader
    {
        [Inject]
        public IDeadLetterQueueProcessor DeadLetterQueueProcessorImpl { get; set; }

        public ServerProcessorLoader()
        {
            InitializeInjector();
        }

        private void InitializeInjector()
        {
            using IKernel kernel = new DLQMessageKernelResolver().ResolveKernel();
            kernel.Inject(this);
        }
    }
}
