using System;
using System.Collections.Generic;

namespace TcpFramework
{
    public class TcpServiceConfiguration
    {
        private List<TcpServiceEndPoint> m_EndPoints;
        private int m_ReceiveBufferSize;
        private int m_ReceiveBufferPoolCount;
        private int m_SendBufferSize;
        private int m_SendBufferPoolCount;
        private int m_SendEventArgsPoolCount;
        private int m_TCSPoolCount;
        private TcpServicePoolType m_SendBufferPoolType;
        private TcpServicePoolType m_ReceiveBufferPoolType;
        private TcpServicePoolType m_SendEventArgsPoolType;

        /// <summary>
        /// Currently added <see cref="TcpServiceEndPoint"/>'s
        /// </summary>
        public IReadOnlyList<TcpServiceEndPoint> EndPoints => m_EndPoints;

        /// <summary>
        /// Indicates <see cref="TcpServiceConfiguration"/> marked as readonly
        /// </summary>
        public bool IsReadonly { get; private set; }

        /// <summary>
        /// Size of buffers of receive buffer pool
        /// </summary>
        public int ReceiveBufferSize
        {
            get => m_ReceiveBufferSize;
            set
            {
                if (m_ReceiveBufferSize <= 0)
                    throw new ArgumentOutOfRangeException(nameof(value));

                m_ReceiveBufferSize = value;
            }
        }

        /// <summary>
        /// Size of buffers of send buffer pool
        /// </summary>
        public int SendBufferSize
        {
            get => m_SendBufferSize;
            set
            {
                if (SendBufferSize <= 0)
                    throw new ArgumentOutOfRangeException(nameof(value));

                CheckReadonly();
                m_SendBufferSize = value;
            }
        }

        /// <summary>
        /// Pool count of receive buffers 
        /// </summary>
        public int ReceiveBufferPoolCount
        {
            get => m_ReceiveBufferPoolCount;
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(value));

                CheckReadonly();
                m_ReceiveBufferPoolCount = value;
            }
        }

        /// <summary>
        /// Pool count of send buffers
        /// </summary>
        public int SendBufferPoolCount
        {
            get => m_SendBufferPoolCount;
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(value));

                CheckReadonly();
                m_SendBufferPoolCount = value;
            }
        }


        /// <summary>
        /// Pool count of [bufferless] <see cref="System.Net.Sockets.SocketAsyncEventArgs"/> for send purpose
        /// </summary>
        public int SendEventArgsPoolCount
        {
            get => m_SendEventArgsPoolCount;
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(value));

                CheckReadonly();
                m_SendEventArgsPoolCount = value;
            }
        }

        /// <summary>
        /// Type of pool that contains <see cref="System.Threading.Tasks.TaskCompletionSource{TResult}"/> pool
        /// </summary>
        public int TaskCompletionSourcePoolCount
        {
            get => m_TCSPoolCount;
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(value));

                CheckReadonly();
                m_TCSPoolCount = value;
            }
        }

        /// <summary>
        /// Type of pool that contains send buffers
        /// </summary>
        public TcpServicePoolType SendBufferPoolType
        {
            get => m_SendBufferPoolType;
            set
            {
                CheckReadonly();
                m_SendBufferPoolType = value;
            }
        }

        /// <summary>
        /// Type of pool that contains receive buffers
        /// </summary>
        public TcpServicePoolType ReceiveBufferPoolType
        {
            get => m_ReceiveBufferPoolType;
            set
            {
                CheckReadonly();
                m_ReceiveBufferPoolType = value;
            }
        }

        /// <summary>
        /// Type of pool that contains [bufferless] <see cref="System.Net.Sockets.SocketAsyncEventArgs"/> for send purpose
        /// </summary>
        public TcpServicePoolType SendEventArgsPoolType
        {
            get => m_SendEventArgsPoolType;
            set
            {
                CheckReadonly();
                m_SendEventArgsPoolType = value;
            }
        }

        /// <summary>
        /// Constructs <see cref="TcpServiceConfiguration"/> with defualt values
        /// </summary>
        public TcpServiceConfiguration()
        {
            IsReadonly = false;

            m_EndPoints = new List<TcpServiceEndPoint>();

            m_ReceiveBufferSize = 5000;
            m_ReceiveBufferPoolCount = 10;

            m_SendBufferSize = 5000;
            m_SendBufferPoolCount = 10;
            m_SendEventArgsPoolCount = 10;

            m_TCSPoolCount = 10;

            ReceiveBufferPoolType = TcpServicePoolType.DemandCache;
            SendBufferPoolType = TcpServicePoolType.DemandCache;
        }

        /// <summary>
        /// Adds <see cref="TcpServiceEndPoint"/> to service EndPoints list
        /// </summary>
        /// <param name="endPoint"></param>
        public void AddEndPoint(TcpServiceEndPoint endPoint)
        {
            m_EndPoints.Add(endPoint ?? throw new ArgumentNullException(nameof(endPoint)));
        }

        /// <summary>
        /// Removes <see cref="TcpServiceEndPoint"/> from service EndPoints list 
        /// </summary>
        /// <param name="endPoint"></param>
        /// <returns></returns>
        public bool RemoveEndPoint(TcpServiceEndPoint endPoint)
        {
            return m_EndPoints.Remove(endPoint);
        }

        /// <summary>
        /// Marks configuration as readonly
        /// </summary>
        public void MakeReadonly()
        {
            IsReadonly = true;
        }

        private void CheckReadonly()
        {
            if (IsReadonly)
                throw new InvalidOperationException(nameof(TcpServiceConfiguration) + " is marked as readonly");
        }
    }
}
