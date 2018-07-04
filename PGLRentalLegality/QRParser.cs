using System;
using System.Net;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

using com.google.zxing.qrcode;
using com.google.zxing;
using com.google.zxing.common;

using Org.BouncyCastle.Security;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;

using PKHeX.Core;

namespace PGLRentalLegality
{
    public class QRPoke
    {
        private readonly uint Key;
        private readonly byte HyperTrainingFlags;
        private readonly byte field_5;
        private readonly byte field_6;
        private readonly byte field_7;
        private readonly byte[] PPUps;
        private readonly uint IvFlags;
        private readonly uint field_10;
        private readonly ushort MonsNo;
        private readonly ushort HoldItem;
        private readonly ushort[] Moves;
        private readonly byte field_20;
        private readonly byte AbilityFlags;
        private readonly byte Nature;
        private readonly byte EncounterFlags;
        private readonly byte EffortHp;
        private readonly byte EffortAtk;
        private readonly byte EffortDef;
        private readonly byte EffortSpeed;
        private readonly byte EffortSpAtk;
        private readonly byte EffortSpDef;
        private readonly byte field_2A;
        private readonly byte Familiarity;
        private readonly byte Pokeball;
        private readonly byte Level;
        private readonly byte CassetteVersion;
        private readonly byte LangId;
        private readonly byte[] rawData;
        private readonly string gender;
        private readonly int formID;
        private readonly int[] IVs;

        public QRPoke(byte[] pkmData)
        {
            Console.WriteLine(pkmData.Length);
            try
            {
                //split the data down into the correct sections.
                rawData = pkmData.Skip(0).ToArray();
                Key = BitConverter.ToUInt32(pkmData, 0);
                HyperTrainingFlags = pkmData[4];
                field_5 = pkmData[5];
                field_6 = pkmData[6];
                field_7 = pkmData[7];
                PPUps = pkmData.Skip(8).Take(4).ToArray();
                IvFlags = BitConverter.ToUInt32(pkmData, 12);
                field_10 = BitConverter.ToUInt32(pkmData, 16);
                MonsNo = BitConverter.ToUInt16(pkmData, 20);
                //Console.WriteLine(MonsNo);

                HoldItem = BitConverter.ToUInt16(pkmData, 22);

                Moves = new ushort[4];
                Moves[0] = BitConverter.ToUInt16(pkmData, 24);
                Moves[1] = BitConverter.ToUInt16(pkmData, 26);
                Moves[2] = BitConverter.ToUInt16(pkmData, 28);
                Moves[3] = BitConverter.ToUInt16(pkmData, 30);

                field_20 = pkmData[32];
                AbilityFlags = pkmData[33];
                Nature = pkmData[34];

                EncounterFlags = pkmData[35];
                //From Project Pokemon.
                //Stored as: Bit 0: Fateful Encounter, Bit 1: Female, Bit 2: Genderless, Bits 3-7: Form Data.

                gender = ((EncounterFlags & 0x2) != 0) ? "(F) " : ((EncounterFlags & 0x4) != 0) ? "" : "(M) ";
                formID = EncounterFlags >> 3;

                EffortHp = pkmData[36];
                EffortAtk = pkmData[37];
                EffortDef = pkmData[38];
                EffortSpeed = pkmData[39];
                EffortSpAtk = pkmData[40];
                EffortSpDef = pkmData[41];
                field_2A = pkmData[42];
                Familiarity = pkmData[43];

                Pokeball = pkmData[44];
                Level = pkmData[45];
                CassetteVersion = pkmData[46];
                LangId = pkmData[47];

                Console.WriteLine("Familiarity: " + formID);

                int[] IVTemp = new int[6];

                for (int i = 0; i < 6; i++)
                {
                    IVTemp[i] = ((int)IvFlags >> (i * 5)) & 0x1F;
                }

                IVs = new int[6]{
                    IVTemp[0],
                    IVTemp[1],
                    IVTemp[2],
                    IVTemp[4],
                    IVTemp[5],
                    IVTemp[3]
                };
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
            }
        }

        public String GetHiddenPowerType()
        {
            switch (HiddenPower.GetType(IVs))
            {
                case 0: return "Fighting";
                case 1: return "Flying";
                case 2: return "Poison";
                case 3: return "Ground";
                case 4: return "Rock";
                case 5: return "Bug";
                case 6: return "Ghost";
                case 7: return "Steel";
                case 8: return "Fire";
                case 9: return "Water";
                case 10: return "Grass";
                case 11: return "Electric";
                case 12: return "Psychic";
                case 13: return "Ice";
                case 14: return "Dragon";
                case 15: return "Dark";
            }
            return string.Empty;
        }

        public string ToShowdownFormat(bool HT)
        {
            /* TODO:
             *  - Add the ability to remove the IVs.
             *  - Add the Hidden Power type.
             * */

            string HTFlags = Convert.ToString(HyperTrainingFlags, 2);
            string[] IVString = new string[6];
            for (int i = 0; i < 30 - HTFlags.Length; i++)
            {
                HTFlags = "0" + HTFlags;
            }

            char[] chars = HTFlags.Reverse().ToArray();

            for (int i = 0; i < 6; i++)
            {
                if (chars[i] == '1')
                {
                    if (HT)
                    {
                        IVString[i] = IVs[i].ToString() + "(HT)";
                    }
                    else
                    {
                        IVString[i] = "31";
                    }
                }
                else
                {
                    IVString[i] = IVs[i].ToString();
                }
            }

            string[] format =
            {
                DataFetch.GetSpecies(MonsNo,formID) + /*" ("  +")" +*/ " @ " + DataFetch.GetItem(HoldItem),
                "Ability: "+DataFetch.GetAbility(MonsNo,formID,AbilityFlags),
                "Level: "+Level,
                "Happiness: 0",
                "EVs: " + EffortHp + " HP / " + EffortAtk + " Atk / " + EffortDef + " Def / " + EffortSpAtk + " SpA / " + EffortSpDef + " SpD / " + EffortSpeed + " Spe",
                DataFetch.GetNature(Nature) + " Nature",
                "IVs: " + IVString[0] + " HP / " + IVString[1] + " Atk / " + IVString[2] + " Def / " + IVString[3] + " SpA / " + IVString[4] + " SpD / " + IVString[5] + " Spe ",
                " - " + DataFetch.GetMove(Moves[0],this),
                " - " + DataFetch.GetMove(Moves[1],this),
                " - " + DataFetch.GetMove(Moves[2],this),
                " - " + DataFetch.GetMove(Moves[3],this)
            };

            return string.Join("\n", format);
        }

        public string GetStatsData()
        {
            string HTFlags = Convert.ToString(HyperTrainingFlags, 2);
            for (int i = 0; i < 30 - HTFlags.Length; i++)
            {
                HTFlags = "0" + HTFlags;
            }

            char[] chars = HTFlags.Reverse().ToArray();
            string HT = "HT: ";

            for (int i = 0; i < 6; i++)
            {
                if (i != 0)
                {
                    HT += "/";
                }
                if (chars[i] == '1')
                {
                    HT += "HT";
                }
                else
                {
                    HT += "X";
                }
            }

            string[] format =
            {
                "Item: " + DataFetch.GetItem(HoldItem),
                HT,
                "EVs: " + EffortHp + "H " + EffortAtk + "A " + EffortDef + "B " + EffortSpAtk + "C " + EffortSpDef + "D " + EffortSpeed + "S",
                "IVs: " + IVs[0] + "/" + IVs[1] + "/" + IVs[2] + "/" + IVs[3] + "/" + IVs[4] + "/" + IVs[5]
            };

            return string.Join("\n", format);
        }

        public string GetMovesString()
        {
            string[] format =
            {
                " - " + DataFetch.GetMove(Moves[0],this),
                " - " + DataFetch.GetMove(Moves[1],this),
                " - " + DataFetch.GetMove(Moves[2],this),
                " - " + DataFetch.GetMove(Moves[3],this)
            };

            return string.Join("\n", format);
        }

        public override string ToString()
        {
            try
            {
                return "\n" + ToShowdownFormat(true) + "\n";
            }
            catch (Exception)
            {
                return "No Pokemon in this Slot";
            }
        }
    }

    public class RentalTeam
    {
        public List<QRPoke> team;
        public byte[] GLid;
        public byte[] UnknownData;

        public RentalTeam(byte[] data)
        {
            Console.WriteLine(data.Length);
            team = new List<QRPoke>();
            team.Add(new QRPoke(data.Take(0x30).ToArray()));
            team.Add(new QRPoke(data.Skip(0x30).Take(0x30).ToArray()));
            team.Add(new QRPoke(data.Skip(0x60).Take(0x30).ToArray()));
            team.Add(new QRPoke(data.Skip(0x90).Take(0x30).ToArray()));
            team.Add(new QRPoke(data.Skip(0xC0).Take(0x30).ToArray()));
            team.Add(new QRPoke(data.Skip(0xF0).Take(0x30).ToArray()));

            foreach (QRPoke p in team)
                Console.WriteLine(p.ToShowdownFormat(true) + "\n");

            GLid = data.Skip(0x120).Take(8).ToArray();
            UnknownData = data.Skip(0x128).ToArray();
        }
    }

    public class QRParser
    {
        private string sID, tID, cookie;

        public QRParser()
        {
            sID = "";
            tID = "";
            cookie = "";
        }

        public void printByteArray(byte[] b)
        {
            if (b == null) return;

            //Console.WriteLine(ByteArrayExtensions.ToHexString(b));
            foreach (byte B in b)
            {
                Console.Write(Convert.ToString(B, 16) + " ");
                Console.WriteLine(Convert.ToString(B, 2));
            }
        }

        //Get QR from HTTP requests.

        public Image getQRData()
        {
            byte[] data = Encoding.ASCII.GetBytes("savedataId=" + sID + "&battleTeamCd=" + tID);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://3ds.pokemon-gl.com/frontendApi/battleTeam/getQr");
            request.Method = "POST";
            request.Accept = "*/*";
            request.Headers["Accept-Encoding"] = "gzip, deflate, br";
            request.Headers["Accept-Language"] = "en-US,en;q=0.8";
            request.KeepAlive = true;
            request.ContentLength = 73;
            request.ContentType = "application/x-www-form-urlencoded";
            request.Headers["Cookie"] = cookie;
            request.Host = "3ds.pokemon-gl.com";
            request.Headers["Origin"] = "https://3ds.pokemon-gl.com/";
            request.Referer = "https://3ds.pokemon-gl.com/rentalteam/" + tID;
            request.UserAgent = "Mozilla/5.0 (Windows NT 10.0) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/59.0.3071.115 Safari/537.36";

            using (Stream stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }

            using (WebResponse response = request.GetResponse())
            {
                using (Stream stream = response.GetResponseStream())
                {
                    //add failing validation.
                    try
                    {
                        return Image.FromStream(stream);
                    }
                    catch (Exception)
                    {
                        //invalid QR
                        return null;
                    }
                }
            }
        }

        public byte[] parseQR(Image q)
        {
            var bitmap = new Bitmap(q);
            var img = new RGBLuminanceSource(bitmap, bitmap.Width, bitmap.Height);
            var hybrid = new HybridBinarizer(img);
            var binaryMap = new BinaryBitmap(hybrid);
            var reader = new QRCodeReader().decode(binaryMap, null);
            return Array.ConvertAll(reader.RawBytes, a => (byte)a);
        }

        public byte[] shiftArray(byte[] b)
        {
            byte[] array = new byte[507];
            byte lb = 0;
            byte rb = 0;
            for (int i = 0; i < array.Length; i++)
            {
                byte B = b[i];
                lb = (byte)((B & 0xF0) >> 4);
                array[i] = (byte)(rb << 4 | lb);
                rb = (byte)(B & 0xF);
            }

            return array;
        }

        public byte[] ToByteArray(string toTransform)
        {
            return Enumerable
                .Range(0, toTransform.Length / 2)
                .Select(i => Convert.ToByte(toTransform.Substring(i * 2, 2), 16))
                .ToArray();
        }

        public byte[] qr_t(byte[] qr)
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

            IBufferedCipher cipher = CipherUtilities.GetCipher("AES/CTR/NoPadding");
            cipher.Init(false, new ParametersWithIV(new KeyParameter(aes_ctr_key), ctr_aes));

            return cipher.ProcessBytes(data);
        }

        public RentalTeam decryptQRCode(Image QR)
        {
            //Read the bytes of the QR code
            byte[] data = parseQR(QR);

            //All data is shifted to the left by 4. Shift the data to the correct location.
            data = shiftArray(data);

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
                byte[] qrDec = qr_t(qrt);

                //build the rental team.
                return new RentalTeam(qrDec);
            }
        }


        public void setsID(string newID)
        {
            sID = newID;
        }

        public void settID(string newID)
        {
            tID = newID;
        }

        public void setCookie(string newCookie)
        {
            cookie = newCookie;
        }
    }
}