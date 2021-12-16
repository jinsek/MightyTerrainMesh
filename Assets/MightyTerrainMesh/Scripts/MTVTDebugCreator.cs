namespace MightyTerrainMesh
{
    using System.Collections;
    using System.Collections.Generic;
    using System;
    using System.IO;
    using UnityEngine;

    public class MTDebugTexture : IMTVirtualTexture
    {
        int IMTVirtualTexture.size { get { return mSize; } }
        Texture IMTVirtualTexture.Tex { get { return tex; } }
        private int mSize = 32;
        private Texture2D tex;
        public MTDebugTexture(int n, Texture2D t)
        {
            mSize = n;
            tex = t;
        }
    }

    public class MTVTDebugCreator : MonoBehaviour, IVTCreator
    {
        public Texture2D tex64;
        public Texture2D tex128;
        public Texture2D tex256;
        public Texture2D tex512;
        public Texture2D tex1024;
        public Texture2D tex2048;
        private Queue<MTVTCreateCmd> cmds = new Queue<MTVTCreateCmd>();
        void IVTCreator.AppendCmd(MTVTCreateCmd cmd)
        {
            cmds.Enqueue(cmd);
        }
        void IVTCreator.DisposeTextures(IMTVirtualTexture[] textures) { }
        private void Update()
        {
            while (cmds.Count > 0)
            {
                var cmd = cmds.Dequeue();
                switch (cmd.size)
                {
                    case 64:
                        cmd.receiver.OnTextureReady(cmd.cmdId, new MTDebugTexture[] { new MTDebugTexture(64, tex64) });
                        break;
                    case 128:
                        cmd.receiver.OnTextureReady(cmd.cmdId, new MTDebugTexture[] { new MTDebugTexture(128, tex128) });
                        break;
                    case 256:
                        cmd.receiver.OnTextureReady(cmd.cmdId, new MTDebugTexture[] { new MTDebugTexture(256, tex256)});
                        break;
                    case 512:
                        cmd.receiver.OnTextureReady(cmd.cmdId, new MTDebugTexture[] { new MTDebugTexture(512, tex512) });
                        break;
                    case 1024:
                        cmd.receiver.OnTextureReady(cmd.cmdId, new MTDebugTexture[] { new MTDebugTexture(1024, tex1024) });
                        break;
                    case 2048:
                        cmd.receiver.OnTextureReady(cmd.cmdId, new MTDebugTexture[] { new MTDebugTexture(2048, tex2048) });
                        break;
                    default:
                        if (cmd.size > 2048)
                        {
                            cmd.receiver.OnTextureReady(cmd.cmdId, new MTDebugTexture[] { new MTDebugTexture(2048, tex2048) });
                        }
                        else if (cmd.size < 64)
                        {
                            cmd.receiver.OnTextureReady(cmd.cmdId, new MTDebugTexture[] { new MTDebugTexture(64, tex64) });
                        }
                        break;
                }
                MTVTCreateCmd.Push(cmd);
            }
        }
    }
}
