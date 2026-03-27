using VRProject.Domain.Common.Events;
using VRProject.Domain.Interaction.ValueObjects;

namespace VRProject.Domain.Interaction.Events
{
    public sealed class SpeechRecognizedEvent : DomainEventBase
    {
        public TranscribedText Transcription { get; }

        public SpeechRecognizedEvent(TranscribedText transcription)
        {
            Transcription = transcription;
        }
    }
}
