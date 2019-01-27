using System;
using System.IO;
using PKHeX.Core;

namespace PGLRentalLegality
{
    public static class DataFetch
    {
        public static string GetSpecies(ushort ID, int form)
        {
            try
            {
                if (form > 0)
                {
                    //Has a different form from normal
                    foreach (string line in GetCSV("pokemonFormAbilities"))
                    {
                        var currLine = line.Split(',');
                        if (int.Parse(currLine[0]) == ID && int.Parse(currLine[1]) == form)
                            return currLine[2];
                    }
                }
                return Util.GetStringList("text_Species_en")[ID];
            }
            catch (Exception)
            {
                return ID.ToString();
            }
        }

        public static string GetMove(ushort ID, QRPoke p)
        {
            string[] names = Util.GetStringList("text_Moves_en");
            try
            {
                if (names[ID] == "Hidden Power")
                {
                    return "Hidden Power [" + p.GetHiddenPowerTypeName() + "]";
                }
                return names[ID];
            }
            catch (Exception)
            {
                return ID.ToString();
            }
        }

        public static string GetAbility(ushort ID, int form, int ability)
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
                    foreach (string line in GetCSV("pokemonFormAbilities"))
                    {
                        var currLine = line.Split(',');
                        if (int.Parse(currLine[0]) != ID)
                            continue;

                        if (int.Parse(currLine[1]) == form)
                            return currLine[3 + (int) Math.Log(ability, 2)];
                    }
                }
                return GetCSV("pokemonAbilities")[ID].Split(',')[(int)Math.Log(ability, 2)];
            }
            catch (Exception)
            {
                return ability.ToString();
            }
        }

        public static string GetItem(ushort ID)
        {
            string[] names = Util.GetStringList("text_Items_en");
            try
            {
                return names[ID];
            }
            catch (Exception)
            {
                return ID.ToString();
            }
        }

        public static string GetNature(byte ID)
        {
            string[] names = Util.GetStringList("text_Natures_en");
            try
            {
                return names[ID];
            }
            catch (Exception)
            {
                return ID.ToString();
            }
        }

        private static string[] GetCSV(string loc)
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var resourceName = "PGLRentalLegality.Resources.text." + loc + ".csv";
            Stream stream = assembly.GetManifestResourceStream(resourceName);
            StreamReader file = new StreamReader(stream);
            var txt = file.ReadToEnd();
            if (string.IsNullOrWhiteSpace(txt))
                return Array.Empty<string>();

            string[] rawlist = txt.Split('\n');

            for (int i = 0; i < rawlist.Length; i++)
                rawlist[i] = rawlist[i].Trim();

            return rawlist;
        }
    }
}