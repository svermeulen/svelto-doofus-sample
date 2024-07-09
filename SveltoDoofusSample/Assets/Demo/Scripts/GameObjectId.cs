using System;

namespace SveltoDoofusSample
{
    public struct GameObjectId : IEquatable<GameObjectId>
    {
        public readonly int Id;

        public GameObjectId(int id)
        {
            Id = id;
        }

        public override bool Equals(object obj)
        {
            return obj is GameObjectId other && Equals(other);
        }

        public bool Equals(GameObjectId other)
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
