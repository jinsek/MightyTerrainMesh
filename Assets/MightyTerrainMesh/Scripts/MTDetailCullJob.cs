using System.Collections;
using System.Collections.Generic;
using System.IO;
using MightyTerrainMesh;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;
using UnityEngine.Rendering;

[BurstCompile]
struct MTDetailCullJob : IJobParallelFor
{
    [ReadOnly] public float3 CamerPos;
    [NativeDisableParallelForRestriction] [ReadOnly] public NativeArray<int> CullDataOffset;
    [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<float4> Planes;
    [NativeDisableParallelForRestriction] [ReadOnly] public NativeArray<CullData> CullDatas;

    [NativeDisableParallelForRestriction] public NativeArray<BatchVisibility> Batches;
    [NativeDisableParallelForRestriction] public NativeArray<int> IndexList;

    public void Execute(int index)
    {
        var bv = Batches[index];
        var cullDataOffset = CullDataOffset[index];
        var visibleInstancesIndex = 0;
        for (int j = 0; j < bv.instancesCount; ++j)
        {
            if (cullDataOffset + j >= CullDatas.Length)
                continue;
            var cullData = CullDatas[cullDataOffset + j];
            var rootLodDistance = math.length(CamerPos - cullData.position);
            var rootLodIntersect = (rootLodDistance < cullData.maxDistance) && (rootLodDistance >= cullData.minDistance);
            if (rootLodIntersect)
            {
                var chunkIn = true;
                for (int p = 0; p < 6; p++)
                {
                    float3 planeNormal = Planes[p].xyz;
                    float planeConstant = Planes[p].w;
                    if (math.dot(cullData.extents, math.abs(planeNormal)) + math.dot(planeNormal, cullData.position) + planeConstant <= 0)
                        chunkIn = false;
                }
                if (chunkIn && bv.offset + visibleInstancesIndex < IndexList.Length)
                {
                    IndexList[bv.offset + visibleInstancesIndex] = j;
                    visibleInstancesIndex++;
                }
            }
        }
        bv.visibleCount = visibleInstancesIndex;
        Batches[index] = bv;
    }
}