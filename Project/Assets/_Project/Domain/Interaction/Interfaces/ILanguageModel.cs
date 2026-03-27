using System;
using System.Collections.Generic;

namespace VRProject.Domain.Interaction.Interfaces
{
    public interface ILanguageModel
    {
        event Action<string> OnTextDelta;
        event Action<string> OnResponseComplete;
        event Action<FunctionCallData> OnFunctionCall;
        event Action<string> OnError;
        void SendChat(string prompt, string systemPrompt, bool useTools = true);
        bool IsBusy { get; }
    }

    public sealed class FunctionCallData
    {
        public string Name { get; }
        public IReadOnlyDictionary<string, object> Arguments { get; }

        public FunctionCallData(string name, IReadOnlyDictionary<string, object> arguments)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Arguments = arguments ?? new Dictionary<string, object>();
        }
    }
}
