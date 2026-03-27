using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using VRProject.Domain.Interaction.Interfaces;

namespace VRProject.Infrastructure.XR
{
    /// <summary>
    /// ILanguageModel implementation that sends chat requests to the Python
    /// AI server via WebSocket, which proxies to Ollama.
    /// </summary>
    public sealed class OllamaWebSocketAdapter : ILanguageModel
    {
        private readonly AIServerConnection _connection;
        private readonly StringBuilder _fullTextBuffer = new();
        private readonly List<FunctionCallData> _functionCalls = new();
        private bool _isBusy;

        public bool IsBusy => _isBusy;
        public event Action<string> OnTextDelta;
        public event Action<string> OnResponseComplete;
        public event Action<FunctionCallData> OnFunctionCall;
        public event Action<string> OnError;

        public OllamaWebSocketAdapter(AIServerConnection connection)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }

        public async void SendChat(string prompt, string systemPrompt, bool useTools = true)
        {
            if (_isBusy)
            {
                OnError?.Invoke("LLM is already processing a request.");
                return;
            }

            _isBusy = true;
            _fullTextBuffer.Clear();
            _functionCalls.Clear();

            try
            {
                string json = JsonUtility.ToJson(new ChatPayload
                {
                    type = "chat",
                    prompt = prompt,
                    system = systemPrompt,
                    use_tools = useTools,
                });
                await _connection.SendTextAsync(json);
            }
            catch (Exception ex)
            {
                _isBusy = false;
                OnError?.Invoke($"Failed to send chat: {ex.Message}");
            }
        }

        public void HandleServerMessage(string type, string json)
        {
            switch (type)
            {
                case "text_delta":
                {
                    var evt = JsonUtility.FromJson<TextDeltaPayload>(json);
                    if (!string.IsNullOrEmpty(evt.content))
                    {
                        _fullTextBuffer.Append(evt.content);
                        OnTextDelta?.Invoke(evt.content);
                    }
                    break;
                }
                case "function_call":
                {
                    var evt = JsonUtility.FromJson<FunctionCallPayload>(json);
                    if (!string.IsNullOrEmpty(evt.name))
                    {
                        var data = new FunctionCallData(evt.name, new Dictionary<string, object>());
                        _functionCalls.Add(data);
                        OnFunctionCall?.Invoke(data);
                    }
                    break;
                }
                case "done":
                {
                    var evt = JsonUtility.FromJson<DonePayload>(json);
                    string finalText = !string.IsNullOrEmpty(evt.full_text)
                        ? evt.full_text
                        : _fullTextBuffer.ToString();

                    _isBusy = false;
                    OnResponseComplete?.Invoke(finalText);
                    break;
                }
                case "error":
                {
                    var evt = JsonUtility.FromJson<ErrorPayload>(json);
                    _isBusy = false;
                    OnError?.Invoke(evt.content ?? "Unknown error");
                    break;
                }
            }
        }

        [Serializable]
        private struct ChatPayload
        {
            public string type;
            public string prompt;
            public string system;
            public bool use_tools;
        }

        [Serializable]
        private struct TextDeltaPayload
        {
            public string type;
            public string content;
        }

        [Serializable]
        private struct FunctionCallPayload
        {
            public string type;
            public string name;
        }

        [Serializable]
        private struct DonePayload
        {
            public string type;
            public string full_text;
        }

        [Serializable]
        private struct ErrorPayload
        {
            public string type;
            public string content;
        }
    }
}
