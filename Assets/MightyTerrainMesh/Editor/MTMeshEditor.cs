using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Unity.Collections;
using Unity.Jobs;
using MightyTerrainMesh;

public class MTLODSetting : MeshLODCreate
{
    public bool bEditorUIFoldout = true;
    public void OnGUIDraw(int idx)
    {
        bEditorUIFoldout = EditorGUILayout.Foldout(bEditorUIFoldout, string.Format("LOD {0}", idx));
        if (!bEditorUIFoldout)
        {
            int curRate = Mathf.FloorToInt(Mathf.Pow(2, Subdivision));
            int sampleRate = EditorGUILayout.IntField("Sample(NxN)", curRate);
            if (curRate != sampleRate)
            {
                curRate = Mathf.NextPowerOfTwo(sampleRate);
                Subdivision = Mathf.FloorToInt(Mathf.Log(curRate, 2));
            }
            SlopeAngleError = EditorGUILayout.FloatField("Slope Angle Error", SlopeAngleError);
        }
    }
}

public class MTMeshEditor : EditorWindow
{
    [MenuItem("MightyTerrainMesh/MeshCreator")]
    private static void ShowWindow()
    {
        EditorWindow.CreateWindow<MTMeshEditor>();
    }
    //properties
    private int QuadTreeDepth = 2;
    private MTLODSetting[] LODSettings = new MTLODSetting[0];
    private Terrain terrainTarget;
    private bool BakeMaterial = false;
    private int BakeTexRes = 2048;
    private bool genUV2 = false;
    //
    private CreateDataJob dataCreateJob;
    private TessellationJob tessellationJob;
    //
    private void OnGUI()
    {
        Terrain curentTarget = EditorGUILayout.ObjectField("Convert Target",terrainTarget, typeof(Terrain), true) as Terrain;
        if (curentTarget != terrainTarget)
        {
            ClearAll();
            terrainTarget = curentTarget;
        }
        int curSliceCount = Mathf.FloorToInt(Mathf.Pow(2, QuadTreeDepth));
        int sliceCount = EditorGUILayout.IntField("Slice Count(NxN)", curSliceCount);
        if (sliceCount != curSliceCount)
        {
            curSliceCount = Mathf.NextPowerOfTwo(sliceCount);
            QuadTreeDepth = Mathf.FloorToInt(Mathf.Log(curSliceCount, 2));
        }

        int lodCount = EditorGUILayout.IntField("LOD Count", LODSettings.Length);
        if (lodCount != LODSettings.Length)
        {
            MTLODSetting[] old = LODSettings;
            LODSettings = new MTLODSetting[lodCount];
            for (int i=0; i<Mathf.Min(lodCount, old.Length); ++i)
            {
                LODSettings[i] = old[i];
            }
            for (int i = Mathf.Min(lodCount, old.Length); i < Mathf.Max(lodCount, old.Length); ++i)
            {
                LODSettings[i] = new MTLODSetting();
            }
        }
        if (LODSettings.Length > 0)
        {
            for (int i = 0; i < LODSettings.Length; ++i)
                LODSettings[i].OnGUIDraw(i);
        }
        genUV2 = EditorGUILayout.ToggleLeft("Generate UV2", genUV2);
        BakeMaterial = EditorGUILayout.ToggleLeft("Bake Material", BakeMaterial);
        if (BakeMaterial)
        {
            BakeTexRes = EditorGUILayout.IntField("Bake Texture Resolution", BakeTexRes);
            BakeTexRes = Mathf.Min(2048, Mathf.NextPowerOfTwo(BakeTexRes));
        }
        if (GUILayout.Button("Generate"))
        {
            if (LODSettings == null || LODSettings.Length == 0)
            {
                MTLog.LogError("no lod setting");
                return;
            }
            if (terrainTarget == null)
            {
                MTLog.LogError("no target terrain");
                return;
            }
            string[] lodFolder = new string[LODSettings.Length];
            for (int i = 0; i < LODSettings.Length; ++i)
            {
                string guid = AssetDatabase.CreateFolder("Assets", string.Format("{0}_LOD{1}", terrainTarget.name, i));
                lodFolder[i] = AssetDatabase.GUIDToAssetPath(guid);
            }
            int gridMax = 1 << QuadTreeDepth;
            dataCreateJob = new CreateDataJob(terrainTarget.terrainData.bounds, gridMax, gridMax, LODSettings);
            for (int i = 0; i < int.MaxValue; ++i)
            {
                dataCreateJob.Update();
                EditorUtility.DisplayProgressBar("creating data", "scaning volumn", dataCreateJob.progress);
                if (dataCreateJob.IsDone)
                    break;
            }
            dataCreateJob.EndProcess();
            tessellationJob = new TessellationJob(dataCreateJob.LODs);
            for (int i = 0; i < int.MaxValue; ++i)
            {
                tessellationJob.Update();
                EditorUtility.DisplayProgressBar("creating data", "tessellation", tessellationJob.progress);
                if (tessellationJob.IsDone)
                    break;
            }
            for (int i = 0; i < tessellationJob.mesh.Length; ++i)
            {
                EditorUtility.DisplayProgressBar("saving data", "processing", (float)i/tessellationJob.mesh.Length);
                MTMeshData data = tessellationJob.mesh[i];
                for(int lod=0; lod<data.lods.Length; ++lod)
                {
                    SaveMesh(lodFolder[lod], data.meshId, data.lods[lod], sliceCount, genUV2);
                    if (!BakeMaterial)
                    {
                        MTMatUtils.SaveMaterials(lodFolder[lod], terrainTarget.name, terrainTarget);
                    }
                }
            }
            if (BakeMaterial)
            {
                string guid = AssetDatabase.CreateFolder("Assets", string.Format("{0}_BakedMats", terrainTarget.name));
                AssetDatabase.Refresh();
                RunBakeMaterial(AssetDatabase.GUIDToAssetPath(guid), terrainTarget, BakeTexRes);
            }
            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
        }
    }
    //functions
    void ClearAll()
    {
        QuadTreeDepth = 2;
        LODSettings = new MTLODSetting[0]; 
    }
    void SaveMesh(string folder, int dataID, MTMeshData.LOD data, float uvScale, bool genUV2)
    {
        Mesh mesh = new Mesh();
        mesh.vertices = data.vertices;
        mesh.normals = data.normals;
        Vector2[] scaledUV = new Vector2[data.uvs.Length];
        float min_u = float.MaxValue;
        float min_v = float.MaxValue;
        for (int i=0; i<data.uvs.Length; ++i)
        {
            scaledUV[i] = data.uvs[i] * uvScale;
            if (scaledUV[i].x < min_u)
                min_u = scaledUV[i].x;
            if (scaledUV[i].y < min_v)
                min_v = scaledUV[i].y;
        }
        for (int i = 0; i < scaledUV.Length; ++i)
        {
            scaledUV[i].x -= min_u;
            scaledUV[i].y -= min_v;
        }
        mesh.uv = scaledUV;
        if (genUV2)
        {
            mesh.uv2 = data.uvs;
        }
        mesh.triangles = data.faces;
        AssetDatabase.CreateAsset(mesh, string.Format("{0}/{1}.mesh", folder, dataID));
    }
    void RunBakeMaterial(string path, Terrain t, int sliceTexRes)
    {
        int sliceCount = Mathf.FloorToInt(Mathf.Pow(2, QuadTreeDepth));
        int ctrlMapResolution = t.terrainData.alphamapResolution;
        int pixelStep = ctrlMapResolution / sliceCount;
        if (t == null)
        {
            Debug.LogWarning("terrain is empty");
            return;
        }
        if (bakers != null)
        {
            Debug.LogWarning("job is running");
            return;
        }
        bakers = new Queue<TerrainLayerBaker>();
        Color[][] layerColors = new Color[t.terrainData.terrainLayers.Length][];
        Color[][] layerNorms = new Color[t.terrainData.terrainLayers.Length][];
        for (int i = 0; i < t.terrainData.terrainLayers.Length; ++i)
        {
            int mipMap = GetLayerMipmapLevel(sliceCount * sliceTexRes, t.terrainData.size, t.terrainData.terrainLayers[i]);
            Debug.Log("mip map level " + mipMap);
            if (t.terrainData.terrainLayers[i].diffuseTexture != null)
                layerColors[i] = t.terrainData.terrainLayers[i].diffuseTexture.GetPixels(mipMap);
            if (t.terrainData.terrainLayers[i].normalMapTexture != null)
                layerNorms[i] = t.terrainData.terrainLayers[i].normalMapTexture.GetPixels(mipMap);
        }
        int matCount = t.terrainData.alphamapTextureCount;
        if (matCount <= 0)
        {
            Debug.LogWarning("terrain has no material");
            return;
        }
        Color[][] ctrlMaps = new Color[matCount][];
        for (int i = 0; i < matCount; ++i)
        {
            ctrlMaps[i] = t.terrainData.alphamapTextures[i].GetPixels();
        }
        for (int u = 0; u < sliceCount; ++u)
        {
            for (int v = 0; v < sliceCount; ++v)
            {
                EditorUtility.DisplayProgressBar("prepare jobs", "processing", (float)(u * sliceCount + v) / (sliceCount * sliceCount));
                var baker = new TerrainLayerBaker(path, u, v);
                for (int i = 0; i < matCount; ++i)
                {
                    var job = CreateDiffuseTexBakeJob(t, sliceTexRes, i * 4, layerColors, ctrlMaps[i], u * pixelStep, v * pixelStep);
                    job.pixelStep = pixelStep;
                    baker.Enqueue(job);
                }
                bakers.Enqueue(baker);
                //normal
                for (int i = 0; i < matCount; ++i)
                {
                    var job = CreateNormalTexBakeJob(t, sliceTexRes, i * 4, layerNorms, ctrlMaps[i], u * pixelStep, v * pixelStep);
                    job.pixelStep = pixelStep;
                    baker.Enqueue(job);
                }
                bakers.Enqueue(baker);
            }
        }
        //base pass
        if (bakers.Count > 0)
        {
            bakerTotalCounts = bakers.Count;
            EditorApplication.update += UpdateJobs;
        }
        else
        {
            bakers = null;
        }
    }
    private int GetLayerMipmapLevel(int totalTexSize, Vector3 terrainSize, TerrainLayer l)
    {
        //获得每张贴贴图实际需要的像素
        float pixelW = totalTexSize /(terrainSize.x / l.tileSize.x);
        float pixelH = totalTexSize / (terrainSize.z / l.tileSize.y);
        int ori_size = Mathf.Max(l.diffuseTexture.width, l.diffuseTexture.height);
        float cur_size = Mathf.Max(pixelW, pixelH);
        int mipmap_l = 1;
        int mipMapSize = ori_size >> mipmap_l;
        while (mipMapSize > cur_size)
        {
            ++mipmap_l;
            mipMapSize = mipMapSize >> 1;
        }
        return mipmap_l - 1;
    }
    private interface ITextureBaker
    {
        string texPath { get; }
        int texSize { get; }
        bool IsLastJob { get; }
        JobHandle StartSchedule();
        JobHandle NextJob();
        Color[] FinalizeJob();
    }
    private class TextureBaker : ITextureBaker
    {
        public Queue<MTTextureBakeJob> jobs = new Queue<MTTextureBakeJob>();
        public string texPath { get; private set; }
        public int texSize { get; private set; }
        public TextureBaker(string path, int u, int v)
        {
            texPath = string.Format("{0}/diffuse_{1}{2}.tga", path, u, v);
        }
        public bool IsLastJob
        {
            get
            {
                return jobs.Count == 1;
            }
        }
        public JobHandle StartSchedule()
        {
            var job = jobs.Peek();
            texSize = job.sliceTexRes;
            return job.Schedule();
        }
        public JobHandle NextJob()
        {
            if (jobs.Count < 2)
            {
                throw new System.Exception("NextJob need 2 jobs");
            }
            MTTextureBakeJob job = jobs.Dequeue();
            Color[] result = job.result.ToArray();
            job.DisposeData();
            MTTextureBakeJob nextJob = jobs.Peek();
            nextJob.baseLayer.CopyFrom(result);
            return nextJob.Schedule();
        }
        public Color[] FinalizeJob()
        {
            if (jobs.Count > 0)
            {
                MTTextureBakeJob job = jobs.Dequeue();
                Color[] result = job.result.ToArray();
                job.DisposeData();
                return result;
            }
            return null;
        }
    }
    private class NormalBaker : ITextureBaker
    {
        public Queue<MTNormalBakeJob> jobs = new Queue<MTNormalBakeJob>();
        public string texPath { get; private set; }
        public int texSize { get; private set; }
        public NormalBaker(string path, int u, int v)
        {
            texPath = string.Format("{0}/norm_{1}{2}.tga", path, u, v);
        }
        public bool IsLastJob
        {
            get
            {
                return jobs.Count == 1;
            }
        }
        public JobHandle StartSchedule()
        {
            var job = jobs.Peek();
            texSize = job.sliceTexRes;
            return job.Schedule();
        }
        public JobHandle NextJob()
        {
            if (jobs.Count < 2)
            {
                throw new System.Exception("NextJob need 2 jobs");
            }
            MTNormalBakeJob job = jobs.Dequeue();
            Vector3[] result = job.result.ToArray();
            job.DisposeData();
            MTNormalBakeJob nextJob = jobs.Peek();
            nextJob.baseLayer.CopyFrom(result);
            return nextJob.Schedule();
        }
        public Color[] FinalizeJob()
        {
            if (jobs.Count > 0)
            {
                MTNormalBakeJob job = jobs.Dequeue();
                Color[] result = job.GetResultColor();
                job.DisposeData();
                return result;
            }
            return null;
        }
    }
    private class TerrainLayerBaker
    {
        public TextureBaker Diffuse { get; private set; }
        public NormalBaker Normal { get; private set; }
        public bool IsCompleted { get; private set; }
        private JobHandle jobHandle;
        private ITextureBaker curBaker;
        private string matPath;
        public TerrainLayerBaker(string path, int u, int v)
        {
            Diffuse = new TextureBaker(path, u, v);
            Normal = new NormalBaker(path, u, v);
            matPath = string.Format("{0}/mat_{1}{2}.mat", path, u, v);
            IsCompleted = false;
        }
        public void Enqueue(MTTextureBakeJob job)
        {
            Diffuse.jobs.Enqueue(job);
        }
        public void Enqueue(MTNormalBakeJob job)
        {
            Normal.jobs.Enqueue(job);
        }
        private void SaveTextureIMG(Texture2D tex, string path)
        {
            byte[] mapData = tex.EncodeToTGA();
            if (File.Exists(path))
                File.Delete(path);
            FileStream stream = File.Open(path, FileMode.Create);
            stream.Write(mapData, 0, mapData.Length);
            stream.Close();
        }
        public void OnUpdate()
        {
            if (IsCompleted)
                return;
            if (curBaker == null)
            {
                curBaker = Diffuse;
                jobHandle = curBaker.StartSchedule();
            }
            else
            {
                if (jobHandle.IsCompleted)
                {
                    jobHandle.Complete();
                    if (curBaker.IsLastJob)
                    {
                        Color[] result = curBaker.FinalizeJob();
                        Texture2D bakedTexture = new Texture2D(curBaker.texSize, curBaker.texSize);
                        bakedTexture.SetPixels(result);
                        SaveTextureIMG(bakedTexture, curBaker.texPath);
                        //save
                        if (curBaker == Diffuse)
                        {
                            curBaker = Normal;
                            jobHandle = curBaker.StartSchedule();
                        }
                        else
                        {
                            AssetDatabase.Refresh();
                            try
                            {
                                TextureImporter importer_diff = AssetImporter.GetAtPath(Diffuse.texPath) as TextureImporter;
                                importer_diff.textureType = TextureImporterType.Default;
                                importer_diff.wrapMode = TextureWrapMode.Clamp;
                                EditorUtility.SetDirty(importer_diff);
                                importer_diff.SaveAndReimport();
                                TextureImporter importer_norm = AssetImporter.GetAtPath(Normal.texPath) as TextureImporter;
                                importer_norm.textureType = TextureImporterType.NormalMap;
                                importer_norm.wrapMode = TextureWrapMode.Clamp;
                                EditorUtility.SetDirty(importer_norm);
                                importer_norm.SaveAndReimport();
                                Material tMat = new Material(Shader.Find("Mobile/Bumped Diffuse"));
                                tMat.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture>(Diffuse.texPath));
                                tMat.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture>(Normal.texPath));
                                AssetDatabase.CreateAsset(tMat, matPath);
                            }
                            catch
                            {
                                Debug.LogError("reimport assets failed");
                            }
                            curBaker = null;
                            IsCompleted = true;
                        }
                    }
                    else
                    {
                        jobHandle = curBaker.NextJob();
                    }
                }
            }
        }
    }
    private Queue<TerrainLayerBaker> bakers;
    public JobHandle jobHandle;
    private int bakerTotalCounts = 1;
    private void UpdateJobs()
    {
        if (bakers == null)
            return;
        var baker = bakers.Peek();
        baker.OnUpdate();
        EditorUtility.DisplayProgressBar("bake texture", "processing", (float)(bakerTotalCounts - bakers.Count) / bakerTotalCounts);
        if (baker.IsCompleted)
        {
            bakers.Dequeue();
            if (bakers.Count == 0)
            {
                bakers = null;
                EditorUtility.ClearProgressBar();
                EditorApplication.update -= UpdateJobs;
                AssetDatabase.Refresh();
            }
        }
    }
    private MTTextureBakeJob CreateDiffuseTexBakeJob(Terrain t, int resultSize, int layerStart, Color[][] colorData, Color[] ctrlData, int startU, int startV)
    {
        MTTextureBakeJob jobData = new MTTextureBakeJob();
        int[] layerTexSize = new int[]{ 0, 0, 0, 0 };
        Color[] colorDefault = new Color[resultSize * resultSize];
        for (int i = 0; i < colorDefault.Length; ++i)
            colorDefault[i] = Color.black;
        jobData.terrainDataSize = t.terrainData.size;
        jobData.baseLayer = new NativeArray<Color>(colorDefault, Allocator.TempJob);
        jobData.result = new NativeArray<Color>(colorDefault, Allocator.TempJob);
        if (colorData.Length > layerStart && colorData[layerStart] != null)
        {
            Color[] data = colorData[layerStart];
            jobData.layer0 = new NativeArray<Color>(data, Allocator.TempJob);
            layerTexSize[0] = (int)Mathf.Sqrt(data.Length);
        }
        else
        {
            jobData.layer0 = new NativeArray<Color>(new Color[0], Allocator.TempJob);
        }
        if (colorData.Length > layerStart + 1 && colorData[layerStart + 1] != null)
        {
            Color[] data = colorData[layerStart + 1];
            jobData.layer1 = new NativeArray<Color>(data, Allocator.TempJob);
            layerTexSize[1] = (int)Mathf.Sqrt(data.Length);
        }
        else
        {
            jobData.layer1 = new NativeArray<Color>(new Color[0], Allocator.TempJob);
        }
        if (colorData.Length > layerStart + 2 && colorData[layerStart + 2] != null)
        {
            Color[] data = colorData[layerStart + 2];
            jobData.layer2 = new NativeArray<Color>(data, Allocator.TempJob);
            layerTexSize[2] = (int)Mathf.Sqrt(data.Length);
        }
        else
        {
            jobData.layer2 = new NativeArray<Color>(new Color[0], Allocator.TempJob);
        }
        if (colorData.Length > layerStart + 3 && colorData[layerStart + 3] != null)
        {
            Color[] data = colorData[layerStart + 3];
            jobData.layer3 = new NativeArray<Color>(data, Allocator.TempJob);
            layerTexSize[3] = (int)Mathf.Sqrt(data.Length);
        }
        else
        {
            jobData.layer3 = new NativeArray<Color>(new Color[0], Allocator.TempJob);
        }
        Vector2[] tileOffset = new Vector2[] { Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero};
        Vector2[] tileSize = new Vector2[] { Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero };
        for (int i = 0; i < 4; ++i)
        {
            if (t.terrainData.terrainLayers.Length > layerStart + i)
            {
                tileOffset[i] = t.terrainData.terrainLayers[layerStart + i].tileOffset;
                tileSize[i] = t.terrainData.terrainLayers[layerStart + i].tileSize;
            }
        }
        jobData.layerSize = new NativeArray<int>(layerTexSize, Allocator.TempJob);
        jobData.tileOffset = new NativeArray<Vector2>(tileOffset, Allocator.TempJob);
        jobData.tileSize = new NativeArray<Vector2>(tileSize, Allocator.TempJob);
        jobData.ctrlMap = new NativeArray<Color>(ctrlData, Allocator.TempJob);
        jobData.ctrlRes = t.terrainData.alphamapResolution;
        jobData.sliceTexRes = resultSize;
        jobData.startU = startU;
        jobData.startV = startV;
        return jobData;
    }
    private MTNormalBakeJob CreateNormalTexBakeJob(Terrain t, int resultSize, int layerStart, Color[][] colorData, Color[] ctrlData, int startU, int startV)
    {
        MTNormalBakeJob jobData = new MTNormalBakeJob();
        int[] layerTexSize = new int[] { 0, 0, 0, 0 };
        Vector3[] vecDefault = new Vector3[resultSize * resultSize];
        for (int i = 0; i < vecDefault.Length; ++i)
            vecDefault[i] = Vector3.zero;
        jobData.terrainDataSize = t.terrainData.size;
        jobData.baseLayer = new NativeArray<Vector3>(vecDefault, Allocator.TempJob);
        jobData.result = new NativeArray<Vector3>(vecDefault, Allocator.TempJob);
        if (colorData.Length > layerStart && colorData[layerStart] != null)
        {
            Color[] data = colorData[layerStart];
            jobData.layer0 = new NativeArray<Color>(data, Allocator.TempJob);
            layerTexSize[0] = (int)Mathf.Sqrt(data.Length);
        }
        else
        {
            jobData.layer0 = new NativeArray<Color>(new Color[0], Allocator.TempJob);
        }
        if (colorData.Length > layerStart + 1 && colorData[layerStart + 1] != null)
        {
            Color[] data = colorData[layerStart + 1];
            jobData.layer1 = new NativeArray<Color>(data, Allocator.TempJob);
            layerTexSize[1] = (int)Mathf.Sqrt(data.Length);
        }
        else
        {
            jobData.layer1 = new NativeArray<Color>(new Color[0], Allocator.TempJob);
        }
        if (colorData.Length > layerStart + 2 && colorData[layerStart + 2] != null)
        {
            Color[] data = colorData[layerStart + 2];
            jobData.layer2 = new NativeArray<Color>(data, Allocator.TempJob);
            layerTexSize[2] = (int)Mathf.Sqrt(data.Length);
        }
        else
        {
            jobData.layer2 = new NativeArray<Color>(new Color[0], Allocator.TempJob);
        }
        if (colorData.Length > layerStart + 3 && colorData[layerStart + 3] != null)
        {
            Color[] data = colorData[layerStart + 3];
            jobData.layer3 = new NativeArray<Color>(data, Allocator.TempJob);
            layerTexSize[3] = (int)Mathf.Sqrt(data.Length);
        }
        else
        {
            jobData.layer3 = new NativeArray<Color>(new Color[0], Allocator.TempJob);
        }
        Vector2[] tileOffset = new Vector2[] { Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero };
        Vector2[] tileSize = new Vector2[] { Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero };
        float[] normScales = new float[] { 0, 0, 0, 0 };
        for (int i = 0; i < 4; ++i)
        {
            if (t.terrainData.terrainLayers.Length > layerStart + i)
            {
                tileOffset[i] = t.terrainData.terrainLayers[layerStart + i].tileOffset;
                tileSize[i] = t.terrainData.terrainLayers[layerStart + i].tileSize;
                normScales[i] = t.terrainData.terrainLayers[layerStart + i].normalScale;
            }
        }
        jobData.layerSize = new NativeArray<int>(layerTexSize, Allocator.TempJob);
        jobData.tileOffset = new NativeArray<Vector2>(tileOffset, Allocator.TempJob);
        jobData.tileSize = new NativeArray<Vector2>(tileSize, Allocator.TempJob);
        jobData.normalScale = new NativeArray<float>(normScales, Allocator.TempJob);
        jobData.ctrlMap = new NativeArray<Color>(ctrlData, Allocator.TempJob);
        jobData.ctrlRes = t.terrainData.alphamapResolution;
        jobData.sliceTexRes = resultSize;
        jobData.startU = startU;
        jobData.startV = startV;
        return jobData;
    }
}
