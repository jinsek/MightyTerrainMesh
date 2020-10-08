using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
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
                    SaveMesh(lodFolder[lod], data.meshId, data.lods[lod], sliceCount);
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
    void SaveMesh(string folder, int dataID, MTMeshData.LOD data, float uvScale)
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
        mesh.triangles = data.faces;
        AssetDatabase.CreateAsset(mesh, string.Format("{0}/{1}.mesh", folder, dataID));
    }
    void RunBakeMaterial(string path, Terrain t, int sliceTexRes)
    {
        int sliceCount = Mathf.FloorToInt(Mathf.Pow(2, QuadTreeDepth));
        int ctrlMapResolution = t.terrainData.alphamapResolution;
        int pixelStep = ctrlMapResolution / sliceCount;
        for(int u = 0; u < sliceCount; ++u)
        {
            for (int v = 0; v < sliceCount; ++v)
            {
                EditorUtility.DisplayProgressBar("bake texture", "processing", (float)(u * sliceCount + v) /(sliceCount * sliceCount));
                string diffusePath = string.Format("{0}/diffuse_{1}{2}.tga", path, u, v);
                string normalPath = string.Format("{0}/norm_{1}{2}.tga", path, u, v);
                string matPath = string.Format("{0}/mat_{1}{2}.mat", path, u, v);
                BakeMaterialTexture(diffusePath, normalPath, matPath, t, pixelStep, sliceTexRes, u * pixelStep, v * pixelStep);
            }
        }
        
    }
    void BakeMaterialTexture(string diffuse, string normal, string matPath, Terrain t, int ctrlMapRes, int sliceTexRes, float startU, float startV)
    {
        Texture2D bakedDiffuse = new Texture2D(sliceTexRes, sliceTexRes);
        Texture2D bakedNormal = new Texture2D(sliceTexRes, sliceTexRes);
        float stride = (float)ctrlMapRes / sliceTexRes;
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
                Color mixed = MTMatUtils.BakePixel(t, startU + pu, startV + pv);
                bakedDiffuse.SetPixel(u, v, mixed);
                Color norm = MTMatUtils.BakeNormal(t, startU + pu, startV + pv);
                bakedNormal.SetPixel(u, v, norm);
            }
        }
        SaveTextureIMG(bakedDiffuse, diffuse);
        SaveTextureIMG(bakedNormal, normal);
        AssetDatabase.Refresh();
        //reimport texture for materials
        TextureImporter importer_diff = AssetImporter.GetAtPath(diffuse) as TextureImporter;
        importer_diff.textureType = TextureImporterType.Default;
        importer_diff.wrapMode = TextureWrapMode.Clamp;
        EditorUtility.SetDirty(importer_diff);
        importer_diff.SaveAndReimport();
        TextureImporter importer_norm = AssetImporter.GetAtPath(normal) as TextureImporter;
        importer_norm.textureType = TextureImporterType.NormalMap;
        importer_norm.wrapMode = TextureWrapMode.Clamp;
        EditorUtility.SetDirty(importer_norm);
        importer_norm.SaveAndReimport();
        Material tMat = new Material(Shader.Find("Mobile/Bumped Diffuse"));
        tMat.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture>(diffuse));
        tMat.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture>(normal));
        AssetDatabase.CreateAsset(tMat, matPath);
    }
    void SaveTextureIMG(Texture2D tex, string path)
    {
        byte[] mapData = tex.EncodeToTGA();
        if (File.Exists(path))
            File.Delete(path);
        FileStream stream = File.Open(path, FileMode.Create);
        stream.Write(mapData, 0, mapData.Length);
        stream.Close();
    }
}
