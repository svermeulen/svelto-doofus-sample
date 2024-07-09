using Svelto.ECS.SveltoOnDOTS;
using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Svelto.DataStructures;
using Svelto.ECS;

namespace SveltoDoofusSample
{
    public class VelocityToPosition : IJobifiedEngine, IQueryingEntitiesEngine
    {
        public VelocityToPosition()
        {
        }

        public string name => nameof(SpawnFood);
        public EntitiesDB entitiesDB { get; set; }

        public void Ready()
        {
        }

        public JobHandle Execute(JobHandle inputDeps)
        {
            float deltaTime = Time.deltaTime;

            var allJobs = inputDeps;

            foreach (var ((speeds, velocities, positions, _, count), _) in entitiesDB
                .QueryEntities<SpeedComponent, VelocityComponent, PositionComponent>(GameGroups.DoofusesEating.Groups))
            {
                var job = new UpdatePositionJob()
                {
                    Positions = positions,
                    Velocities = velocities,
                    Speeds = speeds,
                    DeltaTime = Time.deltaTime,
                }
                .ScheduleParallel(count, inputDeps);

                allJobs = JobHandle.CombineDependencies(allJobs, job);
            }

            return allJobs;
        }

        [BurstCompile]
        public struct UpdatePositionJob : IJobParallelFor
        {
            public NB<PositionComponent> Positions;

            [ReadOnly]
            public NB<VelocityComponent> Velocities;

            [ReadOnly]
            public NB<SpeedComponent> Speeds;

            public float DeltaTime;

            public void Execute(int i)
            {
                ref readonly var velocity = ref Velocities[i];
                ref readonly var speed = ref Speeds[i];
                ref var position = ref Positions[i];

                position.Value += velocity.Value * speed.Value * DeltaTime;
            }
        }
    }
}
