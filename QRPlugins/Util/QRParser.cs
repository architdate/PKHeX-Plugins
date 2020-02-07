using System;
using System.Diagnostics;
using System.Net;
using System.IO;
using System.Linq;
using System.Text;
using System.Drawing;

using Org.BouncyCastle.Security;
using Org.BouncyCastle.Crypto.Parameters;

using PKHeX.Core;
using PKHeX.Core.AutoMod;
using ZXing;

namespace AutoModPlugins
{
    public static class QRParser
    {
        /// <summary>
        /// Gets QR image from HTTP requests.
        /// </summary>
        public static Image GetQRData(string SaveID, string TeamID, string Cookie)
        {
            byte[] data = Encoding.ASCII.GetBytes($"savedataId={SaveID}&battleTeamCd={TeamID}");

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://3ds.pokemon-gl.com/frontendApi/battleTeam/getQr");
            request.Method = "POST";
            request.Accept = "*/*";
            request.Headers["Accept-Encoding"] = "gzip, deflate, br";
            request.Headers["Accept-Language"] = "en-US,en;q=0.8";
            request.KeepAlive = true;
            request.ContentLength = 73;
            request.ContentType = "application/x-www-form-urlencoded";
            request.Headers["Cookie"] = Cookie;
            request.Host = "3ds.pokemon-gl.com";
            request.Headers["Origin"] = "https://3ds.pokemon-gl.com/";
            request.Referer = $"https://3ds.pokemon-gl.com/rentalteam/{TeamID}";
            request.UserAgent = "Mozilla/5.0 (Windows NT 10.0) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/59.0.3071.115 Safari/537.36";

            using (Stream reqStream = request.GetRequestStream())
                reqStream.Write(data, 0, data.Length);

            using WebResponse response = request.GetResponse();
            using Stream stream = response.GetResponseStream();
            //add failing validation.
            try
            {
                return Image.FromStream(stream);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                //invalid QR
                return null;
            }
        }

        private static byte[] ParseQR(Image q)
        {
            using var bitmap = new Bitmap(q);
            BarcodeReader reader = new BarcodeReader { AutoRotate = true, TryInverted = true };
            var data = reader.Decode(bitmap).RawBytes;
            return Array.ConvertAll(data, a => a);
        }

        private static byte[] ShiftArray(byte[] b)
        {
            byte[] array = new byte[507];
            byte rb = 0;
            for (int i = 0; i < array.Length; i++)
            {
                byte B = b[i];
                var lb = (byte)((B & 0xF0) >> 4);
                array[i] = (byte)(rb << 4 | lb);
                rb = (byte)(B & 0xF);
            }

            return array;
        }

        private static byte[] ToByteArray(string toTransform)
        {
            return Enumerable
                .Range(0, toTransform.Length / 2)
                .Select(i => Convert.ToByte(toTransform.Substring(i * 2, 2), 16))
                .ToArray();
        }

        private static byte[] QR_t(byte[] qr)
        {
            byte[] aes_ctr_key = ToByteArray("0F8E2F405EAE51504EDBA7B4E297005B");

            byte[] metadata_flags = new byte[0x8];
            byte[] ctr_aes = new byte[0x10];
            byte[] data = new byte[0x1CE];
            byte[] sha1 = new byte[0x8];

            Array.Copy(qr, 0, metadata_flags, 0, 0x8);
            Array.Copy(qr, 0x8, ctr_aes, 0, 0x10);
            Array.Copy(qr, 0x18, data, 0, 0x1CE);
            Array.Copy(qr, 0x1E6, sha1, 0, 0x8);

            var cipher = CipherUtilities.GetCipher("AES/CTR/NoPadding");
            cipher.Init(false, new ParametersWithIV(new KeyParameter(aes_ctr_key), ctr_aes));

            return cipher.ProcessBytes(data);
        }

        public static RentalTeam DecryptQRCode(Image QR)
        {
            //Read the bytes of the QR code
            byte[] data = ParseQR(QR);

            //All data is shifted to the left by 4. Shift the data to the correct location.
            data = ShiftArray(data);

            //ZXing has added the header bytes to the raw bytes. These are the first 3, so skip them.
            var qrue = data.Skip(3).ToArray();

            //MEME CRYPTO!!! De-Meme the data
            if (!MemeCrypto.VerifyMemePOKE(qrue, out var qrt))
            {
                Console.WriteLine("it failed");
                return null;
            }
            else
            {
                //unencrypt the data in the plaintext.
                byte[] qrDec = QR_t(qrt);

                //build the rental team.
                return new RentalTeam(qrDec);
            }
        }
    }
}