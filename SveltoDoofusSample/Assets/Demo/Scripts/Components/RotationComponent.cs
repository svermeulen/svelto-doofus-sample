
using Svelto.ECS;
using Unity.Mathematics;

namespace SveltoDoofusSample
{
    public struct RotationComponent : IEntityComponent
    {
        public quaternion Value;
    }
}
