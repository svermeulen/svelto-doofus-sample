using GPUInstancer;
using Svelto.ECS;
using Unity.Jobs;

namespace SveltoDoofusSample
{
    public class GpuInstancesSendToGpuEngine : IQueryingEntitiesEngine
    {
        readonly GPUInstancerPrefabManager _prefabManager;
        readonly PrefabManager _gamePrefabManager;

        public GpuInstancesSendToGpuEngine(
            PrefabManager gamePrefabManager, GPUInstancerPrefabManager prefabManager)
        {
            _gamePrefabManager = gamePrefabManager;
            _prefabManager = prefabManager;
        }

        public string name => nameof(SpawnFood);
        public EntitiesDB entitiesDB { get; set; }

        public void Ready()
        {
        }

        public JobHandle Execute()
        {
            foreach (var ((prefabInfos, count), _) in entitiesDB.QueryEntities<GpuInstancerPrefabComponent>(GameGroups.GpuInstancerPrefab.Groups))
            {
                for (int i = 0; i < count; i++)
                {
                    ref readonly var info = ref prefabInfos[i];

                    if (info.CurrentBufferSize > 0)
                    {
                        // TODO - cache this
                        var gpuPrefab = _gamePrefabManager.Get(info.Prefab).GetNonNullComponent<GPUInstancerPrefab>();

                        GPUInstancerAPI.UpdateVisibilityBufferWithNativeArray(
                            _prefabManager,
                            gpuPrefab.prefabPrototype,
                            info.TransformBuffer.AsArray()
                        );
                    }
                }
            }

            return default;
        }
    }
}
