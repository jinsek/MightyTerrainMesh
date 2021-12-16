namespace MightyTerrainMesh
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public static class MTLog
    {
        public static void Log(object message)
        {
            Debug.Log(message);
        }
        public static void LogError(object message)
        {
            Debug.LogError(message);
        }
    }
    //array
    public class MTArray<T>
    {
        public T[] Data;
        public int Length { get; private set; }
        public MTArray(int len)
        {
            Reallocate(len);
        }
        public void Reallocate(int len)
        {
            if (Data != null && len < Data.Length)
                return;
            Data = new T[len];
            Length = 0;
        }
        public void Reset()
        {
            Length = 0;
        }
        public void Add(T item)
        {
            if (Data == null || Length >= Data.Length)
            {
                MTLog.LogError("MTArray overflow : " + typeof(T));
            }
            Data[Length] = item;
            ++Length;
        }
        public bool Contains(T item)
        {
            for(int i=0; i<Length; ++i)
            {
                if (Data[i].Equals(item))
                    return true;
            }
            return false;
        }
    }
}
