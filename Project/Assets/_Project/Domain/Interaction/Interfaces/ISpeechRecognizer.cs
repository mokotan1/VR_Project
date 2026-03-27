using System;

namespace VRProject.Domain.Interaction.Interfaces
{
    public interface ISpeechRecognizer
    {
        event Action<string> OnTranscriptionReceived;
        void StartListening();
        void StopListening();
        bool IsListening { get; }
    }
}
