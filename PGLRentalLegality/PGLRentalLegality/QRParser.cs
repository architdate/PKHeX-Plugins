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

namespace PKHeX.WinForms.Misc
{
    public partial class DataFetch
    {
        public static string getSpecies(ushort ID, int form)
        {

            try
            {
                if (form > 0)
                {
                    //Has a different form from normal
                    foreach (string line in getCSV("pokemonFormAbilities"))
                    {
                        var currLine = line.Split(',');
                        if (int.Parse(currLine[0]) == ID)
                        {
                            if (int.Parse(currLine[1]) == form)
                            {
                                return currLine[2];
                            }
                        }
                    }
                }
                return PKHeX.Core.Util.GetStringList("text_Species_en")[ID];

            }
            catch (Exception e)
            {
                return ID.ToString();
            }
        }
        public static string getMove(ushort ID, Pokemon p)
        {
            string[] names = PKHeX.Core.Util.GetStringList("text_Moves_en");
            try
            {
                if (names[ID] == "Hidden Power")
                {
                    return "Hidden Power [" + p.getHiddenPowerType() + "]";
                }
                return names[ID];
            }
            catch (Exception e)
            {
                return ID.ToString();
            }
        }
        public static string getAbility(ushort ID, int form, int ability)
        {
            //Update to deal with the 1,2,4.
            /*
             * P3DS thoughts: 
             *  - Split abilities into abilities and forms w/ different abilities
             *  - Then, first check if Pokemon has a form ID > 0. If it does, check the form list for it
             *    since it will require more processing than a standard list like normal.
             *  - Maybe include an exclusion list as well, to help sort the out.
             * 
            */

            try
            {
                if (form > 0)
                {
                    //Has a different form from normal
                    foreach (string line in getCSV("pokemonFormAbilities"))
                    {
                        var currLine = line.Split(',');
                        if (int.Parse(currLine[0]) == ID)
                        {
                            if (int.Parse(currLine[1]) == form)
                            {
                                return currLine[3 + (int)Math.Log(ability, 2)];
                            }
                        }
                    }
                }
                return getCSV("pokemonAbilities")[ID].Split(',')[(int)Math.Log(ability, 2)];

            }
            catch (Exception e)
            {
                return ability.ToString();
            }

        }
        public static string getItem(ushort ID)
        {
            string[] names = PKHeX.Core.Util.GetStringList("text_Items_en");
            try
            {
                return names[ID];
            }
            catch (Exception e)
            {
                return ID.ToString();
            }
        }
        public static string getNature(byte ID)
        {
            string[] names = PKHeX.Core.Util.GetStringList("text_Natures_en");
            try
            {
                return names[ID];
            }
            catch (Exception e)
            {
                return ID.ToString();
            }
        }
        private static string[] getCSV(string loc)
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var resourceName = "PKHeX.WinForms.PGLRentalLegality.Resources.text." + loc + ".csv";
            System.IO.Stream stream = assembly.GetManifestResourceStream(resourceName);
            System.IO.StreamReader file = new System.IO.StreamReader(stream);
            var txt = file.ReadToEnd();
            if (txt == null)
                return new string[0];

            string[] rawlist = (txt).Split('\n');

            for (int i = 0; i < rawlist.Length; i++)
                rawlist[i] = rawlist[i].Trim();

            return rawlist;
        }
        private static string[] getList(string loc)
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var resourceName = "PKHeX.Core.Resources.text.en."+ loc +".txt";
            System.IO.Stream stream = assembly.GetManifestResourceStream(resourceName);
            System.IO.StreamReader file = new System.IO.StreamReader(stream);
            var txt = file.ReadToEnd();
            if (txt == null)
                return new string[0];

            string[] rawlist = (txt).Split('\n');

            for (int i = 0; i < rawlist.Length; i++)
                rawlist[i] = rawlist[i].Trim();

            return rawlist;
        }
    }
    public partial class Pokemon
    {
        uint Key;
        byte HyperTrainingFlags;
        byte field_5;
        byte field_6;
        byte field_7;
        byte[] PPUps;
        uint IvFlags;
        uint field_10;
        ushort MonsNo;
        ushort HoldItem;
        ushort[] Moves;
        byte field_20;
        byte AbilityFlags;
        byte Nature;
        byte EncounterFlags;
        byte EffortHp;
        byte EffortAtk;
        byte EffortDef;
        byte EffortSpeed;
        byte EffortSpAtk;
        byte EffortSpDef;
        byte field_2A;
        byte Familiarity;
        byte Pokeball;
        byte Level;
        byte CassetteVersion;
        byte LangId;
        byte[] rawData;
        string gender;
        int formID;
        int[] IVs;

        public Pokemon(byte[] pkmData)
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

        public String getHiddenPowerType()
        {
            string rtn = "";
            int type = IVs[0] % 2 + (IVs[1] % 2) * 2 + (IVs[2] % 2) * 4 + (IVs[5] % 2) * 8 + (IVs[3] % 2) * 16 + (IVs[4] % 2) * 32;
            type = (type * 15) / 63;
            switch (type)
            {
                case 0: rtn = "Fighting"; break;
                case 1: rtn = "Flying"; break;
                case 2: rtn = "Poison"; break;
                case 3: rtn = "Ground"; break;
                case 4: rtn = "Rock"; break;
                case 5: rtn = "Bug"; break;
                case 6: rtn = "Ghost"; break;
                case 7: rtn = "Steel"; break;
                case 8: rtn = "Fire"; break;
                case 9: rtn = "Water"; break;
                case 10: rtn = "Grass"; break;
                case 11: rtn = "Electric"; break;
                case 12: rtn = "Psychic"; break;
                case 13: rtn = "Ice"; break;
                case 14: rtn = "Dragon"; break;
                case 15: rtn = "Dark"; break;
            }
            return rtn;
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
                DataFetch.getSpecies(MonsNo,formID) + /*" ("  +")" +*/ " @ " + DataFetch.getItem(HoldItem),
                "Ability: "+DataFetch.getAbility(MonsNo,formID,AbilityFlags),
                "Level: "+Level,
                "Happiness: 0",
                "EVs: " + EffortHp + " HP / " + EffortAtk + " Atk / " + EffortDef + " Def / " + EffortSpAtk + " SpA / " + EffortSpDef + " SpD / " + EffortSpeed + " Spe",
                DataFetch.getNature(Nature) + " Nature",
                "IVs: " + IVString[0] + " HP / " + IVString[1] + " Atk / " + IVString[2] + " Def / " + IVString[3] + " SpA / " + IVString[4] + " SpD / " + IVString[5] + " Spe ",
                " - " + DataFetch.getMove(Moves[0],this),
                " - " + DataFetch.getMove(Moves[1],this),
                " - " + DataFetch.getMove(Moves[2],this),
                " - " + DataFetch.getMove(Moves[3],this)
            };

            return string.Join("\n", format);
        }

        public string getStatsData()
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
                "Item: " + DataFetch.getItem(HoldItem),
                HT,
                "EVs: " + EffortHp + "H " + EffortAtk + "A " + EffortDef + "B " + EffortSpAtk + "C " + EffortSpDef + "D " + EffortSpeed + "S",
                "IVs: " + IVs[0] + "/" + IVs[1] + "/" + IVs[2] + "/" + IVs[3] + "/" + IVs[4] + "/" + IVs[5]
            };

            return string.Join("\n", format);
        }

        public string getMovesString()
        {
            string[] format =
            {
                " - " + DataFetch.getMove(Moves[0],this),
                " - " + DataFetch.getMove(Moves[1],this),
                " - " + DataFetch.getMove(Moves[2],this),
                " - " + DataFetch.getMove(Moves[3],this)
            };

            return string.Join("\n", format);
        }

        public override string ToString()
        {
            try
            {
                return "\n" + ToShowdownFormat(true) + "\n";
            }
            catch (Exception e)
            {
                return "No Pokemon in this Slot";
            }
        }
    }
    public partial class RentalTeam
    {
        public List<Pokemon> team;
        public byte[] GLid;
        public byte[] UnknownData;

        public RentalTeam(byte[] data)
        {
            Console.WriteLine(data.Length);
            team = new List<Pokemon>();
            team.Add(new Pokemon(data.Take(0x30).ToArray()));
            team.Add(new Pokemon(data.Skip(0x30).Take(0x30).ToArray()));
            team.Add(new Pokemon(data.Skip(0x60).Take(0x30).ToArray()));
            team.Add(new Pokemon(data.Skip(0x90).Take(0x30).ToArray()));
            team.Add(new Pokemon(data.Skip(0xC0).Take(0x30).ToArray()));
            team.Add(new Pokemon(data.Skip(0xF0).Take(0x30).ToArray()));

            foreach (Pokemon p in team)
            {
                Console.WriteLine(p.ToShowdownFormat(true) + "\n");
            }

            GLid = data.Skip(0x120).Take(8).ToArray();
            UnknownData = data.Skip(0x128).ToArray();
        }
    }
    public partial class QRParser
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
                    catch (Exception e)
                    {
                        //invalid QR
                        return null;
                    }

                }
            }
        }

        public byte[] parseQR(Image q)
        {
            Bitmap bitmap = new Bitmap(q);
            var img = new RGBLuminanceSource(bitmap, bitmap.Width, bitmap.Height);
            var hybrid = new HybridBinarizer(img);
            BinaryBitmap binaryMap = new BinaryBitmap(hybrid);
            var reader = new QRCodeReader().decode(binaryMap, null);
            byte[] data = Array.ConvertAll(reader.RawBytes, (a) => (byte)(a));
            return data;
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
                rb = (byte)((B & 0xF));
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
            if (!Core.MemeCrypto.VerifyMemePOKE(qrue, out var qrt))
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