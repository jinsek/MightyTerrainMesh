namespace MightyTerrainMesh
{
    using System;
    using System.IO;
    using System.Collections.Generic;
    using UnityEngine;

    public interface IMTVirtualTexutreReceiver
    {
        long WaitCmdId { get; }
        void OnTextureReady(long cmdId, IMTVirtualTexture[] textures);
    }

    public class MTPooledRenderMesh : IMTVirtualTexutreReceiver
    {
        private static Queue<MTPooledRenderMesh> _qPool = new Queue<MTPooledRenderMesh>();
        public static MTPooledRenderMesh Pop()
        {
            if (_qPool.Count > 0)
            {
                return _qPool.Dequeue();
            }
            return new MTPooledRenderMesh();
        }
        public static void Push(MTPooledRenderMesh p)
        {
            p.OnPushBackPool();
            _qPool.Enqueue(p);
        }
        public static void Clear()
        {
            while (_qPool.Count > 0)
            {
                _qPool.Dequeue().DestroySelf();
            }
        }
        private MTData mDataHeader;
        private MTRenderMesh mRM;
        private GameObject mGo;
        private MeshFilter mMesh;
        private MeshRenderer mRenderer;
        private Material[] mMats;
        private IVTCreator mVTCreator;
        private float mDiameter = 0;
        private Vector3 mCenter = Vector3.zero;
        private int mTextureSize = -1;
        //this is the texture for rendering, till the baking texture ready, this can be push back to pool
        private IMTVirtualTexture[] mTextures;
        //baking parameters
        private long waitBackCmdId = 0;
        private MTVTCreateCmd lastPendingCreateCmd;
        //
        public MTPooledRenderMesh()
        {
            mGo = new GameObject("_mtpatch");
            mMesh = mGo.AddComponent<MeshFilter>();
            mRenderer = mGo.AddComponent<MeshRenderer>();
            mRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        public void Reset(MTData header, IVTCreator vtCreator, MTRenderMesh m, Vector3 offset)
        {
            mDataHeader = header;
            mVTCreator = vtCreator;
            mRM = m;
            mGo.SetActive(true);
            mGo.transform.position = offset;
            //mesh and material
            mMesh.mesh = mRM.mesh;
            if (mMats == null)
            {
                mMats = new Material[1];
                mMats[0] = GameObject.Instantiate(mDataHeader.BakedMat);
            }
            ClearRendererMaterial();
            mRenderer.materials = mDataHeader.DetailMats;
            //
            mDiameter = mRM.mesh.bounds.size.magnitude;
            mCenter = mRM.mesh.bounds.center + offset;
            mTextureSize = -1;
            waitBackCmdId = 0;
        }
        private void ClearRendererMaterial()
        {
            if (mRenderer != null && mRenderer.materials != null)
            {
                for(int i=0; i<mRenderer.materials.Length; ++i)
                {
                    var mat = mRenderer.materials[i];
                    GameObject.Destroy(mat);
                }
            }
        }
        public void OnPushBackPool()
        {
            mMats[0].SetTexture("_Diffuse", null);
            mMats[0].SetTexture("_Normal", null);
            if (mGo != null)
                mGo.SetActive(false);
            if (mTextures != null)
            {
                mVTCreator.DisposeTextures(mTextures);
                mTextures = null;
            }
            waitBackCmdId = 0;
            if (lastPendingCreateCmd != null)
            {
                MTVTCreateCmd.Push(lastPendingCreateCmd);
                lastPendingCreateCmd = null;
            }
            mTextureSize = -1;
            mRM = null;
        }
        private int CalculateTextureSize(Vector3 viewCenter, float fov, float screenH)
        {
            float distance = Vector3.Distance(viewCenter, mCenter);
            float pixelSize = (mDiameter * Mathf.Rad2Deg * screenH) / (distance * fov);
            return Mathf.NextPowerOfTwo(Mathf.FloorToInt(pixelSize));
        }
        private void RequestTexture(int size)
        {
            size = Mathf.Clamp(size, 128, 2048);
            //use size to fixed the render texture format, otherwise the texture will always receate
            if (size != mTextureSize)
            {
                mTextureSize = size;
                var cmd = MTVTCreateCmd.Pop();
                cmd.cmdId = MTVTCreateCmd.GenerateID();
                cmd.size = size;
                cmd.uvMin = mRM.uvmin;
                cmd.uvMax = mRM.uvmax;
                cmd.BakeDiffuse = mDataHeader.BakeDiffuseMats;
                cmd.BakeNormal = mDataHeader.BakeNormalMats;
                cmd.receiver = this;
                if (waitBackCmdId > 0)
                {
                    if (lastPendingCreateCmd != null)
                    {
                        MTVTCreateCmd.Push(lastPendingCreateCmd);
                    }
                    lastPendingCreateCmd = cmd;
                }
                else
                {
                    waitBackCmdId = cmd.cmdId;
                    mVTCreator.AppendCmd(cmd);
                }
            }
        }
        private void ApplyTextures()
        {
            Vector2 size = mRM.uvmax - mRM.uvmin;
            var scale = new Vector2(1f / size.x, 1f / size.y);
            var offset = -new Vector2(scale.x * mRM.uvmin.x, scale.y * mRM.uvmin.y);
            mMats[0].SetTexture("_Diffuse", mTextures[0].Tex);
            mMats[0].SetTextureScale("_Diffuse", scale);
            mMats[0].SetTextureOffset("_Diffuse", offset);
            mMats[0].SetTexture("_Normal", mTextures[1].Tex);
            mMats[0].SetTextureScale("_Normal", scale);
            mMats[0].SetTextureOffset("_Normal", offset);
        }
        long IMTVirtualTexutreReceiver.WaitCmdId { get { return waitBackCmdId; } }
        void IMTVirtualTexutreReceiver.OnTextureReady(long cmdId, IMTVirtualTexture[] textures)
        {
            if (mRM == null || cmdId != waitBackCmdId)
            {
                mVTCreator.DisposeTextures(textures);
                return;
            }
            if (mTextures != null)
            {
                mVTCreator.DisposeTextures(mTextures);
                mTextures = null;
            }
            mTextures = textures;
            ApplyTextures();
            ClearRendererMaterial();
            mRenderer.materials = mMats;
            waitBackCmdId = 0;
            if (lastPendingCreateCmd != null)
            {
                waitBackCmdId = lastPendingCreateCmd.cmdId;
                mVTCreator.AppendCmd(lastPendingCreateCmd);
                lastPendingCreateCmd = null;
            }
        }
        private void DestroySelf()
        {
            ClearRendererMaterial();
            if (mMats != null)
            {
                foreach(var m in mMats)
                    GameObject.Destroy(m);
            }
            mMats = null;
            if (mGo != null)
                MonoBehaviour.Destroy(mGo);
            mGo = null;
            mMesh = null;
        }
        public void UpdatePatch(Vector3 viewCenter, float fov, float screenH, float screenW)
        {
            int curTexSize = CalculateTextureSize(viewCenter, fov, screenH);
            if (curTexSize != mTextureSize)
            {
                RequestTexture(curTexSize);
            }
        }
    }
    public class MTRenderMesh
    {
        public Mesh mesh;
        public Vector2 uvmin;
        public Vector2 uvmax;
        public void Clear()
        {
            MonoBehaviour.Destroy(mesh);
            mesh = null;
        }
    }
}
