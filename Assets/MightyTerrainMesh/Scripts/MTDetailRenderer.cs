namespace MightyTerrainMesh
{
    using System.IO;
    using System.Collections.Generic;
    using System.Diagnostics;
    using UnityEngine;
    using UnityEngine.Rendering;
    using Unity.Collections;
    using UnityEngine.Rendering.Universal;

    public class MTDetailPatchDrawParam
    {
        private static Queue<MTDetailPatchDrawParam> _qPool = new Queue<MTDetailPatchDrawParam>();
        public static MTDetailPatchDrawParam Pop()
        {
            if (_qPool.Count > 0)
            {
                return _qPool.Dequeue();
            }
            return new MTDetailPatchDrawParam();
        }
        public static void Push(MTDetailPatchDrawParam p)
        {
            p.Used = 0;
            _qPool.Enqueue(p);
        }
        public static void Clear()
        {
            _qPool.Clear();
        }
        public int Used = 0;
        public Matrix4x4[] matrixs;
        public Vector4[] colors;
        public MaterialPropertyBlock matBlock;
        public MTDetailPatchDrawParam()
        {}
        public void Reset(int size)
        {
            if (matrixs == null || size > matrixs.Length)
            {
                size = Mathf.Min(size, 1023);
                matrixs = new Matrix4x4[size];
                colors = new Vector4[size];
            }
            if (matBlock == null)
                matBlock = new MaterialPropertyBlock();
            matBlock.Clear();
            Used = 0;
        }
    }
    internal class PatchMaterialCutoffAnim
    {
        const float maxCutoffVal = 1.01f;
        const float cutoffAnimDuration = 0.3f;
        public const int Playing = 0;
        public const int PlayDone = 1;
        public int State { get; private set; }
        protected float cutoffAnimStartTime = 0;
        protected float cutoffVal = 0.5f;
        protected float animCutoffVal = 0.5f;
        public bool Reversed { get; private set; }
        public bool MatInvisible
        {
            get
            {
                return State == PlayDone && Reversed;
            }
        }
        protected Material target;
        public PatchMaterialCutoffAnim(Material mat)
        {
            Reversed = false;
            target = mat;
            cutoffVal = target.GetFloat("_Cutoff");
            State = PlayDone;
        }
        public void Replay(bool isReverse)
        {
            if (State == Playing && Reversed == isReverse)
                return;
            Reversed = isReverse;
            if (State == Playing)
            {
                float timeSkiped = cutoffAnimDuration - (Time.time - cutoffAnimStartTime);
                cutoffAnimStartTime = Time.time - timeSkiped;
                InterpolateValue(timeSkiped);
            }
            else
            {
                cutoffAnimStartTime = Time.time;
                animCutoffVal = Reversed ? cutoffVal : maxCutoffVal;
            }
            State = Playing;
        }
        private void InterpolateValue(float timePast)
        {
            float rate = timePast / cutoffAnimDuration;
            if (Reversed)
            {
                animCutoffVal = Mathf.Lerp(cutoffVal, maxCutoffVal, rate);
            }
            else
            {
                animCutoffVal = Mathf.Lerp(maxCutoffVal, cutoffVal, rate);
            }
        }
        public void Update()
        {
            if (State == PlayDone)
                return;
            float timePast = Time.time - cutoffAnimStartTime;
            if (timePast >= cutoffAnimDuration)
            {
                State = PlayDone;
                timePast = cutoffAnimDuration;
            }
            InterpolateValue(timePast);
            target.SetFloat("_Cutoff", animCutoffVal);
        }
    }
    public abstract class MTDetailPatchLayer
    {
        public abstract bool IsSpawnDone { get; }
        protected Vector3 localScale = Vector3.one;
        protected Mesh mesh;
        protected Material material_lod0;
        protected Material material_lod1;
        protected MTDetailLayerData layerData;
        protected MTArray<MTDetailPatchDrawParam> drawParam;
        protected int totalPrototypeCount;
        protected bool isReceiveShadow = false;
        //
        PatchMaterialCutoffAnim cutoffAnim;
        public MTDetailPatchLayer(MTDetailLayerData data, bool receiveShadow)
        {
            layerData = data;
            localScale = data.prototype.transform.localScale;
            mesh = data.prototype.GetComponent<MeshFilter>().sharedMesh;
            var matSrc = data.prototype.GetComponent<MeshRenderer>().sharedMaterial;
            material_lod0 = new Material(matSrc);
            isReceiveShadow = receiveShadow;
            if (isReceiveShadow)
            {
                material_lod0.EnableKeyword("_MAIN_LIGHT_SHADOWS");
                if (UniversalRenderPipeline.asset.supportsSoftShadows)
                {
                    material_lod0.EnableKeyword("_SHADOWS_SOFT");
                }
                else
                {
                    material_lod0.DisableKeyword("_SHADOWS_SOFT");
                }
                if (UniversalRenderPipeline.asset.shadowCascadeCount > 1)
                {
                    material_lod0.EnableKeyword("_MAIN_LIGHT_SHADOWS_CASCADE");
                }
                else
                {
                    material_lod0.DisableKeyword("_MAIN_LIGHT_SHADOWS_CASCADE");
                }
            }
            material_lod1 = new Material(material_lod0);
            material_lod1.DisableKeyword("_NORMALMAP");
            material_lod1.EnableKeyword("FORCE_UP_NORMAL");
            cutoffAnim = new PatchMaterialCutoffAnim(material_lod1);
        }
        public virtual void OnActivate(bool rebuild)
        {
            if (cutoffAnim.State != PatchMaterialCutoffAnim.PlayDone)
            {
                //still visible
                cutoffAnim.Replay(false);
            }
            if (rebuild)
            {
                //go to rebuild parameters
                totalPrototypeCount = 0;
            }
        }
        public virtual void OnDrawParamReady()
        {
            cutoffAnim.Replay(false);
        }
        public virtual void OnDeactive()
        {
            cutoffAnim.Replay(true);
        }
        public virtual void PushData()
        {
            //has to return memory
            if (drawParam != null)
            {
                for (int i = 0; i < drawParam.Length; ++i)
                {
                    MTDetailPatchDrawParam.Push(drawParam.Data[i]);
                }
                drawParam.Reset();
            }
        }
        public abstract void TickBuild();
        public virtual void OnDraw(Camera drawCamera, int lod, ref bool matInvisible)
        {
            if (drawParam != null)
            {
                cutoffAnim.Update();
                if (cutoffAnim.MatInvisible)
                {
                    matInvisible = true;
                    return;
                }
                matInvisible = false;
                for (int i = 0; i < drawParam.Length; ++i)
                {
                    if (drawParam.Data[i].Used <= 0)
                        continue;
                    var mat = material_lod0;
                    if (lod > 0)
                        mat = material_lod1;
                    drawParam.Data[i].matBlock.SetVectorArray("_PerInstanceColor", drawParam.Data[i].colors);
                    Graphics.DrawMeshInstanced(mesh, 0, mat, drawParam.Data[i].matrixs, drawParam.Data[i].Used,
                        drawParam.Data[i].matBlock, ShadowCastingMode.Off,
                        isReceiveShadow, LayerMask.NameToLayer("Default"), drawCamera);
                }
            }
        }
        public virtual void Clear()
        {
            PushData();
            cutoffAnim = null;
            if (material_lod0 != null)
            {
                Object.Destroy(material_lod0);
                material_lod0 = null;
            }
            if (material_lod1 != null)
            {
                Object.Destroy(material_lod1);
                material_lod1 = null;
            }
        }
    }

    public abstract class MTDetailPatch
    {
        public abstract bool IsBuildDone { get; }
        protected int den_x;
        protected int den_z;
        protected Vector3 pos_Param;
        protected MTData headerData;
        protected MTDetailPatchLayer[] layers;
        protected Vector2 center;
        protected float lod0Range;
        public MTDetailPatch(int dx, int dz, Vector3 posParam, MTData header)
        {
            den_x = dx;
            den_z = dz;
            pos_Param = posParam;
            headerData = header;
            center = new Vector2(pos_Param.x + (den_x + 0.5f) * pos_Param.z, pos_Param.y + (den_z + 0.5f) * pos_Param.z);
            lod0Range = pos_Param.z * 1.5f;
        }
        public virtual void Activate()
        {
            for (int l = 0; l < layers.Length; ++l)
            {
                layers[l].OnActivate(true);
            }
        }
        public virtual void Deactivate()
        {
            for (int l = 0; l < layers.Length; ++l)
            {
                layers[l].OnDeactive();
            }
        }
        public virtual void PushData()
        {
            for (int l = 0; l < layers.Length; ++l)
            {
                layers[l].PushData();
            }
        }
        public virtual void Clear()
        {
            for (int l = 0; l < layers.Length; ++l)
            {
                layers[l].Clear();
            }
        }
        public abstract void TickBuild();
        public void Draw(Camera drawCamera, ref bool bInvisible)
        {
            int lod = 1;
            if (drawCamera != null)
            {
                Vector2 distance = new Vector2(center.x - drawCamera.transform.position.x, center.y - drawCamera.transform.position.z);
                if (distance.magnitude < lod0Range)
                    lod = 0;
            }
            bInvisible = true;
            for (int i = 0; i < layers.Length; ++i)
            {
                bool matInvisible = true;
                layers[i].OnDraw(drawCamera, lod, ref matInvisible);
                if (!matInvisible)
                    bInvisible = false;
            }
        }
        public void DrawDebug()
        {
#if UNITY_EDITOR
            Gizmos.color = Color.yellow;
            //
            var min = new Vector3(pos_Param.x + den_x * pos_Param.z, 0, pos_Param.y + den_z * pos_Param.z);
            var size = new Vector3(pos_Param.z, 100, pos_Param.z);
            Gizmos.DrawWireCube(min + 0.5f * size, size);
#endif
        }
    }

    internal class MTDetailQuadTreeNode
    {
        public Bounds BND;
        public MTDetailQuadTreeNode[] children;
        public int PatchId = -1;
        private int depth = 0;
        public MTDetailQuadTreeNode(int top, Bounds nodeBnd, Bounds worldBounds)
        {
            BND = nodeBnd;
            depth = top;
            if (depth < 1)
            {
                var localCenter = BND.center - worldBounds.min;
                int iWidth = Mathf.FloorToInt(worldBounds.size.x / nodeBnd.size.x);
                int px = Mathf.FloorToInt(localCenter.x / nodeBnd.size.x);
                int pz = Mathf.FloorToInt(localCenter.z / nodeBnd.size.z);
                PatchId = pz * iWidth + px;
                return;
            }
            children = new MTDetailQuadTreeNode[4];
            Vector3 subSize = nodeBnd.size;
            subSize.x *= 0.5f;
            subSize.z *= 0.5f;
            Vector3 subCenter = nodeBnd.center;
            subCenter.x -= 0.5f * subSize.x;
            subCenter.z -= 0.5f * subSize.z;
            children[0] = new MTDetailQuadTreeNode(top - 1, new Bounds(subCenter, subSize), worldBounds); 
            subCenter = nodeBnd.center;
            subCenter.x += 0.5f * subSize.x;
            subCenter.z -= 0.5f * subSize.z;
            children[1] = new MTDetailQuadTreeNode(top - 1, new Bounds(subCenter, subSize), worldBounds);
            subCenter = nodeBnd.center;
            subCenter.x += 0.5f * subSize.x;
            subCenter.z += 0.5f * subSize.z;
            children[2] = new MTDetailQuadTreeNode(top - 1, new Bounds(subCenter, subSize), worldBounds);
            subCenter = nodeBnd.center;
            subCenter.x -= 0.5f * subSize.x;
            subCenter.z += 0.5f * subSize.z;
            children[3] = new MTDetailQuadTreeNode(top - 1, new Bounds(subCenter, subSize), worldBounds);
        }
        public void CullQuadtree(Plane[] cullPlanes, MTArray<int> visible)
        {
            if (GeometryUtility.TestPlanesAABB(cullPlanes, BND))
            {
                if (children == null)
                {
                    visible.Add(PatchId);
                }
                else
                {
                    foreach(var child in children)
                    {
                        child.CullQuadtree(cullPlanes, visible);
                    }
                }
            }
        }
    }

    public class MTDetailRenderer
    {
        private Bounds mapBND;
        private MTData headerData;
        private int[] patchDataOffsets;
        private NativeArray<byte> densityData;
        private MTDetailPatch[] patches;
        private MTDetailQuadTreeNode tree;
        private int patch_x = 1;
        private int patch_z = 1;
        private List<int> buildingPatches = new List<int>();
        private bool receiveShadow = true;
        private List<int> drawablePatches = new List<int>();
        private MTArray<int> currentVisible;
        private MTArray<int> activePatches;
        private Vector3 patchParam = Vector3.zero;// x offset x, y offset z, z patch size
        public MTDetailRenderer(MTData data, Bounds bnd, bool shadow)
        {
            mapBND = bnd;
            headerData = data;
            patch_x = Mathf.CeilToInt((float)headerData.DetailWidth / headerData.DetailResolutionPerPatch);
            patch_z = Mathf.CeilToInt((float)headerData.DetailHeight / headerData.DetailResolutionPerPatch);
            patches = new MTDetailPatch[patch_x * patch_z];
            patchParam = new Vector3(mapBND.min.x, mapBND.min.z, Mathf.Max(mapBND.size.x / patch_x, mapBND.size.z / patch_z));
            receiveShadow = shadow;
            //quadtree depth
            int treeDepth = Mathf.FloorToInt(Mathf.Log(Mathf.Max(patch_x, patch_z), 2));
            tree = new MTDetailQuadTreeNode(treeDepth, mapBND, mapBND);
            currentVisible = new MTArray<int>(patch_x * patch_z);
            activePatches = new MTArray<int>(patch_x * patch_z);
            //jobs data
            densityData = new NativeArray<byte>(headerData.DetailLayers.bytes, Allocator.Persistent);
            //parse detail data
            patchDataOffsets = new int[patch_x * patch_z * headerData.DetailPrototypes.Length];
            MemoryStream stream = new MemoryStream(headerData.DetailLayers.bytes);
            for(int i=0; i< patchDataOffsets.Length; ++i)
            {
                patchDataOffsets[i] = MTFileUtils.ReadInt(stream);
            }
            stream.Close();
        }
        public void Cull(Plane[] cullPlanes)
        {
            tree.CullQuadtree(cullPlanes, currentVisible);
            for (int i = 0; i < currentVisible.Length; ++i)
            {
                var pId = currentVisible.Data[i];
                if (!activePatches.Contains(pId))
                {
                    ActivePatch(pId);
                }
            }
            for (int i = 0; i < activePatches.Length; ++i)
            {
                var pId = activePatches.Data[i];
                if (!currentVisible.Contains(pId))
                {
                    DeactivePatch(pId);
                }
            }
            var temp = activePatches;
            activePatches = currentVisible;
            currentVisible = temp;
            currentVisible.Reset();
        }
        public void OnUpdate(Camera drawCamera)
        {            
            for (int i = buildingPatches.Count - 1; i>= 0; --i)
            {
                var pid = buildingPatches[i];
                var p = patches[pid];
                p.TickBuild();
                if (p.IsBuildDone)
                {
                    buildingPatches.RemoveAt(i);
                    //MTLog.Log("buildingPatches : " + buildingPatches.Count);
                    drawablePatches.Add(pid);
                }
            }
            for (int i = drawablePatches.Count - 1; i >= 0; --i )
            {
                var pid = drawablePatches[i];
                var p = patches[pid];
                bool invisible = false;
                p.Draw(drawCamera, ref invisible);
                if (invisible)
                {
                    p.PushData();
                    drawablePatches.RemoveAt(i);
                }
            }
        }
        public void Clear()
        {
            drawablePatches.Clear();
            buildingPatches.Clear();
            MTDetailPatchDrawParam.Clear();
            foreach (var patch in patches)
            {
                if (patch != null)
                    patch.Clear();
            }
            densityData.Dispose();
        }
        protected MTDetailPatch CreatePatch(int px, int pz)
        {
            return new MTDetailPatchJobs(px, pz, patch_x, patch_z, patchParam, receiveShadow,
                                   headerData, patchDataOffsets, densityData);
        }
        private void ActivePatch(int pId)
        {
            if (patches[pId] == null)
            {
                int px = pId % patch_x;
                int pz = pId / patch_z;
                patches[pId] = CreatePatch(px, pz);
            }
            var p = patches[pId];
            if (p != null)
            {
                p.Activate();
                if (!p.IsBuildDone)
                    buildingPatches.Add(pId);
            }
        }
        private void DeactivePatch(int pId)
        {
            var p = patches[pId];
            p.Deactivate();
            if (!p.IsBuildDone)
            {
                p.PushData();
                buildingPatches.Remove(pId);
            }
        }
        public void DrawDebug()
        {
            for (int i = 0; i < activePatches.Length; ++i)
            {
                var p = patches[activePatches.Data[i]];
                p.DrawDebug();
            }
        }
    }
}
