//
// In order to convert some functionality to Visual C#, the Java Language Conversion Assistant
// creates "support classes" that duplicate the original functionality.
//
// Support classes replicate the functionality of the original code, but in some cases they are
// substantially different architecturally. Although every effort is made to preserve the
// original architecture of the application in the converted project, the user should be aware that
// the primary goal of these support classes is to replicate functionality, and that at times
// the architecture of the resulting solution may differ somewhat.
//

using System;

namespace com.google.zxing
{
    /// <summary>
    /// Contains conversion support elements such as classes, interfaces and static methods.
    /// </summary>
    public static class SupportClass
    {
        /// <summary>
        /// Converts an array of sbytes to an array of bytes
        /// </summary>
        /// <param name="sbyteArray">The array of sbytes to be converted</param>
        /// <returns>The new array of bytes</returns>
        public static byte[] ToByteArray(sbyte[] sbyteArray)
        {
            byte[] byteArray = null;

            if (sbyteArray != null)
            {
                byteArray = new byte[sbyteArray.Length];
                for (int index = 0; index < sbyteArray.Length; index++)
                    byteArray[index] = (byte)sbyteArray[index];
            }
            return byteArray;
        }

        /*******************************/
        /// <summary>
        /// Performs an unsigned bitwise right shift with the specified number
        /// </summary>
        /// <param name="number">Number to operate on</param>
        /// <param name="bits">Ammount of bits to shift</param>
        /// <returns>The resulting number from the shift operation</returns>
        public static int URShift(int number, int bits)
        {
            if (number >= 0)
                return number >> bits;
            else
                return (number >> bits) + (2 << ~bits);
        }

        /*******************************/
        /// <summary>
        /// This method returns the literal value received
        /// </summary>
        /// <param name="literal">The literal to return</param>
        /// <returns>The received value</returns>
        public static long Identity(long literal)
        {
            return literal;
        }

        /*******************************/
        /// <summary>
        /// Sets the capacity for the specified ArrayList
        /// </summary>
        /// <param name="vector">The ArrayList which capacity will be set</param>
        /// <param name="newCapacity">The new capacity value</param>
        public static void SetCapacity(System.Collections.ArrayList vector, int newCapacity)
        {
            if (newCapacity > vector.Count)
                vector.AddRange(new Array[newCapacity - vector.Count]);
            else if (newCapacity < vector.Count)
                vector.RemoveRange(newCapacity, vector.Count - newCapacity);
            vector.Capacity = newCapacity;
        }
    }
}