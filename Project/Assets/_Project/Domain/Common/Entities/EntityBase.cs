using System;

namespace VRProject.Domain.Common.Entities
{
    public abstract class EntityBase<TId> : IEquatable<EntityBase<TId>>
        where TId : IEquatable<TId>
    {
        public TId Id { get; }

        protected EntityBase(TId id)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            Id = id;
        }

        public bool Equals(EntityBase<TId> other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Id.Equals(other.Id);
        }

        public override bool Equals(object obj)
        {
            return obj is EntityBase<TId> entity && Equals(entity);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public static bool operator ==(EntityBase<TId> left, EntityBase<TId> right)
        {
            if (left is null) return right is null;
            return left.Equals(right);
        }

        public static bool operator !=(EntityBase<TId> left, EntityBase<TId> right)
        {
            return !(left == right);
        }
    }
}
