using System;
using System.Buffers;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace TcpFramework
{
    public abstract partial class TcpServiceClientBase
    {
        protected void SetExclusiveReceiveEvent(SocketAsyncEventArgs eventArgs)
        {
            if (TrySetExclusiveReceiveEvent(eventArgs))
                return;

            throw new InvalidOperationException("Failed to set exclusive receiver's SocketAsyncEventArgs");
        }

        protected bool TrySetExclusiveReceiveEvent(SocketAsyncEventArgs eventArgs)
        {
            CheckReceiveEventArgs(eventArgs);

            lock (SyncObject)
            {
                if (HasStateFlags(StateFlags.IsReceivingOrShutdown))
                    return false;

                if (m_ExclusiveReceiveEvent != null)
                    m_ExclusiveReceiveEvent.Dispose();

                m_ExclusiveReceiveEvent = eventArgs;
                return true;
            }
        }

        protected bool TrySetExclusiveReceiveEvent(SocketAsyncEventArgs eventArgs, out SocketAsyncEventArgs oldEventArgs)
        {
            CheckReceiveEventArgs(eventArgs);

            lock (SyncObject)
            {
                if (HasStateFlags(StateFlags.IsReceivingOrShutdown))
                {
                    oldEventArgs = null;
                    return false;
                }

                if (m_ExclusiveReceiveEvent == null)
                {
                    oldEventArgs = null;
                    return true;
                }

                oldEventArgs = m_ExclusiveReceiveEvent;
                m_ExclusiveReceiveEvent = eventArgs;
                return true;
            }
        }

        protected SocketAsyncEventArgs GetExclusiveReceiveEvent()
        {
            lock (SyncObject)
            {
                if (HasStateFlags(StateFlags.IsReceiving))
                    return null;

                return m_ExclusiveReceiveEvent;
            }
        }

        protected bool TryUnsetExclusiveReceiveEvent()
        {
            lock (SyncObject)
            {
                if (HasStateFlags(StateFlags.IsReceiving))
                    return false;

                if (m_ExclusiveReceiveEvent != null)
                {
                    if (m_CurrentReceiveEvent == m_ExclusiveReceiveEvent)
                        m_CurrentReceiveEvent = null;

                    m_ExclusiveReceiveEvent.Dispose();
                    m_ExclusiveReceiveEvent = null;
                }

                return true;
            }
        }

        protected bool TryUnsetExclusiveReceiveEvent(out SocketAsyncEventArgs eventArgs)
        {
            lock (SyncObject)
            {
                if (HasStateFlags(StateFlags.IsReceiving))
                {
                    eventArgs = null;
                    return false;
                }

                if (m_ExclusiveReceiveEvent == null)
                {
                    eventArgs = null;
                    return true;
                }

                if (m_ExclusiveReceiveEvent == m_CurrentReceiveEvent)
                {
                    m_CurrentReceiveEvent.Completed -= AsyncReceive_Completed;
                    m_CurrentReceiveEvent = null;
                }

                eventArgs = m_ExclusiveReceiveEvent;
                m_ExclusiveReceiveEvent = null;
                return true;
            }
        }

        /// <summary>
        /// Starts the receiver
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the receiver is shutdowned
        /// </exception>
        protected void StartReceive()
        {
            if (TryStartReceive())
                return;

            throw new InvalidOperationException("Failed to start the receiver");
        }

        /// <summary>
        /// Starts the receiver
        /// </summary>
        /// <returns>returns false if the receiver shutdowned, else returns true</returns>
        protected bool TryStartReceive()
        {
            lock (SyncObject)
            {
                if (HasStateFlags(StateFlags.ReceiveShutdown))
                    return false;

                if (HasStateFlags(StateFlags.ReceiverStarted))
                    return true;

                SetStateFlags(StateFlags.ReceiverStartedOrReceiving);
            }

            ThreadPool.QueueUserWorkItem(o => StartAsyncReceive());
            return true;
        }

        /// <summary>
        /// Tells the receiver to don't continue reciving
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the receiver is receiving
        /// </exception>
        protected void StopReceive()
        {
            if (TryStopReceive())
                return;

            throw new InvalidOperationException("Failed to stop the receiver");
        }

        /// <summary>
        /// Tells the receiver to don't continue receiving
        /// </summary>
        /// <returns>Returns false if the receiver is receiving, else returns true</returns>
        protected bool TryStopReceive()
        {
            lock (SyncObject)
            {
                if (!HasStateFlags(StateFlags.ReceiverStarted) || HasStateFlags(StateFlags.ReceiveShutdown))
                    return true;

                if (HasStateFlags(StateFlags.IsReceiving))
                    return false;

                UnsetStateFlags(StateFlags.ReceiverStarted);
                return true;
            }
        }

        private async void StartAsyncReceive()
        {
            if (m_ExclusiveReceiveEvent == null)
            {
                var (taken, eventArgs) = await ClientService.ReceiveAsyncEventArgsPool.TryTakeAsync();
                if (!taken)
                {
                    await HandleClose();
                    return;
                }

                m_CurrentReceiveEvent = eventArgs;
            }
            else
            {
                m_CurrentReceiveEvent = m_ExclusiveReceiveEvent;
            }

            m_CurrentReceiveEvent.Completed += AsyncReceive_Completed;

            try
            {
                if (ClientSocket.ReceiveAsync(m_CurrentReceiveEvent))
                    return;
            }
            catch (SocketException ex)
            {
                m_CurrentReceiveEvent.Completed -= AsyncReceive_Completed;

                if (m_ExclusiveReceiveEvent == null)
                {
                    ClientService.ReceiveAsyncEventArgsPool.Return(m_CurrentReceiveEvent);
                }

                m_CurrentReceiveEvent = null;
                await HandleReceiveSocketError(ex.SocketErrorCode);
                return;
            }
            catch (ObjectDisposedException)
            {
                m_CurrentReceiveEvent.Completed -= AsyncReceive_Completed;

                if (m_ExclusiveReceiveEvent == null)
                {
                    ClientService.ReceiveAsyncEventArgsPool.Return(m_CurrentReceiveEvent);
                }

                m_CurrentReceiveEvent = null;
                await HandleClose();
                return;
            }

            ContinueAsyncReceive();
        }

        private async void ContinueAsyncReceive()
        {
            var _continue = await ProcessCurrentAsyncReceiveEvent();
            if (!_continue)
            {
                if (m_CurrentReceiveEvent != null)
                {
                    m_CurrentReceiveEvent.Completed -= AsyncReceive_Completed;

                    if (m_CurrentReceiveEvent != m_ExclusiveReceiveEvent)
                        ClientService.ReceiveAsyncEventArgsPool.Return(m_CurrentReceiveEvent);

                    m_CurrentReceiveEvent = null;
                }

                return;
            }

            if (m_CurrentReceiveEvent == null)
            {
                if (m_ExclusiveReceiveEvent != null)
                {
                    m_CurrentReceiveEvent = m_ExclusiveReceiveEvent;
                    m_CurrentReceiveEvent.Completed += AsyncReceive_Completed;
                }
                else
                {
                    var (taken, eventArgs) = await ClientService.ReceiveAsyncEventArgsPool.TryTakeAsync();
                    if (!taken)
                    {
                        await HandleClose();
                        return;
                    }

                    m_CurrentReceiveEvent = eventArgs;
                    m_CurrentReceiveEvent.Completed += AsyncReceive_Completed;
                }
            }
            else if (ClientService.ReceiveAsyncEventArgsPool.IsCyclic)
            {
                if (m_CurrentReceiveEvent != m_ExclusiveReceiveEvent)
                {
                    m_CurrentReceiveEvent.Completed -= AsyncReceive_Completed;
                    ClientService.ReceiveAsyncEventArgsPool.Return(m_CurrentReceiveEvent);
                    var (taken, eventArgs) = await ClientService.ReceiveAsyncEventArgsPool.TryTakeAsync();
                    if (!taken)
                    {
                        m_CurrentReceiveEvent = null;
                        await HandleClose();
                        return;
                    }

                    m_CurrentReceiveEvent = eventArgs;
                    m_CurrentReceiveEvent.Completed += AsyncReceive_Completed;
                }
            }

            try
            {
                if (ClientSocket.ReceiveAsync(m_CurrentReceiveEvent))
                    return;
            }
            catch (SocketException ex)
            {
                m_CurrentReceiveEvent.Completed -= AsyncReceive_Completed;

                if (m_ExclusiveReceiveEvent == null)
                {
                    ClientService.ReceiveAsyncEventArgsPool.Return(m_CurrentReceiveEvent);
                }

                m_CurrentReceiveEvent = null;
                await HandleReceiveSocketError(ex.SocketErrorCode);
                return;
            }
            catch (ObjectDisposedException)
            {
                m_CurrentReceiveEvent.Completed -= AsyncReceive_Completed;

                if (m_ExclusiveReceiveEvent == null)
                {
                    ClientService.ReceiveAsyncEventArgsPool.Return(m_CurrentReceiveEvent);
                }

                m_CurrentReceiveEvent = null;
                await HandleClose();
                return;
            }

            ContinueAsyncReceive();
        }

        private void AsyncReceive_Completed(object sender, SocketAsyncEventArgs e)
        {
            ContinueAsyncReceive();
        }

        private async ValueTask<bool> ProcessCurrentAsyncReceiveEvent()
        {
            if (m_CurrentReceiveEvent.SocketError != SocketError.Success)
            {
                await HandleReceiveSocketError(m_CurrentReceiveEvent.SocketError);
                return false;
            }

            if (m_CurrentReceiveEvent.BytesTransferred <= 0)
            {
                await HandleReceiveShutdown();
                return false;
            }

            bool receiveBufferFilled = (m_CurrentReceiveEvent.BytesTransferred ==
                                        m_CurrentReceiveEvent.Count);

            lock (SyncObject)
            {
                UnsetStateFlags(StateFlags.IsReceiving);
            }

#if NETCOREAPP2_1
            await OnReceive(m_CurrentReceiveEvent.MemoryBuffer.Slice(m_CurrentReceiveEvent.BytesTransferred));
#else
            await OnReceive(new Memory<byte>(m_CurrentReceiveEvent.Buffer,
                                             m_CurrentReceiveEvent.Offset,
                                             m_CurrentReceiveEvent.BytesTransferred));
#endif

            lock (SyncObject)
            {
                if (!HasStateFlags(StateFlags.ReceiverStarted))
                    return false;

                SetStateFlags(StateFlags.IsReceiving);
            }

            if (!receiveBufferFilled)
                return true;

            while (true)
            {
                int bytesAvailable;
                try
                {
                    bytesAvailable = ClientSocket.Available;
                    if (bytesAvailable <= 0)
                        return true;
                }
                catch (SocketException ex)
                {
                    await HandleReceiveSocketError(ex.SocketErrorCode);
                    return false;
                }
                catch (ObjectDisposedException)
                {
                    await HandleClose();
                    return false;
                }

                if (m_CurrentReceiveEvent == null)
                {
                    if (await ReceiveSyncUsingArrayPool(bytesAvailable))
                        continue;

                    return false;
                }

                if (m_CurrentReceiveEvent.Count < bytesAvailable)
                {
                    // TODO: Improve if possible

                    if (await ReceiveSyncUsingArrayPool(bytesAvailable))
                        continue;

                    return false;
                }

                if (await ReceiveSyncUsingCurrentReceiveEvent())
                    continue;

                return false;
            }
        }

        private ValueTask<bool> ReceiveSyncUsingCurrentReceiveEvent()
        {
#if NETCOREAPP2_1
            return ReceiveSync(m_CurrentReceiveEvent.MemoryBuffer);
#else
            return ReceiveSync(m_CurrentReceiveEvent.Buffer,
                               m_CurrentReceiveEvent.Offset,
                               m_CurrentReceiveEvent.Count);
#endif
        }

        private async ValueTask<bool> ReceiveSyncUsingArrayPool(int minimumBufferLength)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(minimumBufferLength);
            try
            {
#if NETCOREAPP2_1
                return await ReceiveSync(buffer);
#else
                return await ReceiveSync(buffer, 0, buffer.Length);
#endif
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

#if NETCOREAPP2_1
        private async ValueTask<bool> ReceiveSync(Memory<byte> memory)

#else
        private async ValueTask<bool> ReceiveSync(byte[] buffer, int offset, int count)
#endif
        {
            int bytesRead;
            try
            {
#if NETCOREAPP2_1
                bytesRead = ClientSocket.Receive(memory.Span);
#else
                bytesRead = ClientSocket.Receive(buffer, offset, count, SocketFlags.None);
#endif
            }
            catch (SocketException ex)
            {
                if (ex.SocketErrorCode == SocketError.WouldBlock)
                    return true;

                await HandleReceiveSocketError(ex.SocketErrorCode);
                return false;
            }
            catch (ObjectDisposedException)
            {
                await HandleClose();
                return false;
            }

            if (bytesRead <= 0)
            {
                await HandleReceiveShutdown();
                return false;
            }

            lock (SyncObject)
            {
                UnsetStateFlags(StateFlags.IsReceiving);
            }

#if NETCOREAPP2_1
            await OnReceive(memory.Slice(bytesRead));
#else
            await OnReceive(new Memory<byte>(buffer, offset, bytesRead));
#endif

            lock (SyncObject)
            {
                if (!HasStateFlags(StateFlags.ReceiverStarted))
                    return false;

                SetStateFlags(StateFlags.IsReceiving);
            }

            return true;
        }

        private async ValueTask HandleReceiveSocketError(SocketError socketError)
        {
            if (socketError == SocketError.OperationAborted)
            {
                await HandleClose();
                return;
            }

            lock (SyncObject)
            {
                if (HasStateFlags(StateFlags.ReceiveShutdown))
                    return;

                SetStateFlags(StateFlags.ReceiveShutdown);
            }

            if (socketError == SocketError.Shutdown)
            {
                await OnReceiveShutdown();
            }
            else 
            {
                await OnReceiveError(socketError);
            }
        }

        private async ValueTask HandleReceiveShutdown()
        {
            lock (SyncObject)
            {
                if (HasStateFlags(StateFlags.ReceiveShutdown))
                    return;

                SetStateFlags(StateFlags.ReceiveShutdown);
            }

            await OnReceiveShutdown();
        }

        private void CheckReceiveEventArgs(SocketAsyncEventArgs eventArgs)
        {
            if (eventArgs == null)
                throw new ArgumentNullException(nameof(eventArgs));

#if NETCOREAPP2_1
            if (eventArgs.MemoryBuffer.Length == 0)
                throw new ArgumentNullException(nameof(eventArgs.Buffer) + " or " + nameof(eventArgs.MemoryBuffer) + " is not set");
#else
            if (eventArgs.Buffer == null)
               throw new ArgumentNullException(nameof(eventArgs.Buffer));
#endif
        }
    }
}
