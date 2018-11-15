using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace TcpFramework
{
    public class TcpService<TClient> : TcpService 
        where TClient : TcpServiceClientBase, new()
    {
        public TcpService(TcpServiceConfiguration configuration) : base(configuration)
        {

        }

        protected override void HandleClientSocket(TcpServiceEndPoint serviceEndPoint, Socket clientSocket, IPEndPoint remoteEndPoint)
        {
            var cli = new TClient();
            cli.ClientService = this;
            cli.ClientSocket = clientSocket;
            cli.ClientEndPoint = remoteEndPoint;
            ThreadPool.QueueUserWorkItem(o => (o as TClient).OnConnect(), cli);
        }

        /// <summary>
        /// Creates <see cref="TcpService{TClient}"/> with specified port
        /// </summary>
        public static TcpService<TClient> Create(int port) => Create(new IPEndPoint(IPAddress.Any, port));

        /// <summary>
        /// Creates <see cref="TcpService{TClient}"/> with specified port and listening backlog
        /// </summary>
        public static TcpService<TClient> Create(int port, int backlog) => Create(new IPEndPoint(IPAddress.Any, port), backlog);

        /// <summary>
        /// Creates <see cref="TcpService{TClient}"/> with specified <see cref="System.Net.IPEndPoint"/>
        /// </summary>
        public static TcpService<TClient> Create(IPEndPoint endPoint) => Create(endPoint, 100);

        /// <summary>
        /// Creates <see cref="TcpService{TClient}"/> with specified <see cref="System.Net.IPEndPoint"/> and listening backlog
        /// </summary>
        public static TcpService<TClient> Create(IPEndPoint endPoint, int backlog)
        {
            var config = new TcpServiceConfiguration();
            config.AddEndPoint(new TcpServiceEndPoint(endPoint, 100));
            return new TcpService<TClient>(config);
        }

        /// <summary>
        /// Creates <see cref="TcpService{TClient}"/> using <see cref="TcpServiceConfigurationBuilder"/>
        /// </summary>
        public static TcpService<TClient> Create(Func<TcpServiceConfigurationBuilder, TcpServiceConfigurationBuilder> configBuildFunc)
        {
            return new TcpService<TClient>(configBuildFunc(new TcpServiceConfigurationBuilder()).ServiceConfiguration);
        }
    }
}
