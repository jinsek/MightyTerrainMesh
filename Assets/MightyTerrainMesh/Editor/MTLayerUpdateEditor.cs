using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class MTLayerUpdateEditor : EditorWindow
{
    [MenuItem("MightyTerrainMesh/LayerUpdator")]
    private static void ShowWindow()
    {
        EditorWindow.CreateWindow<MTLayerUpdateEditor>();
    }
    private Terrain terrainTarget;
    private Texture2D[] controlmaps;
    private void OnGUI()
    {
        terrainTarget = EditorGUILayout.ObjectField("Update Target", terrainTarget, typeof(Terrain), true) as Terrain;
        if (terrainTarget != null)
        {
            TerrainData data = terrainTarget.terrainData;
            if (controlmaps == null || controlmaps.Length != data.alphamapTextureCount)
            {
                controlmaps = new Texture2D[data.alphamapTextureCount];
            }
            for (int i = 0; i < data.alphamapTextureCount; ++i)
            {
                controlmaps[i] = EditorGUILayout.ObjectField(string.Format("ctrlmap{0}", i), controlmaps[i], typeof(Texture2D), true) as Texture2D;
            }
            if (GUILayout.Button("Apply"))
            {
                for (int i=0; i< data.alphamapTextureCount; ++i)
                {
                    if (controlmaps[i] == null)
                        continue;
                    if (data.alphamapTextures[i].width != controlmaps[i].width || data.alphamapTextures[i].height != controlmaps[i].height)
                    {
                        Debug.LogError("需要覆盖的贴图尺寸不对");
                        continue;
                    }
                    if (!controlmaps[i].isReadable)
                    {
                        Debug.LogError("需要覆盖的贴图请勾上可读写");
                        continue;
                    }
                    Color[] c = controlmaps[i].GetPixels(0);
                    data.alphamapTextures[i].SetPixels(c);
                    data.alphamapTextures[i].Apply();
                }
                AssetDatabase.Refresh();
                AssetDatabase.SaveAssets();
            }
        }
    }
}
