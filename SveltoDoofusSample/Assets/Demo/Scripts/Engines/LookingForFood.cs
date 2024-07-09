using System.Collections.Generic;
using Svelto.DataStructures;
using Svelto.DataStructures.Native;
using Svelto.ECS;
using Svelto.ECS.Internal;
using Svelto.ECS.Native;
using Svelto.ECS.SveltoOnDOTS;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace SveltoDoofusSample
{
    public class LookingForFood : IJobifiedEngine, IQueryingEntitiesEngine
    {
        readonly NativeEntitySwap _doofusNativeGroupSwapper;
        readonly NativeEntitySwap _foodNativeGroupSwapper;

        public LookingForFood(IEntityFunctions entityFunctions)
        {
            _doofusNativeGroupSwapper = entityFunctions.ToNativeSwap<DoofusEntityDescriptor>(nameof(LookingForFood));
            _foodNativeGroupSwapper = entityFunctions.ToNativeSwap<FoodEntityDescriptor>(nameof(LookingForFood));
        }

        public string name => nameof(LookingForFood);
        public EntitiesDB entitiesDB { get; set; }

        private JobHandle ExecuteForColor(
            JobHandle inputDeps,
            FasterReadOnlyList<ExclusiveGroupStruct> groupsWithAvailableFood,
            FasterReadOnlyList<ExclusiveGroupStruct> groupsWithAvailableDoofuses,
            ExclusiveBuildGroup doofusMoveGroup,
            ExclusiveBuildGroup foodMoveGroup)
        {
            var availableFoodComponents = entitiesDB.QueryEntities<PositionComponent>(
                groupsWithAvailableFood).GetEnumerator();

            var availableDoofusesComponents = entitiesDB.QueryEntities<DoofusMealComponent>(
                groupsWithAvailableDoofuses).GetEnumerator();

            JobHandle allJobs = default;

            while (availableFoodComponents.MoveNext() && availableDoofusesComponents.MoveNext())
            {
                var ((foodPositions, foodIds, availableFoodCount), currentFoodGroup) =
                    availableFoodComponents.Current;

                var ((doofuses, doofusIds, doofusesCount), currentDoofusesGroup) =
                    availableDoofusesComponents.Current;
                {
                    var amountEatable = math.min(availableFoodCount, doofusesCount);
                    Assert.That(amountEatable > 0);

                    var entityReferenceMap = entitiesDB.GetEntityReferenceMap(currentFoodGroup);

                    var job = new StartGetMealJob()
                    {
                        EntityReferenceMap = entityReferenceMap,
                        Doofuses = doofuses,
                        FoodPositions = foodPositions,
                        CurrentFoodGroup = currentFoodGroup,
                        CurrentDoofusesGroup = currentDoofusesGroup,
                        FoodIds = foodIds,
                        DoofusIds = doofusIds,
                        DoofusSwapper = _doofusNativeGroupSwapper,
                        MealSwapper = _foodNativeGroupSwapper,
                        DoofusDestinationGroup = doofusMoveGroup,
                        FoodDestinationGroup = foodMoveGroup,
                    }
                    .ScheduleParallel(amountEatable, inputDeps);

                    allJobs = JobHandle.CombineDependencies(allJobs, job);
                }
            }

            return allJobs;
        }

        public void Ready()
        {
        }

        public JobHandle Execute(JobHandle inputDeps)
        {
            var job1 = ExecuteForColor(inputDeps, GameGroups.RedFoodNotEaten.Groups, GameGroups.RedDoofusesNotEating.Groups, GameGroups.RedDoofusesEating.BuildGroup, GameGroups.RedFoodEaten.BuildGroup);
            var job2 = ExecuteForColor(inputDeps, GameGroups.BlueFoodNotEaten.Groups, GameGroups.BlueDoofusesNotEating.Groups, GameGroups.BlueDoofusesEating.BuildGroup, GameGroups.BlueFoodEaten.BuildGroup);

            return JobHandle.CombineDependencies(job1, job2);
        }

        [BurstCompile]
        public struct StartGetMealJob : IJobParallelFor
        {
            public NB<DoofusMealComponent> Doofuses;

            [ReadOnly]
            public NB<PositionComponent> FoodPositions;

            public ExclusiveGroupStruct CurrentFoodGroup;
            public ExclusiveGroupStruct CurrentDoofusesGroup;
            public NativeEntityIDs FoodIds;
            public NativeEntityIDs DoofusIds;
            public SharedSveltoDictionaryNative<uint, EntityReference> EntityReferenceMap;
            public NativeEntitySwap DoofusSwapper;
            public NativeEntitySwap MealSwapper;
            public ExclusiveGroupStruct DoofusDestinationGroup;
            public ExclusiveGroupStruct FoodDestinationGroup;

            [NativeSetThreadIndex]
            readonly int _threadIndex;

            public void Execute(int i)
            {
                ref var doofusState = ref Doofuses[i];
                ref readonly var foodPosition = ref FoodPositions[i];

                Assert.That(doofusState.Meal == EntityReference.Invalid);

                var foodId = new EGID(FoodIds[i], CurrentFoodGroup);
                var doofusId = new EGID(DoofusIds[i], CurrentDoofusesGroup);

                var foodEntityReference = EntityReferenceMap[foodId.entityID];

                doofusState.Meal = foodEntityReference;
                doofusState.DestinationPosition = foodPosition.Value;

                DoofusSwapper.SwapEntity(doofusId, DoofusDestinationGroup, _threadIndex);
                MealSwapper.SwapEntity(foodId, FoodDestinationGroup, _threadIndex);
            }
        }
    }
}
