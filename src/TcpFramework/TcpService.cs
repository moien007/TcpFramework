using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using TcpFramework.Pooling;

namespace TcpFramework
{
    /// <summary>
    /// Implements base class for TCP service that listens and handle incoming connections
    /// </summary>
    public abstract class TcpService 
    {
        private class ServiceSocket
        {
            public Socket Socket { get; }
            public TcpServiceEndPoint ServiceEndPoint { get; }

            public ServiceSocket(Socket socket, TcpServiceEndPoint endPoint)
            {
                Socket = socket;
                ServiceEndPoint = endPoint;
            }
        }

        private ServiceSocket[] m_SvcSockets;

        ~TcpService() => Dispose(false);

        internal Pool<SocketAsyncEventArgs> BufferlessSendAsyncEventArgsPool { get; private set; }
        internal Pool<SocketAsyncEventArgs> SendAsyncEventArgsPool { get; private set; }
        internal Pool<SocketAsyncEventArgs> ReceiveAsyncEventArgsPool { get; private set; }
        internal Pool<TaskCompletionSource<bool>> TaskCompletionSourcePool { get; private set; }

        /// <summary>
        /// Indicates the service has started
        /// </summary>
        public bool Started { get; private set; }

        /// <summary>
        /// Indicates the service has disposed
        /// </summary>
        public bool Disposed { get; private set; }

        /// <summary>
        /// Services Configuration
        /// </summary>
        public TcpServiceConfiguration Configuration { get; }

        protected TcpService(TcpServiceConfiguration configuration)
        {
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            Started = false;
            Disposed = false;
            m_SvcSockets = null;
        }

        /// <summary>
        /// Initialize the service and starts the listeners and uses current thread to accept incoming connections
        /// </summary>
        public void Start()
        {
            Init();

            if (m_SvcSockets == null)
                return;

            if (m_SvcSockets.Length == 1)
            {
                var svcSocket = m_SvcSockets[0];
                svcSocket.Socket.Listen(svcSocket.ServiceEndPoint.Backlog);
                AcceptLoop(svcSocket);
                return;
            }

            foreach (var svcSocket in m_SvcSockets)
            {
                svcSocket.Socket.Listen(svcSocket.ServiceEndPoint.Backlog);
            }

            while (true)
            {
                foreach (var svcSocket in m_SvcSockets)
                {
                    try
                    {
                        if (!svcSocket.Socket.Poll(100000, SelectMode.SelectRead))
                            continue;

                        var client = svcSocket.Socket.Accept();
                        HandleClientSocket(svcSocket.ServiceEndPoint, client, client.RemoteEndPoint as IPEndPoint);
                    }
                    catch  (SocketException)
                    {
                        Dispose();
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Initialize the service, and starts listener and accepts connections asynchronously
        /// </summary>
        public void StartAsync()
        {
            Init();

            if (m_SvcSockets == null)
                return;

            for (int i = 0; i < m_SvcSockets.Length; i++)
            {
                var eventArgs = new SocketAsyncEventArgs();
                eventArgs.UserToken = m_SvcSockets[i];
                eventArgs.Completed += AsyncAccept_Completed;

                m_SvcSockets[i].Socket.Listen(m_SvcSockets[i].ServiceEndPoint.Backlog);
                if (!m_SvcSockets[i].Socket.AcceptAsync(eventArgs))
                {
                    ThreadPool.QueueUserWorkItem(o => ProcessSocketAcceptEvent(eventArgs));
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected abstract void HandleClientSocket(TcpServiceEndPoint serviceEndPoint, Socket clientSocket, IPEndPoint remoteEndPoint);

        protected virtual Socket CreateSocket(TcpServiceEndPoint serviceEndPoint)
        {
            var socket = new Socket(serviceEndPoint.EndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(serviceEndPoint.EndPoint);
            return socket;
        }

        private void Init()
        {
            if (Started) throw new InvalidOperationException("Service is already started");
            Started = true;

            if (Configuration.EndPoints.Count == 0)
                return;

            Configuration.MakeReadonly();

            var endPoints = Configuration.EndPoints;
            m_SvcSockets = new ServiceSocket[endPoints.Count];
            for (int i = 0; i < endPoints.Count; i++)
            {
                var endPoint = endPoints[i];
                var socket = CreateSocket(endPoint);
                m_SvcSockets[i] = new ServiceSocket(socket, endPoint);
            }

            TaskCompletionSourcePool = CreatePool(
                TcpServicePoolType.Cache,
                Configuration.TaskCompletionSourcePoolCount,
                () => new TaskCompletionSource<bool>());

            BufferlessSendAsyncEventArgsPool = CreatePool(
                Configuration.SendEventArgsPoolType, 
                Configuration.SendEventArgsPoolCount, 
                () => new SocketAsyncEventArgs());

            SendAsyncEventArgsPool = CreateSAEABufferPool(
                Configuration.SendBufferPoolCount,
                Configuration.SendBufferSize,
                Configuration.SendBufferPoolType);

            ReceiveAsyncEventArgsPool = CreateSAEABufferPool(
                Configuration.ReceiveBufferPoolCount,
                Configuration.ReceiveBufferSize,
                Configuration.ReceiveBufferPoolType);

        }

        private void AcceptLoop(ServiceSocket serviceSocket)
        {
            var socket = serviceSocket.Socket;
            var endPoint = serviceSocket.ServiceEndPoint;

            while (true)
            {
                try
                {
                    var client = socket.Accept();
                    HandleClientSocket(endPoint, client, client.RemoteEndPoint as IPEndPoint);
                }
                catch (SocketException)
                {
                    Dispose();
                    break;
                }
            }
        }

        private void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            if (Disposed) return;
            Disposed = true;

            if (m_SvcSockets == null)
                return;

            foreach (var svcSocket in m_SvcSockets)
            { 
                svcSocket.Socket.Dispose();
            }

            m_SvcSockets = null;

            BufferlessSendAsyncEventArgsPool.Dispose();
            ReceiveAsyncEventArgsPool.Dispose();
            SendAsyncEventArgsPool.Dispose();
            TaskCompletionSourcePool.Dispose();
        }

        private void AsyncAccept_Completed(object sender, SocketAsyncEventArgs e)
        {
            ProcessSocketAcceptEvent(e);
        }

        private void ProcessSocketAcceptEvent(SocketAsyncEventArgs e)
        {
            var svcSocket = e.UserToken as ServiceSocket;

            if (e.SocketError != SocketError.Success)
            {
                e.Dispose();
                this.Dispose();
                return;
            }

            var remoteEP = e.AcceptSocket.RemoteEndPoint as IPEndPoint;
            HandleClientSocket(svcSocket.ServiceEndPoint, e.AcceptSocket, remoteEP);

            if (!svcSocket.Socket.AcceptAsync(e))
                ProcessSocketAcceptEvent(e);
        }


        static Pool<T> CreatePool<T>(TcpServicePoolType poolType, int count, Func<T> activator)
        {
            switch (poolType)
            {
                case TcpServicePoolType.Cache:
                case TcpServicePoolType.DemandCache:
                    return new CachedPool<T>(count, activator, TcpServicePoolType.DemandCache == poolType);

                case TcpServicePoolType.Cyclic:
                case TcpServicePoolType.DemandCyclic:
                    return new CyclicPool<T>(count, activator, TcpServicePoolType.DemandCyclic == poolType);

                default:
                    throw new NotImplementedException(poolType.ToString());
            }
        }

        static Pool<SocketAsyncEventArgs> CreateSAEABufferPool(int count, int bufferSize, TcpServicePoolType poolType)
        {
            if (poolType == TcpServicePoolType.Cyclic)
            {
                var buffer = new byte[bufferSize * count];
                var offset = 0;

                return new CyclicPool<SocketAsyncEventArgs>(count, () =>
                {
                    var eventArgs = new SocketAsyncEventArgs();
                    eventArgs.SetBuffer(buffer, offset, bufferSize);
                    offset += bufferSize;
                    return eventArgs;
                }, false);
            }

            if (poolType == TcpServicePoolType.Cache)
            {
                var buffer = new byte[bufferSize * count];
                var offset = 0;

                return new CachedPool<SocketAsyncEventArgs>(count, () =>
                {
                    var eventArgs = new SocketAsyncEventArgs();
                    if (offset != count)
                    {
                        eventArgs.SetBuffer(buffer, offset, bufferSize);
                        offset += bufferSize;
                    }
                    else
                    {
                        eventArgs.SetBuffer(new byte[bufferSize], 0, bufferSize);
                    }
                    return eventArgs;
                }, false);
            }

            if (poolType == TcpServicePoolType.DemandCyclic)
            {
                return new CyclicPool<SocketAsyncEventArgs>(count, () =>
                {
                    var eventArgs = new SocketAsyncEventArgs();
                    var buffer = new byte[bufferSize];
                    eventArgs.SetBuffer(buffer, 0, buffer.Length);
                    return eventArgs;
                }, true);
            }

            if (poolType == TcpServicePoolType.DemandCache)
            {
                return new CyclicPool<SocketAsyncEventArgs>(count, () =>
                {
                    var eventArgs = new SocketAsyncEventArgs();
                    var buffer = new byte[bufferSize];
                    eventArgs.SetBuffer(buffer, 0, buffer.Length);
                    return eventArgs;
                }, true);
            }

            throw new NotImplementedException(poolType.ToString());
        }
    }
}
