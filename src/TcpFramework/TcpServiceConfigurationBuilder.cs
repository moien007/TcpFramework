using System;
using System.Net;

namespace TcpFramework
{
    /// <summary>
    /// Provides fluent way to create <see cref="TcpServiceConfiguration"/>
    /// </summary>
    public struct TcpServiceConfigurationBuilder
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

        public TcpServiceConfigurationBuilder AddEndPoint(IPEndPoint endPoint, int backlog)
        {
            ServiceConfiguration.AddEndPoint(new TcpServiceEndPoint(endPoint, backlog));
            return this;
        }

        public TcpServiceConfigurationBuilder AddEndPoint(IPAddress address, int port, int backlog)
        {
            return AddEndPoint(new IPEndPoint(address, port), backlog);
        }

        public TcpServiceConfigurationBuilder AddEndPoint(IPEndPoint endPoint)
        {
            return AddEndPoint(endPoint, DefaultBacklog);
        }

        public TcpServiceConfigurationBuilder AddEndPoint(IPAddress address, int port)
        {
            return AddEndPoint(address, port, DefaultBacklog);
        }

        public TcpServiceConfigurationBuilder AddPort(int port)
        {
            return AddEndPoint(IPAddress.Any, port);
        }

        /// <summary>
        /// Sets <see cref="TcpServiceConfiguration.ReceiveBufferSize"/>
        /// </summary>
        public TcpServiceConfigurationBuilder WithReceiveBufferSize(int bufferSize)
        {
            ServiceConfiguration.ReceiveBufferSize = bufferSize;
            return this;
        }

        /// <summary>
        /// Sets <see cref="TcpServiceConfiguration.SendBufferSize"/>
        /// </summary>
        public TcpServiceConfigurationBuilder WithSendBufferSize(int bufferSize)
        {
            ServiceConfiguration.SendBufferSize = bufferSize;
            return this;
        }

        /// <summary>
        /// Sets <see cref="TcpServiceConfiguration.ReceiveBufferPoolCount"/>
        /// </summary>
        public TcpServiceConfigurationBuilder WithReceiveBufferPoolCount(int count)
        {
            ServiceConfiguration.ReceiveBufferPoolCount = count;
            return this;
        }

        /// <summary>
        /// Sets <see cref="TcpServiceConfiguration.SendBufferPoolCount"/>
        /// </summary>
        public TcpServiceConfigurationBuilder WithSendBufferPoolCount(int count)
        {
            ServiceConfiguration.SendBufferPoolCount = count;
            return this;
        }

        /// <summary>
        /// Sets <see cref="TcpServiceConfiguration.SendBufferPoolType"/>
        /// </summary>
        public TcpServiceConfigurationBuilder WithSendBufferPoolType(TcpServicePoolType poolType)
        {
            ServiceConfiguration.SendBufferPoolType = poolType;
            return this;
        }

        /// <summary>
        /// Sets <see cref="TcpServiceConfiguration.ReceiveBufferPoolType"/>
        /// </summary>
        public TcpServiceConfigurationBuilder WithReceiveBufferPoolType(TcpServicePoolType poolType)
        {
            ServiceConfiguration.ReceiveBufferPoolType = poolType;
            return this;
        }

        /// <summary>
        /// Sets <see cref="TcpServiceConfiguration.ReceiveBufferSize"/> and <see cref="TcpServiceConfiguration.SendBufferSize"/>
        /// </summary>
        public TcpServiceConfigurationBuilder WithBufferSize(int bufferSize)
        {
            WithReceiveBufferSize(bufferSize);
            return WithSendBufferSize(bufferSize);
        }

        /// <summary>
        /// Sets <see cref="TcpServiceConfiguration.ReceiveBufferPoolCount"/> and <see cref="TcpServiceConfiguration.SendBufferPoolCount"/>
        /// </summary>
        public TcpServiceConfigurationBuilder WithBufferPoolCount(int count)
        {
            WithSendBufferPoolCount(count);
            return WithReceiveBufferPoolCount(count);
        }

        /// <summary>
        /// Sets <see cref="TcpServiceConfiguration.ReceiveBufferPoolType"/> and <see cref="TcpServiceConfiguration.SendBufferPoolType"/>
        /// </summary>
        public TcpServiceConfigurationBuilder WithBufferPoolType(TcpServicePoolType poolType)
        {
            WithReceiveBufferPoolType(poolType);
            return WithSendBufferPoolType(poolType);
        }

        /// <summary>
        /// Sets <see cref="TcpServiceConfiguration.SendEventArgsPoolCount"/>
        /// </summary>
        public TcpServiceConfigurationBuilder WithSendEventArgsPoolCount(int count)
        {
            ServiceConfiguration.SendEventArgsPoolCount = count;
            return this;
        }

        /// <summary>
        /// Sets <see cref="TcpServiceConfiguration.SendEventArgsPoolType"/>
        /// </summary>
        public TcpServiceConfigurationBuilder WithSendEventArgsPoolType(TcpServicePoolType poolType)
        {
            ServiceConfiguration.SendEventArgsPoolType = poolType;
            return this;
        }

        /// <summary>
        /// Sets <see cref="TcpServiceConfiguration.TaskCompletionSourcePoolCount"/>
        /// </summary>
        public TcpServiceConfigurationBuilder WithTaskCompletionSourcePoolCount(int count)
        {
            ServiceConfiguration.TaskCompletionSourcePoolCount = count;
            return this;
        }
    }
}
