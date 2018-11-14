using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

using TcpFramework;

namespace TestServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting server");
            TcpService<TestServiceClient>.Create(9339).Start();
        }
    }

    class TestServiceClient : TcpServiceClientBase
    {
        protected override void OnConnect()
        {
            Console.WriteLine("Connected ({0})", ClientEndPoint);
            StartReceive();
        }

        protected override ValueTask OnReceive(byte[] buffer, int offset, int count)
        {
            Console.WriteLine("Received {0} bytes", count);
            return new ValueTask();
        }

        protected override ValueTask OnClose()
        {
            Console.WriteLine("Disconnected ({0})", ClientEndPoint);
            return new ValueTask();
        }

        protected override ValueTask OnReceiveError(SocketError socketError)
        {
            Console.WriteLine("Receive error: {0}", socketError);
            return base.OnReceiveError(socketError);
        }

        protected override ValueTask OnReceiveShutdown()
        {
            Console.WriteLine("Receive shutdown");
            return base.OnReceiveShutdown();
        }

        protected override ValueTask OnSendError(SocketError socketError)
        {
            Console.WriteLine("Error while sending: {0}", socketError);
            return base.OnSendError(socketError);
        }

        protected override ValueTask OnSendShutdown()
        {
            Console.WriteLine("Send shutdown");
            return base.OnSendShutdown();
        }
    }
}
