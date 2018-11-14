using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TcpFramework.Common;

namespace TcpFramework.Pooling
{
    internal class CyclicPool<T> : Pool<T> 
    {
        private readonly object SyncObject = new object();

        private AsyncSemaphore m_Semaphore;
        private Stack<T> m_Stack;
        private Func<T> m_Activator;

        public override bool IsCyclic => true;

        public CyclicPool(int count, Func<T> activator, bool activateOnDemand)
        {
            m_Stack = new Stack<T>(count);
            m_Semaphore = new AsyncSemaphore(count);

            if (activateOnDemand)
            {
                m_Activator = activator;
            }
            else
            {
                m_Activator = null;

                for (int i = 0; i < count; i++)
                {
                    m_Stack.Push(activator());
                }
            }
        }

        public override async ValueTask<(bool, T)> TryTakeAsync()
        {
            try
            {
                await m_Semaphore.WaitOneAsync();
            }
            catch (TaskCanceledException)
            {
                return (false, default(T));
            }

            try
            {
                lock (SyncObject)
                {
                    if (m_Stack == null)
                    {
                        return (false, default(T));
                    }

                    if (m_Stack.Count == 0)
                    {
                        return (true, m_Activator());
                    }

                    return (true, m_Stack.Pop());
                }
            }
            finally
            {
                m_Semaphore.Release();
            }
        }

        public override void Return(T item)
        {
            lock (SyncObject)
            {
                if (m_Stack == null)
                {
                    if (item is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }

                    return;
                }

                m_Stack.Push(item);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            lock (SyncObject)
            {
                if (m_Stack == null) return;

                foreach (var item in m_Stack)
                {
                    if (item is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }

                m_Semaphore.Dispose();
                m_Stack = null;
                m_Activator = null;
            }
        }
    }
}
