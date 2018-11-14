using System;
using System.Threading.Tasks;

namespace TcpFramework.Pooling
{
    internal abstract class Pool<T> : IDisposable
    {
        public abstract bool IsCyclic { get; }

        ~Pool() => Dispose(false);

        public abstract ValueTask<(bool, T)> TryTakeAsync();
        public abstract void Return(T item);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) { }
    }
}