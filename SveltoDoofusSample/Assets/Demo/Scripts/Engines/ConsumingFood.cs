using Svelto.ECS.SveltoOnDOTS;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using Svelto.ECS.Native;
using Svelto.DataStructures;
using Svelto.ECS.Internal;
using static Svelto.ECS.EnginesRoot;
using Svelto.ECS;

namespace SveltoDoofusSample
{
    public class ConsumingFood : IQueryingEntitiesEngine
    {
        readonly NativeEntitySwap _nativeSwap;
        readonly NativeEntityRemove _nativeRemove;

        public ConsumingFood(IEntityFunctions entityFunctions)
        {
            _nativeSwap = entityFunctions.ToNativeSwap<DoofusEntityDescriptor>(nameof(ConsumingFood));
            _nativeRemove = entityFunctions.ToNativeRemove<FoodEntityDescriptor>(nameof(ConsumingFood));
        }

        public string name => nameof(ConsumingFood);
        public EntitiesDB entitiesDB { get; set; }

        JobHandle ExecuteForColor(
            JobHandle inputDeps, FasterReadOnlyList<ExclusiveGroupStruct> eatingGroups, ExclusiveGroupStruct destinationGroupWhenDone, FasterReadOnlyList<ExclusiveGroupStruct> foodGroups)
        {
            JobHandle allJobs = default;

            var entityReferenceMap = entitiesDB.GetEntityReferenceMap();

            foreach (var ((positions, velocities, doofuses, doofusIds, count), group) in entitiesDB
                .QueryEntities<PositionComponent, VelocityComponent, DoofusMealComponent>(
                    eatingGroups))
            {
                // Debug.Log("Running ConsumingFood on group {0}", group);

                var consumeJob = new ConsumingFoodJob()
                {
                    Positions = positions,
                    Doofuses = doofuses,
                    EntityReferenceMap = entityReferenceMap,
                    Velocities = velocities,
                    DoofusIds = doofusIds,
                    DoofusGroup = group,
                    NotEatingDoofusGroup = destinationGroupWhenDone,
                    NativeSwap = _nativeSwap,
                    NativeRemove = _nativeRemove,
                }
                .ScheduleParallel(count, inputDeps);

                allJobs = JobHandle.CombineDependencies(allJobs, consumeJob);
            }

            return allJobs;
        }

        public void Ready()
        {
        }

        public JobHandle Execute(JobHandle inputDeps)
        {
            var job1 = ExecuteForColor(inputDeps, GameGroups.RedDoofusesEating.Groups, GameGroups.RedDoofusesNotEating.BuildGroup, GameGroups.RedFoodEaten.Groups);
            var job2 = ExecuteForColor(inputDeps, GameGroups.BlueDoofusesEating.Groups, GameGroups.BlueDoofusesNotEating.BuildGroup, GameGroups.BlueFoodEaten.Groups);

            return JobHandle.CombineDependencies(job1, job2);
        }

        [BurstCompile]
        public struct ConsumingFoodJob : IJobParallelFor
        {
            [ReadOnly]
            public NB<PositionComponent> Positions;

            public NB<DoofusMealComponent> Doofuses;
            public NB<VelocityComponent> Velocities;

            public NativeEntityIDs DoofusIds;

            public EntityReferenceMap EntityReferenceMap;

            public ExclusiveGroupStruct DoofusGroup;
            public ExclusiveGroupStruct NotEatingDoofusGroup;

            public NativeEntitySwap NativeSwap;
            public NativeEntityRemove NativeRemove;

            [NativeSetThreadIndex]
            readonly int _threadIndex;

            public void Execute(int i)
            {
                ref readonly var position = ref Positions[i];
                ref var velocity = ref Velocities[i];
                ref var doofus = ref Doofuses[i];

                var delta = doofus.DestinationPosition - position.Value;
                delta.y = 0;

                if (math.lengthsq(delta) < 2f)
                {
                    velocity.Value = float3.zero;

                    if (!EntityReferenceMap.TryGetEGID(doofus.Meal, out var mealEgid))
                    {
                        throw Assert.CreateException("Failed to convert meal entity reference to EGID");
                    }

                    NativeRemove.RemoveEntity(mealEgid, _threadIndex);

                    doofus.Meal = EntityReference.Invalid;
                    NativeSwap.SwapEntity(new EGID(DoofusIds[i], DoofusGroup), NotEatingDoofusGroup, _threadIndex);
                }
                else
                {
                    velocity.Value = delta;
                }
            }
        }
    }
}
