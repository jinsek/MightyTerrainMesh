namespace MightyTerrainMesh
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public class MTVTCreateCmd
    {
        private static Queue<MTVTCreateCmd> _qPool = new Queue<MTVTCreateCmd>();
        public static MTVTCreateCmd Pop()
        {
            if (_qPool.Count > 0)
            {
                return _qPool.Dequeue();
            }
            return new MTVTCreateCmd();
        }
        public static void Push(MTVTCreateCmd p)
        {
            p.BakeDiffuse = null;
            p.BakeNormal = null;
            p.receiver = null;
            _qPool.Enqueue(p);
        }
        public static void Clear()
        {
            _qPool.Clear();
        }
        private static long _cmd_id_seed = 0;
        public static long GenerateID()
        {
            ++_cmd_id_seed;
            return _cmd_id_seed;
        }
        protected MTVTCreateCmd() { }
        public long cmdId = 0;
        public int size = 64;
        public Material[] BakeDiffuse;
        public Material[] BakeNormal;
        public Vector2 uvMin;
        public Vector2 uvMax;
        public IMTVirtualTexutreReceiver receiver;
    }

    public interface IMTVirtualTexture
    {
        int size { get; }
        Texture Tex { get; }
    }

    public interface IVTCreator
    {
        void AppendCmd(MTVTCreateCmd cmd);
        void DisposeTextures(IMTVirtualTexture[] textures);
    }
    
    public class MTVTCreator : MonoBehaviour, IVTCreator
    {
        public enum TextureQuality
        {
            Full,
            Half,
            Quater,
        }
        public TextureQuality texQuality = TextureQuality.Full;
        public int maxBakeCountPerFrame = 8;
        private Queue<MTVTCreateCmd> qVTCreateCmds = new Queue<MTVTCreateCmd>();
        private Dictionary<int, Queue<IMTVirtualTexture[]>> texturePools = new Dictionary<int, Queue<IMTVirtualTexture[]>>();
        private List<IMTVirtualTexture> activeTextures = new List<IMTVirtualTexture>();
        private Queue<VTRenderJob> bakedJobs = new Queue<VTRenderJob>();
        private RuntimeBakeTexture[] PopTexture(int size)
        {
            var texSize = size;
            if (texQuality == TextureQuality.Half)
            {
                texSize = size >> 1;
            }
            else if (texQuality == TextureQuality.Quater)
            {
                texSize = size >> 2;
            }
            RuntimeBakeTexture[] ret = null;
            if (!texturePools.ContainsKey(texSize))
                texturePools.Add(texSize, new Queue<IMTVirtualTexture[]>());
            var q = texturePools[texSize];
            if (q.Count > 0)
            {
                ret = q.Dequeue() as RuntimeBakeTexture[];
            }
            else
            {
                ret = new RuntimeBakeTexture[] { new RuntimeBakeTexture(texSize), new RuntimeBakeTexture(texSize) };
            }
            return ret;
        }
        void IVTCreator.AppendCmd(MTVTCreateCmd cmd)
        {
            qVTCreateCmds.Enqueue(cmd);
        }
        void IVTCreator.DisposeTextures(IMTVirtualTexture[] ts)
        {
            var size = ts[0].size;
            activeTextures.Remove(ts[0]);
            activeTextures.Remove(ts[1]);
            if (texturePools.ContainsKey(size))
            {
                texturePools[size].Enqueue(ts);
            }
            else
            {
                Debug.LogWarning("DisposeTextures Invalid texture size : " + size);
            }
        }
        void OnDestroy()
        {
            foreach(var q in texturePools.Values)
            {
                while (q.Count > 0)
                {
                    var rbt = q.Dequeue() as RuntimeBakeTexture[];
                    rbt[0].Clear();
                    rbt[1].Clear();
                }
            }
            texturePools.Clear();
            MTVTCreateCmd.Clear();
        }
        // Update is called once per frame
        void Update()
        {
            while (bakedJobs.Count > 0)
            {
                var job = bakedJobs.Dequeue();
                job.SendTexturesReady();
                activeTextures.Add(job.textures[0]);
                activeTextures.Add(job.textures[1]);
                VTRenderJob.Push(job);
            }
            int bakeCount = 0;
            while (qVTCreateCmds.Count > 0 && bakeCount < maxBakeCountPerFrame)
            {
                var cmd = qVTCreateCmds.Dequeue();
                if (cmd.receiver.WaitCmdId == cmd.cmdId)
                {
                    var ts = PopTexture(cmd.size);
                    ts[0].Reset(cmd.uvMin, cmd.uvMax, cmd.BakeDiffuse);
                    ts[1].Reset(cmd.uvMin, cmd.uvMax, cmd.BakeNormal);
                    var job = VTRenderJob.Pop();
                    job.Reset(cmd.cmdId, ts, cmd.receiver);
                    job.DoJob();
                    bakedJobs.Enqueue(job);
                    MTVTCreateCmd.Push(cmd);
                    ++bakeCount;
                }
                else
                {
                    MTVTCreateCmd.Push(cmd);
                }
            }
            for(int count = activeTextures.Count - 1; count >= 0; --count)
            {
                var tex = activeTextures[count] as RuntimeBakeTexture;
                bool needRender = tex.Validate();
                if (needRender)
                {
                    tex.Bake();
                }
            }
        }
    }
}