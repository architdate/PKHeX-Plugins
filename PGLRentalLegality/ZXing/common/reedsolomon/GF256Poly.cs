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
namespace com.google.zxing.common.reedsolomon
{
    /// <summary> <para>
    /// <p>Represents a polynomial whose coefficients are elements of GF(256).
    /// Instances of this class are immutable.</p>
    /// </para>
    /// <para>
    /// <p>Much credit is due to William Rucklidge since portions of this code are an indirect
    /// port of his C++ Reed-Solomon implementation.</p>
    /// </para>
    ///
    /// </summary>
    /// <author>  Sean Owen
    /// </author>
    /// <author>www.Redivivus.in (suraj.supekar@redivivus.in) - Ported from ZXING Java Source
    /// </author>
    internal sealed class GF256Poly
    {
        internal int[] Coefficients { get; }

        /// <summary> degree of this polynomial
        /// </summary>
        internal int Degree => Coefficients.Length - 1;

        /// <summary> true iff this polynomial is the monomial "0"
        /// </summary>
        internal bool Zero => Coefficients[0] == 0;

        //UPGRADE_NOTE: Final was removed from the declaration of 'field '. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1003'"
        private readonly GF256 field;
        //UPGRADE_NOTE: Final was removed from the declaration of 'coefficients '. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1003'"

        /// <param name="field">the {@link GF256} instance representing the field to use
        /// to perform computations
        /// </param>
        /// <param name="coefficients">coefficients as ints representing elements of GF(256), arranged
        /// from most significant (highest-power term) coefficient to least significant
        /// </param>
        /// <throws>  IllegalArgumentException if argument is null or empty, </throws>
        /// <summary> or if leading coefficient is 0 and this is not a
        /// constant polynomial (that is, it is not the monomial "0")
        /// </summary>
        internal GF256Poly(GF256 field, int[] coefficients)
        {
            if (coefficients == null || coefficients.Length == 0)
            {
                throw new ArgumentException();
            }
            this.field = field;
            int coefficientsLength = coefficients.Length;
            if (coefficientsLength > 1 && coefficients[0] == 0)
            {
                // Leading term must be non-zero for anything except the constant polynomial "0"
                int firstNonZero = 1;
                while (firstNonZero < coefficientsLength && coefficients[firstNonZero] == 0)
                {
                    firstNonZero++;
                }
                if (firstNonZero == coefficientsLength)
                {
                    Coefficients = field.Zero.Coefficients;
                }
                else
                {
                    Coefficients = new int[coefficientsLength - firstNonZero];
                    Array.Copy(coefficients, firstNonZero, Coefficients, 0, Coefficients.Length);
                }
            }
            else
            {
                Coefficients = coefficients;
            }
        }

        /// <summary> coefficient of x^degree term in this polynomial
        /// </summary>
        internal int GetCoefficient(int degree)
        {
            return Coefficients[Coefficients.Length - 1 - degree];
        }

        /// <summary> evaluation of this polynomial at a given point
        /// </summary>
        internal int EvaluateAt(int a)
        {
            if (a == 0)
            {
                // Just return the x^0 coefficient
                return GetCoefficient(0);
            }
            int size = Coefficients.Length;
            if (a == 1)
            {
                // Just the sum of the coefficients
                int result = 0;
                for (int i = 0; i < size; i++)
                {
                    result = GF256.AddOrSubtract(result, Coefficients[i]);
                }
                return result;
            }
            int result2 = Coefficients[0];
            for (int i = 1; i < size; i++)
            {
                result2 = GF256.AddOrSubtract(field.Multiply(a, result2), Coefficients[i]);
            }
            return result2;
        }

        internal GF256Poly AddOrSubtract(GF256Poly other)
        {
            if (!field.Equals(other.field))
            {
                throw new ArgumentException("GF256Polys do not have same GF256 field");
            }
            if (Zero)
            {
                return other;
            }
            if (other.Zero)
            {
                return this;
            }

            int[] smallerCoefficients = Coefficients;
            int[] largerCoefficients = other.Coefficients;
            if (smallerCoefficients.Length > largerCoefficients.Length)
            {
                int[] temp = smallerCoefficients;
                smallerCoefficients = largerCoefficients;
                largerCoefficients = temp;
            }
            int[] sumDiff = new int[largerCoefficients.Length];
            int lengthDiff = largerCoefficients.Length - smallerCoefficients.Length;
            // Copy high-order terms only found in higher-degree polynomial's coefficients
            Array.Copy(largerCoefficients, 0, sumDiff, 0, lengthDiff);

            for (int i = lengthDiff; i < largerCoefficients.Length; i++)
            {
                sumDiff[i] = GF256.AddOrSubtract(smallerCoefficients[i - lengthDiff], largerCoefficients[i]);
            }

            return new GF256Poly(field, sumDiff);
        }

        internal GF256Poly Multiply(GF256Poly other)
        {
            if (!field.Equals(other.field))
            {
                throw new ArgumentException("GF256Polys do not have same GF256 field");
            }
            if (Zero || other.Zero)
            {
                return field.Zero;
            }
            int[] aCoefficients = Coefficients;
            int aLength = aCoefficients.Length;
            int[] bCoefficients = other.Coefficients;
            int bLength = bCoefficients.Length;
            int[] product = new int[aLength + bLength - 1];
            for (int i = 0; i < aLength; i++)
            {
                int aCoeff = aCoefficients[i];
                for (int j = 0; j < bLength; j++)
                {
                    product[i + j] = GF256.AddOrSubtract(product[i + j], field.Multiply(aCoeff, bCoefficients[j]));
                }
            }
            return new GF256Poly(field, product);
        }

        internal GF256Poly Multiply(int scalar)
        {
            if (scalar == 0)
                return field.Zero;
            if (scalar == 1)
                return this;

            int size = Coefficients.Length;
            int[] product = new int[size];
            for (int i = 0; i < size; i++)
            {
                product[i] = field.Multiply(Coefficients[i], scalar);
            }
            return new GF256Poly(field, product);
        }

        internal GF256Poly MultiplyByMonomial(int degree, int coefficient)
        {
            if (degree < 0)
                throw new ArgumentException();
            if (coefficient == 0)
                return field.Zero;

            int size = Coefficients.Length;
            int[] product = new int[size + degree];
            for (int i = 0; i < size; i++)
            {
                product[i] = field.Multiply(Coefficients[i], coefficient);
            }
            return new GF256Poly(field, product);
        }

        internal GF256Poly[] Divide(GF256Poly other)
        {
            if (!field.Equals(other.field))
                throw new ArgumentException("GF256Polys do not have same GF256 field");
            if (other.Zero)
                throw new ArgumentException("Divide by 0");

            GF256Poly quotient = field.Zero;
            GF256Poly remainder = this;

            int denominatorLeadingTerm = other.GetCoefficient(other.Degree);
            int inverseDenominatorLeadingTerm = field.Inverse(denominatorLeadingTerm);

            while (remainder.Degree >= other.Degree && !remainder.Zero)
            {
                int degreeDifference = remainder.Degree - other.Degree;
                int scale = field.Multiply(remainder.GetCoefficient(remainder.Degree), inverseDenominatorLeadingTerm);
                GF256Poly term = other.MultiplyByMonomial(degreeDifference, scale);
                GF256Poly iterationQuotient = field.BuildMonomial(degreeDifference, scale);
                quotient = quotient.AddOrSubtract(iterationQuotient);
                remainder = remainder.AddOrSubtract(term);
            }

            return new[]{quotient, remainder};
        }

        public override string ToString()
        {
            System.Text.StringBuilder result = new System.Text.StringBuilder(8 * Degree);
            for (int degree = Degree; degree >= 0; degree--)
            {
                int coefficient = GetCoefficient(degree);
                if (coefficient != 0)
                {
                    if (coefficient < 0)
                    {
                        result.Append(" - ");
                        coefficient = - coefficient;
                    }
                    else
                    {
                        if (result.Length > 0)
                        {
                            result.Append(" + ");
                        }
                    }
                    if (degree == 0 || coefficient != 1)
                    {
                        int alphaPower = field.Log(coefficient);
                        if (alphaPower == 0)
                        {
                            result.Append('1');
                        }
                        else if (alphaPower == 1)
                        {
                            result.Append('a');
                        }
                        else
                        {
                            result.Append("a^");
                            result.Append(alphaPower);
                        }
                    }
                    if (degree != 0)
                    {
                        if (degree == 1)
                        {
                            result.Append('x');
                        }
                        else
                        {
                            result.Append("x^");
                            result.Append(degree);
                        }
                    }
                }
            }
            return result.ToString();
        }
    }
}