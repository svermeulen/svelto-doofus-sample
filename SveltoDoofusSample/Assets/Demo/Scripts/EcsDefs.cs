
using Svelto.ECS;

namespace SveltoDoofusSample
{
    public class DoofusEntityDescriptor : IEntityDescriptor
    {
        static readonly IComponentBuilder[] _componentsToBuild =
        {
            new ComponentBuilder<DoofusMealComponent>(),
            new ComponentBuilder<SpeedComponent>(),
            new ComponentBuilder<PositionComponent>(),
            new ComponentBuilder<RotationComponent>(),
            new ComponentBuilder<ScaleComponent>(),
            new ComponentBuilder<VelocityComponent>(),
        };

        public IComponentBuilder[] componentsToBuild => _componentsToBuild;
    }

    public class FoodEntityDescriptor : IEntityDescriptor
    {
        static readonly IComponentBuilder[] _componentsToBuild =
        {
            new ComponentBuilder<PositionComponent>(),
            new ComponentBuilder<RotationComponent>(),
            new ComponentBuilder<ScaleComponent>(),
        };

        public IComponentBuilder[] componentsToBuild => _componentsToBuild;
    }

    public class GpuInstancerPrefabDescriptor : IEntityDescriptor
    {
        static readonly IComponentBuilder[] _componentsToBuild =
        {
            new ComponentBuilder<GpuInstancerPrefabComponent>(),
        };

        public IComponentBuilder[] componentsToBuild => _componentsToBuild;
    }

    public static class GameGroups
    {
        public class Doofuses : GroupTag<Doofuses> { }
        public class Meals : GroupTag<Meals> { }

        public class NotEating : GroupTag<NotEating> { }
        public class Eating : GroupTag<Eating> { }

        public class Red : GroupTag<Red> { }
        public class Blue : GroupTag<Blue> { }

        public class DoofusesEating : GroupCompound<Doofuses, Eating> { }

        public class RedDoofusesEating : GroupCompound<Doofuses, Red, Eating> { }
        public class BlueDoofusesEating : GroupCompound<Doofuses, Blue, Eating> { }

        public class RedDoofusesNotEating : GroupCompound<Doofuses, Red, NotEating> { }
        public class RedDoofuses : GroupCompound<Doofuses, Red> { }

        public class BlueDoofusesNotEating : GroupCompound<Doofuses, Blue, NotEating> { }
        public class BlueDoofuses : GroupCompound<Doofuses, Blue> { }

        public class RedMeals : GroupCompound<Meals, Red> { }
        public class BlueMeals : GroupCompound<Meals, Blue> { }

        public class RedFoodEaten : GroupCompound<Meals, Red, Eating> { }
        public class BlueFoodEaten : GroupCompound<Meals, Blue, Eating> { }

        public class RedFoodNotEaten : GroupCompound<Meals, Red, NotEating> { }
        public class BlueFoodNotEaten : GroupCompound<Meals, Blue, NotEating> { }

        public class GpuInstancerPrefab : GroupTag<GpuInstancerPrefab> { }
    }
}
