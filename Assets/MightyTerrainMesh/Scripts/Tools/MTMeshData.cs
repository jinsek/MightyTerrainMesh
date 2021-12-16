namespace MightyTerrainMesh
{
    using System.Collections;
    using System.Collections.Generic;
    using System;
    using System.IO;
    using UnityEngine;

    public static class MTMeshUtils
    {
        public static void Serialize(Stream stream, MTMeshData.LOD lod)
        {
            MTFileUtils.WriteVector2(stream, lod.uvmin);
            MTFileUtils.WriteVector2(stream, lod.uvmax);
            //vertices
            byte[] uBuff = BitConverter.GetBytes(lod.vertices.Length);
            stream.Write(uBuff, 0, uBuff.Length);
            foreach (var v in lod.vertices)
                MTFileUtils.WriteVector3(stream, v);
            //normals
            uBuff = BitConverter.GetBytes(lod.normals.Length);
            stream.Write(uBuff, 0, uBuff.Length);
            foreach (var n in lod.normals)
                MTFileUtils.WriteVector3(stream, n);
            //uvs
            uBuff = BitConverter.GetBytes(lod.uvs.Length);
            stream.Write(uBuff, 0, uBuff.Length);
            foreach (var uv in lod.uvs)
                MTFileUtils.WriteVector2(stream, uv);
            //faces
            uBuff = BitConverter.GetBytes(lod.faces.Length);
            stream.Write(uBuff, 0, uBuff.Length);
            foreach (var face in lod.faces)
            {
                //强转为ushort
                ushort val = (ushort)face;
                uBuff = BitConverter.GetBytes(val);
                stream.Write(uBuff, 0, uBuff.Length);
            }
        }
        public static void Deserialize(Stream stream, MTRenderMesh rm)
        {
            rm.mesh = new Mesh();
            rm.uvmin = MTFileUtils.ReadVector2(stream);
            rm.uvmax = MTFileUtils.ReadVector2(stream);
            //vertices
            List<Vector3> vec3Cache = new List<Vector3>();
            byte[] nBuff = new byte[sizeof(int)];
            stream.Read(nBuff, 0, sizeof(int));
            int len = BitConverter.ToInt32(nBuff, 0);
            for (int i = 0; i < len; ++i)
                vec3Cache.Add(MTFileUtils.ReadVector3(stream));
            rm.mesh.SetVertices(vec3Cache.ToArray());
            //normals
            vec3Cache.Clear();
            stream.Read(nBuff, 0, sizeof(int));
            len = BitConverter.ToInt32(nBuff, 0);
            for (int i = 0; i < len; ++i)
                vec3Cache.Add(MTFileUtils.ReadVector3(stream));
            rm.mesh.SetNormals(vec3Cache.ToArray());
            //uvs
            List<Vector2> vec2Cache = new List<Vector2>();
            stream.Read(nBuff, 0, sizeof(int));
            len = BitConverter.ToInt32(nBuff, 0);
            for (int i = 0; i < len; ++i)
                vec2Cache.Add(MTFileUtils.ReadVector2(stream));
            rm.mesh.SetUVs(0, vec2Cache.ToArray());
            //faces
            List<int> intCache = new List<int>();
            stream.Read(nBuff, 0, sizeof(int));
            len = BitConverter.ToInt32(nBuff, 0);
            byte[] fBuff = new byte[sizeof(ushort)];
            for (int i = 0; i < len; ++i)
            {
                stream.Read(fBuff, 0, sizeof(ushort));
                intCache.Add(BitConverter.ToUInt16(fBuff, 0));
            }
            rm.mesh.SetTriangles(intCache.ToArray(), 0);
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
            public Vector2 uvmin;
            public Vector2 uvmax;
        }
        public int meshId { get; private set; }
        public Bounds BND { get; private set; }
        public LOD[] lods;
        public int lodLv = -1;
        public MTMeshData(int id, Bounds bnd)
        {
            meshId = id;
            BND = bnd;
        }
        public MTMeshData(int id, Bounds bnd, int lv)
        {
            meshId = id;
            BND = bnd;
            lodLv = lv;
        }
    }

}