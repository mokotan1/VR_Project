using System;
using System.Collections.Generic;
using UnityEngine;
using VRProject.Application.Common.Services;
using VRProject.Application.Interaction.DTOs;
using VRProject.Application.Interaction.Services;
using VRProject.Domain.Interaction.Entities;
using VRProject.Domain.Interaction.Interfaces;
using VRProject.Infrastructure.XR;
using VRProject.Presentation.Interaction.Views;

namespace VRProject.Presentation.Interaction.Controllers
{
    /// <summary>
    /// Orchestrates the full voice pipeline: Mic -> STT -> LLM -> UI.
    /// Attach to a GameObject in the scene alongside MicrophoneCaptureAdapter.
    /// </summary>
    public sealed class VoiceInputController : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private string serverUri = "ws://localhost:8765/ws";
        [SerializeField] private string systemPrompt = "당신은 FPS VR 게임의 AI 도우미입니다. 간결하게 답변하세요.";
        [SerializeField] private KeyCode pushToTalkKey = KeyCode.V;

        [Header("References")]
        [SerializeField] private DialogueView dialogueView;

        private AIServerConnection _connection;
        private MicrophoneCaptureAdapter _mic;
        private WhisperWebSocketAdapter _whisper;
        private OllamaWebSocketAdapter _ollama;
        private ProcessVoiceCommandUseCase _voiceCommandUseCase;
        private DialogueSession _session;

        private bool _isInitialized;

        private async void Start()
        {
            _mic = GetComponent<MicrophoneCaptureAdapter>();
            if (_mic == null)
                _mic = gameObject.AddComponent<MicrophoneCaptureAdapter>();

            _connection = new AIServerConnection(serverUri);
            _whisper = new WhisperWebSocketAdapter(_connection, _mic);
            _ollama = new OllamaWebSocketAdapter(_connection);

            _session = new DialogueSession("voice-cmd", systemPrompt);

            _voiceCommandUseCase = new ProcessVoiceCommandUseCase(_ollama, _session);

            _whisper.OnTranscriptionReceived += HandleTranscription;
            _ollama.OnTextDelta += HandleTextDelta;
            _ollama.OnResponseComplete += HandleResponseComplete;
            _ollama.OnFunctionCall += HandleFunctionCall;
            _ollama.OnError += HandleError;

            try
            {
                await _connection.ConnectAsync();
                _isInitialized = true;
                Debug.Log("[VoiceInput] Connected and ready.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[VoiceInput] Failed to connect: {ex.Message}");
            }
        }

        private void Update()
        {
            if (!_isInitialized) return;

            ProcessIncomingMessages();

            if (Input.GetKeyDown(pushToTalkKey))
            {
                _whisper.StartListening();
                dialogueView?.ShowListeningIndicator(true);
            }
            else if (Input.GetKeyUp(pushToTalkKey))
            {
                _whisper.StopListening();
                dialogueView?.ShowListeningIndicator(false);
            }
        }

        private void ProcessIncomingMessages()
        {
            while (_connection.TryDequeueMessage(out string json))
            {
                try
                {
                    var header = JsonUtility.FromJson<MessageHeader>(json);
                    _whisper.HandleServerMessage(header.type, header.text);
                    _ollama.HandleServerMessage(header.type, json);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[VoiceInput] Failed to parse message: {ex.Message}");
                }
            }
        }

        private void HandleTranscription(string text)
        {
            Debug.Log($"[STT] {text}");
            dialogueView?.SetPlayerText(text);
        }

        private void HandleTextDelta(string delta)
        {
            dialogueView?.AppendAIText(delta);
        }

        private void HandleResponseComplete(string fullText)
        {
            Debug.Log($"[LLM] Complete: {fullText}");
            dialogueView?.FinalizeAIText(fullText);
        }

        private void HandleFunctionCall(FunctionCallData data)
        {
            Debug.Log($"[FunctionCall] {data.Name}");
            ExecuteGameAction(data);
        }

        private void HandleError(string error)
        {
            Debug.LogError($"[AI Error] {error}");
            dialogueView?.ShowError(error);
        }

        private void ExecuteGameAction(FunctionCallData data)
        {
            switch (data.Name)
            {
                case "execute_command":
                    Debug.Log("[Game] Execute command triggered");
                    break;
                case "npc_emote":
                    Debug.Log("[Game] NPC emote triggered");
                    break;
                case "give_hint":
                    Debug.Log("[Game] Hint triggered");
                    break;
                default:
                    Debug.LogWarning($"[Game] Unknown function: {data.Name}");
                    break;
            }
        }

        private async void OnDestroy()
        {
            _whisper.OnTranscriptionReceived -= HandleTranscription;
            _ollama.OnTextDelta -= HandleTextDelta;
            _ollama.OnResponseComplete -= HandleResponseComplete;
            _ollama.OnFunctionCall -= HandleFunctionCall;
            _ollama.OnError -= HandleError;

            if (_connection != null)
                await _connection.DisconnectAsync();

            _connection?.Dispose();
        }

        [Serializable]
        private struct MessageHeader
        {
            public string type;
            public string content;
            public string text;
            public string full_text;
            public string name;
        }
    }
}
