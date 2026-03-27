using System;
using VRProject.Application.Common.Services;
using VRProject.Application.Interaction.DTOs;
using VRProject.Domain.Interaction.Entities;
using VRProject.Domain.Interaction.Interfaces;
using VRProject.Domain.Interaction.ValueObjects;

namespace VRProject.Application.Interaction.Services
{
    public sealed class ProcessVoiceCommandUseCase : IUseCase<ChatMessageDto>
    {
        private readonly ILanguageModel _languageModel;
        private readonly DialogueSession _session;

        public ProcessVoiceCommandUseCase(ILanguageModel languageModel, DialogueSession session)
        {
            _languageModel = languageModel ?? throw new ArgumentNullException(nameof(languageModel));
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public Result Execute(ChatMessageDto request)
        {
            if (request == null || !request.IsValid())
                return Result.Failure("Invalid chat message.");

            if (_languageModel.IsBusy)
                return Result.Failure("LLM is currently processing another request.");

            _session.AddUserMessage(new TranscribedText(request.Prompt));
            _languageModel.SendChat(request.Prompt, request.SystemPrompt, request.UseTools);

            return Result.Success();
        }
    }
}
