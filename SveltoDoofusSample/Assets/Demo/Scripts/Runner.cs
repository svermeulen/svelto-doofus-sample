using System.Collections.Generic;
using GPUInstancer;
using Svelto.ECS;
using Svelto.ECS.Schedulers;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Profiling;

namespace SveltoDoofusSample
{
    public class Runner : MonoBehaviour
    {
        [SerializeField]
        GPUInstancerPrefabManager _gpuInstancerPrefabManager;

        [SerializeField]
        Camera _camera;

        RenderingGpuInstanceSyncEngine _renderingGpuInstanceSyncEngine;
        SpawnFood _spawnFood;
        LookingForFood _lookingForFood;
        ConsumingFood _consumingFood;
        VelocityToPosition _velocityToPosition;
        GpuInstancesSendToGpuEngine _gpuInstancesSendToGpuEngine;
        EntitiesSubmissionScheduler _entitiesSubmissionScheduler;
        EnginesRoot _enginesRoot;

        public void Start()
        {
            Debug.Log("Initializing game");

            var gameSettings = GameSettings.Instance;

            var prefabManager = new PrefabManager(
                new Dictionary<PrefabId, GameObject>()
                {
                    { GamePrefabIds.DoofusFoodBlue, gameSettings.Prefabs.DoofusFoodBlue },
                    { GamePrefabIds.SimplePlane, gameSettings.Prefabs.SimplePlane },
                    { GamePrefabIds.DoofusFoodRed, gameSettings.Prefabs.DoofusFoodRed },
                    { GamePrefabIds.DoofusBlue, gameSettings.Prefabs.DoofusBlue },
                    { GamePrefabIds.DoofusRed, gameSettings.Prefabs.DoofusRed },
                });

            var gameObjectResourceManager = new GameObjectResourceManager(prefabManager);

            var gpuInstancesSendToGpuEngine = new GpuInstancesSendToGpuEngine(
                prefabManager, _gpuInstancerPrefabManager);

            var entitiesSubmissionScheduler = new EntitiesSubmissionScheduler();
            var enginesRoot = new EnginesRoot(entitiesSubmissionScheduler);

            var entityFactory = enginesRoot.GenerateEntityFactory();
            var entityFuncs = enginesRoot.GenerateEntityFunctions();

            var entityIdCounter = new EntityIdCounter();

            var velocityToPosition = new VelocityToPosition();
            var spawnFood = new SpawnFood(entityIdCounter, entityFactory);
            var lookingForFood = new LookingForFood(entityFuncs);
            var consumingFood = new ConsumingFood(entityFuncs);

            var renderingGpuInstanceSyncEngine = new RenderingGpuInstanceSyncEngine(
                entityIdCounter,
                entityFactory,
                prefabManager,
                new RenderingGpuInstanceSyncEngine.Settings()
                {
                    PrefabGroupPairs = new List<RenderingGpuInstanceSyncEngine.GroupsPrefabPair>()
                    {
                        new RenderingGpuInstanceSyncEngine.GroupsPrefabPair()
                        {
                            Prefab = GamePrefabIds.DoofusRed,
                            FixedBufferSize = DoofusConstants.NumDoofusesPerTeam,
                            QueryGroups = GameGroups.RedDoofuses.Groups,
                        },
                        new RenderingGpuInstanceSyncEngine.GroupsPrefabPair()
                        {
                            Prefab = GamePrefabIds.DoofusBlue,
                            FixedBufferSize = DoofusConstants.NumDoofusesPerTeam,
                            QueryGroups = GameGroups.BlueDoofuses.Groups,
                        },
                        new RenderingGpuInstanceSyncEngine.GroupsPrefabPair()
                        {
                            Prefab = GamePrefabIds.DoofusFoodRed,
                            FixedBufferSize = DoofusConstants.MaxNumFoodPerTeam,
                            QueryGroups = GameGroups.RedMeals.Groups,
                        },
                        new RenderingGpuInstanceSyncEngine.GroupsPrefabPair()
                        {
                            Prefab = GamePrefabIds.DoofusFoodBlue,
                            FixedBufferSize = DoofusConstants.MaxNumFoodPerTeam,
                            QueryGroups = GameGroups.BlueMeals.Groups,
                        },
                    }
                },
                _gpuInstancerPrefabManager);

            var sceneInitializer = new SceneInitializer(
                _camera, gameObjectResourceManager, entityIdCounter, entityFactory);

            enginesRoot.AddEngine(velocityToPosition);
            enginesRoot.AddEngine(spawnFood);
            enginesRoot.AddEngine(lookingForFood);
            enginesRoot.AddEngine(consumingFood);
            enginesRoot.AddEngine(renderingGpuInstanceSyncEngine);
            enginesRoot.AddEngine(gpuInstancesSendToGpuEngine);

            renderingGpuInstanceSyncEngine.Initialize();
            sceneInitializer.Initialize();
            entitiesSubmissionScheduler.SubmitEntities();

            _renderingGpuInstanceSyncEngine = renderingGpuInstanceSyncEngine;
            _spawnFood = spawnFood;
            _lookingForFood = lookingForFood;
            _consumingFood = consumingFood;
            _velocityToPosition = velocityToPosition;
            _gpuInstancesSendToGpuEngine = gpuInstancesSendToGpuEngine;
            _entitiesSubmissionScheduler = entitiesSubmissionScheduler;
            _enginesRoot = enginesRoot;

            Debug.Log("Done initializing game");
        }

        public void OnGUI()
        {
            GUI.Label(new Rect(10, 10, 400, 50), "Total GPU Instances:");
            GUI.Label(new Rect(10, 30, 400, 50), _renderingGpuInstanceSyncEngine.TotalNumInstances.ToString());
        }

        public void OnApplicationQuit()
        {
            _enginesRoot.Dispose();
        }

        public void Update()
        {
            var spawnFoodJob = _spawnFood.Execute(default);

            var lookingForFoodJob = _lookingForFood.Execute(default);
            var consumingFoodJob = _consumingFood.Execute(default);

            var velocityToPositionJob = _velocityToPosition.Execute(consumingFoodJob);
            velocityToPositionJob.Complete();
            var renderingGpuInstanceSyncJob = _renderingGpuInstanceSyncEngine.Execute();

            var allJobs = JobHandle.CombineDependencies(
                spawnFoodJob, lookingForFoodJob, consumingFoodJob);

            allJobs = JobHandle.CombineDependencies(
                allJobs, velocityToPositionJob, renderingGpuInstanceSyncJob);

            allJobs.Complete();

            _gpuInstancesSendToGpuEngine.Execute();

            Profiler.BeginSample("Svelto Entity Submit");
            {
                _entitiesSubmissionScheduler.SubmitEntities();
            }
            Profiler.EndSample();
        }
    }
}
