using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace TcpFramework
{
    public abstract partial class TcpServiceClientBase
    {
        [Flags]
        private enum StateFlags
        {
            None,
            SendShutdown        = 0b0001,
            ReceiveShutdown     = 0b0010,
            ReceiverStarted     = 0b0100,
            IsReceiving         = 0b1000,

            Closed = SendShutdown | ReceiveShutdown,
            IsReceivingOrShutdown = IsReceiving | ReceiveShutdown,
            ReceiverStartedOrShutdown = ReceiverStarted | ReceiveShutdown,
            ReceiverStartedOrReceiving = ReceiverStarted | IsReceiving,
        }

        private readonly object SyncObject = new object();
        private StateFlags m_State;
        private SocketAsyncEventArgs m_ExclusiveReceiveEvent, m_CurrentReceiveEvent;

        /// <summary>
        /// <see cref="TcpService"/> that client cames from
        /// </summary>
        public TcpService ClientService { get; internal set; }

        /// <summary>
        /// Client <see cref="System.Net.Sockets.Socket"/>
        /// </summary>
        public Socket ClientSocket { get; internal set; }

        /// <summary>
        /// Client remote endpoint
        /// </summary>
        public IPEndPoint ClientEndPoint { get; internal set; }

        protected TcpServiceClientBase()
        {
            m_State = StateFlags.None;
            m_ExclusiveReceiveEvent = m_CurrentReceiveEvent = null;
        }

        protected internal abstract void OnConnect();

        /// <summary>
        /// Called when data receives
        /// </summary>
        protected abstract ValueTask OnReceive(byte[] buffer, int offset, int count);

        /// <summary>
        /// Called [by default] when socket error occurs or remote side closes connections
        /// </summary>
        /// <returns></returns>
        protected abstract ValueTask OnClose();

        /// <summary>
        /// Called when remote side shutdowns receive
        /// </summary>
        protected virtual ValueTask OnReceiveShutdown() => OnClose();

        /// <summary>
        /// Called when socket error occurs while receiving
        /// </summary>
        protected virtual ValueTask OnReceiveError(SocketError socketError) => OnClose();

        /// <summary>
        /// Called when <see cref="Socket.Shutdown(SocketShutdown)"/> called 
        /// </summary>
        protected virtual ValueTask OnSendShutdown() => OnClose();

        /// <summary>
        /// Called when socket error occurs while sending data 
        /// </summary>
        protected virtual ValueTask OnSendError(SocketError socketError) => HandleClose();


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetStateFlags(StateFlags flags) => m_State |= flags;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UnsetStateFlags(StateFlags flags) => m_State &= ~flags;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool HasStateFlags(StateFlags flags) => (m_State & flags) != 0;

       
        private bool SendShutdowned()
        {
            lock (SyncObject)
            {
                return HasStateFlags(StateFlags.SendShutdown);
            }
        }

        private async ValueTask HandleClose()
        {
            lock (SyncObject)
            {
                if (HasStateFlags(StateFlags.Closed))
                    return;

                m_State = StateFlags.Closed;
            }

            await OnClose();
        }
    }
}
