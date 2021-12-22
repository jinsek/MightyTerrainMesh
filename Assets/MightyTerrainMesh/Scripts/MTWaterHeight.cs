namespace MightyTerrainMesh
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    
    public interface IMTWaterHeightProvider
    {
        bool Contains(Vector3 worldPos);
        float GetHeight(Vector3 worldPos);
    }

    public class MTWaterHeight
    {
        private static List<IMTWaterHeightProvider> providers = new List<IMTWaterHeightProvider>();
        public static void RegProvider(IMTWaterHeightProvider provider)
        {
            providers.Add(provider);
        }
        public static void UnregProvider(IMTWaterHeightProvider provider)
        {
            providers.Remove(provider);
        }
        public static float GetWaterHeight(Vector3 groundWorldPos)
        {
            float h = groundWorldPos.y;
            for(int i=0; i<providers.Count; ++i)
            {
                var water = providers[i];
                if (water.Contains(groundWorldPos))
                {
                    float wh = water.GetHeight(groundWorldPos);
                    if (wh > h)
                        return wh;
                }
            }
            return h;
        }
    }
}
