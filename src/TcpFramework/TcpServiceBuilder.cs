using System;
using System.Net;

namespace TcpFramework
{
    public struct TcpServiceBuilder
    {
        private const int DefaultBacklog = 100;

        private TcpServiceConfiguration m_ServiceConfiguration;

        public TcpServiceConfiguration ServiceConfiguration
        {
            get
            {
                if (m_ServiceConfiguration == null)
                {
                    m_ServiceConfiguration = new TcpServiceConfiguration();
                }

                return m_ServiceConfiguration;
            }
        }

        public TcpServiceBuilder AddEndPoint(IPEndPoint endPoint, int backlog)
        {
            ServiceConfiguration.AddEndPoint(new TcpServiceEndPoint(endPoint, backlog));
            return this;
        }

        public TcpServiceBuilder AddEndPoint(IPAddress address, int port, int backlog)
        {
            return AddEndPoint(new IPEndPoint(address, port), backlog);
        }

        public TcpServiceBuilder AddEndPoint(IPEndPoint endPoint)
        {
            return AddEndPoint(endPoint, DefaultBacklog);
        }

        public TcpServiceBuilder AddEndPoint(IPAddress address, int port)
        {
            return AddEndPoint(address, port, DefaultBacklog);
        }

        public TcpServiceBuilder AddPort(int port)
        {
            return AddEndPoint(IPAddress.Any, port);
        }

        public TcpServiceBuilder WithReceiveBufferSize(int bufferSize)
        {
            ServiceConfiguration.ReceiveBufferSize = bufferSize;
            return this;
        }

        public TcpServiceBuilder WithSendBufferSize(int bufferSize)
        {
            ServiceConfiguration.SendBufferSize = bufferSize;
            return this;
        }

        public TcpServiceBuilder WithReceiveBufferPoolCount(int count)
        {
            ServiceConfiguration.ReceiveBufferPoolCount = count;
            return this;
        }

        public TcpServiceBuilder WithSendBufferPoolCount(int count)
        {
            ServiceConfiguration.SendBufferPoolCount = count;
            return this;
        }

        public TcpServiceBuilder WithSendBufferPoolType(TcpBufferPoolType poolType)
        {
            ServiceConfiguration.SendBufferPoolType = poolType;
            return this;
        }

        public TcpServiceBuilder WithReceiveBufferPoolType(TcpBufferPoolType poolType)
        {
            ServiceConfiguration.ReceiveBufferPoolType = poolType;
            return this;
        }

        public TcpServiceBuilder WithBufferSize(int bufferSize)
        {
            WithReceiveBufferSize(bufferSize);
            return WithSendBufferSize(bufferSize);
        }

        public TcpServiceBuilder WithBufferPoolCount(int count)
        {
            WithSendBufferPoolCount(count);
            return WithReceiveBufferPoolCount(count);
        }

        public TcpServiceBuilder WithBufferPoolType(TcpBufferPoolType poolType)
        {
            WithReceiveBufferPoolType(poolType);
            return WithSendBufferPoolType(poolType);
        }

        public TcpServiceBuilder WithTaskCompletionSourcePoolCount(int count)
        {
            ServiceConfiguration.TaskCompletionSourcePoolCount = count;
            return this;
        }

        public static TcpService<T> Build<T>(Func<TcpServiceBuilder, TcpServiceBuilder> func) where T : TcpServiceClientBase, new()
        {
            if (func == null)
                throw new ArgumentNullException(nameof(func));

            var config = func(new TcpServiceBuilder()).ServiceConfiguration;
            if (config == null)
                throw new InvalidOperationException();

            return new TcpService<T>(config);
        }
    }
}
