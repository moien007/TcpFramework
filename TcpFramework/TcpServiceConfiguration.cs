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
        private TcpBufferPoolType m_SendBufferPoolType;
        private TcpBufferPoolType m_ReceiveBufferPoolType;

        public IReadOnlyList<TcpServiceEndPoint> EndPoints => m_EndPoints;
        public bool IsReadonly { get; private set; }

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

        public TcpBufferPoolType SendBufferPoolType
        {
            get => m_SendBufferPoolType;
            set
            {
                CheckReadonly();
                m_SendBufferPoolType = value;
            }
        }

        public TcpBufferPoolType ReceiveBufferPoolType
        {
            get => m_ReceiveBufferPoolType;
            set
            {
                CheckReadonly();
                m_ReceiveBufferPoolType = value;
            }
        }

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

            ReceiveBufferPoolType = TcpBufferPoolType.DemandCache;
            SendBufferPoolType = TcpBufferPoolType.DemandCache;
        }

        public void AddEndPoint(TcpServiceEndPoint endPoint)
        {
            m_EndPoints.Add(endPoint ?? throw new ArgumentNullException(nameof(endPoint)));
        }

        public bool RemoveEndPoint(TcpServiceEndPoint endPoint)
        {
            return m_EndPoints.Remove(endPoint);
        }

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
