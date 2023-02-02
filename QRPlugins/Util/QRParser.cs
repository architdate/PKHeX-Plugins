using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using PKHeX.Core;
using PKHeX.Core.Enhancements;
using ZXing.Common;
using ZXing.Windows.Compatibility;

namespace AutoModPlugins
{
    public static class QRParser
    {
        /// <summary>
        /// Gets QR image from HTTP requests.
        /// </summary>
        public static async Task<Image?> GetQRData(string SaveID, string TeamID, string Cookie)
        {
            byte[] data = Encoding.ASCII.GetBytes($"savedataId={SaveID}&battleTeamCd={TeamID}");

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Accept.ParseAdd("*/*");
            httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
            httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.8");
            httpClient.DefaultRequestHeaders.Add("Cookie", Cookie);
            httpClient.DefaultRequestHeaders.Host = "3ds.pokemon-gl.com";
            httpClient.DefaultRequestHeaders.Add("Origin", "https://3ds.pokemon-gl.com/");
            httpClient.DefaultRequestHeaders.Referrer = new Uri($"https://3ds.pokemon-gl.com/rentalteam/{TeamID}");
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/59.0.3071.115 Safari/537.36");

            const string pglURL = "https://3ds.pokemon-gl.com/frontendApi/battleTeam/getQr";
            var response = await httpClient.PostAsync(pglURL, new ByteArrayContent(data));
            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync();

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
            var reader = new BarcodeReader { AutoRotate = true, Options = new DecodingOptions { TryInverted = true } };
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

        private static byte[] QR_t(ReadOnlySpan<byte> qr)
        {
            byte[] aes_ctr_key = ToByteArray("0F8E2F405EAE51504EDBA7B4E297005B");

          //var metadata_flags = qr[..0x08].ToArray();
            var ctr_aes = qr.Slice(0x08, 0x10).ToArray();
            var data = qr.Slice(0x18, 0x1CE).ToArray();
          //var sha1 = qr.Slice(0x1E6, 8).ToArray();

            var cipher = CipherUtilities.GetCipher("AES/CTR/NoPadding");
            cipher.Init(false, new ParametersWithIV(new KeyParameter(aes_ctr_key), ctr_aes));

            return cipher.ProcessBytes(data);
        }

        public static RentalTeam? DecryptQRCode(Image QR)
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

            //unencrypt the data in the plaintext.
            byte[] qrDec = QR_t(qrt);

            //build the rental team.
            return new RentalTeam(qrDec);
        }
    }
}