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
        public TessellationJob(MTTerrainScanner[] s)
        {
            scanners = s;
            mesh = new MTMeshData[scanners[0].Trees.Length];
        }
        private int curIdx = 0;
        private void RunTessellation(List<SampleVertexData> lVerts, MTMeshData.LOD lod)
        {
            if (lVerts.Count < 3)
            {
                ++curIdx;
                return;
            }
            InputGeometry geometry = new InputGeometry();
            for (int i = 0; i < lVerts.Count; i++)
            {
                geometry.AddPoint(lVerts[i].Position.x, lVerts[i].Position.z, 0);
            }
            TriangleNet.Mesh meshRepresentation = new TriangleNet.Mesh();
            meshRepresentation.Triangulate(geometry);
            int vIdx = 0;
            lod.vertices = new Vector3[meshRepresentation.Vertices.Count];
            lod.normals = new Vector3[meshRepresentation.Vertices.Count];
            lod.uvs = new Vector2[meshRepresentation.Vertices.Count];
            lod.faces = new int[meshRepresentation.triangles.Count * 3];
            foreach (var v in meshRepresentation.Vertices)
            {
                lod.vertices[vIdx] = new Vector3(v.x, lVerts[vIdx].Position.y, v.y);
                lod.normals[vIdx] = lVerts[vIdx].Normal;
                lod.uvs[vIdx] = lVerts[vIdx].UV;
                ++vIdx;
            }
            vIdx = 0;
            foreach (var t in meshRepresentation.triangles.Values)
            {
                lod.faces[vIdx] = t.P2;
                lod.faces[vIdx + 1] = t.P1;
                lod.faces[vIdx + 2] = t.P0;
                vIdx += 3;
            }
        }
        public void Update()
        {
            if (IsDone)
                return;
            mesh[curIdx] = new MTMeshData(curIdx, scanners[0].Trees[curIdx].Center);
            mesh[curIdx].lods = new MTMeshData.LOD[scanners.Length];
            for (int lod = 0; lod <scanners.Length; ++lod)
            {
                mesh[curIdx].lods[lod] = new MTMeshData.LOD();
                RunTessellation(scanners[lod].Trees[curIdx].Vertices, mesh[curIdx].lods[lod]);
            }
            //update idx
            ++curIdx;
        }
    }
}
