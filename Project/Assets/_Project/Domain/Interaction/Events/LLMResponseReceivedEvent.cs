using VRProject.Domain.Common.Events;
using VRProject.Domain.Interaction.ValueObjects;

namespace VRProject.Domain.Interaction.Events
{
    public sealed class LLMResponseReceivedEvent : DomainEventBase
    {
        public LLMResponse Response { get; }

        public LLMResponseReceivedEvent(LLMResponse response)
        {
            Response = response;
        }
    }
}
