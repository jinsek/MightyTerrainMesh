using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using MightyTerrainMesh;

public class MTDetailDataCheck : MonoBehaviour
{
    public TextAsset Data;
    private Vector3[] positions;
    private Color[] colors;
    // Start is called before the first frame update
    void Start()
    {
        MemoryStream ms = new MemoryStream(Data.bytes);
        int totalCount = MTFileUtils.ReadInt(ms);
        positions = new Vector3[totalCount];
        colors = new Color[totalCount];
        int spawned = 0;
        while (ms.Position < ms.Length && spawned < totalCount)
        {
            ushort spawnedCount = MTFileUtils.ReadUShort(ms);
            for (int i = 0; i < spawnedCount; ++i)
            {
                positions[spawned] = MTFileUtils.ReadVector3(ms);
                var scale = MTFileUtils.ReadVector3(ms);
                colors[spawned] = MTFileUtils.ReadColor(ms);
                ++spawned;
            }
        }
        ms.Close();
    }

    // Update is called once per frame
    void Update()
    {        
    }
    private void OnDrawGizmos()
    {
        if (positions == null)
            return;
        for (int i = 0; i < positions.Length; ++i)
        {
            Gizmos.color = colors[i];
            Gizmos.DrawSphere(positions[i], 1);
        }
    }
}
