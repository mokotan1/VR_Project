using System;
using System.Collections.Generic;
using VRProject.Domain.Common.ValueObjects;

namespace VRProject.Domain.Interaction.ValueObjects
{
    public sealed class TranscribedText : ValueObject
    {
        public string Text { get; }
        public DateTime Timestamp { get; }
        public bool IsEmpty => string.IsNullOrWhiteSpace(Text);

        public TranscribedText(string text)
        {
            Text = text ?? string.Empty;
            Timestamp = DateTime.UtcNow;
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Text;
        }

        public override string ToString() => Text;
    }
}
