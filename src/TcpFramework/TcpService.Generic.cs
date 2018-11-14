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

        public static TcpService<TClient> Create(int port) => Create(new IPEndPoint(IPAddress.Any, port));
        public static TcpService<TClient> Create(int port, int backlog) => Create(new IPEndPoint(IPAddress.Any, port), backlog);
        public static TcpService<TClient> Create(IPEndPoint endPoint) => Create(endPoint, 100);
        public static TcpService<TClient> Create(IPEndPoint endPoint, int backlog)
        {
            var config = new TcpServiceConfiguration();
            config.AddEndPoint(new TcpServiceEndPoint(endPoint, 100));
            return new TcpService<TClient>(config);
        }
    }
}
