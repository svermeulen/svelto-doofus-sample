
namespace SveltoDoofusSample
{
    public static class DoofusConstants
    {
        // public const int NumDoofusesPerTeam = 10;
        // public const int NumDoofusesPerTeam = 100;
        // public const int NumDoofusesPerTeam = 1000;
        public const int NumDoofusesPerTeam = 10000;
        // public const int NumDoofusesPerTeam = 100000;
        public const int MaxFoodToSpawnPerSecond = (int)(0.75f * NumDoofusesPerTeam);
        public const int MaxNumFoodPerTeam = (int)(NumDoofusesPerTeam * 3 / 2);
        public const float DoofusHeight = 1.8f;
        public const float FoodSize = 0.5f;
        public const int SpawnRadius = 100;
    }
}
