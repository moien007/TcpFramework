using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TcpFramework.Pooling
{
    internal class CachedPool<T> : Pool<T>
    {
        private readonly object SyncObject = new object();

        private Func<T> m_Activator;
        private Stack<T> m_Stack;
        private int m_MaxCount;

        public override bool IsCyclic => false;

        public CachedPool(int count, Func<T> activator, bool activateOnDemand)
        {
            m_MaxCount = count;
            m_Stack = new Stack<T>(count);
            m_Activator = activator;

            if (!activateOnDemand)
            {
                for (int i = 0; i < count; i++)
                {
                    m_Stack.Push(activator());
                }
            }
        }


        public override ValueTask<(bool, T)> TryTakeAsync()
        {
            lock (SyncObject)
            {
                if (m_Stack == null)
                {
                    return new ValueTask<(bool, T)>((false, default(T)));
                }

                if (m_Stack.Count == 0)
                {
                    return new ValueTask<(bool, T)>((true, m_Activator()));
                }

                return new ValueTask<(bool, T)>((true, m_Stack.Pop()));
            }
        }

        public override void Return(T item)
        {
            lock (SyncObject)
            {
                if (m_Stack == null || m_Stack.Count == m_MaxCount)
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

                m_Stack = null;
                m_Activator = null;
            }
        }
    }
}
