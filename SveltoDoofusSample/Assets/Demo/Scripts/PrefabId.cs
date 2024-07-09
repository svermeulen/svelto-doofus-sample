using System;

namespace SveltoDoofusSample
{
    public struct PrefabId : IEquatable<PrefabId>
    {
        public readonly int Id;

        public PrefabId(int id)
        {
            Id = id;
        }

        public override bool Equals(object obj)
        {
            return obj is PrefabId other && Equals(other);
        }

        public bool Equals(PrefabId other)
        {
            return Id == other.Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public override string ToString()
        {
            return Id.ToString();
        }
    }
}
