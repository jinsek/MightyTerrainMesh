namespace MightyTerrainMesh
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using System;
    using System.IO;
    public static class MTFileUtils
    {
        public static void WriteByte(Stream stream, byte v)
        {
            byte[] sBuff = {v};
            stream.Write(sBuff, 0, sBuff.Length);
        }
        public static byte ReadByte(Stream stream)
        {
            byte[] sBuff = new byte[1];
            stream.Read(sBuff, 0, 1);
            return sBuff[0];
        }
        public static void WriteInt(Stream stream, int v)
        {
            byte[] sBuff = BitConverter.GetBytes(v);
            stream.Write(sBuff, 0, sBuff.Length);
        }
        public static void WriteIntTo(Stream stream, int v, int offset)
        {
            byte[] sBuff = BitConverter.GetBytes(v);
            stream.Write(sBuff, offset, sBuff.Length);
        }
        public static int ReadInt(Stream stream)
        {
            int v = 0;
            byte[] sBuff = new byte[sizeof(int)];
            stream.Read(sBuff, 0, sizeof(int));
            v = BitConverter.ToInt32(sBuff, 0);
            return v;
        }
        public static void WriteUShort(Stream stream, ushort v)
        {
            byte[] sBuff = BitConverter.GetBytes(v);
            stream.Write(sBuff, 0, sBuff.Length);
        }
        public static ushort ReadUShort(Stream stream)
        {
            ushort v = 0;
            byte[] sBuff = new byte[sizeof(ushort)];
            stream.Read(sBuff, 0, sizeof(ushort));
            v = BitConverter.ToUInt16(sBuff, 0);
            return v;
        }
        public static void WriteFloat(Stream stream, float v)
        {
            byte[] sBuff = BitConverter.GetBytes(v);
            stream.Write(sBuff, 0, sBuff.Length);
        }
        public static float ReadFloat(Stream stream)
        {
            float v = 0;
            byte[] sBuff = new byte[sizeof(float)];
            stream.Read(sBuff, 0, sizeof(float));
            v = BitConverter.ToSingle(sBuff, 0);
            return v;
        }
        public static void WriteVector3(Stream stream, Vector3 v)
        {
            byte[] sBuff = BitConverter.GetBytes(v.x);
            stream.Write(sBuff, 0, sBuff.Length);
            sBuff = BitConverter.GetBytes(v.y);
            stream.Write(sBuff, 0, sBuff.Length);
            sBuff = BitConverter.GetBytes(v.z);
            stream.Write(sBuff, 0, sBuff.Length);
        }
        public static Vector3 ReadVector3(Stream stream)
        {
            Vector3 v = Vector3.zero;
            byte[] sBuff = new byte[sizeof(float)];
            stream.Read(sBuff, 0, sizeof(float));
            v.x = BitConverter.ToSingle(sBuff, 0);
            stream.Read(sBuff, 0, sizeof(float));
            v.y = BitConverter.ToSingle(sBuff, 0);
            stream.Read(sBuff, 0, sizeof(float));
            v.z = BitConverter.ToSingle(sBuff, 0);
            return v;
        }
        public static void WriteVector2(Stream stream, Vector2 v)
        {
            byte[] sBuff = BitConverter.GetBytes(v.x);
            stream.Write(sBuff, 0, sBuff.Length);
            sBuff = BitConverter.GetBytes(v.y);
            stream.Write(sBuff, 0, sBuff.Length);
        }
        public static Vector2 ReadVector2(Stream stream)
        {
            Vector2 v = Vector2.zero;
            byte[] sBuff = new byte[sizeof(float)];
            stream.Read(sBuff, 0, sizeof(float));
            v.x = BitConverter.ToSingle(sBuff, 0);
            stream.Read(sBuff, 0, sizeof(float));
            v.y = BitConverter.ToSingle(sBuff, 0);
            return v;
        }
    }
}
