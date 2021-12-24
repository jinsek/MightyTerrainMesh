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
        const float maxCutoffVal = 1.01f;
        const float cutoffAnimDuration = 0.3f;
        protected float cutoffAnimStartTime = 0;
        protected float cutoffVal = 0.5f;
        protected float animCutoffVal = 0.5f;
        public MTDetailPatchLayer(MTDetailLayerData data, bool receiveShadow)
        {
            layerData = data;
            localScale = data.prototype.transform.localScale;
            mesh = data.prototype.GetComponent<MeshFilter>().sharedMesh;
            var matSrc = data.prototype.GetComponent<MeshRenderer>().sharedMaterial;
            cutoffVal = matSrc.GetFloat("_Cutoff");
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
        }
        public virtual void OnActivate()
        {
            totalPrototypeCount = 0;
        }
        public virtual void OnDrawParamReady()
        {
            animCutoffVal = 1.01f;
            cutoffAnimStartTime = Time.time;
            ResetCutOffVal();
        }
        protected void ResetCutOffVal()
        {
            material_lod0.SetFloat("_Cutoff", animCutoffVal);
            material_lod1.SetFloat("_Cutoff", animCutoffVal);
        }
        public virtual void OnDeactive()
        {
            //has to return memory
            if (drawParam != null)
            {
                for(int i=0; i<drawParam.Length; ++i)
                {
                    MTDetailPatchDrawParam.Push(drawParam.Data[i]);
                }
                drawParam.Reset();
            }
        }
        public abstract void TickBuild();
        public virtual void OnDraw(Camera drawCamera, int lod)
        {
            if (drawParam != null)
            {
                animCutoffVal = Mathf.Lerp(maxCutoffVal, cutoffVal, (Time.time - cutoffAnimStartTime) / cutoffAnimDuration);
                if (animCutoffVal > cutoffVal)
                {
                    ResetCutOffVal();
                }
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
        public abstract bool IsActive { get; }
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
                layers[l].OnActivate();
            }
        }
        public virtual void Deactivate()
        {
            for (int l = 0; l < layers.Length; ++l)
            {
                layers[l].OnDeactive();
            }
        }
        public virtual void Clear()
        {
            Deactivate();
        }
        public abstract void TickBuild();
        public void Draw(Camera drawCamera)
        {
            int lod = 1;
            if (drawCamera != null)
            {
                Vector2 distance = new Vector2(center.x - drawCamera.transform.position.x, center.y - drawCamera.transform.position.z);
                if (distance.magnitude < lod0Range)
                    lod = 0;
            }
            for (int i = 0; i < layers.Length; ++i)
            {
                layers[i].OnDraw(drawCamera, lod);
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
        private Queue<int> buildingPatches = new Queue<int>();
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
            int loopCount = buildingPatches.Count;
            while (loopCount > 0)
            {
                --loopCount;
                var pid = buildingPatches.Dequeue();
                var p = patches[pid];
                if (p.IsActive)
                {
                    p.TickBuild();
                    if (p.IsBuildDone)
                    {
                        //MTLog.Log("buildingPatches : " + buildingPatches.Count);
                        drawablePatches.Add(pid);
                    }
                    else
                    {
                        buildingPatches.Enqueue(pid);
                    }
                }
            }
            foreach(var pid in drawablePatches)
            {
                patches[pid].Draw(drawCamera);
            }
        }
        public void Clear()
        {
            while (buildingPatches.Count > 0)
            {
                var pid = buildingPatches.Dequeue();
            }
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
                buildingPatches.Enqueue(pId);
            }
        }
        private void DeactivePatch(int pId)
        {
            var p = patches[pId];
            p.Deactivate();
            drawablePatches.Remove(pId);
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
