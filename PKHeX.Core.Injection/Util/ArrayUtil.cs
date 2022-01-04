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
    }
}