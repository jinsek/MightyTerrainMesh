namespace MightyTerrainMesh
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Rendering;

    public class RuntimeBakeTexture : IMTVirtualTexture
    {
        static Mesh s_FullscreenMesh = null;

        /// <summary>
        /// Returns a mesh that you can use with <see cref="CommandBuffer.DrawMesh(Mesh, Matrix4x4, Material)"/> to render full-screen effects.
        /// </summary>
        public static Mesh fullscreenMesh
        {
            get
            {
                if (s_FullscreenMesh != null)
                    return s_FullscreenMesh;

                float topV = 1.0f;
                float bottomV = 0.0f;

                s_FullscreenMesh = new Mesh { name = "Fullscreen Quad" };
                s_FullscreenMesh.SetVertices(new List<Vector3>
                {
                    new Vector3(-1.0f, -1.0f, 0.0f),
                    new Vector3(-1.0f,  1.0f, 0.0f),
                    new Vector3(1.0f, -1.0f, 0.0f),
                    new Vector3(1.0f,  1.0f, 0.0f)
                });

                s_FullscreenMesh.SetUVs(0, new List<Vector2>
                {
                    new Vector2(0.0f, bottomV),
                    new Vector2(0.0f, topV),
                    new Vector2(1.0f, bottomV),
                    new Vector2(1.0f, topV)
                });

                s_FullscreenMesh.SetIndices(new[] { 0, 1, 2, 2, 1, 3 }, MeshTopology.Triangles, 0, false);
                s_FullscreenMesh.UploadMeshData(true);
                return s_FullscreenMesh;
            }
        }
        private static int _rtt_count = 0;
        int IMTVirtualTexture.size { get { return texSize; } }
        Texture IMTVirtualTexture.Tex { get { return RTT; } }
        public Material[] layers { get; private set; }
        public RenderTexture RTT { get; private set; }
        private int texSize = 32;
        private Vector4 scaleOffset;
        private CommandBuffer cmdBuffer;
        public RuntimeBakeTexture(int size)
        {
            texSize = size;
            scaleOffset = new Vector4(1, 1, 0, 0);
            cmdBuffer = new CommandBuffer();
            cmdBuffer.name = "RuntimeBakeTexture";
            CreateRTT();
        }
        private void CreateRTT()
        {
            var format = RenderTextureFormat.Default;
            RTT = new RenderTexture(texSize, texSize, 0, format, RenderTextureReadWrite.Default);
            RTT.wrapMode = TextureWrapMode.Clamp;
            RTT.Create();
            RTT.DiscardContents();
            ++_rtt_count;
            //Debug.Log("rtt count : " + _rtt_count);
        }
        public void Reset(Vector2 uvMin, Vector2 uvMax, Material[] mats)
        {
            scaleOffset.x = uvMax.x - uvMin.x;
            scaleOffset.y = uvMax.y - uvMin.y;
            scaleOffset.z = uvMin.x;
            scaleOffset.w = uvMin.y;
            layers = mats;
            Validate();
        }
        public void Bake()
        {
            for (int i = 0; i < layers.Length; ++i)
            {
                layers[i].SetVector("_BakeScaleOffset", scaleOffset);
            }
            RTT.DiscardContents();
            cmdBuffer.Clear();
            cmdBuffer.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
            cmdBuffer.SetViewport(new Rect(0, 0, RTT.width, RTT.height));
            cmdBuffer.SetRenderTarget(RTT, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare);
            for (int i = 0; i < layers.Length; ++i)
            {
                cmdBuffer.DrawMesh(fullscreenMesh, Matrix4x4.identity, layers[i]);
            }
            Graphics.ExecuteCommandBuffer(cmdBuffer);
        }
        public bool Validate()
        {
            if (!RTT.IsCreated())
            {
                RTT.Release();
                CreateRTT();
                return true;
            }
            return false;
        }
        public void Clear()
        {
            if (RTT != null)
            {
                RTT.Release();
                RTT = null;
            }
            layers = null;
            cmdBuffer.Clear();
            cmdBuffer = null;
        }
    }

    public class VTRenderJob
    {
        private static Queue<VTRenderJob> _qPool = new Queue<VTRenderJob>();
        public static VTRenderJob Pop()
        {
            if (_qPool.Count > 0)
            {
                return _qPool.Dequeue();
            }
            return new VTRenderJob();
        }
        public static void Push(VTRenderJob p)
        {
            p.textures = null;
            p.receiver = null;
            _qPool.Enqueue(p);
        }
        public static void Clear()
        {
            _qPool.Clear();
        }
        public RuntimeBakeTexture[] textures;
        private IMTVirtualTexutreReceiver receiver;
        private long cmdId = 0;
        public void Reset(long cmd, RuntimeBakeTexture[] ts, IMTVirtualTexutreReceiver r)
        {
            cmdId = cmd;
            textures = ts;
            receiver = r;
        }
        public void DoJob()
        {
            for (int i=0; i<textures.Length; ++i)
            {
                var tex = textures[i];
                tex.Bake();
            }
        }
        public void SendTexturesReady()
        {
            receiver.OnTextureReady(cmdId, textures);
            receiver = null;
        }
    }
}