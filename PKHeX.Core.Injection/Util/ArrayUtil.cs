using System;
using System.Runtime.InteropServices;

namespace PKHeX.Core.Injection
{
    /// <summary>
    /// Array reusable logic
    /// </summary>
    public static class ArrayUtil
    {
        internal static T[] ConcatAll<T>(params T[][] arr)
        {
            int len = 0;
            foreach (var a in arr)
                len += a.Length;

            var result = new T[len];

            int ctr = 0;
            foreach (var a in arr)
            {
                a.CopyTo(result, ctr);
                ctr += a.Length;
            }

            return result;
        }

        public static T[][] Split<T>(this ReadOnlySpan<T> data, int size)
        {
            var result = new T[data.Length / size][];
            for (int i = 0; i < data.Length; i += size)
                result[i / size] = data.Slice(i, size).ToArray();
            return result;
        }

        internal static T[] ConcatAll<T>(T[] arr1, T[] arr2)
        {
            int len = arr1.Length + arr2.Length;
            var result = new T[len];
            arr1.CopyTo(result, 0);
            arr2.CopyTo(result, arr1.Length);
            return result;
        }

        internal static T[] ConcatAll<T>(T[] arr1, T[] arr2, T[] arr3)
        {
            int len = arr1.Length + arr2.Length + arr3.Length;
            var result = new T[len];
            arr1.CopyTo(result, 0);
            arr2.CopyTo(result, arr1.Length);
            arr3.CopyTo(result, arr1.Length + arr2.Length);
            return result;
        }

        internal static T? ToClass<T>(this byte[] bytes)
            where T : class
        {
            var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            try
            {
                return Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T)) as T;
            }
            finally
            {
                handle.Free();
            }
        }

        internal static byte[] ToBytesClass<T>(this T obj)
            where T : class
        {
            int size = Marshal.SizeOf(obj);
            byte[] arr = new byte[size];

            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(obj, ptr, true);
            Marshal.Copy(ptr, arr, 0, size);
            Marshal.FreeHGlobal(ptr);
            return arr;
        }
    }
}
