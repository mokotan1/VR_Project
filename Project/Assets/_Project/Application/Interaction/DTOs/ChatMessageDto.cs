using VRProject.Application.Common.DTOs;

namespace VRProject.Application.Interaction.DTOs
{
    public sealed class ChatMessageDto : DtoBase
    {
        public string Prompt { get; }
        public string SystemPrompt { get; }
        public bool UseTools { get; }

        public ChatMessageDto(string prompt, string systemPrompt, bool useTools = true)
        {
            Prompt = prompt ?? string.Empty;
            SystemPrompt = systemPrompt ?? string.Empty;
            UseTools = useTools;
        }

        public override bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(Prompt)
                && !string.IsNullOrWhiteSpace(SystemPrompt);
        }
    }
}
