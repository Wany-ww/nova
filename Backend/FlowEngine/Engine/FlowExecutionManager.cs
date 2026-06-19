using System;

namespace FlowEngine.Engine
{
    public static class FlowExecutionManager
    {
        private static volatile bool _stopRequested = false;

        public static bool StopRequested
        {
            get => _stopRequested;
            set => _stopRequested = value;
        }
    }
}
