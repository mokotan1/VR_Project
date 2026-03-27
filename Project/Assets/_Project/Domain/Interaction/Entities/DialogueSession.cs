using System;
using System.Collections.Generic;
using VRProject.Domain.Common.Entities;
using VRProject.Domain.Interaction.Events;
using VRProject.Domain.Interaction.ValueObjects;

namespace VRProject.Domain.Interaction.Entities
{
    public sealed class DialogueSession : AggregateRoot<string>
    {
        private readonly List<DialogueTurn> _history = new();

        public IReadOnlyList<DialogueTurn> History => _history.AsReadOnly();
        public string SystemPrompt { get; }

        private const int MaxHistorySize = 20;

        public DialogueSession(string id, string systemPrompt) : base(id)
        {
            if (string.IsNullOrWhiteSpace(systemPrompt))
                throw new ArgumentException("System prompt cannot be empty.", nameof(systemPrompt));

            SystemPrompt = systemPrompt;
        }

        public void AddUserMessage(TranscribedText transcription)
        {
            if (transcription == null || transcription.IsEmpty)
                return;

            _history.Add(new DialogueTurn("user", transcription.Text));
            TrimHistory();

            RaiseDomainEvent(new SpeechRecognizedEvent(transcription));
        }

        public void AddAssistantResponse(LLMResponse response)
        {
            if (response == null || string.IsNullOrWhiteSpace(response.FullText))
                return;

            _history.Add(new DialogueTurn("assistant", response.FullText));
            TrimHistory();

            RaiseDomainEvent(new LLMResponseReceivedEvent(response));
        }

        private void TrimHistory()
        {
            while (_history.Count > MaxHistorySize)
                _history.RemoveAt(0);
        }
    }

    public sealed class DialogueTurn
    {
        public string Role { get; }
        public string Content { get; }
        public DateTime Timestamp { get; }

        public DialogueTurn(string role, string content)
        {
            Role = role;
            Content = content;
            Timestamp = DateTime.UtcNow;
        }
    }
}
