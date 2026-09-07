using System.Threading;

namespace Mox
{
    namespace Utility
    {
        public class IPCUtility
        {
            private static Mutex _mutex = new Mutex();

            private static bool _pipeConnecting = false;

            public static bool TryLockPipeConnecting()
            {
                bool result = false;

                _mutex.WaitOne();

                if (_pipeConnecting == false)
                {
                    _pipeConnecting = true;
                    result = true;
                }

                _mutex.ReleaseMutex();

                return result;
            }

            public static void ReleasePipeConnecting()
            {
                _mutex.WaitOne();

                _pipeConnecting = false;

                _mutex.ReleaseMutex();
            }
        }
    }
}
