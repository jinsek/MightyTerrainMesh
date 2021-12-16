namespace MightyTerrainMesh
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    
    public class MTHeightMap
    {
        //static interface
        private static Dictionary<uint, MTHeightMap> _dictMaps = new Dictionary<uint, MTHeightMap>();
        private static int _mapWidth = 512;
        private static int _mapHeight = 512;
        private static float _half_range = 0;
        public static uint FormatId(Vector3 pos)
        {
            //transform to (0 ~ short.MaxValue * _mapWidth)
            int x = Mathf.CeilToInt(pos.x + _half_range) / _mapWidth;
            int y = Mathf.CeilToInt(pos.z + _half_range) / _mapHeight;
            uint id = (uint)x;
            id = (id << 16) | (uint)y;
            return id;
        }
        public static void RegisterMap(MTHeightMap map)
        {
            var width = Mathf.FloorToInt(map.BND.size.x);
            var height = Mathf.FloorToInt(map.BND.size.z);
            if (_dictMaps.Count == 0)
            {
                _mapWidth = width;
                _mapHeight = height;
                _half_range = Mathf.Max(_mapWidth, _mapHeight) * short.MaxValue;
            }
            if (_mapWidth != width || _mapHeight != height)
            {
                Debug.LogError(string.Format("height map size is not valid : {0}, {1}", width, height));
                return;
            }
            uint id = FormatId(map.BND.min);
            //Debug.Log(map.BND.min + ", " + id);
            if (_dictMaps.ContainsKey(id))
            {
                Debug.LogError(string.Format("height map id overlapped : {0}, {1}", map.BND.min.x, map.BND.min.z));
                return;
            }
            _dictMaps.Add(id, map);
        }
        public static void UnregisterMap(MTHeightMap map)
        {
            uint id = FormatId(map.BND.min);
            if (!_dictMaps.ContainsKey(id))
            {
                Debug.LogError(string.Format("height map not exist : {0}, {1}", map.BND.center.x, map.BND.center.z));
                return;
            }
            _dictMaps.Remove(id);
        }
        public static bool GetHeightInterpolated(Vector3 pos, ref float h)
        {
            uint id = FormatId(pos);
            if (_dictMaps.ContainsKey(id))
            {
                return _dictMaps[id].GetInterpolatedHeight(pos, ref h);
            }
            return false;
        }
        public static bool GetHeightSimple(Vector3 pos, ref float h)
        {
            uint id = FormatId(pos);
            if (_dictMaps.ContainsKey(id))
            {
                return _dictMaps[id].GetHeight(pos, ref h);
            }
            return false;
        }
        public Bounds BND { get; private set; }
        private int heightResolusion = 513;
        private byte[] heights;
        private Vector3 heightScale;
        public MTHeightMap(Bounds bnd, int resolution, Vector3 scale, byte[] data)
        {
            BND = bnd;
            heightResolusion = resolution;
            heightScale = scale;
            heights = data;
            RegisterMap(this);
        }
        private float SampleHeightMapData(int x, int y)
        {
            int idx = y * heightResolusion * 2 + x * 2;
            byte h = heights[idx];
            byte l = heights[idx + 1];
            return h + l / 255f;
        }
        private float GetInterpolatedHeightVal(Vector3 pos)
        {
            var local_x = Mathf.Clamp01((pos.x - BND.min.x) / BND.size.x) * (heightResolusion - 1);
            var local_y = Mathf.Clamp01((pos.z - BND.min.z) / BND.size.z) * (heightResolusion - 1);
            int x = Mathf.FloorToInt(local_x);
            int y = Mathf.FloorToInt(local_y);
            float tx = local_x - x;
            float ty = local_y - y;
            float y00 = SampleHeightMapData(x, y);
            float y10 = SampleHeightMapData(x + 1, y);
            float y01 = SampleHeightMapData(x, y + 1);
            float y11 = SampleHeightMapData(x + 1, y + 1);
            return Mathf.Lerp(Mathf.Lerp(y00, y10, tx), Mathf.Lerp(y01, y11, tx), ty);
        }
        public bool GetInterpolatedHeight(Vector3 pos, ref float h)
        {
            var checkPos = pos;
            checkPos.y = BND.center.y;
            if (!BND.Contains(checkPos))
                return false;
            float val = GetInterpolatedHeightVal(pos);
            h = val * heightScale.y / 255f + BND.min.y;
            return true;
        }
        public bool GetHeight(Vector3 pos, ref float h)
        {
            var local_x = pos.x - BND.min.x;
            var local_y = pos.z - BND.min.z;
            int x = Mathf.FloorToInt(local_x);
            int y = Mathf.FloorToInt(local_y);
            if (x >= 0 && x < heightResolusion && y >= 0 && y < heightResolusion)
            {
                float val = SampleHeightMapData(x, y) * heightScale.y / 255f;
                h = val + BND.min.y;
                return true;
            }
            return false;
        }
    }
}
