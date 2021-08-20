using System.Collections.Generic;

namespace PKHeX.Core.AutoMod
{
    // Thanks to https://stackoverflow.com/questions/14663168/an-integer-array-as-a-key-for-dictionary
    public class ArrayEqualityComparator : IEqualityComparer<int[]>
    {
        public bool Equals(int[] x, int[] y)
        {
            if (x.Length != y.Length)
            {
                return false;
            }
            for (int i = 0; i < x.Length; i++)
            {
                if (x[i] != y[i])
                {
                    return false;
                }
            }
            return true;
        }

        public int GetHashCode(int[] obj)
        {
            int hash = 17;
            foreach (var r in obj)
                hash = (hash * 23) + r;
            return hash;
        }
    }
}
