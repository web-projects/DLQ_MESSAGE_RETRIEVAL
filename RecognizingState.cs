using System;
using System.Collections.Generic;
using System.Text;

namespace DLQ.MessageRetrieval
{
    public class DLQEvaluationTime
    {
        public ErrorState ErrorState
        {
            get => default;
            set
            {
            }
        }

        public DLQMessageProcessing BusyState
        {
            get => default;
            set
            {
            }
        }
    }
}