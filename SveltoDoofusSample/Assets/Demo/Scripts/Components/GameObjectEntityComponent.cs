using Svelto.ECS;

namespace SveltoDoofusSample
{
    public struct GameObjectEntityComponent : IEntityComponent
    {
        public GameObjectId Id;
        public bool IsEnabled;
        public bool IsOwned; // Determines if it is deleted with the entity
    }
}
