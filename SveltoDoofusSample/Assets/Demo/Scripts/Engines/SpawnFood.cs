using System;
using Svelto.DataStructures;
using Svelto.ECS;
using Svelto.ECS.Native;
using Svelto.ECS.SveltoOnDOTS;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace SveltoDoofusSample
{
    public class SpawnFood : IJobifiedEngine, IQueryingEntitiesEngine, IDisposable
    {
        readonly NativeEntityFactory _nativeFactory;

        NativeList<uint> _seedOffsets = new NativeList<uint>(Allocator.Persistent);
        readonly EntityIdCounter _entityIdCounter;

        public SpawnFood(EntityIdCounter entityIdCounter, IEntityFactory entityFactory)
        {
            _entityIdCounter = entityIdCounter;
            _nativeFactory = entityFactory.ToNative<FoodEntityDescriptor>(nameof(SpawnFood));
        }

        public string name => nameof(SpawnFood);
        public EntitiesDB entitiesDB { get; set; }

        public void Ready()
        {
        }

        public void Dispose()
        {
            _seedOffsets.Dispose();
        }

        void ExpandSeedOffsets(int requiredLength)
        {
            var currentLength = _seedOffsets.Length;

            if (requiredLength > currentLength)
            {
                _seedOffsets.ResizeUninitialized(requiredLength);

                for (int i = currentLength; i < requiredLength; i++)
                {
                    _seedOffsets[i] = (uint)UnityEngine.Random.Range(0, int.MaxValue);
                }
            }
        }

        public JobHandle Execute(JobHandle inputDeps)
        {
            FasterReadOnlyList<ExclusiveGroupStruct> mealGroups;
            ExclusiveBuildGroup newMealGroup;

            if (UnityEngine.Random.value < 0.5f)
            {
                mealGroups = GameGroups.RedMeals.Groups;
                newMealGroup = GameGroups.RedFoodNotEaten.BuildGroup;
            }
            else
            {
                mealGroups = GameGroups.BlueMeals.Groups;
                newMealGroup = GameGroups.BlueFoodNotEaten.BuildGroup;
            }

            var currentNumMeals = 0;

            for (int i = 0; i < mealGroups.count; i++)
            {
                currentNumMeals += entitiesDB.Count<PositionComponent>(mealGroups[i]);
            }

            // Could also do this which is more interesting but takes longer for stuff to stuff to stabilize and produce good profiling info
            // float deltaTime = Time.deltaTime;
            // int desiredNewAmount = Mathf.FloorToInt(1 + DoofusConstants.MaxFoodToSpawnPerSecond * deltaTime);
            // int newAmount = Mathf.Min(DoofusConstants.MaxNumFoodPerTeam, currentNumMeals + desiredNewAmount);
            // int amountToAdd = newAmount - currentNumMeals;

            int amountToAdd = DoofusConstants.MaxNumFoodPerTeam - currentNumMeals;

            if (amountToAdd > 0)
            {
                var baseRandomSeed = (uint)UnityEngine.Random.Range(0, int.MaxValue);
                uint egidStart = _entityIdCounter.CreateMultipleEntityIds((uint)amountToAdd);

                ExpandSeedOffsets(amountToAdd);

                return new SpawnFoodJob()
                {
                    Group = newMealGroup,
                    EntityFactory = _nativeFactory,
                    EgidStart = egidStart,
                    BaseRandomSeed = baseRandomSeed,
                    SeedOffsets = _seedOffsets
                }
                .ScheduleParallel(amountToAdd, inputDeps);
            }

            return default;
        }

        [BurstCompile]
        public struct SpawnFoodJob : IJobParallelFor
        {
            public ExclusiveGroupStruct Group;
            public NativeEntityFactory EntityFactory;
            public uint EgidStart;
            public uint BaseRandomSeed;

            [ReadOnly]
            public NativeList<uint> SeedOffsets;

            [NativeSetThreadIndex]
            readonly int _threadIndex;

            public void Execute(int i)
            {
                var random = new Random(BaseRandomSeed ^ SeedOffsets[i]);

                var initializer = EntityFactory.BuildEntity(
                    new EGID(EgidStart + (uint)i, Group), _threadIndex);

                float theta = random.NextFloat() * 2 * Mathf.PI;
                float radius = random.NextFloat() * DoofusConstants.SpawnRadius;

                initializer.Init(new RotationComponent()
                {
                    Value = Quaternion.identity
                });

                initializer.Init(new ScaleComponent()
                {
                    Value = Vector3.one * DoofusConstants.FoodSize
                });

                initializer.Init(new PositionComponent()
                {
                    Value = new float3(
                        Mathf.Cos(theta) * radius,
                        0.5f * DoofusConstants.FoodSize,
                        Mathf.Sin(theta) * radius
                    )
                });
            }
        }
    }
}

