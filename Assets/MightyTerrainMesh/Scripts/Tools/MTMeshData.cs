namespace MightyTerrainMesh
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public class MTMeshHeader
    {
        public int MeshID { get; private set; }
        public Vector3 Center { get; private set; }
        public MTMeshHeader(int id, Vector3 c)
        {
            MeshID = id;
            Center = c;
        }
    }

    public class MTQuadTreeHeader
    {
        public int QuadTreeDepth = 0;
        public Vector3 BoundMin = Vector3.zero;
        public Vector3 BoundMax = Vector3.zero;
        public int LOD = 1;
        public string DataName { get; private set; }
        public Dictionary<int, MTMeshHeader> Meshes = new Dictionary<int, MTMeshHeader>();
        //runtime materials
        public Material[] RuntimeMats;
        public MTQuadTreeHeader(string name)
        {
            DataName = name;
        }
    }

    public class MTMeshData
    {
        public class LOD
        {
            public Vector3[] vertices;
            public Vector3[] normals;
            public Vector2[] uvs;
            public int[] faces;
        }
        public int meshId { get; private set; }
        public Vector3 center { get; private set; }
        public LOD[] lods;
        public MTMeshData(int id, Vector3 c)
        {
            meshId = id;
            center = c;
        }
    }

    public class MTQuadTreeNode
    {
        public Bounds Bound { get; private set; }
        public int MeshID { get; private set; }
        protected MTQuadTreeNode[] mSubNode;
        public MTQuadTreeNode(int depth, Vector3 min, Vector3 max)
        {
            Vector3 center = 0.5f * (min + max);
            Vector3 size = max - min;
            Bound = new Bounds(center, size);
            if (depth > 0)
            {
                mSubNode = new MTQuadTreeNode[4];
                Vector3 subMin = new Vector3(center.x - 0.5f * size.x, min.y, center.z - 0.5f * size.z);
                Vector3 subMax = new Vector3(center.x, max.y, center.z);
                mSubNode[0] = new MTQuadTreeNode(depth - 1, subMin, subMax);
                subMin = new Vector3(center.x, min.y, center.z - 0.5f * size.z);
                subMax = new Vector3(center.x + 0.5f * size.x, max.y, center.z);
                mSubNode[1] = new MTQuadTreeNode(depth - 1, subMin, subMax);
                subMin = new Vector3(center.x - 0.5f * size.x, min.y, center.z);
                subMax = new Vector3(center.x, max.y, center.z + 0.5f * size.z);
                mSubNode[2] = new MTQuadTreeNode(depth - 1, subMin, subMax);
                subMin = new Vector3(center.x, min.y, center.z);
                subMax = new Vector3(center.x + 0.5f * size.x, max.y, center.z + 0.5f * size.z);
                mSubNode[3] = new MTQuadTreeNode(depth - 1, subMin, subMax);
            }
        }
        public void RetrieveVisibleMesh(Plane[] planes, Vector3 viewCenter, float[] lodPolicy, MTArray<uint> visible)
        {
            if (GeometryUtility.TestPlanesAABB(planes, Bound))
            {
                if (mSubNode == null)
                {
                    float distance = Vector3.Distance(viewCenter, Bound.center);
                    for (uint lod=0; lod<lodPolicy.Length; ++lod)
                    {
                        if (distance <= lodPolicy[lod])
                        {
                            uint patchId = (uint)MeshID;
                            patchId <<= 2;
                            patchId |= lod;
                            visible.Add(patchId);
                            break;
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < 4; ++i)
                    {
                        mSubNode[i].RetrieveVisibleMesh(planes, viewCenter, lodPolicy, visible);
                    }
                }
            }
        }
        public void AddMesh(MTMeshHeader meshh)
        {
            if (mSubNode == null && Bound.Contains(meshh.Center))
            {
                MeshID = meshh.MeshID;
            }
            else if (mSubNode != null)
            {
                for (int i = 0; i < 4; ++i)
                {
                    mSubNode[i].AddMesh(meshh);
                }
            }
        }
    }
}