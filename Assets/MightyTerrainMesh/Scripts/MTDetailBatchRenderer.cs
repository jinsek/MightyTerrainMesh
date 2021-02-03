using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;
using UnityEngine.Rendering;

public class MTDetailBatchRenderer : MonoBehaviour
{
    [System.Serializable]
    public class ProtoLayerInfo
    {
        public TextAsset detailData;
        public Mesh mesh;
        public Material mat;
    }
    public ProtoLayerInfo[] layers = new ProtoLayerInfo[0];
    [Range(1, 255)]
    public float detailDistance = 50;
    [Range(0, 1)]
    public float detailDensity = 0.5f;
    public int detailResolution = 1024;
    public int detailResolutionPerPatch = 16;
    public Bounds bnds;

    private BatchSplitor[] splitors;
    private BatchRendererGroup batchRendererGroup;
    private JobHandle cullingDependency;
    private NativeArray<CullData> cullData;
    private NativeArray<int> cullDataOffset;

    private void Awake()
    {
        if (layers.Length == 0)
            return;
        splitors = new BatchSplitor[layers.Length];
        Bounds worldBND = new Bounds(transform.position + bnds.center, bnds.size);
        for(int i=0; i<layers.Length; ++i)
        {
            splitors[i] = new BatchSplitor(layers[i].detailData.bytes, worldBND, layers[i].mesh);
        }
    }

    private void Update()
    {
        if (splitors != null)
        {
            bool completed = true;
            foreach(var splitor in splitors)
            {
                splitor.OnUpdate();
                if (!splitor.IsComplete)
                    completed = false;
            }
            if (completed)
            {
                int totalCullCnt = 0;
                int batchCount = 0;
                foreach (var splitor in splitors)
                {
                    totalCullCnt += splitor.TotalCount;
                    foreach(var batch in splitor.SplitedBatches)
                    {
                        if (batch.InstanceCount > 0)
                            ++batchCount;
                    }
                }
                batchRendererGroup = new BatchRendererGroup(this.OnPerformCulling);
                cullData = new NativeArray<CullData>(totalCullCnt, Allocator.Persistent);
                cullDataOffset = new NativeArray<int>(batchCount, Allocator.Persistent);
                int cullOffset = 0;
                int batchIdx = 0;
                for(int i=0; i<splitors.Length; ++i)
                {
                    var splitor = splitors[i];
                    if (splitor.TotalCount <= 0)
                        continue;
                    var proto = layers[i];
                    foreach (var batch in splitor.SplitedBatches)
                    {
                        if (batch.InstanceCount > 0)
                        {
                            cullDataOffset[batchIdx] = cullOffset;
                            ++batchIdx;
                            AddBatch(proto.mesh, proto.mat, batch, ref cullOffset);
                        }
                    }
                }
                //clear
                foreach (var splitor in splitors)
                    splitor.Clear();
                splitors = null;
            }
        }
    }

    private void AddBatch(Mesh mesh, Material mat, BatchParameters param, ref int cullOffset)
    {
        MaterialPropertyBlock matBlock = new MaterialPropertyBlock();
        matBlock.SetVectorArray("_PerInstanceColor", param.Colors);
        var batchIndex = this.batchRendererGroup.AddBatch(mesh, 0, mat, 0,
                ShadowCastingMode.On, true, false,
                param.Bnd, param.InstanceCount, matBlock, null);
        var batchMatrices = this.batchRendererGroup.GetBatchMatrices(batchIndex);
        var pos = new float4(0, 0, 0, 1);
        for (int i = 0; i < param.InstanceCount; i++)
        {
            batchMatrices[i] = param.Matrices[i];
            cullData[cullOffset + i] = new CullData()
            {
                extents = 0.5f * param.Bnd.size,
                position = math.mul(param.Matrices[i], pos).xyz,
                minDistance = 0,
                maxDistance = detailDistance,
            };
        }
        cullOffset += param.InstanceCount;
    }
    private JobHandle OnPerformCulling(BatchRendererGroup rendererGroup, BatchCullingContext cullingContext)
    {
        var planes = new NativeArray<float4>(cullingContext.cullingPlanes.Length, Allocator.TempJob);
        for(int i=0; i< cullingContext.cullingPlanes.Length; ++i)
        {
            var p = cullingContext.cullingPlanes[i];
            planes[i] = new float4(p.normal, p.distance);
        }
        var cull = new MTDetailCullJob()
        {
            Planes = planes,
            CamerPos = cullingContext.lodParameters.cameraPosition,
            IndexList = cullingContext.visibleIndices,
            Batches = cullingContext.batchVisibility,
            CullDataOffset = cullDataOffset,
            CullDatas = cullData,
        };
        var handle = cull.Schedule(cullingContext.batchVisibility.Length, 16, cullingDependency);
        cullingDependency = JobHandle.CombineDependencies(handle, cullingDependency);
        return handle;
    }

    private void OnDestroy()
    {
        if (this.batchRendererGroup != null)
        {
            cullingDependency.Complete();
            this.batchRendererGroup.Dispose();
            this.batchRendererGroup = null;
            cullData.Dispose();
            cullDataOffset.Dispose();
        }
    }
}
