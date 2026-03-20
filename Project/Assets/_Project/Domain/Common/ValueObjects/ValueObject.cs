using System;
using System.Collections.Generic;
using System.Linq;

namespace VRProject.Domain.Common.ValueObjects
{
    public abstract class ValueObject : IEquatable<ValueObject>
    {
        protected abstract IEnumerable<object> GetEqualityComponents();

        public bool Equals(ValueObject other)
        {
            if (other is null) return false;
            if (GetType() != other.GetType()) return false;
            return GetEqualityComponents()
                .SequenceEqual(other.GetEqualityComponents());
        }

        public override bool Equals(object obj)
        {
            return obj is ValueObject valueObject && Equals(valueObject);
        }

        public override int GetHashCode()
        {
            return GetEqualityComponents()
                .Aggregate(17, (current, component) =>
                    current * 31 + (component?.GetHashCode() ?? 0));
        }

        public static bool operator ==(ValueObject left, ValueObject right)
        {
            if (left is null) return right is null;
            return left.Equals(right);
        }

        public static bool operator !=(ValueObject left, ValueObject right)
        {
            return !(left == right);
        }
    }
}
