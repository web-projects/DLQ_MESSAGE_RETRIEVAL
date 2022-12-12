using DLQ.Message.Processor.Kernel;
using DLQ.Message.Processor.Providers;
using Ninject;

namespace DLQ.Message.Retriever
{
    internal class RetrieverProcessorLoader
    {
        [Inject]
        public IDeadLetterQueueProcessor DeadLetterQueueProcessorImpl { get; set; }

        public RetrieverProcessorLoader()
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
