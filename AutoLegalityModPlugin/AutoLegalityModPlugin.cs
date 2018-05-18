using PKHeX.Core;
using PKHeX.WinForms.AutoLegality;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace PKHeX.WinForms.Controls
{
    public partial class AutoLegalityModPlugin : IPlugin
    {
        public string Name => nameof(AutoLegalityModPlugin);
        public ISaveFileProvider SaveFileEditor { get; private set; }
        public IPKMView PKMEditor { get; private set; }
        public bool APILegalized = false;

        #region Path Variables

        public static string WorkingDirectory => WinFormsUtil.IsClickonceDeployed ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "PKHeX") : Application.StartupPath;
        public static string DatabasePath => Path.Combine(WorkingDirectory, "pkmdb");
        public static string MGDatabasePath => Path.Combine(WorkingDirectory, "mgdb");
        public static string BackupPath => Path.Combine(WorkingDirectory, "bak");
        public static string CryPath => Path.Combine(WorkingDirectory, "sounds");
        private static string TemplatePath => Path.Combine(WorkingDirectory, "template");
        private static string PluginPath => Path.Combine(WorkingDirectory, "plugins");
        private const string ThreadPath = "https://projectpokemon.org/pkhex/";
        private const string VersionPath = "https://raw.githubusercontent.com/kwsch/PKHeX/master/PKHeX.WinForms/Resources/text/version.txt";

        #endregion

        public bool allowAPI = true;
       
        public void Initialize(params object[] args)
        {
            Console.WriteLine($"Loading {Name}...");
            if (args == null)
                return;
            SaveFileEditor = (ISaveFileProvider)Array.Find(args, z => z is ISaveFileProvider);
            PKMEditor = (IPKMView)Array.Find(args, z => z is IPKMView);
            var menu = (ToolStrip)Array.Find(args, z => z is ToolStrip);
            LoadMenuStrip(menu);
        }

        private void LoadMenuStrip(ToolStrip menuStrip)
        {
            var items = menuStrip.Items;
            var tools = items.Find("Menu_Tools", false)[0] as ToolStripDropDownItem;
            AddPluginControl(tools);
        }

        private void AddPluginControl(ToolStripDropDownItem tools)
        {
            var ctrl = new ToolStripMenuItem(Name);
            tools.DropDownItems.Add(ctrl);
            ctrl.Click += new EventHandler(AutoLegalityImport);
        }

        private void AutoLegalityImport(object sender, EventArgs e)
        {
            bool allowAPI = true; // Use true to allow experimental API usage
            APILegalized = false; // Initialize to false everytime command is used
            if (!showdownData() || (Control.ModifierKeys & Keys.Shift) == Keys.Shift)
            {
                if (WinFormsUtil.OpenSAVPKMDialog(new string[] { "txt" }, out string path))
                {
                    Clipboard.SetText(File.ReadAllText(path).TrimEnd());
                    if (!showdownData())
                    {
                        MessageBox.Show("Text file with invalid data provided. Please provide a text file with proper Showdown data");
                        return;
                    }
                }
                else
                {
                    MessageBox.Show("No data provided.");
                    return;
                }
            }

            if (!Directory.Exists(MGDatabasePath)) Directory.CreateDirectory(MGDatabasePath);

            string source = Clipboard.GetText().TrimEnd();
            string[] stringSeparators = new string[] { "\n\r" };
            string[] result;

            // ...
            result = source.Split(stringSeparators, StringSplitOptions.None);
            if (allowAPI)
            {
                List<string> resList = result.OfType<string>().ToList();
                resList.RemoveAll(r => r.Trim() == "");
                result = resList.ToArray();
            }
            Console.WriteLine(result.Length);
            if (result.Length > 1) AutoLegalityImportMultiple(result);
            else AutoLegalityImportSingle(new ShowdownSet(Clipboard.GetText()));
        }

        private void AutoLegalityImportMultiple(string[] result)
        {
            List<int> emptySlots = new List<int> { };
            IList<PKM> BoxData = SaveFileEditor.SAV.BoxData;
            if ((Control.ModifierKeys & Keys.Control) == Keys.Control) // Hold Ctrl while clicking to replace
            {
                for (int i = 0; i < result.Length; i++) emptySlots.Add(i);
            }
            else
            {
                for (int i = 0; i < SaveFileEditor.SAV.BoxSlotCount; i++)
                {
                    if (SaveFileEditor.SAV.BoxData[SaveFileEditor.CurrentBox * SaveFileEditor.SAV.BoxSlotCount +i].Species < 1) emptySlots.Add(i);
                }
                if (emptySlots.Count < result.Length)
                {
                    MessageBox.Show("Not enough space in the box");
                    return;
                }
            }
            int ctrapi = 0;
            List<string> setsungenned = new List<string>();
            for (int i = 0; i < result.Length; i++)
            {
                ShowdownSet Set = new ShowdownSet(result[i]);
                bool intRegions = false;
                if (Set.InvalidLines.Count > 0)
                    MessageBox.Show("Invalid lines detected:", string.Join(Environment.NewLine, Set.InvalidLines));

                // Set Species & Nickname
                bool resetForm = false;
                hardReset(SaveFileEditor.SAV);
                if (Set.Form == null) { }
                else if (Set.Form.Contains("Mega") || Set.Form == "Primal" || Set.Form == "Busted")
                {
                    resetForm = true;
                    Console.WriteLine(Set.Species);
                }
                LoadShowdownSetDefault(Set);
                PKM p = PKMEditor.PreparePKM();
                p.Version = (int)GameVersion.MN;
                PKM legal;
                if (allowAPI)
                {
                    AutoLegalityMod mod = new AutoLegalityMod();
                    mod.SAV = SaveFileEditor.SAV;
                    bool satisfied = false;
                    PKM APIGenerated = SaveFileEditor.SAV.BlankPKM;
                    try { APIGenerated = mod.APILegality(p, Set, out satisfied); }
                    catch { satisfied = false; }
                    if (!satisfied)
                    {
                        setsungenned.Add(Set.Text);
                        Blah b = new Blah();
                        SAVEditor se = new SAVEditor();
                        se.SAV = SaveFileEditor.SAV;
                        b.C_SAV = se;
                        legal = b.LoadShowdownSetModded_PKSM(p, Set, resetForm);
                        APILegalized = false;
                    }
                    else
                    {
                        ctrapi++;
                        legal = APIGenerated;
                        APILegalized = true;
                    }
                }
                else
                {
                    Blah b = new Blah();
                    SAVEditor se = new SAVEditor();
                    se.SAV = SaveFileEditor.SAV;
                    b.C_SAV = se;
                    legal = b.LoadShowdownSetModded_PKSM(p, Set, resetForm);
                    APILegalized = false;
                }
                PKMEditor.PopulateFields(legal);
                PKM pk = PKMEditor.PreparePKM();
                BoxData[SaveFileEditor.CurrentBox * SaveFileEditor.SAV.BoxSlotCount + emptySlots[i]] = pk;
            }
            SaveFileEditor.SAV.BoxData = BoxData;
            SaveFileEditor.ReloadSlots();
#if DEBUG
            MessageBox.Show("API Genned Sets: " + ctrapi + Environment.NewLine + Environment.NewLine + "Number of sets not genned by the API: " + setsungenned.Count);
            Console.WriteLine(String.Join("\n\n", setsungenned));
#endif
        }

        private void AutoLegalityImportSingle(ShowdownSet Set)
        {
            if (Set.Species < 0)
            { MessageBox.Show("Set data not found in clipboard."); return; }

            if (Set.Nickname?.Length > SaveFileEditor.SAV.NickLength)
                Set.Nickname = Set.Nickname.Substring(0, SaveFileEditor.SAV.NickLength);

            MessageBox.Show(Set.Text, "Importing this Set");

            // Set Species & Nickname
            //PKME_Tabs.LoadShowdownSet(Set);
            bool resetForm = false;
            hardReset(SaveFileEditor.SAV);

            if (Set.Form == null) { }
            else if (Set.Form.Contains("Mega") || Set.Form == "Primal" || Set.Form == "Busted")
            {
                Set = new ShowdownSet(Set.Text.Replace("-" + Set.Form, ""));
                resetForm = true;
                Console.WriteLine(Set.Species);
            }
            LoadShowdownSetDefault(Set);
            PKM p = PKMEditor.PreparePKM();
            p.Version = (int)GameVersion.MN;
            PKM legal;
            if (allowAPI)
            {
                AutoLegalityMod mod = new AutoLegalityMod();
                mod.SAV = SaveFileEditor.SAV;
                bool satisfied = false;
                PKM APIGenerated = SaveFileEditor.SAV.BlankPKM;
                try { APIGenerated = mod.APILegality(p, Set, out satisfied); }
                catch { satisfied = false; }
                if (!satisfied)
                {
                    Blah b = new Blah();
                    SAVEditor se = new SAVEditor();
                    se.SAV = SaveFileEditor.SAV;
                    b.C_SAV = se;
                    legal = b.LoadShowdownSetModded_PKSM(p, Set, resetForm);
                    APILegalized = false;
#if DEBUG
                    MessageBox.Show("Set was not genned by the API");
#endif
                }
                else
                {
                    legal = APIGenerated;
                    APILegalized = true;
                }
            }
            else
            {
                Blah b = new Blah();
                SAVEditor se = new SAVEditor();
                se.SAV = SaveFileEditor.SAV;
                b.C_SAV = se;
                legal = b.LoadShowdownSetModded_PKSM(p, Set, resetForm);
                APILegalized = false;
            }
            PKMEditor.PopulateFields(legal);
        }

        public void NotifySaveLoaded()
        {
            Console.WriteLine($"{Name} was notified that a Save File was just loaded.");
        }

        public bool TryLoadFile(string filePath)
        {
            Console.WriteLine($"{Name} was provided with the file path, but chose to do nothing with it.");
            return false; // no action taken
        }

        public void hardReset(SaveFile SAV = null)
        {
            SaveFile CURRSAV = new SAVEditor().SAV;
            if (SAV != null) CURRSAV = SAV;

            if (CURRSAV.USUM || CURRSAV.SM)
            {
                if (TryLoadPKM(new ConstData().resetpk7, "", "pk7", CURRSAV))
                {
                    return;
                }
            }
            else if (CURRSAV.ORAS || CURRSAV.XY)
            {
                if (TryLoadPKM(new ConstData().resetpk6, "", "pk6", CURRSAV))
                {
                    return;
                }
            }
        }

        /// <summary>
        /// Load PKM from byte array. The output is a boolean, which is true if a byte array is loaded into WinForms, else false
        /// </summary>
        /// <param name="input">Byte array input correlating to the PKM</param>
        /// <param name="path">Path to the file itself</param>
        /// <param name="ext">Extension of the file</param>
        /// <param name="SAV">Type of save file</param>
        /// <returns></returns>
        private bool TryLoadPKM(byte[] input, string path, string ext, SaveFile SAV)
        {
            PKM CurrentPKM = PKMEditor.PreparePKM();
            var temp = PKMConverter.GetPKMfromBytes(input, prefer: ext.Length > 0 ? (ext[ext.Length - 1] - 0x30) & 7 : SAV.Generation);
            if (temp == null)
                return false;

            var type = CurrentPKM.GetType();
            PKM pk = PKMConverter.ConvertToType(temp, type, out string c);
            if (pk == null)
            {
                return false;
            }
            if (SAV.Generation < 3 && ((pk as PK1)?.Japanese ?? ((PK2)pk).Japanese) != SAV.Japanese)
            {
                var strs = new[] { "International", "Japanese" };
                var val = SAV.Japanese ? 0 : 1;
                MessageBox.Show($"Cannot load {strs[val]} {pk.GetType().Name}s to {strs[val ^ 1]} saves.");
                return false;
            }

            PKMEditor.PopulateFields(pk);
            Console.WriteLine(c);
            return true;
        }

        private bool showdownData()
        {
            if (!Clipboard.ContainsText()) return false;
            string source = Clipboard.GetText().TrimEnd();
            string[] stringSeparators = new string[] { "\n\r" };
            string[] result;

            // ...
            result = source.Split(stringSeparators, StringSplitOptions.None);
            if (new ShowdownSet(result[0]).Species < 0) return false;
            return true;
        }

        private void LoadShowdownSetDefault(ShowdownSet Set)
        {
            var pk = PKMEditor.PreparePKM();
            pk.ApplySetDetails(Set);
            PKMEditor.PopulateFields(pk);
        }
    }
}
