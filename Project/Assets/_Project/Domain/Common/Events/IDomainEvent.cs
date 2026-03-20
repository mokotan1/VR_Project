using System;

namespace VRProject.Domain.Common.Events
{
    public interface IDomainEvent
    {
        DateTime OccurredOn { get; }
    }
}
