using System;
using System.Net;

namespace TcpFramework
{
    /// <summary>
    /// Contains an <see cref="System.Net.IPEndPoint"/> and Backlog of <see cref="TcpService"/>  
    /// </summary>
    public class TcpServiceEndPoint
    {
        public IPEndPoint EndPoint { get; }
        public int Backlog { get; }

        public TcpServiceEndPoint(IPEndPoint endPoint, int backlog)
        {
            if (backlog <= 0)
                throw new ArgumentOutOfRangeException(nameof(backlog));

            EndPoint = endPoint ?? throw new ArgumentNullException(nameof(endPoint));
            Backlog = backlog;
        }
    }
}
