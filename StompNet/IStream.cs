using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using StompNet.Models;

namespace StompNet
{
    public interface IStream : IDisposable
    {
        Task WriteByteAsync(byte value, CancellationToken cancellationToken);
        Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken);
        Task FlushAsync(CancellationToken cancellationToken);
        Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken);
    }

    public class WebSocketTransport : IStream
    {
        private readonly ClientWebSocket _webSocket;

        public WebSocketTransport (ClientWebSocket webSocket)
        {
            _webSocket = webSocket;
        }
        public async Task WriteByteAsync(byte value, CancellationToken cancellationToken)
        {
            var endOfMessage = value == StompOctets.EndOfFrameByte;

            await _webSocket.SendAsync(new ArraySegment<byte>(new []{value}), WebSocketMessageType.Text, endOfMessage, cancellationToken);
        }

        public async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var text = Encoding.UTF8.GetString(new ArraySegment<byte>(buffer, offset, count).Array);

            await _webSocket.SendAsync(new ArraySegment<byte>(buffer, offset, count), WebSocketMessageType.Text, false, cancellationToken);
        }

        public async Task FlushAsync(CancellationToken cancellationToken)
        {
            await _webSocket.SendAsync(new ArraySegment<byte>(new byte[]{}, 0, 0), WebSocketMessageType.Text, true, cancellationToken);
        }

        public async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var webSocketResult = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer, offset, count), cancellationToken);
            return webSocketResult.Count;
        }

        public void Dispose()
        {
        }
    }

    public class StreamTransport : IStream
    {
        private readonly Stream _stream;
        private readonly bool _bufferedStreamOwner;

        public StreamTransport(Stream stream, int? bufferCapacity = null)
        {
            _bufferedStreamOwner = !(stream is BufferedStream) || bufferCapacity.HasValue;

            _stream =
                !_bufferedStreamOwner ? stream // CAREFUL! This is a negative(!) condition.
                : bufferCapacity == null ? new BufferedStream(stream)
                : new BufferedStream(stream, bufferCapacity.Value);
        }

        public Task WriteByteAsync(byte buffer, CancellationToken cancellationToken)
        {
            _stream.WriteByte(buffer);
            return Task.FromResult(0);
        }

        public async Task SendAsync(byte[] buffer, int count, CancellationToken cancellationToken)
        {
            await _stream.WriteAsync(buffer, 0, count, cancellationToken);
        }

        public async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            await _stream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public async Task FlushAsync(CancellationToken cancellationToken)
        {
            await _stream.FlushAsync(cancellationToken);
        }

        public async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return await _stream.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public void Dispose()
        {
            if (_bufferedStreamOwner)
                _stream.Dispose();
        }
    }
}