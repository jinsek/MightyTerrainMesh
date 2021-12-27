namespace MightyTerrainMesh
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine.Jobs;
    using Unity.Collections;
    using Unity.Burst;
    using Unity.Jobs;
    using Unity.Mathematics;
    using UnityEngine;

    [BurstCompile]
    internal struct MTDetailLayerCreateJob : IJob
    {
        [ReadOnly]
        public NativeArray<byte> densityData;
        public int detailHeight;
        public int den_x;
        public int den_z;
        public float3 pos_param; //x, offset x, y offset z, z patch size
        public float3 localScale;
        public int detailResolutionPerPatch;
        public int detailMaxDensity;
        public NativeArray<float> noiseSeed;
        public NativeArray<int> dataOffset;
        //prototype define
        public NativeArray<float> minWidth;
        public NativeArray<float> maxWidth;
        public NativeArray<float> minHeight;
        public NativeArray<float> maxHeight;
        public NativeArray<float> noiseSpread;
        public NativeArray<float4> healthyColor;
        public NativeArray<float4> dryColor;
        //output
        public NativeArray<float3> positions;
        public NativeArray<float3> scales;
        public NativeArray<float4> colors;
        public NativeArray<float> orientations;
        public NativeArray<int> spawnedCount;
        public void Execute()
        {
            spawnedCount[0] = 0;
            float stride = pos_param.z / detailResolutionPerPatch;
            for (int i=0; i< dataOffset.Length; ++i)
            {
                GeneratePatch(i, stride); 
            }
        }

        private void GeneratePatch(int i, float stride)
        {
            for (int z = 0; z < detailResolutionPerPatch; ++z)
            {
                for (int x = 0; x < detailResolutionPerPatch; ++x)
                {
                    var d_index = dataOffset[i] + z * detailResolutionPerPatch + x;
                    int density = densityData[d_index];
                    if (density <= 0)
                        continue;
                    density = math.min(16, density);
                    float sx = pos_param.x + den_x * pos_param.z + x * stride;
                    float sz = pos_param.y + den_z * pos_param.z + z * stride;
                    GenerateOnePixel(i, density, sx, sz, stride);
                }
            }
        }
        private void GenerateOnePixel(int i, int density, float sx, float sz, float stride)
        {
            int spread = (int)math.floor(math.sqrt(density) + 0.5f);
            float stride_x = 1f / spread;
            float stride_z = 1f / spread;
            for (int z = 0; z < spread; ++z)
            {
                for (int x = 0; x < spread; ++x)
                {
                    int idx = spawnedCount[0];
                    float fx = sx + x * stride_x * stride;
                    float fz = sz + z * stride_z * stride;
                    float globalNoise = Noise(fx * noiseSpread[i] + noiseSeed[i], fz * noiseSpread[i] + noiseSeed[i]);
                    float localNoise = SNoise((fx + noiseSeed[i]) * detailResolutionPerPatch * noiseSpread[i], 
                        (fz + noiseSeed[i]) * detailResolutionPerPatch * noiseSpread[i]);
                    float min_w = math.min(minWidth[i], maxWidth[i]);
                    float max_w = math.max(minWidth[i], maxWidth[i]);
                    float width = math.lerp(min_w, max_w, localNoise);
                    float min_h = math.min(minHeight[i], maxHeight[i]);
                    float max_h = math.max(minHeight[i], maxHeight[i]);
                    float height = math.lerp(min_h, max_h, localNoise);
                    colors[idx] = math.lerp(healthyColor[i], dryColor[i], globalNoise);
                    positions[idx] = new float3(fx + localNoise, 0, fz + localNoise);
                    scales[idx] = new float3(width * localScale.x, height * localScale.y, height * localScale.z);
                    orientations[idx] = math.lerp(0, 360, localNoise);
                    ++spawnedCount[0];
                }
            }
        }
        private float Noise(float x, float y)
        {
            float2 pos = math.float2(x, y);
            return noise.cnoise(pos);
        }
        private float SNoise(float x, float y)
        {
            float2 pos = math.float2(x, y);
            return noise.snoise(pos);
        }
    }
    internal class MTDetailPatchLayerJob : MTDetailPatchLayer
    {
        private static int _job_running_count = 0;
        private const int _max_concurrent_jobCount = 4;
        private static bool _add_schedule_job()
        {
            if (_job_running_count >= _max_concurrent_jobCount)
                return false;
            ++_job_running_count;
            return true;
        }
        private static void _job_done()
        {
            --_job_running_count;
        }
        private enum JobState
        {
            Wait,
            Running,
            Done,
        }
        private JobState state = JobState.Wait;
        public override bool IsSpawnDone { get { return state == JobState.Done; } }
        private MTDetailLayerCreateJob job;
        private JobHandle createJob;
        public MTDetailPatchLayerJob(MTDetailLayerData data, MTDetailLayerCreateJob j, bool receiveShadow) : base(data, receiveShadow)
        {
            job = j;
        }        
        public override void OnActivate(bool rebuild)
        {
            base.OnActivate(rebuild);
            if (rebuild)
            {
                if (state != JobState.Wait)
                {
                    Debug.LogWarning("MTDetailPatchLayerJob OnActivate state should not be " + state.ToString());
                    return;
                }
                TrySchedualJob();
            }
        }
        public override void OnDeactive()
        {
            base.OnDeactive();
            if (state == JobState.Running)
            {
                _job_done();
            }
            createJob.Complete();
            DisposeJob();
            state = JobState.Wait;
        }
        private void TrySchedualJob()
        {
            if (_add_schedule_job())
            {
                int maxCount = job.detailResolutionPerPatch * job.detailResolutionPerPatch * job.detailMaxDensity;
                job.spawnedCount = new NativeArray<int>(1, Allocator.TempJob);
                job.positions = new NativeArray<float3>(maxCount, Allocator.TempJob);
                job.scales = new NativeArray<float3>(maxCount, Allocator.TempJob);
                job.colors = new NativeArray<float4>(maxCount, Allocator.TempJob);
                job.orientations = new NativeArray<float>(maxCount, Allocator.TempJob);
                createJob = job.Schedule();
                state = JobState.Running;
            }
        }
        private void DisposeJob()
        {
            if (job.spawnedCount.IsCreated)
                job.spawnedCount.Dispose();
            if (job.positions.IsCreated)
                job.positions.Dispose();
            if (job.scales.IsCreated)
                job.scales.Dispose();
            if (job.colors.IsCreated)
                job.colors.Dispose();
            if (job.orientations.IsCreated)
                job.orientations.Dispose();
        }
        public override void TickBuild()
        {
            if (state == JobState.Wait)
            {
                TrySchedualJob();
            }
            if (state == JobState.Running && createJob.IsCompleted)
            {
                _job_done();
                state = JobState.Done;
                createJob.Complete();
                //copy parameters
                totalPrototypeCount = job.spawnedCount[0];
                if (totalPrototypeCount > 0)
                {
                    int batchCount = totalPrototypeCount / 1023 + 1;
                    if (drawParam == null)
                        drawParam = new MTArray<MTDetailPatchDrawParam>(batchCount);
                    drawParam.Reallocate(batchCount);
                    for (int batch = 0; batch < batchCount; ++batch)
                    {
                        var prototypeCount = Mathf.Min(1023, totalPrototypeCount - batch * 1023);
                        var param = MTDetailPatchDrawParam.Pop();
                        param.Reset(prototypeCount);
                        param.Used = prototypeCount;
                        for (int i = 0; i < prototypeCount; ++i)
                        {
                            var idxInJob = batch * 1023 + i;
                            Vector3 pos = job.positions[idxInJob];
                            MTHeightMap.GetHeightInterpolated(pos, ref pos.y);
                            if (this.layerData.waterFloating)
                            {
                                pos.y = MTWaterHeight.GetWaterHeight(pos);
                            }
                            Quaternion q = Quaternion.Euler(0, job.orientations[idxInJob], 0);
                            param.matrixs[i] = Matrix4x4.Translate(pos) * Matrix4x4.Scale(job.scales[idxInJob]) * Matrix4x4.Rotate(q);
                            param.colors[i] = job.colors[idxInJob];
                        }
                        drawParam.Add(param);
                    }
                    OnDrawParamReady();
                }
                else
                {
                    drawParam.Reset();
                }
                //
                DisposeJob();
            }
        }
        public override void Clear()
        {
            base.Clear();
            if (job.dataOffset.IsCreated)
                job.dataOffset.Dispose();
            if (job.noiseSeed.IsCreated)
                job.noiseSeed.Dispose();
            if (job.minWidth.IsCreated)
                job.minWidth.Dispose();
            if (job.maxWidth.IsCreated)
                job.maxWidth.Dispose();
            if (job.minHeight.IsCreated)
                job.minHeight.Dispose();
            if (job.maxHeight.IsCreated)
                job.maxHeight.Dispose();
            if (job.noiseSpread.IsCreated)
                job.noiseSpread.Dispose();
            if (job.healthyColor.IsCreated)
                job.healthyColor.Dispose();
            if (job.dryColor.IsCreated)
                job.dryColor.Dispose();
        }
    }
    public class MTDetailPatchJobs : MTDetailPatch
    {
        public override bool IsBuildDone { get { return buildDone; } }
        private bool buildDone = false;
        public MTDetailPatchJobs(int dx, int dz, int patchX, int patchZ, Vector3 posParam, bool receiveShadow, 
            MTData header, int[] patchDataOffsets, NativeArray<byte> ddata) 
            : base(dx, dz, posParam, header)
        {
            Dictionary<int, List<int>> combinedLayers = new Dictionary<int, List<int>>();
            for (int l = 0; l < headerData.DetailPrototypes.Length; ++l)
            {
                var dataOffset = patchDataOffsets[l * patchX * patchZ + den_z * patchX + den_x];
                if (dataOffset < 0)
                    continue;
                MTDetailLayerData layerData = headerData.DetailPrototypes[l];
                var instanceId = layerData.prototype.GetInstanceID();
                if (!combinedLayers.ContainsKey(instanceId))
                    combinedLayers.Add(instanceId, new List<int>());
                combinedLayers[instanceId].Add(l);
            }
            layers = new MTDetailPatchLayer[combinedLayers.Count];
            var combinedLayerIter = combinedLayers.GetEnumerator();
            var combined = 0;
            while (combinedLayerIter.MoveNext())
            {
                var layerIds = combinedLayerIter.Current.Value;
                var combineCount = layerIds.Count;
                var job = new MTDetailLayerCreateJob();
                job.densityData = ddata;
                job.detailHeight = headerData.DetailHeight;
                job.den_x = den_x;
                job.den_z = den_z;
                job.pos_param = posParam;
                job.detailResolutionPerPatch = headerData.DetailResolutionPerPatch;
                job.localScale = Vector3.one;
                job.detailMaxDensity = 0;
                job.dataOffset = new NativeArray<int>(combineCount, Allocator.Persistent);
                job.noiseSeed = new NativeArray<float>(combineCount, Allocator.Persistent);
                //prototype define
                job.minWidth = new NativeArray<float>(combineCount, Allocator.Persistent);
                job.maxWidth = new NativeArray<float>(combineCount, Allocator.Persistent);
                job.minHeight = new NativeArray<float>(combineCount, Allocator.Persistent);
                job.maxHeight = new NativeArray<float>(combineCount, Allocator.Persistent);
                job.noiseSpread = new NativeArray<float>(combineCount, Allocator.Persistent);
                job.healthyColor = new NativeArray<float4>(combineCount, Allocator.Persistent); 
                job.dryColor = new NativeArray<float4>(combineCount, Allocator.Persistent);
                MTDetailLayerData layerData = null;
                for (int i=0; i<layerIds.Count; ++i)
                {
                    var l = layerIds[i];
                    layerData = headerData.DetailPrototypes[l];
                    job.localScale = layerData.prototype.transform.localScale;
                    job.detailMaxDensity = Mathf.Min(byte.MaxValue, job.detailMaxDensity + layerData.maxDensity);
                    job.dataOffset[i] = patchDataOffsets[l * patchX * patchZ + den_z * patchX + den_x];
                    job.noiseSeed[i] = (float)l / headerData.DetailPrototypes.Length;
                    //prototype define
                    job.minWidth[i] = layerData.minWidth;
                    job.maxWidth[i] = layerData.maxWidth;
                    job.minHeight[i] = layerData.minHeight;
                    job.maxHeight[i] = layerData.maxHeight;
                    job.noiseSpread[i] = layerData.noiseSpread;
                    job.healthyColor[i] = new float4(layerData.healthyColor.r, layerData.healthyColor.g, layerData.healthyColor.b, layerData.healthyColor.a);
                    job.dryColor[i] = new float4(layerData.dryColor.r, layerData.dryColor.g, layerData.dryColor.b, layerData.dryColor.a);
                }
                layers[combined] = new MTDetailPatchLayerJob(layerData, job, receiveShadow);
                ++combined;
            }
        }
        public override void Activate()
        {
            bool rebuild = !buildDone;
            for (int l = 0; l < layers.Length; ++l)
            {
                layers[l].OnActivate(rebuild);
            }
        }
        public override void PushData()
        {
            base.PushData();
            buildDone = false;
        }
        public override void Clear()
        {
            buildDone = false;
            foreach (var l in layers)
            {
                l.Clear();
            }
        }
        public override void TickBuild()
        {
            if (buildDone)
                return;
            buildDone = true;
            foreach (var l in layers)
            {
                l.TickBuild();
                if (!l.IsSpawnDone)
                {
                    buildDone = false;
                    break;
                }
            }
        }
    }
}
