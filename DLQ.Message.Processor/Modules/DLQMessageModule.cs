using DLQ.Message.Processor.Providers;
using Ninject.Modules;

namespace DLQ.Message.Processor.Modules
{
    public sealed class DLQMessageModule : NinjectModule
    {
        public override void Load()
        {
            Bind<IDeadLetterQueueProcessor>().To<DeadLetterQueueProcessorImpl>();

        }
    }
}
