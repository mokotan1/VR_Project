using System;
using System.Collections.Generic;
using VRProject.Domain.Common.ValueObjects;
using VRProject.Domain.Interaction.Interfaces;

namespace VRProject.Domain.Interaction.ValueObjects
{
    public sealed class LLMResponse : ValueObject
    {
        public string FullText { get; }
        public IReadOnlyList<FunctionCallData> FunctionCalls { get; }
        public DateTime Timestamp { get; }

        public LLMResponse(string fullText, IReadOnlyList<FunctionCallData> functionCalls)
        {
            FullText = fullText ?? string.Empty;
            FunctionCalls = functionCalls ?? Array.Empty<FunctionCallData>();
            Timestamp = DateTime.UtcNow;
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return FullText;
            yield return FunctionCalls.Count;
        }
    }
}
