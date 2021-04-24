using System;
using System.Threading;

namespace ReaderWriterLock
{
    public class ReaderPreferenceRwLock : IRwLock
    {
        private readonly object lockObject = new object();
        private int readersCount = 0;

        public void ReadLocked(Action action)
        {
            lock (lockObject)
                readersCount++;
            action();
            lock (lockObject)
                readersCount--;
        }

        public void WriteLocked(Action action)
        {
            while (true)
                lock (lockObject)
                    if (readersCount == 0)
                    {
                        action();
                        break;
                    }
        }
    }

    public class ReaderPreferenceWithoutWriterSpinRwLock : IRwLock
    {
        private readonly object lockObject = new object();
        private int readersCount;

        public void ReadLocked(Action action)
        {
            lock (lockObject)
                Interlocked.Increment(ref readersCount);
            action();
            lock (lockObject)
            {
                Interlocked.Decrement(ref readersCount);
                if (readersCount == 0) Monitor.Pulse(lockObject);
            }
        }

        public void WriteLocked(Action action)
        {
            lock (lockObject)
            {
                while (readersCount > 0)
                    Monitor.Wait(lockObject);
                action();
                Monitor.Pulse(lockObject);
            }
        }
    }

    public class WriterPreferenceRwLock : IRwLock
    {
        private readonly object lockObject = new object();
        private int readersCount;
        private bool hasWriter;

        public void ReadLocked(Action action)
        {
            lock (lockObject)
            {
                while (hasWriter) Monitor.Wait(lockObject);
                Interlocked.Increment(ref readersCount);
            }

            action();

            lock (lockObject)
            {
                Interlocked.Decrement(ref readersCount);
                if (readersCount == 0)
                    Monitor.PulseAll(lockObject);
            }
        }

        public void WriteLocked(Action action)
        {
            lock (lockObject)
            {
                while (hasWriter) Monitor.Wait(lockObject);
                hasWriter = true;
                while (readersCount > 0) Monitor.Wait(lockObject);
            }

            action();

            lock (lockObject)
            {
                hasWriter = false;
                Monitor.PulseAll(lockObject);
            }
        }
    }
}