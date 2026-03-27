using System;
using System.Text;
using VRProject.Application.Common.Services;
using VRProject.Application.Interaction.DTOs;
using VRProject.Domain.Interaction.Entities;
using VRProject.Domain.Interaction.Interfaces;
using VRProject.Domain.Interaction.ValueObjects;

namespace VRProject.Application.Interaction.Services
{
    public sealed class NPCDialogueUseCase : IUseCase<ChatMessageDto>
    {
        private readonly ILanguageModel _languageModel;
        private readonly DialogueSession _session;

        public NPCDialogueUseCase(ILanguageModel languageModel, DialogueSession session)
        {
            _languageModel = languageModel ?? throw new ArgumentNullException(nameof(languageModel));
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public Result Execute(ChatMessageDto request)
        {
            if (request == null || !request.IsValid())
                return Result.Failure("Invalid dialogue message.");

            if (_languageModel.IsBusy)
                return Result.Failure("LLM is currently processing another request.");

            _session.AddUserMessage(new TranscribedText(request.Prompt));

            string enrichedPrompt = BuildPromptWithHistory(request.Prompt);
            _languageModel.SendChat(enrichedPrompt, _session.SystemPrompt, request.UseTools);

            return Result.Success();
        }

        private string BuildPromptWithHistory(string currentPrompt)
        {
            var history = _session.History;
            if (history.Count <= 1)
                return currentPrompt;

            var sb = new StringBuilder();
            int startIdx = Math.Max(0, history.Count - 7);
            for (int i = startIdx; i < history.Count - 1; i++)
            {
                var turn = history[i];
                sb.AppendLine($"[{turn.Role}]: {turn.Content}");
            }
            sb.AppendLine($"[user]: {currentPrompt}");
            return sb.ToString();
        }
    }
}
