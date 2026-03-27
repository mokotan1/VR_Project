using System;
using System.Threading.Tasks;
using UnityEngine;
using VRProject.Domain.Interaction.Interfaces;

namespace VRProject.Infrastructure.XR
{
    /// <summary>
    /// ISpeechRecognizer implementation that streams PCM audio to the Python
    /// AI server via WebSocket for Whisper STT.
    /// </summary>
    public sealed class WhisperWebSocketAdapter : ISpeechRecognizer
    {
        private readonly AIServerConnection _connection;
        private readonly MicrophoneCaptureAdapter _mic;
        private bool _isListening;

        public bool IsListening => _isListening;
        public event Action<string> OnTranscriptionReceived;

        public WhisperWebSocketAdapter(AIServerConnection connection, MicrophoneCaptureAdapter mic)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _mic = mic ?? throw new ArgumentNullException(nameof(mic));
        }

        public void StartListening()
        {
            if (_isListening) return;
            _isListening = true;

            _mic.OnAudioChunkReady += HandleAudioChunk;
            _mic.StartCapture();

            _ = _connection.SendTextAsync("{\"type\":\"audio_start\"}");
        }

        public void StopListening()
        {
            if (!_isListening) return;
            _isListening = false;

            byte[] allAudio = _mic.StopAndGetAllAudio();
            _mic.OnAudioChunkReady -= HandleAudioChunk;

            _ = SendStopSequence(allAudio);
        }

        private async void HandleAudioChunk(byte[] pcmChunk)
        {
            if (!_isListening || !_connection.IsConnected) return;

            try
            {
                await _connection.SendBinaryAsync(pcmChunk, 0, pcmChunk.Length);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Whisper] Failed to send audio chunk: {ex.Message}");
            }
        }

        private async Task SendStopSequence(byte[] finalAudio)
        {
            try
            {
                if (finalAudio != null && finalAudio.Length > 0)
                    await _connection.SendBinaryAsync(finalAudio, 0, finalAudio.Length);

                await _connection.SendTextAsync("{\"type\":\"audio_end\",\"auto_chat\":true}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Whisper] Failed to send stop: {ex.Message}");
            }
        }

        public void HandleServerMessage(string type, string text)
        {
            if (type == "transcription" && !string.IsNullOrEmpty(text))
            {
                OnTranscriptionReceived?.Invoke(text);
            }
        }
    }
}
