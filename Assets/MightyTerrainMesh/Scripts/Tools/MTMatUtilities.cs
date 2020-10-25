namespace MightyTerrainMesh
{
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using UnityEngine;
    using UnityEditor;

    public static class MTMatUtils
    {
        private static void SaveMaterail(string path, string dataName, Terrain t, int matIdx, int layerStart, string shaderName)
        {
#if UNITY_EDITOR
            if (matIdx >= t.terrainData.alphamapTextureCount)
                return;
            string mathPath = string.Format("{0}/{1}_{2}.mat", path, dataName, matIdx);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(mathPath);
            if (mat != null)
                AssetDatabase.DeleteAsset(mathPath);
            //alpha map
            byte[] alphaMapData = t.terrainData.alphamapTextures[matIdx].EncodeToTGA();
            string alphaMapSavePath = string.Format("{0}/{1}_alpha{2}.tga",
                path, dataName, matIdx);
            if (File.Exists(alphaMapSavePath))
                File.Delete(alphaMapSavePath);
            FileStream stream = File.Open(alphaMapSavePath, FileMode.Create);
            stream.Write(alphaMapData, 0, alphaMapData.Length);
            stream.Close();
            AssetDatabase.Refresh();
            string alphaMapPath = string.Format("{0}/{1}_alpha{2}.tga", path,
                dataName, matIdx);
            //the alpha map texture has to be set to best compression quality, otherwise the black spot may
            //show on the ground
            TextureImporter importer = AssetImporter.GetAtPath(alphaMapPath) as TextureImporter;
            if (importer == null)
            {
                MTLog.LogError("export terrain alpha map failed");
                return;
            }
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.textureType = TextureImporterType.Default;
            importer.wrapMode = TextureWrapMode.Clamp;
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
            Texture2D alphaMap = AssetDatabase.LoadAssetAtPath<Texture2D>(alphaMapPath);
            //
            Material tMat = new Material(Shader.Find(shaderName));
            tMat.SetTexture("_Control", alphaMap);
            if (tMat == null)
            {
                MTLog.LogError("export terrain material failed");
                return;
            }
            for (int l = layerStart; l < layerStart + 4 && l < t.terrainData.terrainLayers.Length; ++l)
            {
                int idx = l - layerStart;
                TerrainLayer layer = t.terrainData.terrainLayers[l];
                Vector2 tiling = new Vector2(t.terrainData.size.x / layer.tileSize.x,
                    t.terrainData.size.z / layer.tileSize.y);
                tMat.SetTexture(string.Format("_Splat{0}", idx), layer.diffuseTexture);
                tMat.SetTextureOffset(string.Format("_Splat{0}", idx), layer.tileOffset);
                tMat.SetTextureScale(string.Format("_Splat{0}", idx), tiling);
                tMat.SetTexture(string.Format("_Normal{0}", idx), layer.normalMapTexture);
                tMat.SetFloat(string.Format("_NormalScale{0}", idx), layer.normalScale);
                tMat.SetFloat(string.Format("_Metallic{0}", idx), layer.metallic);
                tMat.SetFloat(string.Format("_Smoothness{0}", idx), layer.smoothness);
            }
            AssetDatabase.CreateAsset(tMat, mathPath);
#endif
        }
        public static void SaveMaterials(string path, string dataName, Terrain t)
        {
#if UNITY_EDITOR
            if (t.terrainData == null)
            {
                MTLog.LogError("terrain data doesn't exist");
                return;
            }
            int matCount = t.terrainData.alphamapTextureCount;
            if (matCount <= 0)
                return;
            //base pass
            SaveMaterail(path, dataName, t, 0, 0, "MT/Standard-BasePass");
            for (int i=1; i<matCount; ++i)
            {
                SaveMaterail(path, dataName, t, i, i * 4, "MT/Standard-AddPass");
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
#endif
        }
        public static Color BakePixel(Terrain t, float u, float v)
        {
            Color c = Color.black;
#if UNITY_EDITOR
            if (t.terrainData == null)
            {
                MTLog.LogError("terrain data doesn't exist");
                return c;
            }
            int matCount = t.terrainData.alphamapTextureCount;
            if (matCount <= 0)
                return c;
            //base pass
            for (int i = 0; i < matCount; ++i)
            {
                c += BakeLayerPixel(t, i, i * 4, u, v);
            }
#endif
            return c;
        }
        public static Color BakeLayerPixel(Terrain t, int matIdx, int layerStart, float u, float v)
        {
            Color c = Color.black;
#if UNITY_EDITOR
            if (matIdx >= t.terrainData.alphamapTextureCount)
                return c;
            float ctrlRes = t.terrainData.alphamapResolution;
            float uvx = u / ctrlRes;
            float uvy = v / ctrlRes;
            Color ctrl = t.terrainData.alphamapTextures[matIdx].GetPixelBilinear(uvx, uvy);
            c += GetLayerDiffusePixel(t.terrainData, layerStart, uvx, uvy, ctrl.r);
            c += GetLayerDiffusePixel(t.terrainData, layerStart + 1, uvx, uvy, ctrl.g);
            c += GetLayerDiffusePixel(t.terrainData, layerStart + 2, uvx, uvy, ctrl.b);
            c += GetLayerDiffusePixel(t.terrainData, layerStart + 3, uvx, uvy, ctrl.a);
#endif
            return c;
        }
        private static Color GetLayerDiffusePixel(TerrainData tData, int l, float uvx, float uvy, float weight)
        {
#if UNITY_EDITOR
            if (l < tData.terrainLayers.Length)
            {
                TerrainLayer layer = tData.terrainLayers[l];
                Vector2 tiling = new Vector2(tData.size.x / layer.tileSize.x,
                   tData.size.z / layer.tileSize.y);
                float u = layer.tileOffset.x + tiling.x * uvx;
                float v = layer.tileOffset.y + tiling.y * uvy;
                return layer.diffuseTexture.GetPixelBilinear(u - Mathf.Floor(u), v - Mathf.Floor(v)) * weight;
            }
#endif
            return new Color(0, 0, 0, 0);
        }
        public static Color BakeNormal(Terrain t, float u, float v)
        {
            Color c = new Color(0, 0, 1, 1);
#if UNITY_EDITOR
            if (t.terrainData == null)
            {
                MTLog.LogError("terrain data doesn't exist");
                return c;
            }
            int matCount = t.terrainData.alphamapTextureCount;
            if (matCount <= 0)
                return c;
            //base pass
            Vector3 normal = BakeLayerNormal(t, 0, 0, u, v);
            for (int i = 1; i < matCount; ++i)
            {
                normal += BakeLayerNormal(t, i, i * 4, u, v);
            }
            c.r = 0.5f * (normal.x + 1f);
            c.g = 0.5f * (normal.y + 1f);
            c.b = 0.5f * (normal.z + 1f);
            c.a = 1;
#endif
            return c;
        }
        public static Vector3 BakeLayerNormal(Terrain t, int matIdx, int layerStart, float u, float v)
        {
#if UNITY_EDITOR
            if (matIdx >= t.terrainData.alphamapTextureCount)
                return Vector3.zero;
            float ctrlRes = t.terrainData.alphamapResolution;
            float uvx = u / ctrlRes;
            float uvy = v / ctrlRes;
            Color ctrl = t.terrainData.alphamapTextures[matIdx].GetPixelBilinear(uvx, uvy);
            Vector3 normal = GetLayerNormal(t.terrainData, layerStart, uvx, uvy) * ctrl.r;
            normal += GetLayerNormal(t.terrainData, layerStart + 1, uvx, uvy) * ctrl.g;
            normal += GetLayerNormal(t.terrainData, layerStart + 2, uvx, uvy) * ctrl.b;
            normal += GetLayerNormal(t.terrainData, layerStart + 3, uvx, uvy) * ctrl.a;
            normal.z = 0.00001f;
            return normal;
#else
            return Vector3.one;
#endif
        }
        private static Vector3 GetLayerNormal(TerrainData tData, int l, float uvx, float uvy)
        {
#if UNITY_EDITOR
            if (l < tData.terrainLayers.Length)
            {
                TerrainLayer layer = tData.terrainLayers[l];
                if (layer.normalMapTexture == null)
                    return Vector3.zero;
                Vector2 tiling = new Vector2(tData.size.x / layer.tileSize.x,
                   tData.size.z / layer.tileSize.y);
                float u = layer.tileOffset.x + tiling.x * uvx;
                float v = layer.tileOffset.y + tiling.y * uvy;
                Color norm = layer.normalMapTexture.GetPixelBilinear(u - Mathf.Floor(u), v - Mathf.Floor(v));
                Vector3 normal = Vector3.up;
                //Unity is saving the normal map in the DXT5 file format, the red channel is stored in the alpha channel 
                normal.x = (norm.a * 2 - 1) * layer.normalScale;
                normal.y = (norm.g * 2 - 1) * layer.normalScale;
                normal.z = 0;
                float z = Mathf.Sqrt(1 - Mathf.Clamp01(Vector3.Dot(normal, normal)));
                normal.z = z;
                return normal;
            }
#endif
            return Vector3.zero;
        }
    }
}
