namespace MightyTerrainMesh
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using TriangleNet.Geometry;
    //
    public class TessellationJob
    {
        public MTMeshData[] mesh;
        public MTTerrainScanner[] scanners;
        public bool IsDone
        {
            get
            {
                return curIdx >= mesh.Length;
            }
        }
        public float progress
        {
            get
            {
                return (float)(curIdx / (float)(mesh.Length));
            }
        }
        public TessellationJob(MTTerrainScanner[] s, float minTriArea)
        {
            scanners = s;
            MinTriArea = minTriArea;
            mesh = new MTMeshData[scanners[0].Trees.Length];
        }
        public float MinTriArea { get; private set; }
        protected int curIdx = 0;
        protected void RunTessellation(List<SampleVertexData> lVerts, MTMeshData.LOD lod, float minTriArea)
        {
            if (lVerts.Count < 3)
            {
                ++curIdx;
                return;
            }
            InputGeometry geometry = new InputGeometry();
            for (int i = 0; i < lVerts.Count; i++)
            {
                var vert = lVerts[i];
                geometry.AddPoint(vert.Position.x, lVerts[i].Position.z, 0);
            }
            TriangleNet.Mesh meshRepresentation = new TriangleNet.Mesh();
            meshRepresentation.Triangulate(geometry);
            if (meshRepresentation.Vertices.Count != lVerts.Count)
            {
                Debug.LogError("trianglate seems failed");
            }
            int vIdx = 0;
            lod.vertices = new Vector3[meshRepresentation.Vertices.Count];
            lod.normals = new Vector3[meshRepresentation.Vertices.Count];
            lod.uvs = new Vector2[meshRepresentation.Vertices.Count];
            lod.faces = new int[meshRepresentation.triangles.Count * 3];
            foreach (var v in meshRepresentation.Vertices)
            {
                lod.vertices[vIdx] = new Vector3(v.x, lVerts[vIdx].Position.y, v.y);
                lod.normals[vIdx] = lVerts[vIdx].Normal;
                var uv = lVerts[vIdx].UV;
                lod.uvs[vIdx] = uv;
                ++vIdx;
            }
            vIdx = 0;
            foreach (var t in meshRepresentation.triangles.Values)
            {
                var p = new Vector2[] { new Vector2(lod.vertices[t.P0].x, lod.vertices[t.P0].z),
                    new Vector2(lod.vertices[t.P1].x, lod.vertices[t.P1].z),
                    new Vector2(lod.vertices[t.P2].x, lod.vertices[t.P2].z)};
                var triarea = UnityEngine.Mathf.Abs((p[2].x - p[0].x) * (p[1].y - p[0].y) -
                       (p[1].x - p[0].x) * (p[2].y - p[0].y)) / 2.0f;
                if (triarea < minTriArea)
                    continue;
                lod.faces[vIdx] = t.P2;
                lod.faces[vIdx + 1] = t.P1;
                lod.faces[vIdx + 2] = t.P0;
                vIdx += 3;
            }
        }
        public virtual void Update()
        {
            if (IsDone)
                return;
            mesh[curIdx] = new MTMeshData(curIdx, scanners[0].Trees[curIdx].BND);
            mesh[curIdx].lods = new MTMeshData.LOD[scanners.Length];
            for (int lod = 0; lod < scanners.Length; ++lod)
            {
                var lodData = new MTMeshData.LOD();
                var tree = scanners[lod].Trees[curIdx];
                RunTessellation(tree.Vertices, lodData, MinTriArea);
                lodData.uvmin = tree.uvMin;
                lodData.uvmax = tree.uvMax;
                mesh[curIdx].lods[lod] = lodData;
            }
            //update idx
            ++curIdx;
        }
    }

    public class TessellationDataJob : TessellationJob
    {
        List<SamplerTree> subTrees = new List<SamplerTree>();
        List<int> lodLvArr = new List<int>();
        public TessellationDataJob(MTTerrainScanner[] s, float minTriArea) : base(s, minTriArea)
        {
            int totalLen = 0;
            foreach(var scaner in scanners)
            {
                totalLen += scaner.Trees.Length;
                lodLvArr.Add(totalLen);
                subTrees.AddRange(scaner.Trees);
            }
            mesh = new MTMeshData[subTrees.Count];
        }
        private int GetLodLv(int idx)
        {
            for(int i=0; i<lodLvArr.Count; ++i)
            {
                if (idx < lodLvArr[i])
                    return i;
            }
            return 0;
        }
        public override void Update()
        {
            if (IsDone)
                return;
            var lodLv = GetLodLv(curIdx);
            mesh[curIdx] = new MTMeshData(curIdx, subTrees[curIdx].BND, lodLv);
            mesh[curIdx].lods = new MTMeshData.LOD[1];
            var lodData = new MTMeshData.LOD();
            var tree = subTrees[curIdx];
            RunTessellation(tree.Vertices, lodData, MinTriArea);
            mesh[curIdx].lods[0] = lodData;
            //update idx
            ++curIdx;
        }
    }
}
