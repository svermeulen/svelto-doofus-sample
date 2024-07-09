using GPUInstancer;
using Svelto.ECS;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SveltoDoofusSample
{
    public class SceneInitializer
    {
        readonly IEntityFactory _entityFactory;
        readonly EntityIdCounter _entityIdCounter;
        readonly GameObjectResourceManager _gameObjectResourceManager;
        readonly Camera _camera;

        public SceneInitializer(Camera camera, GameObjectResourceManager gameObjectResourceManager, EntityIdCounter entityIdCounter, IEntityFactory entityFactory)
        {
            _camera = camera;
            _gameObjectResourceManager = gameObjectResourceManager;
            _entityIdCounter = entityIdCounter;
            _entityFactory = entityFactory;
        }

        public void Initialize()
        {
            SpawnPlane();
            SpawnDoofuses();

            _camera.gameObject.transform.position = new float3(0, 40, -135);
            _camera.gameObject.transform.rotation = Quaternion.Euler(20, 0, 0);
        }

        void SpawnPlane()
        {
            var planeId = _gameObjectResourceManager.Spawn(GamePrefabIds.SimplePlane);
            var planeObj = _gameObjectResourceManager.Get(planeId);
            planeObj.transform.localScale = Vector3.one * 100;
        }

        public void SpawnDoofusForColor(ExclusiveBuildGroup buildGroup)
        {
            for (int i = 0; i < DoofusConstants.NumDoofusesPerTeam; i++)
            {
                var theta = Random.value * 2 * Mathf.PI;
                var radius = Random.value * DoofusConstants.SpawnRadius;

                var initializer = _entityFactory.BuildEntity<DoofusEntityDescriptor>(
                    new EGID(_entityIdCounter.CreateEntityId(), buildGroup));

                initializer.Init(new DoofusMealComponent());
                initializer.Init(new SpeedComponent()
                {
                    Value = 0.1f + Random.value,
                });
                initializer.Init(new VelocityComponent()
                {
                    Value = new float3(1, 0, 1)
                });
                initializer.Init(new PositionComponent()
                {
                    Value = new float3()
                    {
                        x = Mathf.Cos(theta) * radius,
                        y = 0.5f * DoofusConstants.FoodSize,
                        z = Mathf.Sin(theta) * radius,
                    }
                });
                initializer.Init(new RotationComponent()
                {
                    Value = Quaternion.identity,
                });
                initializer.Init(new ScaleComponent()
                {
                    Value = new float3(0.8f, DoofusConstants.DoofusHeight, 0.8f),
                });
            }
        }

        public void SpawnDoofuses()
        {
            SpawnDoofusForColor(GameGroups.RedDoofusesNotEating.BuildGroup);
            SpawnDoofusForColor(GameGroups.BlueDoofusesNotEating.BuildGroup);
        }
    }
}
