using System;
using System.Threading.Tasks;

namespace TcpFramework.Pooling
{
    internal class NullPool<T> : Pool<T>
    {
        private Func<T> m_Activator;

        public override bool IsCyclic => false;

        public NullPool(Func<T> activator)
        {
            m_Activator = activator;
        }

        public override void Return(T item)
        {
            if (item is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        public override ValueTask<(bool, T)> TryTakeAsync() => new ValueTask<(bool, T)>((true, m_Activator()));
    }
}
