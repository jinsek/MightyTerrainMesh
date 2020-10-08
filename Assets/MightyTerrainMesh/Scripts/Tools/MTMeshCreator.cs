using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using MightyTerrainMesh;
using System;
//
[Serializable]
public class MTMeshLODSetting : MeshLODCreate
{
    public bool bEditorUIFoldout = true;
}
//
public class MTMeshCreator : MonoBehaviour
{
    public Bounds VolumnBound;
    public int QuadTreeDepth;
    [HideInInspector]
    public MTMeshLODSetting[] LOD = new MTMeshLODSetting[0];
    public bool DrawGizmo = true;
    public string DataName = "";
    //intermediate data
    private CreateDataJob mCreateDataJob;
    public float EditorCreateDataProgress
    {
        get
        {
            if (mCreateDataJob != null)
            {
                return mCreateDataJob.progress;
            }
            return 0;
        }
    }
    public bool IsEditorCreateDataDone
    {
        get
        {
            if (mCreateDataJob != null)
            {
                return mCreateDataJob.IsDone;
            }
            return true;
        }
    }
    //
    TessellationJob mTessellationJob;
    public float EditorTessProgress
    {
        get
        {
            if (mTessellationJob != null)
            {
                return mTessellationJob.progress;
            }
            return 0;
        }
    }
    public bool IsEditorTessDone
    {
        get
        {
            if (mTessellationJob != null)
            {
                return mTessellationJob.IsDone;
            }
            return true;
        }
    }
    public void EditorCreateDataBegin()
    {
        if (DataName == "")
        {
            MTLog.LogError("data should have a name");
            return;
        }
        if (LOD == null || LOD.Length == 0)
        {
            MTLog.LogError("no lod setting");
            return;
        }
        if (Terrain.activeTerrain == null)
        {
            MTLog.LogError("no active terrain");
            return;
        }
        int gridMax = 1 << QuadTreeDepth;
        mCreateDataJob = new CreateDataJob(VolumnBound, gridMax, gridMax, LOD);
    }
    public bool EditorCreateDataUpdate()
    {
        if (mCreateDataJob == null)
            return true;
        mCreateDataJob.Update();
        return mCreateDataJob.IsDone;
    }
    public void EditorCreateDataEnd()
    {
        if (mCreateDataJob == null)
            return;
        //finaliz the tree data
        mCreateDataJob.EndProcess();        
    }
    public void EditorTessBegin()
    {
        if (mCreateDataJob == null || mCreateDataJob.LODs == null)
            return;
        mTessellationJob = new TessellationJob(mCreateDataJob.LODs);
    }
    public void EditorTessUpdate()
    {
        if (mTessellationJob == null)
            return;
        mTessellationJob.Update();
    }
    public void EditorTessEnd()
    {
        if (mTessellationJob == null || LOD == null)
            return;
        //save data
        MTQuadTreeHeader header = new MTQuadTreeHeader(DataName);
        header.QuadTreeDepth = QuadTreeDepth;
        header.BoundMin = VolumnBound.min;
        header.BoundMax = VolumnBound.max;
        header.LOD = LOD.Length;
        foreach(var m in mTessellationJob.mesh)
        {
            MTMeshHeader mh = new MTMeshHeader(m.meshId, m.center);
            header.Meshes.Add(m.meshId, mh);
            MTFileUtils.SaveMesh(DataName, m);
        }
        MTLog.Log("mesh saved!");
        MTFileUtils.SaveQuadTreeHeader(DataName, header, Terrain.activeTerrain.terrainData.alphamapTextureCount);
        MTLog.Log("header saved!");
        string matPath = "Assets/MightyTerrainMesh/Resources";
        MTMatUtils.SaveMaterials(matPath, DataName, Terrain.activeTerrain);
        MTLog.Log("material saved!");
    }
    public void EditorCreatePreview()
    {
        if (DataName == "")
        {
            MTLog.LogError("data should have a name");
            return;
        }
        try
        {
            Transform[] lodParent = new Transform[LOD.Length];
            for(int i=0; i<LOD.Length; ++i)
            {
                GameObject lodGo = new GameObject("lod" + i);
                lodGo.transform.parent = transform;
                lodParent[i] = lodGo.transform;
            }
            MTQuadTreeHeader header = MTFileUtils.LoadQuadTreeHeader(DataName);
            foreach(var m in header.Meshes.Values)
            {
                Mesh[] lods = new Mesh[LOD.Length];
                MTFileUtils.LoadMesh(lods, DataName, m.MeshID);
                for (int i = 0; i < LOD.Length; ++i)
                {
                    MeshFilter meshF;
                    MeshRenderer meshR;
                    GameObject meshGo = new GameObject("meshObj");
                    meshGo.transform.parent = lodParent[i];
                    meshF = meshGo.AddComponent<MeshFilter>();
                    meshR = meshGo.AddComponent<MeshRenderer>();
                    meshR.materials = header.RuntimeMats;
                    meshF.sharedMesh = lods[i];
                }
            }
        }
        catch
        {
            MTLog.LogError("failed to load datas");
        }
    }
    public void EditorClearPreview()
    {
        while(transform.childCount > 0)
        {
            Transform t = transform.GetChild(0);
            if (t == null || t.gameObject == null)
                break;
            DestroyImmediate(t.gameObject);
        }
    }
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!DrawGizmo)
            return;
        int gridMax = 1 << QuadTreeDepth;
        Vector2 GridSize = new Vector2(VolumnBound.size.x / gridMax, VolumnBound.size.z / gridMax);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(VolumnBound.center, VolumnBound.size);
        if (GridSize.magnitude > 0)
        {
            int uCount = Mathf.CeilToInt(VolumnBound.size.x / GridSize.x);
            int vCount = Mathf.CeilToInt(VolumnBound.size.z / GridSize.y);
            Vector3 vStart = new Vector3(VolumnBound.center.x - VolumnBound.size.x / 2,
                 VolumnBound.center.y - VolumnBound.size.y / 2,
                VolumnBound.center.z - VolumnBound.size.z / 2);
            for (int u = 1; u < uCount; ++u)
            {
                for (int v = 1; v < vCount; ++v)
                {
                    Gizmos.DrawLine(vStart + v * GridSize.y * Vector3.forward,
                        vStart + v * GridSize.y * Vector3.forward + VolumnBound.size.x * Vector3.right);
                    Gizmos.DrawLine(vStart + u * GridSize.x * Vector3.right,
                        vStart + u * GridSize.x * Vector3.right + VolumnBound.size.x * Vector3.forward);
                }
            }
        }
    }
#endif
}
