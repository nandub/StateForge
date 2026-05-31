using System;
using System.Threading;

namespace StateForge.FileStore
{
    internal sealed class StateForgeKeyMutex : IDisposable
    {
        private readonly Mutex _mutex;
        private bool _hasHandle;

        public StateForgeKeyMutex(string keyHash, int timeoutMilliseconds)
        {
            string mutexName = @"Local\StateForge_" + keyHash;
            _mutex = new Mutex(false, mutexName);

            try
            {
                _hasHandle = _mutex.WaitOne(timeoutMilliseconds);
            }
            catch (AbandonedMutexException)
            {
                _hasHandle = true;
            }

            if (!_hasHandle)
            {
                throw new TimeoutException("Timed out waiting for StateForge key mutex.");
            }
        }

        public void Dispose()
        {
            if (_hasHandle)
            {
                _mutex.ReleaseMutex();
                _hasHandle = false;
            }

            _mutex.Dispose();
        }
    }
}
