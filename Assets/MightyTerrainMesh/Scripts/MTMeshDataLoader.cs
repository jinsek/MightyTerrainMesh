namespace MightyTerrainMesh
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public interface IMeshDataLoader
    {
        byte[] LoadMeshData(string path);
        void UnloadAsset(string path);
    }

    public class MeshDataResLoader : IMeshDataLoader
    {
        public byte[] LoadMeshData(string path)
        {
            var res_path = string.Format("MeshData/{0}", path);
            var asset = Resources.Load(res_path) as TextAsset;
            return asset.bytes;
        }
        public void UnloadAsset(string path)
        { }
    }
}