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
    public virtual void OnGUIDraw(int idx)
    {
        bEditorUIFoldout = EditorGUILayout.Foldout(bEditorUIFoldout, string.Format("LOD {0}", idx));
        if (!bEditorUIFoldout)
        {
            EditorGUI.indentLevel++;
            int curRate = Mathf.FloorToInt(Mathf.Pow(2, Subdivision));
            int sampleRate = EditorGUILayout.IntField("Sample(NxN)", curRate);
            if (curRate != sampleRate)
            {
                curRate = Mathf.NextPowerOfTwo(sampleRate);
                Subdivision = Mathf.FloorToInt(Mathf.Log(curRate, 2));
            }
            var error = EditorGUILayout.FloatField("Slope Angle Error", SlopeAngleError);
            SlopeAngleError = Mathf.Max(0.01f, error);
            EditorGUI.indentLevel--;
        }
    }
}

internal class MeshTextureBaker
{
    public Vector2 uvMin { get; private set; }
    public Vector2 uvMax { get; private set; }
    public Material layer0 { get; private set; }
    public Material layer1 { get; private set; }
    public Texture2D BakeResult { get; set; }
    public int Size { get { return texSize; } }
    private int texSize = 32;
    public MeshTextureBaker(int size, Vector2 min, Vector2 max, Material m0, Material m1)
    {
        texSize = size;
        uvMin = min;
        uvMax = max;
        layer0 = m0;
        layer1 = m1;
        BakeResult = new Texture2D(texSize, texSize, TextureFormat.ARGB32, false);
    }
}
internal class MeshPrefabBaker
{
    public int lod { get; private set; }
    public int meshId { get; private set; }
    public Mesh mesh { get; private set; }
    public Vector4 scaleOffset { get; private set; }
    public MeshPrefabBaker(int i, int mid, Mesh m, Vector2 uvMin, Vector2 uvMax)
    {
        lod = i;
        meshId = mid;
        mesh = m;
        var v = new Vector4(1, 1, 0, 0);
        v.x = uvMax.x - uvMin.x;
        v.y = uvMax.y - uvMin.y;
        v.z = uvMin.x;
        v.w = uvMin.y;
        scaleOffset = v;
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
    private bool genUV2 = false;
    private int lodCount = 1;
    private bool bakeMaterial = false;
    private int bakeTextureSize = 2048;
    //
    private CreateMeshJob dataCreateJob;
    private TessellationJob tessellationJob;
    //
    private void OnGUI()
    {
        Terrain curentTarget = EditorGUILayout.ObjectField("Convert Target",terrainTarget, typeof(Terrain), true) as Terrain;
        if (curentTarget != terrainTarget)
        {
            terrainTarget = curentTarget;
        }
        int curSliceCount = Mathf.FloorToInt(Mathf.Pow(2, QuadTreeDepth));
        int sliceCount = EditorGUILayout.IntField("Slice Count(NxN)", curSliceCount);
        if (sliceCount != curSliceCount)
        {
            curSliceCount = Mathf.NextPowerOfTwo(sliceCount);
            QuadTreeDepth = Mathf.FloorToInt(Mathf.Log(curSliceCount, 2));
        }
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
        lodCount = EditorGUILayout.IntField("LOD Count", LODSettings.Length);
        if (LODSettings.Length > 0)
        {
            EditorGUI.indentLevel++;
            for (int i = 0; i < LODSettings.Length; ++i)
                LODSettings[i].OnGUIDraw(i);
            EditorGUI.indentLevel--;
        }
        bakeMaterial = EditorGUILayout.ToggleLeft("Bake Material", bakeMaterial);
        if (bakeMaterial)
        {
            bakeTextureSize = EditorGUILayout.IntField("Bake Texture Size", bakeTextureSize);
            bakeTextureSize = Mathf.NextPowerOfTwo(bakeTextureSize);
        }
        genUV2 = EditorGUILayout.ToggleLeft("Generate UV2", genUV2);
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
            int gridMax = 1 << QuadTreeDepth;
            var tBnd = new Bounds(terrainTarget.transform.TransformPoint(terrainTarget.terrainData.bounds.center),
                terrainTarget.terrainData.bounds.size);
            dataCreateJob = new CreateMeshJob(terrainTarget, tBnd, gridMax, gridMax, LODSettings);
            for (int i = 0; i < int.MaxValue; ++i)
            {
                dataCreateJob.Update();
                EditorUtility.DisplayProgressBar("creating data", "scaning volumn", dataCreateJob.progress);
                if (dataCreateJob.IsDone)
                    break;
            }
            dataCreateJob.EndProcess();
            //caculate min_tri size
            int max_sub = 1;
            foreach(var setting in LODSettings)
            {
                if (setting.Subdivision > max_sub)
                    max_sub = setting.Subdivision;
            }
            float max_sub_grids = gridMax * (1 << max_sub);
            float minArea = Mathf.Max(terrainTarget.terrainData.bounds.size.x, terrainTarget.terrainData.bounds.size.z) / max_sub_grids;
            minArea = minArea * minArea / 8f;
            //
            tessellationJob = new TessellationJob(dataCreateJob.LODs, minArea);
            for (int i = 0; i < int.MaxValue; ++i)
            {
                tessellationJob.Update();
                EditorUtility.DisplayProgressBar("creating data", "tessellation", tessellationJob.progress);
                if (tessellationJob.IsDone)
                    break;
            }
            string[] lodFolder = new string[LODSettings.Length];
            for (int i = 0; i < LODSettings.Length; ++i)
            {
                string guid = AssetDatabase.CreateFolder("Assets", string.Format("{0}_LOD{1}", terrainTarget.name, i));
                lodFolder[i] = AssetDatabase.GUIDToAssetPath(guid);
            }
            //save meshes
            List<MeshPrefabBaker> bakers = new List<MeshPrefabBaker>();
            for (int i = 0; i < tessellationJob.mesh.Length; ++i)
            {
                EditorUtility.DisplayProgressBar("saving data", "processing", (float)i / tessellationJob.mesh.Length);
                MTMeshData data = tessellationJob.mesh[i];
                for (int lod = 0; lod < data.lods.Length; ++lod)
                {
                    var folder = lodFolder[lod];
                    if (!AssetDatabase.IsValidFolder(Path.Combine(folder, "Meshes")))
                    {
                        AssetDatabase.CreateFolder(folder, "Meshes");
                        AssetDatabase.Refresh();
                    }
                    var mesh = SaveMesh(folder + "/Meshes", data.meshId, data.lods[lod], genUV2);
                    var baker = new MeshPrefabBaker(lod, data.meshId, mesh, data.lods[lod].uvmin, data.lods[lod].uvmax);
                    bakers.Add(baker);
                }
            }
            //bake mesh prefab
            GameObject[] prefabRoot = new GameObject[LODSettings.Length];
            if (bakeMaterial)
            {
                FullBakeMeshes(bakers, curentTarget, lodFolder, prefabRoot);
            }
            else
            {
                List<Material> mats = new List<Material>();
                var folder = lodFolder[0];
                List<string> matPath = new List<string>();
                MTMatUtils.SaveMixMaterials(folder, curentTarget.name, curentTarget, matPath);
                foreach(var p in matPath)
                {
                    var m = AssetDatabase.LoadAssetAtPath<Material>(p);
                    mats.Add(m);
                }                
                for (int i = 0; i < bakers.Count; ++i)
                {
                    EditorUtility.DisplayProgressBar("saving data", "processing", (float)i / bakers.Count);
                    var baker = bakers[i];
                    //prefab
                    if (prefabRoot[baker.lod] == null)
                    {
                        prefabRoot[baker.lod] = new GameObject(curentTarget.name);
                    }
                    GameObject meshGo = new GameObject(baker.meshId.ToString());
                    var filter = meshGo.AddComponent<MeshFilter>();
                    filter.mesh = baker.mesh;
                    var renderer = meshGo.AddComponent<MeshRenderer>();
                    renderer.sharedMaterials = mats.ToArray();
                    meshGo.transform.parent = prefabRoot[baker.lod].transform;
                }
            }
            //
            for (int i=prefabRoot.Length - 1; i>= 0; --i)
            {
                var folder = lodFolder[i];
                PrefabUtility.SaveAsPrefabAsset(prefabRoot[i], folder + "/" + curentTarget.name + ".prefab");
                DestroyImmediate(prefabRoot[i]);
            }
            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
        }
    }
    void FullBakeMeshes(List<MeshPrefabBaker> bakers, Terrain curentTarget, string[] lodFolder, GameObject[] prefabRoot)
    {
        //reload Mats
        var arrAlbetoMats = new Material[2];
        var arrNormalMats = new Material[2];
        MTMatUtils.GetBakeMaterials(curentTarget, arrAlbetoMats, arrNormalMats);
        var texture = new Texture2D(bakeTextureSize, bakeTextureSize, TextureFormat.RGBA32, false);
        RenderTexture renderTexture = RenderTexture.GetTemporary(bakeTextureSize, bakeTextureSize);
        //
        for (int i = 0; i < bakers.Count; ++i)
        {
            EditorUtility.DisplayProgressBar("saving data", "processing", (float)i / bakers.Count);
            var baker = bakers[i];
            var folder = lodFolder[baker.lod];
            if (!AssetDatabase.IsValidFolder(Path.Combine(folder, "Textures")))
            {
                AssetDatabase.CreateFolder(folder, "Textures");
                AssetDatabase.Refresh();
            }
            //Debug.Log("mesh id : " + baker.meshId + " scale offset : " + baker.scaleOffset);
            var albeto = string.Format("{0}/Textures/albeto_{1}.png", folder, baker.meshId);
            SaveBakedTexture(albeto, renderTexture, texture, arrAlbetoMats, baker.scaleOffset);
            var normal = string.Format("{0}/Textures/normal_{1}.png", folder, baker.meshId);
            SaveBakedTexture(normal, renderTexture, texture, arrNormalMats, baker.scaleOffset);
            AssetDatabase.Refresh();
            var albetoTex = AssetDatabase.LoadAssetAtPath<Texture2D>(albeto);
            var normalTex = AssetDatabase.LoadAssetAtPath<Texture2D>(normal);
            if (!AssetDatabase.IsValidFolder(Path.Combine(folder, "Materials")))
            {
                AssetDatabase.CreateFolder(folder, "Materials");
                AssetDatabase.Refresh();
            }
            var matPath = string.Format("{0}/Materials/mat_{1}.mat", folder, baker.meshId);
            SaveBakedMaterial(matPath, albetoTex, normalTex, new Vector2(baker.scaleOffset.x, baker.scaleOffset.y));
            AssetDatabase.Refresh();
            //prefab
            if (prefabRoot[baker.lod] == null)
            {
                prefabRoot[baker.lod] = new GameObject(curentTarget.name);
            }
            GameObject meshGo = new GameObject(baker.meshId.ToString());
            var filter = meshGo.AddComponent<MeshFilter>();
            filter.mesh = baker.mesh;
            var renderer = meshGo.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            meshGo.transform.parent = prefabRoot[baker.lod].transform;
        }
        RenderTexture.ReleaseTemporary(renderTexture);
        foreach (var mat in arrAlbetoMats)
            DestroyImmediate(mat);
        foreach (var mat in arrNormalMats)
            DestroyImmediate(mat);
        DestroyImmediate(texture);
    }
    Mesh SaveMesh(string folder, int dataID, MTMeshData.LOD data, bool genUV2)
    {
        Mesh mesh = new Mesh();
        mesh.vertices = data.vertices;
        mesh.normals = data.normals;
        mesh.uv = data.uvs;
        if (genUV2)
        {
            mesh.uv2 = data.uvs;
        }
        mesh.triangles = data.faces;
        AssetDatabase.CreateAsset(mesh, string.Format("{0}/{1}.mesh", folder, dataID));
        return mesh;
    }
    void SaveBakedTexture(string path, RenderTexture renderTexture, Texture2D texture, Material[] arrMats, Vector4 scaleOffset)
    {
        //don't know why, need render twice to make the uv work correct
        for (int loop=0; loop<2; ++loop)
        {
            Graphics.Blit(null, renderTexture, arrMats[0]);
            arrMats[0].SetVector("_BakeScaleOffset", scaleOffset);
            if (arrMats[1] != null)
            {
                Graphics.Blit(null, renderTexture, arrMats[1]);
                arrMats[1].SetVector("_BakeScaleOffset", scaleOffset);
            }
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = renderTexture;
            texture.ReadPixels(new Rect(Vector2.zero, new Vector2(texture.width, texture.height)), 0, 0);
            texture.Apply();
            RenderTexture.active = previous;
        }
        byte[] tga = texture.EncodeToTGA();
        File.WriteAllBytes(path, tga);
    }
    void SaveBakedMaterial(string path, Texture2D albeto, Texture2D normal, Vector2 size)
    {
        var scale = new Vector2(1f / size.x, 1f / size.y);
        Material tMat = new Material(Shader.Find("MT/TerrainVTLit"));
        tMat.SetTexture("_Diffuse", albeto);
        tMat.SetTextureScale("_Diffuse", scale);
        tMat.SetTexture("_Normal", normal);
        tMat.SetTextureScale("_Normal", scale);
        tMat.EnableKeyword("_NORMALMAP");
        AssetDatabase.CreateAsset(tMat, path);
    }
}
