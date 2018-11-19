using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TcpFramework.Threading
{
    internal class AsyncSemaphore : IDisposable
    {
        private readonly object SyncObject = new object();

        private Queue<TaskCompletionSource<bool>> m_CompletionSources;
        private int m_Count;

        ~AsyncSemaphore() => Dispose(false);
        public AsyncSemaphore(int count)
        {
            m_Count = count;
            m_CompletionSources = new Queue<TaskCompletionSource<bool>>(16);
        }

        public async ValueTask<bool> WaitOneAsync()
        {
            TaskCompletionSource<bool> completionSource;

            lock (SyncObject)
            {
                if (m_CompletionSources == null)
                    return false;

                if (m_Count > 0)
                {
                    m_Count--;
                    return true;
                }

                completionSource =
#if NET45
                new TaskCompletionSource<bool>();
#else
                new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
#endif

                m_CompletionSources.Enqueue(completionSource);
            }

            return await completionSource.Task;
        }

        public void Release()
        {
            lock (SyncObject)
            {
                if (m_CompletionSources == null)
                    return;

                m_Count++;

                if (m_CompletionSources.Count == 0)
                    return;

                var completionSource = m_CompletionSources.Dequeue();
#if NET45
                ThreadPool.QueueUserWorkItem((o) => (o as TaskCompletionSource<bool>).SetResult(true), completionSource);
#else
                completionSource.SetResult(true);
#endif
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!disposing) return;

            lock (SyncObject)
            {
                if (m_CompletionSources == null)
                    return;

                foreach (var completionSource in m_CompletionSources)
                {
#if NET45
                    ThreadPool.QueueUserWorkItem(o => (o as TaskCompletionSource<bool>).SetCanceled(), completionSource);
#else
                    completionSource.SetCanceled();
#endif
                }

                m_CompletionSources = null;
            }
        }
    }
}
