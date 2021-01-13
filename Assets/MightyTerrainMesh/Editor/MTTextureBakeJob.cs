using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;

[BurstCompile]
public struct MTTextureBakeJob : IJob
{
    public NativeArray<Color> result;
    public Vector3 terrainDataSize;
    public int ctrlRes;
    public int pixelStep;
    public int sliceTexRes;
    public int startU;
    public int startV;
    //terrain layers, should assign right mipmap
    public NativeArray<Vector2> tileOffset;
    public NativeArray<Vector2> tileSize;
    public NativeArray<int> layerSize;
    public NativeArray<Color> ctrlMap;
    public NativeArray<Color> layer0;
    public NativeArray<Color> layer1;
    public NativeArray<Color> layer2;
    public NativeArray<Color> layer3;
    public NativeArray<Color> baseLayer;
    public void Execute()
    {
        float stride = (float)pixelStep / sliceTexRes;
        for (int u = 0; u < sliceTexRes; ++u)
        {
            for (int v = 0; v < sliceTexRes; ++v)
            {
                float pu = u * stride;
                float pv = v * stride;
                Color baseC = baseLayer[DataId(u, v, sliceTexRes)];
                result[DataId(u, v, sliceTexRes)] = baseC + BakeLayerPixel(startU + pu, startV + pv);
            }
        }
    }
    public void DisposeData()
    {
        result.Dispose();
        tileOffset.Dispose();
        tileSize.Dispose();
        layerSize.Dispose();
        ctrlMap.Dispose();
        layer0.Dispose();
        layer1.Dispose();
        layer2.Dispose();
        layer3.Dispose();
        baseLayer.Dispose();
    }
    private int DataId(int u, int v, int size)
    {
        u = (u + size) % size;
        v = (v + size) % size;
        return u * size + v;
    }
    private Color BakeLayerPixel(float u, float v)
    {
        Color c = Color.black;
        float uvx = u / ctrlRes;
        float uvy = v / ctrlRes;
        Color ctrl = GetPixelBilinear(ctrlMap, u, v, ctrlRes);
        if (layerSize[0] > 0)
            c += GetLayerDiffusePixel(layer0, layerSize[0], tileOffset[0], tileSize[0], uvx, uvy, ctrl.r);
        if (layerSize[1] > 0)
            c += GetLayerDiffusePixel(layer1, layerSize[1], tileOffset[1], tileSize[1], uvx, uvy, ctrl.g);
        if (layerSize[2] > 0)
            c += GetLayerDiffusePixel(layer2, layerSize[2], tileOffset[2], tileSize[2], uvx, uvy, ctrl.b);
        if (layerSize[3] > 0)
            c += GetLayerDiffusePixel(layer3, layerSize[3], tileOffset[3], tileSize[3], uvx, uvy, ctrl.a);
        return c;
    }
    private Color GetPixelBilinear(NativeArray<Color> layer, float u, float v, int size)
    {
        int iu = Mathf.FloorToInt(u);
        int iv = Mathf.FloorToInt(v);
        Color btm = Color.Lerp(layer[DataId(iu, iv, size)], layer[DataId(iu + 1, iv, size)], u - iu);
        Color top = Color.Lerp(layer[DataId(iu, iv + 1, size)], layer[DataId(iu + 1, iv + 1, size)], u - iu);
        Color left = Color.Lerp(layer[DataId(iu, iv, size)], layer[DataId(iu, iv + 1, size)], v - iv);
        Color right = Color.Lerp(layer[DataId(iu + 1, iv, size)], layer[DataId(iu + 1, iv + 1, size)], v - iv);
        Color c = 0.5f * Color.Lerp(btm, top, v - iv);
        c += 0.5f * Color.Lerp(left, right, u - iu);
        return c;
    }
    private Color GetLayerDiffusePixel(NativeArray<Color> layer, int size, Vector2 tileOffset, Vector2 tileSize, float uvx, float uvy, float weight)
    {
        Vector2 tiling = new Vector2(terrainDataSize.x / tileSize.x, terrainDataSize.z / tileSize.y);
        float u = tileOffset.x + tiling.x * uvx;
        float v = tileOffset.y + tiling.y * uvy;
        int iu = Mathf.FloorToInt((u - Mathf.Floor(u)) * size);
        int iv = Mathf.FloorToInt((v - Mathf.Floor(v)) * size);
        return GetPixelBilinear(layer, iu, iv, size) * weight;
    }
}

[BurstCompile]
public struct MTNormalBakeJob : IJob
{
    public NativeArray<Vector3> result;
    public Vector3 terrainDataSize;
    public int ctrlRes;
    public int pixelStep;
    public int sliceTexRes;
    public int startU;
    public int startV;
    //terrain layers, should assign right mipmap
    public NativeArray<Vector2> tileOffset;
    public NativeArray<Vector2> tileSize;
    public NativeArray<int> layerSize;
    public NativeArray<float> normalScale;
    public NativeArray<Color> ctrlMap;
    public NativeArray<Color> layer0;
    public NativeArray<Color> layer1;
    public NativeArray<Color> layer2;
    public NativeArray<Color> layer3;
    public NativeArray<Vector3> baseLayer;
    public void Execute()
    {
        float stride = (float)pixelStep / sliceTexRes;
        for (int u = 0; u < sliceTexRes; ++u)
        {
            for (int v = 0; v < sliceTexRes; ++v)
            {
                float offset = 0.5f;
                if (u == 0 || v == 0)
                    offset = 0;
                else if (u == sliceTexRes - 1 || v == sliceTexRes - 1)
                    offset = 1f;
                float pu = (u + offset) * stride;
                float pv = (v + offset) * stride;
                var baseNorm = baseLayer[DataId(u, v, sliceTexRes)];
                result[DataId(u, v, sliceTexRes)] = baseNorm + BakeLayerPixel(startU + pu, startV + pv);
            }
        }
    }
    public Color[] GetResultColor()
    {
        Vector3[] normal = result.ToArray();
        Color[] c = new Color[normal.Length];
        for (int i=0; i< normal.Length; ++i)
        {
            c[i].r = 0.5f * (normal[i].x + 1f);
            c[i].g = 0.5f * (normal[i].y + 1f);
            c[i].b = 0.5f * (normal[i].z + 1f);
            c[i].a = 1;
        }
        return c;
    }
    public void DisposeData()
    {
        result.Dispose();
        tileOffset.Dispose();
        tileSize.Dispose();
        layerSize.Dispose();
        normalScale.Dispose();
        ctrlMap.Dispose();
        layer0.Dispose();
        layer1.Dispose();
        layer2.Dispose();
        layer3.Dispose();
        baseLayer.Dispose();
    }
    private int DataId(int u, int v, int size)
    {
        u = (u + size) % size;
        v = (v + size) % size;
        return u * size + v;
    }
    private Vector3 BakeLayerPixel(float u, float v)
    {
        Vector3 norm = Vector3.zero;
        float uvx = u / ctrlRes;
        float uvy = v / ctrlRes;
        Color ctrl = GetPixelBilinear(ctrlMap, u, v, ctrlRes);
        if (layerSize[0] > 0)
            norm += GetLayerNormalPixel(layer0, layerSize[0], normalScale[0], tileOffset[0], tileSize[0], uvx, uvy, ctrl.r);
        if (layerSize[1] > 0)
            norm += GetLayerNormalPixel(layer1, layerSize[1], normalScale[1], tileOffset[1], tileSize[1], uvx, uvy, ctrl.g);
        if (layerSize[2] > 0)
            norm += GetLayerNormalPixel(layer2, layerSize[2], normalScale[2], tileOffset[2], tileSize[2], uvx, uvy, ctrl.b);
        if (layerSize[3] > 0)
            norm += GetLayerNormalPixel(layer3, layerSize[3], normalScale[3], tileOffset[3], tileSize[3], uvx, uvy, ctrl.a);
        //norm.z = 0.00001f;
        return norm;
    }
    private Color GetPixelBilinear(NativeArray<Color> layer, float u, float v, int size)
    {
        int iu = Mathf.FloorToInt(u);
        int iv = Mathf.FloorToInt(v);
        Color btm = Color.Lerp(layer[DataId(iu, iv, size)], layer[DataId(iu + 1, iv, size)], u - iu);
        Color top = Color.Lerp(layer[DataId(iu, iv + 1, size)], layer[DataId(iu + 1, iv + 1, size)], u - iu);
        Color left = Color.Lerp(layer[DataId(iu, iv, size)], layer[DataId(iu, iv + 1, size)], v - iv);
        Color right = Color.Lerp(layer[DataId(iu + 1, iv, size)], layer[DataId(iu + 1, iv + 1, size)], v - iv);
        Color c = 0.5f * Color.Lerp(btm, top, v - iv);
        c += 0.5f * Color.Lerp(left, right, u - iu);
        return c;
    }
    private Vector3 GetLayerNormalPixel(NativeArray<Color> layer, int size, float normScale, Vector2 tileOffset, Vector2 tileSize, float uvx, float uvy, float weight)
    {
        Vector2 tiling = new Vector2(terrainDataSize.x / tileSize.x, terrainDataSize.z / tileSize.y);
        float u = tileOffset.x + tiling.x * uvx;
        float v = tileOffset.y + tiling.y * uvy;
        int iu = Mathf.FloorToInt((u - Mathf.Floor(u)) * size);
        int iv = Mathf.FloorToInt((v - Mathf.Floor(v)) * size);
        Color norm = GetPixelBilinear(layer, iu, iv, size);
        Vector3 normal = Vector3.up;
        //Unity is saving the normal map in the DXT5 file format, the red channel is stored in the alpha channel 
        normal.x = (norm.a * 2 - 1) * normScale;
        normal.y = (norm.g * 2 - 1) * normScale;
        normal.z = 0;
        float z = Mathf.Sqrt(1 - Mathf.Clamp01(Vector3.Dot(normal, normal)));
        normal.z = z;
        return normal * weight;
    }
}