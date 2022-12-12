using DLQ.Message.Processor.Providers;
using DLQ.Message.Processor.Kernel;
using Ninject;

namespace DLQ.Message.Client
{
    internal class ClientProcessorLoader
    {
        [Inject]
        public IDeadLetterQueueProcessor DeadLetterQueueProcessorImpl { get; set; }

        public ClientProcessorLoader()
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
