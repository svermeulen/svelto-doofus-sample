using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;


namespace SveltoDoofusSample
{
    public class GameObjectResourceManager
    {
        readonly Dictionary<GameObjectId, ObjectInfo> _activeMap =
            new Dictionary<GameObjectId, ObjectInfo>();

        readonly Dictionary<PrefabId, Stack<ObjectInfo>> _inactivePools =
            new Dictionary<PrefabId, Stack<ObjectInfo>>();

        readonly PrefabManager _prefabManager;

        int _nextId;

        public GameObjectResourceManager(PrefabManager prefabManager)
        {
            _prefabManager = prefabManager;
        }

        GameObject Instantiate(PrefabId prefabId, bool startActive = true)
        {
            var prefab = _prefabManager.Get(prefabId);
            var gameObject = GameObject.Instantiate(prefab);
            gameObject.SetActive(startActive);
            return gameObject;
        }

        public void Despawn(GameObjectId index)
        {
            if (_activeMap.TryGetValue(index, out var info))
            {
                _activeMap.Remove(index);

                if (info.PrefabId.HasValue)
                {
                    Assert.That(info.GameObject.activeSelf);
                    info.GameObject.SetActive(false);

                    if (!_inactivePools.TryGetValue(info.PrefabId.Value, out var pool))
                    {
                        pool = new Stack<ObjectInfo>();
                        _inactivePools[info.PrefabId.Value] = pool;
                    }

                    pool.Push(info);
                }
                else
                {
                    GameObject.Destroy(info.GameObject);
                }
            }
            else
            {
                throw Assert.CreateException(
                    "Attempted to destroy GameObject with id {0} but it was not found.",
                    index
                );
            }
        }

        public GameObjectId Spawn(PrefabId prefabId, bool startActive = true)
        {
            if (_inactivePools.TryGetValue(prefabId, out var pool) && pool.Count > 0)
            {
                var info = pool.Pop();
                info.GameObject.SetActive(startActive);
                return AddInternal(info);
            }

            var gameObject = Instantiate(prefabId, startActive);
            return AddInternal(new ObjectInfo() { PrefabId = prefabId, GameObject = gameObject, });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GameObject Get(GameObjectId index)
        {
            if (_activeMap.TryGetValue(index, out var info))
            {
                return info.GameObject;
            }

            throw new KeyNotFoundException("GameObject ID not found.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GameObject TryGet(GameObjectId index)
        {
            if (_activeMap.TryGetValue(index, out var info))
            {
                return info.GameObject;
            }

            return null;
        }

        public GameObject this[GameObjectId index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (_activeMap.TryGetValue(index, out var info))
                {
                    return info.GameObject;
                }

                throw new KeyNotFoundException("GameObject ID not found.");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        GameObjectId AddInternal(ObjectInfo info)
        {
            _nextId += 1;

            var id = new GameObjectId(_nextId);
            _activeMap[id] = info;
            return id;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GameObjectId Add(GameObject gameObject, PrefabId? prefabId = null)
        {
            return AddInternal(new ObjectInfo() { PrefabId = prefabId, GameObject = gameObject, });
        }

        struct ObjectInfo
        {
            public GameObject GameObject;
            public PrefabId? PrefabId;
        }
    }
}
