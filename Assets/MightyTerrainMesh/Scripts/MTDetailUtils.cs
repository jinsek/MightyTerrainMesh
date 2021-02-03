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

struct SeparateBound
{
    public float3 min;
    public float3 max;
}

struct CullData
{
    public float3 extents;
    public float3 position;
    public float minDistance;
    public float maxDistance;
}

public class BatchParameters
{
    public List<float4x4> Matrices = new List<float4x4>();
    public List<Vector4> Colors = new List<Vector4>();
    public Bounds Bnd;
    public int InstanceCount = 0;
}

public class BatchSplitor
{
    const int count_per_batch = 1024;
    private MTDetailBatchSeparateJob job;
    private JobHandle separateJobs;
    private Bounds meshBND;
    public bool IsComplete { get; private set; }
    public int TotalCount { get; private set; }
    public BatchParameters[] SplitedBatches { get; private set; }
    private NativeArray<int> batchIds;
    private NativeArray<float4x4> matrices;
    private Vector4[] colors;

    public BatchSplitor(byte[] data, Bounds bnd, Mesh mesh)
    {
        MemoryStream ms = new MemoryStream(data);
        TotalCount = MTFileUtils.ReadInt(ms);
        matrices = new NativeArray<float4x4>(TotalCount, Allocator.Persistent);
        batchIds = new NativeArray<int>(TotalCount, Allocator.Persistent);
        colors = new Vector4[TotalCount];
        int spawned = 0;
        while (ms.Position < ms.Length && spawned < matrices.Length)
        {
            ushort spawnedCount = MTFileUtils.ReadUShort(ms);
            for(int i=0; i< spawnedCount; ++i)
            {
                var pos = MTFileUtils.ReadVector3(ms);
                var scale = MTFileUtils.ReadVector3(ms);
                var color = MTFileUtils.ReadColor(ms);
                matrices[spawned] = float4x4.TRS(pos, Quaternion.identity, scale);                
                colors[spawned] = color;
                batchIds[spawned] = 0;
                ++spawned;
            }
        }
        ms.Close();
        if (spawned != TotalCount)
        {
            Debug.LogError("terrain detail layer total count is different with spawned count");
        }
        //使用空间4叉树分batch
        if (TotalCount >= count_per_batch)
        {
            job = new MTDetailBatchSeparateJob();
            job.batchIds = batchIds;
            job.spawnMatrix = matrices;
            NativeArray<SeparateBound> bounds = new NativeArray<SeparateBound>(4, Allocator.TempJob);
            int offset = 0;
            SplitBounds(TotalCount, bnd.min, bnd.max, bounds, ref offset);
            job.bounds = bounds;
            separateJobs = job.Schedule(TotalCount, 16);
            IsComplete = false;
        }
        else
        {
            SplitedBatches = new BatchParameters[1];
            var param = new BatchParameters() { InstanceCount = TotalCount, Bnd = new Bounds(bnd.center, bnd.size) };
            for (int i = 0; i < matrices.Length; ++i)
            {
                param.Matrices.Add(matrices[i]);
                param.Colors.Add(colors[i]);
            }
            SplitedBatches[0] = param;
            LogResults();
            batchIds.Dispose();
            matrices.Dispose();
            IsComplete = true;
        }
    }
    private void SplitBounds(int totalCount, float3 min, float3 max, NativeArray<SeparateBound> sbnds, ref int offset)
    {
        if (totalCount < count_per_batch)
        {
            SeparateBound sb = new SeparateBound();
            sb.min = min;
            sb.max = max;
            sbnds[offset] = sb; ++offset;
        }
        else
        {
            float3 size = max - min;
            float3 subSize = new float3(0.5f * size.x, size.y, 0.5f * size.z);
            SeparateBound sb = new SeparateBound();
            sb.min = min;
            sb.max = min + subSize;
            sbnds[offset] = sb; ++offset;
            sb.min = min + new float3(subSize.x, 0, 0);
            sb.max = sb.min + subSize;
            sbnds[offset] = sb; ++offset;
            sb.min = min + new float3(subSize.x, 0, subSize.z);
            sb.max = max;
            sbnds[offset] = sb; ++offset;
            sb.min = min + new float3(0, 0, subSize.z);
            sb.max = sb.min + subSize;
            sbnds[offset] = sb; ++offset;
        }
    }
    private int[] GetBatchInstanceCount(int batchCnt)
    {
        int[] instanceCnt = new int[batchCnt];
        for (int i = 0; i < batchCnt; ++i)
            instanceCnt[i] = 0;
        for (int i = 0; i < batchIds.Length; ++i)
        {
            int batchId = batchIds[i] - 1;
            if (batchId < 0)
                continue;
            instanceCnt[batchId] += 1;
        }
        return instanceCnt;
    }
    public void OnUpdate()
    {
        if (IsComplete)
            return;
        if (separateJobs.IsCompleted)
        {
            separateJobs.Complete();
            int batchCnt = 0;
            int[] batchInstanceCnts = GetBatchInstanceCount(job.bounds.Length);
            foreach (var objCnt in batchInstanceCnts)
            {
                //Debug.Log("boundObjCounts " + objCnt);
                if (objCnt >= count_per_batch)
                {
                    batchCnt += 4;
                }
                else
                {
                    batchCnt += 1;
                }
            }
            //Debug.Log("current batch count " + batchCnt);
            if (batchCnt > job.bounds.Length)
            {
                //Debug.Log("split depth int batch count " + batchCnt);
                int offset = 0;
                var nextBnds = new NativeArray<SeparateBound>(batchCnt, Allocator.TempJob);
                for (int i=0; i<job.bounds.Length; ++i)
                {
                    SplitBounds(batchInstanceCnts[i], job.bounds[i].min, job.bounds[i].max, nextBnds, ref offset);
                }
                job.bounds.Dispose();
                for (int i=0; i<job.batchIds.Length; ++i)
                {
                    job.batchIds[i] = 0;
                }
                job.bounds = nextBnds;
                separateJobs = job.Schedule(TotalCount, 16);
            }
            else {
                FinalizeJobs();
                IsComplete = true;
            }
        }
    }
    public void Clear()
    {
        if (!IsComplete)
        {
            separateJobs.Complete();
            job.bounds.Dispose();
            batchIds.Dispose();
            matrices.Dispose();
        }
        colors = null;
    }
    private void LogResults()
    {
        //Debug.Log(string.Format("---------------------------Batch Total Count : {0} -----------------------------------", TotalCount));
        //foreach (var p in SplitedBatches)
        //{
        //    Debug.Log(string.Format("Batch Object Count : {0}  Matrices Count : {1} Colors Count {2}", p.InstanceCount, p.Matrices.Count, p.Colors.Count));
        //    Debug.Log(string.Format("Batch Bounds min = {0}, {1}, {2}  max = {3}, {4}, {5}", p.Bnd.min.x, p.Bnd.min.y, p.Bnd.min.z,
        //        p.Bnd.max.x, p.Bnd.max.y, p.Bnd.max.z));
        //}
        //Debug.Log(string.Format("---------------------------Batch End -----------------------------------", TotalCount));
    }
    private void FinalizeJobs()
    {
        int[] batchInstanceCnts = GetBatchInstanceCount(job.bounds.Length);
        int batchCnt = job.bounds.Length;
        SplitedBatches = new BatchParameters[batchCnt];
        for(int i=0; i<batchCnt; ++i)
        {
            Vector3 size = job.bounds[i].max - job.bounds[i].min;
            Vector3 center = job.bounds[i].min;
            center += 0.5f * size;
            SplitedBatches[i] = new BatchParameters() { InstanceCount = batchInstanceCnts[i], Bnd = new Bounds(center, size) };
        }
        for(int i=0; i<batchIds.Length; ++i)
        {
            int batchId = batchIds[i] - 1;
            if (batchId < 0)
                continue;
            BatchParameters param = SplitedBatches[batchId];
            param.Matrices.Add(matrices[i]);
            param.Colors.Add(colors[i]);
        }
        LogResults();
        job.bounds.Dispose();
        batchIds.Dispose();
        matrices.Dispose();
    }
}
