
using Svelto.ECS;
using Unity.Jobs;
using UnityEngine;

namespace SveltoDoofusSample
{
    public class GameObjectsEcsToUnityEngine
        : IQueryingEntitiesEngine,
            IReactOnRemoveEx<GameObjectEntityComponent>
    {
        readonly GameObjectResourceManager _manager;

        bool _hasWarnedAboutInvalidId;

        public GameObjectsEcsToUnityEngine(
            GameObjectResourceManager manager)
        {
            _manager = manager;
        }

        public string name => nameof(GameObjectsEcsToUnityEngine);
        public EntitiesDB entitiesDB { get; set; }

        public void Ready()
        {
        }

        public void SyncEnabled()
        {
            var groups = entitiesDB.FindGroups<GameObjectEntityComponent>();

            foreach (
                var (
                    (entities, count),
                    _
                ) in entitiesDB.QueryEntities<GameObjectEntityComponent>(groups)
            )
            {
                for (int i = 0; i < count; i++)
                {
                    ref readonly var entity = ref entities[i];

                    var go = TryGetGameObject(entity.Id);

                    if (go == null)
                    {
                        continue;
                    }

                    go.SetActive(entity.IsEnabled);
                }
            }
        }

        void SyncRotations()
        {
            var groups = entitiesDB.FindGroups<GameObjectEntityComponent, RotationComponent>();

            foreach (var ((entity, rotations, count), _) in entitiesDB.QueryEntities<
                    GameObjectEntityComponent,
                    RotationComponent
                >(groups))
            {
                for (int i = 0; i < count; i++)
                {
                    var go = TryGetGameObject(entity[i].Id);

                    if (go == null)
                    {
                        continue;
                    }

                    var transform = go.transform;
                    transform.rotation = rotations[i].Value;
                }
            }
        }

        GameObject TryGetGameObject(GameObjectId id)
        {
            var go = _manager.TryGet(id);

            if (go == null && !_hasWarnedAboutInvalidId)
            {
                _hasWarnedAboutInvalidId = true;
                Debug.LogWarning("Found GameObjectEntityComponent with invalid id");
            }

            return go;
        }

        void SyncPositions()
        {
            var groups = entitiesDB.FindGroups<GameObjectEntityComponent, PositionComponent>();

            foreach (var ((entity, positions, count), _) in entitiesDB.QueryEntities<
                    GameObjectEntityComponent,
                    PositionComponent
                >(groups))
            {
                for (int i = 0; i < count; i++)
                {
                    var go = TryGetGameObject(entity[i].Id);

                    if (go == null)
                    {
                        continue;
                    }

                    var transform = go.transform;
                    transform.position = positions[i].Value;
                }
            }
        }

        public JobHandle Execute()
        {
            SyncPositions();
            SyncRotations();
            SyncEnabled();

            return default;
        }

        void IReactOnRemoveEx<GameObjectEntityComponent>.Remove(
            (uint start, uint end) rangeOfEntities,
            in EntityCollection<GameObjectEntityComponent> entities,
            ExclusiveGroupStruct groupID
        )
        {
            var (buffer, ids, _) = entities;

            for (int i = (int)rangeOfEntities.start; i < rangeOfEntities.end; i++)
            {
                ref readonly var obj = ref buffer[i];

                if (obj.IsOwned)
                {
                    _manager.Despawn(obj.Id);
                }
            }
        }
    }
}
