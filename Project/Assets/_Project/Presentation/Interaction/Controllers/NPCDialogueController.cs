using System;
using UnityEngine;
using VRProject.Application.Interaction.DTOs;
using VRProject.Application.Interaction.Services;
using VRProject.Domain.Interaction.Entities;
using VRProject.Domain.Interaction.Interfaces;
using VRProject.Infrastructure.XR;
using VRProject.Presentation.Interaction.Views;

namespace VRProject.Presentation.Interaction.Controllers
{
    /// <summary>
    /// Per-NPC dialogue controller. Each NPC has its own session and system prompt.
    /// Reuses the shared AIServerConnection from VoiceInputController.
    /// </summary>
    public sealed class NPCDialogueController : MonoBehaviour
    {
        [Header("NPC Settings")]
        [SerializeField] private string npcId = "npc_soldier";
        [SerializeField] [TextArea(3, 8)]
        private string npcSystemPrompt = "당신은 FPS 게임의 동료 병사입니다. 전투 상황에 맞는 짧은 대화를 하세요.";

        [Header("References")]
        [SerializeField] private DialogueView dialogueView;

        private OllamaWebSocketAdapter _ollama;
        private NPCDialogueUseCase _dialogueUseCase;
        private DialogueSession _session;
        private bool _isInitialized;

        public void Initialize(AIServerConnection connection)
        {
            if (_isInitialized) return;

            _ollama = new OllamaWebSocketAdapter(connection);
            _session = new DialogueSession(npcId, npcSystemPrompt);
            _dialogueUseCase = new NPCDialogueUseCase(_ollama, _session);

            _ollama.OnTextDelta += delta => dialogueView?.AppendAIText(delta);
            _ollama.OnResponseComplete += text => dialogueView?.FinalizeAIText(text);
            _ollama.OnError += error => Debug.LogError($"[NPC:{npcId}] {error}");

            _isInitialized = true;
        }

        public void SendDialogue(string playerMessage)
        {
            if (!_isInitialized)
            {
                Debug.LogWarning($"[NPC:{npcId}] Not initialized.");
                return;
            }

            var dto = new ChatMessageDto(playerMessage, npcSystemPrompt, useTools: true);
            var result = _dialogueUseCase.Execute(dto);

            if (result.IsFailure)
                Debug.LogWarning($"[NPC:{npcId}] {result.Error}");
        }

        public void HandleServerMessage(string type, string json)
        {
            _ollama?.HandleServerMessage(type, json);
        }
    }
}
