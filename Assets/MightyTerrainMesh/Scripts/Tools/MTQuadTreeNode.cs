namespace MightyTerrainMesh
{
    using System.Collections;
    using System.Collections.Generic;
    using System;
    using System.IO;
    using UnityEngine;

    public class MTQuadTreeNode
    {
        public Bounds bnd;
        public int cellIdx = -1;
        public int meshIdx = -1;
        public byte lodLv = 0;
        public int[] children = new int[0];
        private float mDiameter = 0;
        public MTQuadTreeNode(int cid)
        {
            cellIdx = cid;
            InnerInit();
        }
        public float PixelSize(Vector3 viewCenter, float fov, float screenH)
        {
            float distance = Vector3.Distance(viewCenter, bnd.center);
            return (mDiameter * Mathf.Rad2Deg * screenH) / (distance * fov);
        }
        private void InnerInit()
        {
            var horizon_size = bnd.size;
            horizon_size.y = 0;
            mDiameter = horizon_size.magnitude;
        }
        public void Serialize(Stream stream)
        {
            MTFileUtils.WriteVector3(stream, bnd.center);
            MTFileUtils.WriteVector3(stream, bnd.size);
            MTFileUtils.WriteInt(stream, meshIdx);
            MTFileUtils.WriteInt(stream, cellIdx);
            MTFileUtils.WriteByte(stream, lodLv);
            MTFileUtils.WriteInt(stream, children.Length);
            foreach (var child in children)
            {
                MTFileUtils.WriteInt(stream, child);
            }
        }
        public void Deserialize(Stream stream, Vector3 offset)
        {
            Vector3 center = MTFileUtils.ReadVector3(stream);
            Vector3 size = MTFileUtils.ReadVector3(stream);
            meshIdx = MTFileUtils.ReadInt(stream);
            cellIdx = MTFileUtils.ReadInt(stream);
            lodLv = MTFileUtils.ReadByte(stream);
            int len = MTFileUtils.ReadInt(stream);
            bnd = new Bounds(center + offset, size);
            children = new int[len];
            for (int i = 0; i < len; ++i)
            {
                children[i] = MTFileUtils.ReadInt(stream);
            }
            InnerInit();
        }
    }

    public class MTQuadTreeUtil
    {
        public int NodeCount
        {
            get
            {
                return treeNodes.Length;
            }
        }
        public Bounds Bound
        {
            get
            {
                return treeNodes[0].bnd;
            }
        }
        public MTArray<MTQuadTreeNode> ActiveNodes { get { return activeMeshes; } }
        public float MinCellSize { get; private set; }
        protected MTQuadTreeNode[] treeNodes;
        protected MTArray<MTQuadTreeNode> candidates;
        protected MTArray<MTQuadTreeNode> activeMeshes;
        protected MTArray<MTQuadTreeNode> visibleMeshes;
        public MTQuadTreeUtil(byte[] data, Vector3 offset)
        {
            MemoryStream stream = new MemoryStream(data);
            int treeLen = MTFileUtils.ReadInt(stream);
            InnerInit(treeLen, stream, offset);
            stream.Close();
        }
        public MTQuadTreeUtil(int treeLen, Stream stream, Vector3 offset)
        {
            InnerInit(treeLen, stream, offset);
        }
        public void InnerInit(int treeLen, Stream stream, Vector3 offset)
        {
            treeNodes = new MTQuadTreeNode[treeLen];
            MinCellSize = float.MaxValue;
            for (int i = 0; i < treeLen; ++i)
            {
                var node = new MTQuadTreeNode(-1);
                node.Deserialize(stream, offset);
                treeNodes[i] = node;
                var size = Mathf.Min(node.bnd.size.x, node.bnd.size.z);
                if (size < MinCellSize)
                {
                    MinCellSize = size;
                }
            }
            candidates = new MTArray<MTQuadTreeNode>(treeNodes.Length);
            activeMeshes = new MTArray<MTQuadTreeNode>(treeNodes.Length);
            visibleMeshes = new MTArray<MTQuadTreeNode>(treeNodes.Length);
        }
        public void ResetRuntimeCache()
        {
            candidates.Reset();
            activeMeshes.Reset();
            visibleMeshes.Reset();
        }

        public void CullQuadtree(Vector3 viewCenter, float fov, float screenH, float screenW, Matrix4x4 world2Cam, Matrix4x4 projectMatrix,
            MTArray<MTQuadTreeNode> activeCmd, MTArray<MTQuadTreeNode> deactiveCmd, MTLODPolicy lodPolicy)
        {
            Plane[] planes = GeometryUtility.CalculateFrustumPlanes(projectMatrix * world2Cam);
            visibleMeshes.Reset();
            candidates.Reset();
            candidates.Add(treeNodes[0]);
            //此处仅是限制最多循环次数
            int loop = 0;
            int next_start_idx = 0;
            for (; loop < treeNodes.Length; ++loop)
            {
                int c_idx = next_start_idx;
                next_start_idx = candidates.Length;
                for (; c_idx < next_start_idx; ++c_idx)
                {
                    var node = candidates.Data[c_idx];
                    var stop_child = false;                    
                    if (node.meshIdx >= 0)
                    {
                        float pixelSize = node.PixelSize(viewCenter, fov, screenH);
                        int lodLv = lodPolicy.GetLODLevel(pixelSize, screenW);
                        if (node.lodLv <= lodLv)
                        {
                            visibleMeshes.Add(node);
                            //此级以下全部隐藏
                            stop_child = true;
                        }
                    }
                    if (!stop_child && node.children.Length > 0)
                    {
                        foreach (var c in node.children)
                        {
                            var childNode = treeNodes[c];
                            if (GeometryUtility.TestPlanesAABB(planes, childNode.bnd))
                            {
                                candidates.Add(childNode);
                            }
                        }
                    }
                }
                if (candidates.Length == next_start_idx)
                    break;
            }
            //new cells
            for (int i=0; i< visibleMeshes.Length; ++i)
            {
                var meshId = visibleMeshes.Data[i];
                if (!activeMeshes.Contains(meshId))
                {
                    activeCmd.Add(meshId);
                }
            }
            //old cells
            for (int i = 0; i < activeMeshes.Length; ++i)
            {
                var meshId = activeMeshes.Data[i];
                if (!visibleMeshes.Contains(meshId))
                {
                    deactiveCmd.Add(meshId);
                }
            }
            var temp = activeMeshes;
            activeMeshes = visibleMeshes;
            visibleMeshes = temp;
        }
    }

    /// <summary>
    /// utility classes
    /// </summary>
    public class MTQuadTreeBuildNode
    {
        public Bounds Bound;
        public int MeshID = -1;
        public int LODLv = -1;
        public MTQuadTreeBuildNode[] SubNode;
        public Vector2 UVMin;
        public Vector2 UVMax;
        public MTQuadTreeBuildNode(int depth, Vector3 min, Vector3 max, Vector2 uvmin, Vector2 uvmax)
        {
            Vector3 center = 0.5f * (min + max);
            Vector3 size = max - min;
            Vector2 uvcenter = 0.5f * (uvmin + uvmax);
            Vector2 uvsize = uvmax - uvmin;
            Bound = new Bounds(center, size);
            UVMin = uvmin;
            UVMax = uvmax;
            if (depth > 0)
            {
                SubNode = new MTQuadTreeBuildNode[4];
                Vector3 subMin = new Vector3(center.x - 0.5f * size.x, min.y, center.z - 0.5f * size.z);
                Vector3 subMax = new Vector3(center.x, max.y, center.z);
                Vector2 uvsubMin = new Vector2(uvcenter.x - 0.5f * uvsize.x, uvcenter.y - 0.5f * uvsize.y);
                Vector2 uvsubMax = new Vector2(uvcenter.x, uvcenter.y);
                SubNode[0] = CreateSubNode(depth - 1, subMin, subMax, uvsubMin, uvsubMax);
                subMin = new Vector3(center.x, min.y, center.z - 0.5f * size.z);
                subMax = new Vector3(center.x + 0.5f * size.x, max.y, center.z);
                uvsubMin = new Vector2(uvcenter.x, uvcenter.y - 0.5f * uvsize.y);
                uvsubMax = new Vector2(uvcenter.x + 0.5f * uvsize.x, uvcenter.y);
                SubNode[1] = CreateSubNode(depth - 1, subMin, subMax, uvsubMin, uvsubMax);
                subMin = new Vector3(center.x - 0.5f * size.x, min.y, center.z);
                subMax = new Vector3(center.x, max.y, center.z + 0.5f * size.z);
                uvsubMin = new Vector2(uvcenter.x - 0.5f * uvsize.x, uvcenter.y);
                uvsubMax = new Vector2(uvcenter.x, uvcenter.y + 0.5f * uvsize.y);
                SubNode[2] = CreateSubNode(depth - 1, subMin, subMax, uvsubMin, uvsubMax);
                subMin = new Vector3(center.x, min.y, center.z);
                subMax = new Vector3(center.x + 0.5f * size.x, max.y, center.z + 0.5f * size.z);
                uvsubMin = new Vector2(uvcenter.x, uvcenter.y);
                uvsubMax = new Vector2(uvcenter.x + 0.5f * uvsize.x, uvcenter.y + 0.5f * uvsize.y);
                SubNode[3] = CreateSubNode(depth - 1, subMin, subMax, uvsubMin, uvsubMax);
            }
        }
        protected virtual MTQuadTreeBuildNode CreateSubNode(int depth, Vector3 min, Vector3 max, Vector2 uvmin, Vector2 uvmax)
        {
            return new MTQuadTreeBuildNode(depth, min, max, uvmin, uvmax);
        }
        public bool AddMesh(MTMeshData data)
        {
            if (Bound.Contains(data.BND.center) && data.BND.size.x > 0.5f * Bound.size.x)
            {
                MeshID = data.meshId;
                LODLv = data.lodLv;
                data.lods[0].uvmin = UVMin;
                data.lods[0].uvmax = UVMax;
                return true;
            }
            else if (SubNode != null)
            {
                for (int i = 0; i < 4; ++i)
                {
                    if (SubNode[i].AddMesh(data))
                        return true;
                }
            }
            return false;
        }
        public bool GetBounds(int meshId, ref Bounds bnd)
        {
            if (SubNode == null && MeshID == meshId)
            {
                bnd = this.Bound;
                return true;
            }
            else if (SubNode != null)
            {
                for (int i = 0; i < 4; ++i)
                {
                    if (SubNode[i].GetBounds(meshId, ref bnd))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}