using System;
using UnityEngine;

namespace SveltoDoofusSample
{
    [Serializable]
    public class PrefabReferences
    {
        public GameObject DoofusRed;
        public GameObject DoofusBlue;
        public GameObject DoofusFoodRed;
        public GameObject DoofusFoodBlue;
        public GameObject SimplePlane;
    }

    [CreateAssetMenu(fileName = "GameSettings", menuName = "Game Settings")]
    public class GameSettings : ScriptableObject
    {
        public PrefabReferences Prefabs;

        static GameSettings _instance;

        public static GameSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Load();
                }

                Assert.IsNotNull(_instance);
                return _instance!;
            }
        }

        private static GameSettings Load()
        {
            var instance = (GameSettings)Resources.Load("GameSettings");
            Assert.IsNotNull(instance);
            return instance;
        }
    }
}
