/*
* Copyright 2007 ZXing authors
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
*      http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/
using System;
namespace com.google.zxing
{
    /// <summary> <p>Encapsulates a point of interest in an image containing a barcode. Typically, this
    /// would be the location of a finder pattern or the corner of the barcode, for example.</p>
    ///
    /// </summary>
    /// <author>  Sean Owen
    /// </author>
    /// <author>www.Redivivus.in (suraj.supekar@redivivus.in) - Ported from ZXING Java Source
    /// </author>

    public class ResultPoint
    {
        public float X { get; }

        public float Y { get; }

        public ResultPoint(float x, float y)
        {
            X = x;
            Y = y;
        }

        public override bool Equals(object obj)
        {
            if (obj is ResultPoint otherPoint)
            {
                return X == otherPoint.X && Y == otherPoint.Y;
            }
            return false;
        }

        public override int GetHashCode()
        {
            // Redivivus.in Java to c# Porting update
            // 30/01/2010
            // Commented function body

            //return 31 * Float.floatToIntBits(x) + Float.floatToIntBits(y);
            return 0;
        }

        public override string ToString()
        {
            System.Text.StringBuilder result = new System.Text.StringBuilder(25);
            result.Append('(');
            result.Append(X);
            result.Append(',');
            result.Append(Y);
            result.Append(')');
            return result.ToString();
        }

        /// <summary> <p>Orders an array of three ResultPoints in an order [A,B,C] such that AB < AC and
        /// BC < AC and the angle between BC and BA is less than 180 degrees.
        /// </summary>
        public static void OrderBestPatterns(ResultPoint[] patterns)
        {
            // Find distances between pattern centers
            float zeroOneDistance = Distance(patterns[0], patterns[1]);
            float oneTwoDistance = Distance(patterns[1], patterns[2]);
            float zeroTwoDistance = Distance(patterns[0], patterns[2]);

            ResultPoint pointA, pointB, pointC;
            // Assume one closest to other two is B; A and C will just be guesses at first
            if (oneTwoDistance >= zeroOneDistance && oneTwoDistance >= zeroTwoDistance)
            {
                pointB = patterns[0];
                pointA = patterns[1];
                pointC = patterns[2];
            }
            else if (zeroTwoDistance >= oneTwoDistance && zeroTwoDistance >= zeroOneDistance)
            {
                pointB = patterns[1];
                pointA = patterns[0];
                pointC = patterns[2];
            }
            else
            {
                pointB = patterns[2];
                pointA = patterns[0];
                pointC = patterns[1];
            }

            // Use cross product to figure out whether A and C are correct or flipped.
            // This asks whether BC x BA has a positive z component, which is the arrangement
            // we want for A, B, C. If it's negative, then we've got it flipped around and
            // should swap A and C.
            if (CrossProductZ(pointA, pointB, pointC) < 0.0f)
            {
                ResultPoint temp = pointA;
                pointA = pointC;
                pointC = temp;
            }

            patterns[0] = pointA;
            patterns[1] = pointB;
            patterns[2] = pointC;
        }

        /// <summary> distance between two points
        /// </summary>
        public static float Distance(ResultPoint pattern1, ResultPoint pattern2)
        {
            float xDiff = pattern1.X - pattern2.X;
            float yDiff = pattern1.Y - pattern2.Y;
            //UPGRADE_WARNING: Data types in Visual C# might be different.  Verify the accuracy of narrowing conversions. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1042'"
            return (float)Math.Sqrt((xDiff * xDiff) + (yDiff * yDiff));
        }

        /// <summary> Returns the z component of the cross product between vectors BC and BA.</summary>
        private static float CrossProductZ(ResultPoint pointA, ResultPoint pointB, ResultPoint pointC)
        {
            float bX = pointB.X;
            float bY = pointB.Y;
            return ((pointC.X - bX) * (pointA.Y - bY)) - ((pointC.Y - bY) * (pointA.X - bX));
        }
    }
}