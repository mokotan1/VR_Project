using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace VRProject.Infrastructure.XR
{
    public sealed class AIServerConnection : IDisposable
    {
        private ClientWebSocket _ws;
        private CancellationTokenSource _cts;
        private readonly ConcurrentQueue<string> _incomingTextMessages = new();
        private readonly string _serverUri;
        private bool _disposed;

        public bool IsConnected => _ws?.State == WebSocketState.Open;

        public AIServerConnection(string serverUri = "ws://localhost:8765/ws")
        {
            _serverUri = serverUri;
        }

        public async Task ConnectAsync()
        {
            if (IsConnected) return;

            _cts = new CancellationTokenSource();
            _ws = new ClientWebSocket();
            await _ws.ConnectAsync(new Uri(_serverUri), _cts.Token);

            _ = Task.Run(() => ReceiveLoop(_cts.Token));
            Debug.Log($"[AIServer] Connected to {_serverUri}");
        }

        public async Task SendTextAsync(string json)
        {
            if (!IsConnected) return;
            var bytes = Encoding.UTF8.GetBytes(json);
            await _ws.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                true,
                _cts.Token
            );
        }

        public async Task SendBinaryAsync(byte[] data, int offset, int count)
        {
            if (!IsConnected) return;
            await _ws.SendAsync(
                new ArraySegment<byte>(data, offset, count),
                WebSocketMessageType.Binary,
                true,
                _cts.Token
            );
        }

        public bool TryDequeueMessage(out string message)
        {
            return _incomingTextMessages.TryDequeue(out message);
        }

        private async Task ReceiveLoop(CancellationToken ct)
        {
            var buffer = new byte[8192];
            var sb = new StringBuilder();

            try
            {
                while (!ct.IsCancellationRequested && _ws.State == WebSocketState.Open)
                {
                    var result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await _ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", ct);
                        break;
                    }

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                        if (result.EndOfMessage)
                        {
                            _incomingTextMessages.Enqueue(sb.ToString());
                            sb.Clear();
                        }
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (WebSocketException ex)
            {
                Debug.LogWarning($"[AIServer] WebSocket error: {ex.Message}");
            }
        }

        public async Task DisconnectAsync()
        {
            if (_ws == null) return;

            try
            {
                if (_ws.State == WebSocketState.Open)
                    await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AIServer] Disconnect error: {ex.Message}");
            }
            finally
            {
                _cts?.Cancel();
                _cts?.Dispose();
                _ws?.Dispose();
                _ws = null;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _cts?.Cancel();
            _cts?.Dispose();
            _ws?.Dispose();
        }
    }
}
