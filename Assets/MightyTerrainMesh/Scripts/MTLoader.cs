using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MightyTerrainMesh;

internal class MTPatch
{
    private static Queue<MTPatch> _qPool = new Queue<MTPatch>();
    public static MTPatch Pop(Material[] mats)
    {
        if (_qPool.Count > 0)
        {
            return _qPool.Dequeue();
        }
        return new MTPatch(mats);
    }
    public static void Push(MTPatch p)
    {
        p.mGo.SetActive(false);
        _qPool.Enqueue(p);
    }
    public static void Clear()
    {
        while(_qPool.Count > 0)
        {
            _qPool.Dequeue().DestroySelf();
        }
    }
    public uint PatchId { get; private set; }
    private GameObject mGo;
    private MeshFilter mMesh;
    public MTPatch(Material[] mats)
    {
        mGo = new GameObject("_mtpatch");
        MeshRenderer meshR;
        mMesh = mGo.AddComponent<MeshFilter>();
        meshR = mGo.AddComponent<MeshRenderer>();
        meshR.materials = mats;
    }
    public void Reset(uint id, Mesh m)
    {
        mGo.SetActive(true);
        PatchId = id;
        mMesh.mesh = m;
    }
    private void DestroySelf()
    {
        if (mGo != null)
            MonoBehaviour.Destroy(mGo);
        mGo = null;
        mMesh = null;
    }
}
internal class MTRuntimeMesh
{
    public int MeshID { get; private set; }
    private Mesh[] mLOD;
    public MTRuntimeMesh(int meshid, int lod, string dataName)
    {
        MeshID = meshid;
        mLOD = new Mesh[lod];
        MTFileUtils.LoadMesh(mLOD, dataName, meshid);
    }
    public Mesh GetMesh(int lod)
    {
        lod = Mathf.Clamp(lod, 0, mLOD.Length - 1);
        return mLOD[lod];
    }
}

public class MTLoader : MonoBehaviour
{
    public string DataName = "";
    [Header("LOD distance")]
    public float[] lodPolicy = new float[1] { 0 };
    private Camera mCamera;
    private MTQuadTreeHeader mHeader;
    private MTQuadTreeNode mRoot;
    //patch identifier [meshid 30bit][lod 2bit]
    private MTArray<uint> mVisiblePatches;
    private Dictionary<uint, MTPatch> mActivePatches = new Dictionary<uint, MTPatch>();
    private Dictionary<uint, MTPatch> mPatchesFlipBuffer = new Dictionary<uint, MTPatch>();
    //meshes
    private Dictionary<int, MTRuntimeMesh> mMeshPool = new Dictionary<int, MTRuntimeMesh>();
    private bool mbDirty = true;
    private Mesh GetMesh(uint patchId)
    {
        int mId = (int)(patchId >> 2);
        int lod = (int)(patchId & 0x00000003);
        if (mMeshPool.ContainsKey(mId))
        {
            return mMeshPool[mId].GetMesh(lod);
        }
        MTRuntimeMesh rm = new MTRuntimeMesh(mId, mHeader.LOD, mHeader.DataName);
        mMeshPool.Add(mId, rm);
        return rm.GetMesh(lod);
    }
    public void SetDirty()
    {
        mbDirty = true;
    }
    private void Awake()
    {
        if (DataName == "")
            return;
        try
        {
            mHeader = MTFileUtils.LoadQuadTreeHeader(DataName);
            mRoot = new MTQuadTreeNode(mHeader.QuadTreeDepth, mHeader.BoundMin, mHeader.BoundMax);
            foreach (var mh in mHeader.Meshes.Values)
                mRoot.AddMesh(mh);
            int gridMax = 1 << mHeader.QuadTreeDepth;
            mVisiblePatches = new MTArray<uint>(gridMax * gridMax);
            if (lodPolicy.Length < mHeader.LOD)
            {
                float[] policy = new float[mHeader.LOD];
                for (int i = 0; i < lodPolicy.Length; ++i)
                    policy[i] = lodPolicy[i];
                lodPolicy = policy;
            }
            lodPolicy[0] = Mathf.Clamp(lodPolicy[0], 0.5f * mRoot.Bound.size.x / gridMax, lodPolicy[0]);
            lodPolicy[lodPolicy.Length - 1] = float.MaxValue;
        }
        catch
        {
            mHeader = null;
            mRoot = null;
            MTLog.LogError("MTLoader load quadtree header failed");
        }
        mCamera = GetComponent<Camera>();
    }
    private void OnDestroy()
    {
        MTPatch.Clear();
    }
    // Start is called before the first frame update
    void Start()
    {}

    // Update is called once per frame
    void Update()
    {
        //every 10 frame update once
        if (mCamera == null || mRoot == null || !mCamera.enabled || !mbDirty)
            return;
        mbDirty = false;
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(mCamera);
        mVisiblePatches.Reset();
        mRoot.RetrieveVisibleMesh(planes, transform.position, lodPolicy,  mVisiblePatches);
        mPatchesFlipBuffer.Clear();
        for (int i = 0; i < mVisiblePatches.Length; ++i)
        {
            uint pId = mVisiblePatches.Data[i];
            if (mActivePatches.ContainsKey(pId))
            {
                mPatchesFlipBuffer.Add(pId, mActivePatches[pId]);
                mActivePatches.Remove(pId);
            }
            else
            {
                //new patches
                Mesh m = GetMesh(pId);
                if (m != null)
                {
                    MTPatch patch = MTPatch.Pop(mHeader.RuntimeMats);
                    patch.Reset(pId, m);
                    mPatchesFlipBuffer.Add(pId, patch);
                }
            }
        }
        Dictionary<uint, MTPatch>.Enumerator iPatch = mActivePatches.GetEnumerator();
        while (iPatch.MoveNext())
        {
            MTPatch.Push(iPatch.Current.Value);
        }
        mActivePatches.Clear();
        Dictionary<uint, MTPatch> temp = mPatchesFlipBuffer;
        mPatchesFlipBuffer = mActivePatches;
        mActivePatches = temp;
    }
}
