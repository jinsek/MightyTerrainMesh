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
    }
}
