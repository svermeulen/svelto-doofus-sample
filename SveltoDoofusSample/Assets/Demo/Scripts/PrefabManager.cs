using System;
using System.Collections.Generic;
using UnityEngine;

namespace SveltoDoofusSample
{
    public class PrefabManager
    {
        readonly Dictionary<PrefabId, GameObject> _prefabMap;

        public PrefabManager(Dictionary<PrefabId, GameObject> prefabMap)
        {
            _prefabMap = prefabMap;
        }

        public void Register(PrefabId id, GameObject gameObject)
        {
            _prefabMap.Add(id, gameObject);
        }

        public GameObject Get(PrefabId id)
        {
            if (_prefabMap.TryGetValue(id, out var obj))
            {
                return obj;
            }

            throw new KeyNotFoundException("prefab id not found.");
        }
    }
}
