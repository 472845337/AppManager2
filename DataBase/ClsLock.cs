using System;
using System.Threading;

namespace AppManager2.DataBase
{
    public sealed class ClsLock
    {
        private readonly struct Lock : IDisposable
        {
            private readonly ReaderWriterLockSlim _lock;
            private readonly bool _isWrite;

            public Lock(ReaderWriterLockSlim rwl, bool isWrite)
            {
                _lock = rwl;
                _isWrite = isWrite;
            }

            public void Dispose()
            {
                if (_isWrite)
                {
                    if (_lock.IsWriteLockHeld)
                    {
                        _lock.ExitWriteLock();
                    }
                }
                else
                {
                    if (_lock.IsReadLockHeld)
                    {
                        _lock.ExitReadLock();
                    }
                }
            }
        }

        private class Disposable : IDisposable
        {
            public static readonly Disposable Empty = new Disposable();
            public void Dispose() { }
        }

        private readonly ReaderWriterLockSlim _lockSlim = new ReaderWriterLockSlim();

        public ClsLock()
        {
            Enabled = true;
        }

        public bool Enabled { get; set; }

        public IDisposable Read()
        {
            if (Enabled == false || _lockSlim.IsReadLockHeld || _lockSlim.IsWriteLockHeld)
            {
                return Disposable.Empty;
            }
            else
            {
                _lockSlim.EnterReadLock();
                return new Lock(_lockSlim, false);
            }
        }

        public IDisposable Write()
        {
            if (Enabled == false || _lockSlim.IsWriteLockHeld)
            {
                return Disposable.Empty;
            }
            else if (_lockSlim.IsReadLockHeld)
            {
                throw new NotImplementedException("ReadMode can't get into writelock state");
            }
            else
            {
                _lockSlim.EnterWriteLock();
                return new Lock(_lockSlim, true);
            }
        }
    }
}
