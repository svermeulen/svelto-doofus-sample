
using System;
using System.Collections.Generic;
using GPUInstancer;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Profiling;
using Svelto.ECS;
using Svelto.DataStructures;

namespace SveltoDoofusSample
{
    public class RenderingGpuInstanceSyncEngine : IQueryingEntitiesEngine, IDisposable
    {
        readonly GPUInstancerPrefabManager _prefabManager;
        readonly Settings _settings;
        readonly PrefabManager _gamePrefabManager;
        readonly Dictionary<PrefabId, GpuPrototypeInfo> _gpuPrototypeInfos =
            new Dictionary<PrefabId, GpuPrototypeInfo>();
        readonly IEntityFactory _entityFactory;
        readonly EntityIdCounter _entityIdCounter;

        int _totalNumInstances;
        bool _hasDisposed;

        public RenderingGpuInstanceSyncEngine(
            EntityIdCounter entityIdCounter, IEntityFactory entityFactory,
            PrefabManager gamePrefabManager,
            Settings settings,
            GPUInstancerPrefabManager prefabManager
        )
        {
            _entityIdCounter = entityIdCounter;
            _entityFactory = entityFactory;
            _gamePrefabManager = gamePrefabManager;
            _settings = settings;
            _prefabManager = prefabManager;
        }

        public string name => nameof(RenderingGpuInstanceSyncEngine);
        public EntitiesDB entitiesDB { get; set; }

        public void Ready()
        {
        }

        public void Initialize()
        {
            foreach (var pair in _settings.PrefabGroupPairs)
            {
                var prefabObj = _gamePrefabManager.Get(pair.Prefab);
                var gpuPrefab = prefabObj.GetNonNullComponent<GPUInstancerPrefab>();

                var initializer = _entityFactory.BuildEntity<GpuInstancerPrefabDescriptor>(
                        new EGID(_entityIdCounter.CreateEntityId(), GameGroups.GpuInstancerPrefab.BuildGroup));

                initializer.Init(new GpuInstancerPrefabComponent()
                {
                    Prefab = pair.Prefab,
                    FixedBufferSize = pair.FixedBufferSize,
                    TransformBuffer = new NativeList<Matrix4x4>(
                        pair.FixedBufferSize ?? 16,
                        Allocator.Persistent
                    ),
                });

                // Debug.Log(
                //     "Rendering instances from group {} ({})",
                //     gpuPrefab.name,
                //     pair.QueryGroups
                // );

                _gpuPrototypeInfos[pair.Prefab] = new GpuPrototypeInfo()
                {
                    DebugName = gpuPrefab.name,
                    Prototype = gpuPrefab,
                    QueryGroups = pair.QueryGroups,
                };
            }
        }

        public void Dispose()
        {
            Assert.That(!_hasDisposed);
            _hasDisposed = true;

            foreach (var ((prefabInfos, count), _) in entitiesDB.QueryEntities<GpuInstancerPrefabComponent>(GameGroups.GpuInstancerPrefab.Groups))
            {
                for (int i = 0; i < count; i++)
                {
                    ref var prefabInfo = ref prefabInfos[i];
                    prefabInfo.TransformBuffer.Dispose();
                }
            }
        }

        void ResizeGpuBuffers()
        {
            foreach (var ((prefabInfos, count), _) in entitiesDB.QueryEntities<GpuInstancerPrefabComponent>(GameGroups.GpuInstancerPrefab.Groups))
            {
                for (int i = 0; i < count; i++)
                {
                    ref var prefabInfo = ref prefabInfos[i];

                    var gpuInfo = _gpuPrototypeInfos[prefabInfo.Prefab];

                    var requiredBufferSize = prefabInfo.TransformBuffer.Length;

                    if (prefabInfo.HasInitialized)
                    {
                        if (prefabInfo.FixedBufferSize.HasValue)
                        {
                            Assert.That(prefabInfo.FixedBufferSize == prefabInfo.CurrentBufferSize);
                            Assert.That(
                                requiredBufferSize <= prefabInfo.FixedBufferSize,
                                "Number of entities exceeded given fixed size for prefab {0}. {1} > {2}",
                                gpuInfo.DebugName,
                                requiredBufferSize, prefabInfo.FixedBufferSize
                            );
                        }
                        else
                        {
                            if (requiredBufferSize > prefabInfo.CurrentBufferSize)
                            {
                                var newSize = Math.Max(requiredBufferSize, prefabInfo.CurrentBufferSize * 2);
                                GPUInstancerAPI.InitializePrototype(
                                    _prefabManager,
                                    gpuInfo.Prototype.prefabPrototype,
                                    newSize
                                );
                                prefabInfo.CurrentBufferSize = newSize;
                            }
                        }
                    }
                    else
                    {
                        int initialBufferSize;

                        if (prefabInfo.FixedBufferSize.HasValue)
                        {
                            Assert.That(
                                requiredBufferSize <= prefabInfo.FixedBufferSize,
                                "Required buffer size ({0}) is greater than fixed buffer size ({1}) for prefab {2} and query groups {3}",
                                requiredBufferSize,
                                prefabInfo.FixedBufferSize,
                                gpuInfo.DebugName,
                                gpuInfo.QueryGroups
                            );
                            initialBufferSize = prefabInfo.FixedBufferSize.Value;
                        }
                        else
                        {
                            initialBufferSize = requiredBufferSize;
                        }

                        prefabInfo.HasInitialized = true;
                        GPUInstancerAPI.InitializePrototype(
                            _prefabManager,
                            gpuInfo.Prototype.prefabPrototype,
                            initialBufferSize
                        );
                        prefabInfo.CurrentBufferSize = initialBufferSize;
                    }

                    GPUInstancerAPI.SetInstanceCount(
                        _prefabManager,
                        gpuInfo.Prototype.prefabPrototype,
                        requiredBufferSize
                    );
                }
            }
        }

        public int TotalNumInstances
        {
            get { return _totalNumInstances; }
        }

        public JobHandle Execute()
        {
            _totalNumInstances = 0;
            JobHandle allJobs = default;

            Profiler.BeginSample("Starting transform jobs");
            {
                foreach (var ((prefabInfos, prefabCount), _) in entitiesDB.QueryEntities<GpuInstancerPrefabComponent>(GameGroups.GpuInstancerPrefab.Groups))
                {
                    for (int i = 0; i < prefabCount; i++)
                    {
                        ref var prefabInfo = ref prefabInfos[i];

                        var gpuInfo = _gpuPrototypeInfos[prefabInfo.Prefab];

                        var enumerable = entitiesDB.QueryEntities<
                            PositionComponent,
                            RotationComponent,
                            ScaleComponent
                        >(gpuInfo.QueryGroups);

                        var totalCount = 0;

                        foreach (var ((_, _, _, count), _) in enumerable)
                        {
                            totalCount += count;
                        }

                        if (prefabInfo.TransformBuffer.Capacity < totalCount)
                        {
                            var newSize = Math.Max(totalCount, prefabInfo.TransformBuffer.Capacity * 2);
                            prefabInfo.TransformBuffer.SetCapacity(newSize);
                        }

                        prefabInfo.TransformBuffer.ResizeUninitialized(totalCount);

                        _totalNumInstances += totalCount;

                        int indexOffset = 0;

                        foreach (var ((positions, rotations, scales, count), group) in enumerable)
                        {
                            var job = new ApplyTransformsJob()
                            {
                                Positions = positions,
                                Rotations = rotations,
                                Scales = scales,
                                Buffer = prefabInfo.TransformBuffer,
                                IndexOffset = indexOffset,
                            }.ScheduleParallel(count, default);

                            allJobs = JobHandle.CombineDependencies(allJobs, job);

                            indexOffset += count;
                        }
                    }
                }
            }
            Profiler.EndSample();

            Profiler.BeginSample("Resizing gpu buffers");
            {
                ResizeGpuBuffers();
            }
            Profiler.EndSample();

            // TODO - a good optimization might be something like what's described here:
            // https://www.sebaslab.com/the-1millioncubes-challenge-how-i-managed-to-animate-a-freakload-of-cubes-on-windows-using-unity/
            // In other words:
            // * Write directly to GPU memory, instead of storing it all in CPU native memory first then copying it with SetData
            // * Use GraphicBuffer instead of ComputeBuffer (would need to abandon GPUInstancer in this case)
            // Also note that you could reduce memory usage by 3/4 by changing to just pass translation data instead
            // of the entire matrix, though this would require changes to GPUInstancer plugin
            // Also - maybe it would help to use unity collection types instead of Vector3, Quaternion, matrix4x4, to allow vectorization?

            return allJobs;
        }

        [BurstCompile]
        public struct ApplyTransformsJob : IJobParallelFor
        {
            [ReadOnly]
            public NB<PositionComponent> Positions;

            [ReadOnly]
            public NB<RotationComponent> Rotations;

            [ReadOnly]
            public NB<ScaleComponent> Scales;

            [NativeDisableContainerSafetyRestriction]
            public NativeList<Matrix4x4> Buffer;

            public int IndexOffset;

            public void Execute(int index)
            {
                Buffer[IndexOffset + index] = Matrix4x4.TRS(
                    Positions[index].Value,
                    Rotations[index].Value,
                    Scales[index].Value
                );
            }
        }

        public class GroupsPrefabPair
        {
            public PrefabId Prefab;
            public int? FixedBufferSize; // Optional
            public FasterReadOnlyList<ExclusiveGroupStruct> QueryGroups;
        }

        public class Settings
        {
            public List<GroupsPrefabPair> PrefabGroupPairs;
        }

        class GpuPrototypeInfo
        {
            public string DebugName;
            public GPUInstancerPrefab Prototype;
            public FasterReadOnlyList<ExclusiveGroupStruct> QueryGroups;
        }
    }
}
