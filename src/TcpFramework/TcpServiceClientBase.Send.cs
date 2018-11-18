using System;
using System.Buffers;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace TcpFramework
{
    public abstract partial class TcpServiceClientBase
    {
        /// <summary>
        /// Sends buffer synchronously (blocking) with <see cref="SocketFlags.None"/>
        /// </summary>
        /// <returns>Returns false if send fails, else returns true</returns>
        protected bool Send(byte[] buffer, int offset, int count) => Send(buffer, offset, count, SocketFlags.None);

        /// <summary>
        /// Sends buffer synchronously (blocking)
        /// </summary>
        /// <returns>Returns false if send fails, else returns true</returns>
        protected bool Send(byte[] buffer, int offset, int count, SocketFlags socketFlags)
        {
            if (SendShutdowned())
                return false;

            try
            {
                int sent = ClientSocket.Send(buffer, offset, count, socketFlags);

                while (sent < count)
                {
                    sent += ClientSocket.Send(buffer, offset + sent, count - sent, socketFlags);
                }

                return true;
            }
            catch (SocketException ex)
            {
                HandleSendSocketError(ex.SocketErrorCode).GetAwaiter().GetResult();
                return false;
            }
            catch (ObjectDisposedException)
            {
                HandleClose().GetAwaiter().GetResult();
                return false;
            }
        }

#if NETCOREAPP2_1

        /// <summary>
        /// Sends buffer synchronously (blocking) with <see cref="SocketFlags.None"/>
        /// </summary>
        /// <returns>Returns false if send fails, else returns true</returns>
        protected bool Send(ReadOnlySpan<byte> span) => Send(span, SocketFlags.None);

        /// <summary>
        /// Sends buffer synchronously (blocking)
        /// </summary>
        /// <returns>Returns false if send fails, else returns true</returns>
        protected bool Send(ReadOnlySpan<byte> span, SocketFlags socketFlags)
        {
            if (SendShutdowned())
                return false;

            try
            {
                int sent = ClientSocket.Send(span, socketFlags);

                while (sent < span.Length)
                {
                    sent += ClientSocket.Send(span.Slice(sent, span.Length - sent), socketFlags);
                }

                return true;
            }
            catch (SocketException ex)
            {
                HandleSendSocketError(ex.SocketErrorCode).GetAwaiter().GetResult();
                return false;
            }
            catch (ObjectDisposedException)
            {
                HandleClose().GetAwaiter().GetResult();
                return false;
            }
        }

#endif

        /// <summary>
        /// Sends copy of buffer synchronously (blocking) with <see cref="SocketFlags.None"/>
        /// </summary>
        /// <returns>Returns false if send fails, else returns true</returns>
        protected bool SendCopy(byte[] buffer, int offset, int count) => SendCopy(buffer, offset, count, SocketFlags.None);

        /// <summary>
        /// Sends copy of buffer synchronously (blocking)
        /// </summary>
        /// <returns>Returns false if send fails, else returns true</returns>
        protected bool SendCopy(byte[] buffer, int offset, int count, SocketFlags socketFlags)
        {
            if (SendShutdowned())
                return false;
            
            var rentedBuffer = ArrayPool<byte>.Shared.Rent(count);
            Buffer.BlockCopy(buffer, offset, rentedBuffer, 0, count);

            try
            {
                int sent = ClientSocket.Send(rentedBuffer, 0, count, socketFlags);

                if (sent == count)
                    return true;

                while (sent < count)
                {
                    sent += ClientSocket.Send(rentedBuffer, sent, count - sent, socketFlags);
                }

                return true;
            }
            catch (SocketException ex)
            {
                HandleSendSocketError(ex.SocketErrorCode).GetAwaiter().GetResult();
                return false;
            }
            catch (ObjectDisposedException)
            {
                HandleClose().GetAwaiter().GetResult();
                return false;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rentedBuffer);
            }
        }

#if NETCOREAPP2_1

        /// <summary>
        /// Sends copy of buffer synchronously (blocking) with <see cref="SocketFlags.None"/>
        /// </summary>
        /// <returns>Returns false if send fails, else returns true</returns>
        protected bool SendCopy(ReadOnlySpan<byte> span) => SendCopy(span, SocketFlags.None);

        /// <summary>
        /// Sends copy of buffer synchronously (blocking)
        /// </summary>
        /// <returns>Returns false if send fails, else returns true</returns>
        protected bool SendCopy(ReadOnlySpan<byte> span, SocketFlags socketFlags)
        {
            if (SendShutdowned())
                return false;
            
            var rentedBuffer = ArrayPool<byte>.Shared.Rent(span.Length);
            span.CopyTo(rentedBuffer);

            try
            {
                int sent = ClientSocket.Send(rentedBuffer, 0, span.Length, socketFlags);

                while (sent < span.Length)
                {
                    sent += ClientSocket.Send(rentedBuffer, sent, span.Length - sent, socketFlags);
                }

                return true;
            }
            catch (SocketException ex)
            {
                HandleSendSocketError(ex.SocketErrorCode).GetAwaiter().GetResult();
                return false;
            }
            catch (ObjectDisposedException)
            {
                HandleClose().GetAwaiter().GetResult();
                return false;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rentedBuffer);
            }
        }

#endif

        /// <summary>
        /// Sends copy of buffer non-blocking with <see cref="SocketFlags.None"/>
        /// </summary>
        /// <returns>Returns false if send fails, else returns true with number of sent bytes</returns>
        protected (bool, int) SendNonBlocking(byte[] buffer, int offset, int count) => SendNonBlocking(buffer, offset, count, SocketFlags.None);

        /// <summary>
        /// Sends copy of buffer non-blocking
        /// </summary>
        /// <returns>Returns false if send fails, else returns true with number of sent bytes</returns>
        protected (bool, int) SendNonBlocking(byte[] buffer, int offset, int count, SocketFlags socketFlags)
        {
            if (SendShutdowned())
                return (false, 0);

            int sent = 0;
            try
            {
                sent = ClientSocket.Send(buffer, offset, count, socketFlags);

                while (sent < count)
                {
                    sent += ClientSocket.Send(buffer, offset + sent, count - sent, socketFlags);
                }

                return (true, sent);
            }
            catch (SocketException ex)
            {
                if (ex.SocketErrorCode == SocketError.WouldBlock)
                {
                    return (true, sent);
                }

                HandleSendSocketError(ex.SocketErrorCode).GetAwaiter().GetResult();
                return (false, 0);
            }
            catch (ObjectDisposedException)
            {
                HandleClose().GetAwaiter().GetResult();
                return (false, 0);
            }
        }

#if NETCOREAPP2_1

        /// <summary>
        /// Sends copy of buffer non-blocking with <see cref="SocketFlags.None"/>
        /// </summary>
        /// <returns>Returns false if send fails, else returns true with number of sent bytes</returns>
        protected (bool, int) SendNonBlocking(ReadOnlySpan<byte> span) => SendNonBlocking(span, SocketFlags.None);

        /// <summary>
        /// Sends copy of buffer non-blocking
        /// </summary>
        /// <returns>Returns false if send fails, else returns true with number of sent bytes</returns>
        protected (bool, int) SendNonBlocking(ReadOnlySpan<byte> span, SocketFlags socketFlags)
        {
            if (SendShutdowned())
                return (false, 0);

            int sent = 0;
            try
            {
                sent = ClientSocket.Send(span, socketFlags);

                while (sent < span.Length)
                {
                    sent += ClientSocket.Send(span.Slice(sent, span.Length - sent), socketFlags);
                }

                return (true, sent);
            }
            catch (SocketException ex)
            {
                if (ex.SocketErrorCode == SocketError.WouldBlock)
                {
                    return (true, sent);
                }

                HandleSendSocketError(ex.SocketErrorCode).GetAwaiter().GetResult();
                return (false, 0);
            }
            catch (ObjectDisposedException)
            {
                HandleClose().GetAwaiter().GetResult();
                return (false, 0);
            }
        }

#endif

        /// <summary>
        /// Begins an asynchronously send operation using <see cref="SocketAsyncEventArgs"/>
        /// </summary>
        /// <returns>Returns false if send fails, else returns true</returns>
        protected async ValueTask<bool> SendAsync(SocketAsyncEventArgs eventArgs)
        {
            if (SendShutdowned())
                return false;

            var userTokenBackup = eventArgs.UserToken;
            eventArgs.Completed += AsyncSend_Completed;

            try
            {
                return await SendAsyncCore(eventArgs);
            }
            finally
            {
                eventArgs.UserToken = userTokenBackup;
                eventArgs.Completed -= AsyncSend_Completed;
            }
        }

        /// <summary>
        /// Sends buffer asynchronously with <see cref="SocketFlags.None"/>
        /// </summary>
        /// <returns>Returns false if send fails, else returns true</returns>
        protected ValueTask<bool> SendAsync(byte[] buffer, int offset, int count)
        {
            return SendAsync(buffer, offset, count, SocketFlags.None);
        }


        /// <summary>
        /// Sends buffer asynchronously
        /// </summary>
        /// <returns>Returns false if send fails, else returns true</returns>
        protected async ValueTask<bool> SendAsync(byte[] buffer, int offset, int count, SocketFlags socketFlags)
        {
            if (SendShutdowned())
                return false;

            return await SendAsyncCore(buffer, offset, count, socketFlags);
        }

#if NETCOREAPP2_1

        /// <summary>
        /// Sends buffer asynchronously with <see cref="SocketFlags.None"/>
        /// </summary>
        /// <returns>Returns false if send fails, else returns true</returns>
        protected ValueTask<bool> SendAsync(Memory<byte> memory)
        {
            return SendAsync(memory, SocketFlags.None);
        }


        /// <summary>
        /// Sends buffer asynchronously
        /// </summary>
        /// <returns>Returns false if send fails, else returns true</returns>
        protected async ValueTask<bool> SendAsync(Memory<byte> memory, SocketFlags socketFlags)
        {
            if (SendShutdowned())
                return false;

            return await SendAsyncCore(memory, socketFlags);
        }

#endif

        /// <summary>
        /// Sends buffer synchronously, if <see cref="SocketError.WouldBlock"/> occurs, retries asynchronously (with <see cref="SocketFlags.None"/>)
        /// </summary>
        /// <returns>Returns false if send fails, else returns true</returns>
        protected ValueTask<bool> SendNonBlockingAsync(byte[] buffer, int offset, int count)
        {
            return SendNonBlockingAsync(buffer, offset, count, SocketFlags.None);
        }

        /// <summary>
        /// Sends buffer synchronously, if <see cref="SocketError.WouldBlock"/> occurs, retries asynchronously
        /// </summary>
        /// <returns>Returns false if send fails, else returns true</returns>
        protected async ValueTask<bool> SendNonBlockingAsync(byte[] buffer, int offset, int count, SocketFlags socketFlags)
        {
            if (SendShutdowned())
                return false;

            int sent = 0;
            try
            {
                sent = ClientSocket.Send(buffer, offset, count, socketFlags);

                if (sent == count)
                    return true;

                while (sent < count)
                {
                    sent += ClientSocket.Send(buffer, offset + sent, count - sent, socketFlags);
                }

                return true;
            }
            catch (SocketException ex)
            {
                if (ex.SocketErrorCode == SocketError.WouldBlock)
                {
                    return await SendAsyncCore(buffer, offset + sent, count - sent, socketFlags);
                }

                await HandleSendSocketError(ex.SocketErrorCode);
                return false;
            }
            catch (ObjectDisposedException)
            {
                await HandleClose();
                return false;
            }
        }

#if NETCOREAPP2_1

        /// <summary>
        /// Sends buffer synchronously, if <see cref="SocketError.WouldBlock"/> occurs, retries asynchronously (with <see cref="SocketFlags.None"/>)
        /// </summary>
        /// <returns>Returns false if send fails, else returns true</returns>
        protected ValueTask<bool> SendNonBlockingAsync(Memory<byte> memory)
        {
            return SendNonBlockingAsync(memory, SocketFlags.None);
        }

        /// <summary>
        /// Sends buffer synchronously, if <see cref="SocketError.WouldBlock"/> occurs, retries asynchronously
        /// </summary>
        /// <returns>Returns false if send fails, else returns true</returns>
        protected async ValueTask<bool> SendNonBlockingAsync(Memory<byte> memory, SocketFlags socketFlags)
        {
            if (SendShutdowned())
                return false;

            int sent = 0;
            try
            {
                sent = ClientSocket.Send(memory.Span, socketFlags);

                while (sent < memory.Length)
                {
                    sent += ClientSocket.Send(memory.Slice(sent, memory.Length - sent).Span, socketFlags);
                }

                return true;
            }
            catch (SocketException ex)
            {
                if (ex.SocketErrorCode == SocketError.WouldBlock)
                {
                    return await SendAsyncCore(memory.Slice(sent, memory.Length - sent), socketFlags);
                }

                await HandleSendSocketError(ex.SocketErrorCode);
                return false;
            }
            catch (ObjectDisposedException)
            {
                await HandleClose();
                return false;
            }
        }

#endif

        /// <summary>
        /// Sends buffer synchronously, if <see cref="SocketError.WouldBlock"/> occurs, retries using copy of buffer asynchronously (with <see cref="SocketFlags.None"/>)
        /// </summary>
        /// <returns>Returns false if send fails, else returns true</returns>
        protected ValueTask<bool> SendCopyNonBlockingAsync(byte[] buffer, int offset, int count)
        {
            return SendCopyNonBlockingAsync(buffer, offset, count, SocketFlags.None);
        }

        /// <summary>
        /// Sends buffer synchronously, if <see cref="SocketError.WouldBlock"/> occurs, retries using copy of buffer asynchronously
        /// </summary>
        /// <returns>Returns false if send fails, else returns true</returns>
        protected async ValueTask<bool> SendCopyNonBlockingAsync(byte[] buffer, int offset, int count, SocketFlags socketFlags)
        {
            if (SendShutdowned())
                return false;

            int sent = 0;
            try
            {
                sent = ClientSocket.Send(buffer, offset, count, socketFlags);

                while (sent < count)
                {
                    sent += ClientSocket.Send(buffer, offset + sent, count - sent, socketFlags);
                }

                return true;
            }
            catch (SocketException ex)
            {
                if (ex.SocketErrorCode == SocketError.WouldBlock)
                {
                    return await SendCopyAsyncCore(buffer, offset + sent, count - sent, socketFlags);
                }

                await HandleSendSocketError(ex.SocketErrorCode);
                return false;
            }
            catch (ObjectDisposedException)
            {
                await HandleClose();
                return false;
            }
        }

#if NETCOREAPP2_1
        /// <summary>
        /// Sends buffer synchronously, if <see cref="SocketError.WouldBlock"/> occurs, retries using copy of buffer asynchronously (with <see cref="SocketFlags.None"/>)
        /// </summary>
        /// <returns>Returns false if send fails, else returns true</returns>
        protected ValueTask<bool> SendCopyNonBlockingAsync(Memory<byte> memory)
        {
            return SendCopyNonBlockingAsync(memory, SocketFlags.None);
        }

        /// <summary>
        /// Sends buffer synchronously, if <see cref="SocketError.WouldBlock"/> occurs, retries using copy of buffer asynchronously
        /// </summary>
        /// <returns>Returns false if send fails, else returns true</returns>
        protected async ValueTask<bool> SendCopyNonBlockingAsync(Memory<byte> memory, SocketFlags socketFlags)
        {
            if (SendShutdowned())
                return false;

            int sent = 0;
            try
            {
                sent = ClientSocket.Send(memory.Span, socketFlags);
                
                while (sent < memory.Length)
                {
                    sent += ClientSocket.Send(memory.Slice(sent, memory.Length - sent).Span, socketFlags);
                }

                return true;
            }
            catch (SocketException ex)
            {
                if (ex.SocketErrorCode == SocketError.WouldBlock)
                {
                    return await SendCopyAsyncCore(memory.Slice(sent, memory.Length - sent), socketFlags);
                }

                await HandleSendSocketError(ex.SocketErrorCode);
                return false;
            }
            catch (ObjectDisposedException)
            {
                await HandleClose();
                return false;
            }
        }
#endif

        private async ValueTask<bool> SendCopyAsyncCore(byte[] buffer, int offset, int count, SocketFlags socketFlags)
        {
            if (count > ClientService.Configuration.SendBufferSize)
            {
                var rentedBuffer = ArrayPool<byte>.Shared.Rent(count);
                Buffer.BlockCopy(buffer, offset, rentedBuffer, 0, count);

                try
                {
                    return await SendAsyncCore(rentedBuffer, 0, count, socketFlags);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(rentedBuffer);
                }
            }

            var (taken, eventArgs) = await ClientService.SendAsyncEventArgsPool.TryTakeAsync();
            if (!taken)
            {
                await HandleClose();
                return false;
            }

            var originalCount = eventArgs.Count;
            eventArgs.Completed += AsyncSend_Completed;
            eventArgs.SocketFlags = socketFlags;
            eventArgs.SetBuffer(eventArgs.Offset, count);

            Buffer.BlockCopy(buffer, offset, eventArgs.Buffer, eventArgs.Offset, count);

            try
            {
                return await SendAsyncCore(eventArgs);
            }
            finally
            {
                eventArgs.Completed -= AsyncSend_Completed;
                eventArgs.SetBuffer(eventArgs.Offset, originalCount);
                ClientService.SendAsyncEventArgsPool.Return(eventArgs);
            }
        }


#if NETCOREAPP2_1
        private async ValueTask<bool> SendCopyAsyncCore(Memory<byte> memory, SocketFlags socketFlags)
        {
            if (ClientService.Configuration.SendBufferSize < memory.Length)
            {
                var rentedBuffer = ArrayPool<byte>.Shared.Rent(memory.Length);
                memory.CopyTo(rentedBuffer);

                try
                {
                    return await SendAsyncCore(rentedBuffer, 0, memory.Length, socketFlags);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(rentedBuffer);
                }
            }

            var (taken, eventArgs) = await ClientService.SendAsyncEventArgsPool.TryTakeAsync();
            if (!taken)
            {
                await HandleClose();
                return false;
            }

            var originalCount = eventArgs.Count;
            eventArgs.Completed += AsyncSend_Completed;
            eventArgs.SocketFlags = socketFlags;
            eventArgs.SetBuffer(eventArgs.Offset, memory.Length);

            memory.CopyTo(new Memory<byte>(eventArgs.Buffer, eventArgs.Offset, memory.Length));

            try
            {
                return await SendAsyncCore(eventArgs);
            }
            finally
            {
                eventArgs.Completed -= AsyncSend_Completed;
                eventArgs.SetBuffer(eventArgs.Offset, originalCount);
                ClientService.SendAsyncEventArgsPool.Return(eventArgs);
            }
        }
#endif

        private async ValueTask<bool> SendAsyncCore(byte[] buffer, int offset, int count, SocketFlags socketFlags)
        {
            var (taken, eventArgs) = await ClientService.BufferlessSendAsyncEventArgsPool.TryTakeAsync();
            if (!taken)
            {
                await HandleClose();
                return false;
            }

            eventArgs.Completed += AsyncSend_Completed;
            eventArgs.SetBuffer(buffer, offset, count);
            eventArgs.SocketFlags = socketFlags;

            try
            {
                return await SendAsyncCore(eventArgs);
            }
            finally
            {
                eventArgs.Completed -= AsyncSend_Completed;
                eventArgs.SetBuffer(null, 0, 0);
                ClientService.BufferlessSendAsyncEventArgsPool.Return(eventArgs);
            }
        }

#if NETCOREAPP2_1
        private async ValueTask<bool> SendAsyncCore(Memory<byte> memory, SocketFlags socketFlags)
        {
            var (taken, eventArgs) = await ClientService.BufferlessSendAsyncEventArgsPool.TryTakeAsync();
            if (!taken)
            {
                await HandleClose();
                return false;
            }

            eventArgs.Completed += AsyncSend_Completed;
            eventArgs.SetBuffer(memory);
            eventArgs.SocketFlags = socketFlags;

            try
            {
                return await SendAsyncCore(eventArgs);
            }
            finally
            {
                eventArgs.Completed -= AsyncSend_Completed;
                eventArgs.SetBuffer(null, 0, 0);
                ClientService.BufferlessSendAsyncEventArgsPool.Return(eventArgs);
            }
        }
#endif

        private async ValueTask<bool> SendAsyncCore(SocketAsyncEventArgs e)
        {
            var (taken, completionSource) = await ClientService.TaskCompletionSourcePool.TryTakeAsync();
            if (!taken)
            {
                await HandleClose();
                return false;
            }

            e.UserToken = completionSource;

            bool completesAsync;
            try
            {
                completesAsync = ClientSocket.SendAsync(e);
            }
            catch (SocketException ex)
            {
                ClientService.TaskCompletionSourcePool.Return(completionSource);
                e.UserToken = null;
                await HandleSendSocketError(ex.SocketErrorCode);
                return false;
            }
            catch (ObjectDisposedException)
            {
                ClientService.TaskCompletionSourcePool.Return(completionSource);
                e.UserToken = null;
                await HandleClose();
                return false;
            }

            if (completesAsync)
            {
                return await completionSource.Task;
            }

            ClientService.TaskCompletionSourcePool.Return(completionSource);
            e.UserToken = null;
            return await ProcessAsyncSendEvent(e);
        }

        private async void AsyncSend_Completed(object sender, SocketAsyncEventArgs e)
        {
            var completionSource = e.UserToken as TaskCompletionSource<bool>;
            e.UserToken = null;
            var result = await ProcessAsyncSendEvent(e);
            completionSource.SetResult(result);
        }

        private async ValueTask<bool> ProcessAsyncSendEvent(SocketAsyncEventArgs e)
        {
            if (e.BytesTransferred != e.Count)
                throw new Exception("e.BytesTransferred != e.Count, Something went wrong.");

            if (e.SocketError != SocketError.Success)
            {
                await HandleSendSocketError(e.SocketError);
                return false;
            }

            return true;
        }

        private ValueTask HandleSendSocketError(SocketError socketError)
        {
            if (socketError == SocketError.Shutdown)
            {
                return HandleSendShutdown();
            }
            else if (socketError == SocketError.OperationAborted)
            {
                return HandleClose();
            }
            else
            {
                return OnSendError(socketError);
            }
        }

        private async ValueTask HandleSendShutdown()
        {
            lock (SyncObject)
            {
                if (HasStateFlags(StateFlags.SendShutdown))
                    return;

                SetStateFlags(StateFlags.SendShutdown);
            }
            
            await OnSendShutdown();
        }
    }
}
