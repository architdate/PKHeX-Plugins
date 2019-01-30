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
namespace com.google.zxing.common.reedsolomon
{
    /// <summary> <para><p>Implements Reed-Solomon decoding, as the name implies.</p></para>
    /// <para>
    /// <p>The algorithm will not be explained here, but the following references were helpful
    /// in creating this implementation:</p>
    /// </para>
    /// <para>
    /// <ul>
    /// <li>Bruce Maggs.
    /// <a href="http://www.cs.cmu.edu/afs/cs.cmu.edu/project/pscico-guyb/realworld/www/rs_decode.ps">
    /// "Decoding Reed-Solomon Codes"</a> (see discussion of Forney's Formula)</li>
    /// <li>J.I. Hall. <a href="www.mth.msu.edu/~jhall/classes/codenotes/GRS.pdf">
    /// "Chapter 5. Generalized Reed-Solomon Codes"</a>
    /// (see discussion of Euclidean algorithm)</li>
    /// </ul>
    /// </para>
    /// <para>
    /// <p>Much credit is due to William Rucklidge since portions of this code are an indirect
    /// port of his C++ Reed-Solomon implementation.</p>
    /// </para>
    ///
    /// </summary>
    /// <author>  Sean Owen
    /// </author>
    /// <author>  William Rucklidge
    /// </author>
    /// <author>  sanfordsquires
    /// </author>
    /// <author>www.Redivivus.in (suraj.supekar@redivivus.in) - Ported from ZXING Java Source
    /// </author>
    public sealed class ReedSolomonDecoder
    {
        //UPGRADE_NOTE: Final was removed from the declaration of 'field '. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1003'"
        private readonly GF256 field;

        public ReedSolomonDecoder(GF256 field)
        {
            this.field = field;
        }

        /// <summary> <p>Decodes given set of received codewords, which include both data and error-correction
        /// codewords. Really, this means it uses Reed-Solomon to detect and correct errors, in-place,
        /// in the input.</p>
        ///
        /// </summary>
        /// <param name="received">data and error-correction codewords
        /// </param>
        /// <param name="twoS">number of error-correction codewords available
        /// </param>
        /// <throws>  ReedSolomonException if decoding fails for any reason </throws>
        public void Decode(int[] received, int twoS)
        {
            GF256Poly poly = new GF256Poly(field, received);
            int[] syndromeCoefficients = new int[twoS];
            bool dataMatrix = field.Equals(GF256.DATA_MATRIX_FIELD);
            bool noError = true;
            for (int i = 0; i < twoS; i++)
            {
                // Thanks to sanfordsquires for this fix:
                int eval = poly.EvaluateAt(field.Exp(dataMatrix?i + 1:i));
                syndromeCoefficients[syndromeCoefficients.Length - 1 - i] = eval;
                if (eval != 0)
                {
                    noError = false;
                }
            }
            if (noError)
            {
                return ;
            }
            GF256Poly syndrome = new GF256Poly(field, syndromeCoefficients);
            GF256Poly[] sigmaOmega = RunEuclideanAlgorithm(field.BuildMonomial(twoS, 1), syndrome, twoS);
            GF256Poly sigma = sigmaOmega[0];
            GF256Poly omega = sigmaOmega[1];
            int[] errorLocations = FindErrorLocations(sigma);
            int[] errorMagnitudes = FindErrorMagnitudes(omega, errorLocations, dataMatrix);
            for (int i = 0; i < errorLocations.Length; i++)
            {
                int position = received.Length - 1 - field.Log(errorLocations[i]);
                if (position < 0)
                {
                    throw new ReedSolomonException("Bad error location");
                }
                received[position] = GF256.AddOrSubtract(received[position], errorMagnitudes[i]);
            }
        }

        private GF256Poly[] RunEuclideanAlgorithm(GF256Poly a, GF256Poly b, int R)
        {
            // Assume a's degree is >= b's
            if (a.Degree < b.Degree)
            {
                GF256Poly temp = a;
                a = b;
                b = temp;
            }

            GF256Poly rLast = a;
            GF256Poly r = b;
            GF256Poly sLast = field.One;
            GF256Poly s = field.Zero;
            GF256Poly tLast = field.Zero;
            GF256Poly t = field.One;

            // Run Euclidean algorithm until r's degree is less than R/2
            while (r.Degree >= R / 2)
            {
                GF256Poly rLastLast = rLast;
                GF256Poly sLastLast = sLast;
                GF256Poly tLastLast = tLast;
                rLast = r;
                sLast = s;
                tLast = t;

                // Divide rLastLast by rLast, with quotient in q and remainder in r
                if (rLast.Zero)
                {
                    // Oops, Euclidean algorithm already terminated?
                    throw new ReedSolomonException("r_{i-1} was zero");
                }
                r = rLastLast;
                GF256Poly q = field.Zero;
                int denominatorLeadingTerm = rLast.GetCoefficient(rLast.Degree);
                int dltInverse = field.Inverse(denominatorLeadingTerm);
                while (r.Degree >= rLast.Degree && !r.Zero)
                {
                    int degreeDiff = r.Degree - rLast.Degree;
                    int scale = field.Multiply(r.GetCoefficient(r.Degree), dltInverse);
                    q = q.AddOrSubtract(field.BuildMonomial(degreeDiff, scale));
                    r = r.AddOrSubtract(rLast.MultiplyByMonomial(degreeDiff, scale));
                }

                s = q.Multiply(sLast).AddOrSubtract(sLastLast);
                t = q.Multiply(tLast).AddOrSubtract(tLastLast);
            }

            int sigmaTildeAtZero = t.GetCoefficient(0);
            if (sigmaTildeAtZero == 0)
            {
                throw new ReedSolomonException("sigmaTilde(0) was zero");
            }

            int inverse = field.Inverse(sigmaTildeAtZero);
            GF256Poly sigma = t.Multiply(inverse);
            GF256Poly omega = r.Multiply(inverse);
            return new[]{sigma, omega};
        }

        private int[] FindErrorLocations(GF256Poly errorLocator)
        {
            // This is a direct application of Chien's search
            int numErrors = errorLocator.Degree;
            if (numErrors == 1)
            {
                // shortcut
                return new[]{errorLocator.GetCoefficient(1)};
            }
            int[] result = new int[numErrors];
            int e = 0;
            for (int i = 1; i < 256 && e < numErrors; i++)
            {
                if (errorLocator.EvaluateAt(i) != 0)
                    continue;
                result[e] = field.Inverse(i);
                e++;
            }
            if (e != numErrors)
            {
                throw new ReedSolomonException("Error locator degree does not match number of roots");
            }
            return result;
        }

        private int[] FindErrorMagnitudes(GF256Poly errorEvaluator, int[] errorLocations, bool dataMatrix)
        {
            // This is directly applying Forney's Formula
            int s = errorLocations.Length;
            int[] result = new int[s];
            for (int i = 0; i < s; i++)
            {
                int xiInverse = field.Inverse(errorLocations[i]);
                int denominator = 1;
                for (int j = 0; j < s; j++)
                {
                    if (i != j)
                    {
                        denominator = field.Multiply(denominator, GF256.AddOrSubtract(1, field.Multiply(errorLocations[j], xiInverse)));
                    }
                }
                result[i] = field.Multiply(errorEvaluator.EvaluateAt(xiInverse), field.Inverse(denominator));
                // Thanks to sanfordsquires for this fix:
                if (dataMatrix)
                {
                    result[i] = field.Multiply(result[i], xiInverse);
                }
            }
            return result;
        }
    }
}