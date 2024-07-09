using UnityEngine;


namespace SveltoDoofusSample
{
    public static class MiscUnityExtensions
    {
        public static T GetNonNullComponent<T>(this GameObject obj)
            where T : Component
        {
            var value = obj.GetComponent<T>();
            Assert.IsNotNull(value);
            return value;
        }
    }
}
