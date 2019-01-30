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
    /// <summary> The general exception class throw when something goes wrong during decoding of a barcode.
    /// This includes, but is not limited to, failing checksums / error correction algorithms, being
    /// unable to locate finder timing patterns, and so on.
    ///
    /// </summary>
    /// <author>  Sean Owen
    /// </author>
    /// <author>www.Redivivus.in (suraj.supekar@redivivus.in) - Ported from ZXING Java Source
    /// </author>

    [Serializable]
    public sealed class ReaderException : Exception
    {
        public static ReaderException Instance { get; } = new ReaderException();

        // EXCEPTION TRACKING SUPPORT
        // Identifies who is throwing exceptions and how often. To use:
        //
        // 1. Uncomment these lines and the code below which uses them.
        // 2. Uncomment the two corresponding lines in j2se/CommandLineRunner.decode()
        // 3. Change core to build as Java 1.5 temporarily
        //  private static int exceptionCount = 0;
        //  private static Map<String,Integer> throwers = new HashMap<String,Integer>(32);

        private ReaderException()
        {
            // do nothing
        }

        public ReaderException(string message) : base(message)
        {
        }

        public ReaderException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}