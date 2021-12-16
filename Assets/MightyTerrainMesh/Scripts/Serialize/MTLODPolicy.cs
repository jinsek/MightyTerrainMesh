namespace MightyTerrainMesh
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public class MTLODPolicy : ScriptableObject
    {
        public float[] ScreenCover;
        public int GetLODLevel(float screenSize, float screenW)
        {
            if (ScreenCover != null)
            {
                float rate = screenSize / screenW;
                for (int lod = 0; lod < ScreenCover.Length; ++lod)
                {
                    if (rate >= ScreenCover[lod])
                        return lod;
                }
            }
            return 0;
        }
    }
}
