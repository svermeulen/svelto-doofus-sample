using Svelto.ECS;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace SveltoDoofusSample
{
    public struct GpuInstancerPrefabComponent : IEntityComponent
    {
        public NativeList<Matrix4x4> TransformBuffer;
        public PrefabId Prefab;
        public bool HasInitialized;
        public int CurrentBufferSize;
        public int? FixedBufferSize;
    }

    public struct SpeedComponent : IEntityComponent
    {
        public float Value;
    }

    public struct DoofusMealComponent : IEntityComponent
    {
        public EntityReference Meal;
        public float3 DestinationPosition;
        public bool HasReachedDestination;
    }

    public struct VelocityComponent : IEntityComponent
    {
        public float3 Value;
    }
}
