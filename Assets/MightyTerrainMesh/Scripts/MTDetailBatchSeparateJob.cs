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
struct MTDetailBatchSeparateJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<float4x4> spawnMatrix;
    [ReadOnly] public NativeArray<SeparateBound> bounds;
    [NativeDisableParallelForRestriction]
    public NativeArray<int> batchIds;

    public void Execute(int index)
    {
        var pos = new float4(0, 0, 0, 1);
        float3 pt = math.mul(spawnMatrix[index], pos).xyz;
        for(int i=0; i<bounds.Length; ++i)
        {
            var bnd = bounds[i];
            if (pt.x < bnd.max.x && pt.y < bnd.max.y && pt.z < bnd.max.z &&
                pt.x >= bnd.min.x && pt.y >= bnd.min.y && pt.z >= bnd.min.z)
            {
                batchIds[index] = i + 1;
                break;
            }
        }
    }
}