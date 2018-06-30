using System;
using System.Windows.Forms;
using PKHeX.Core;

namespace ExportTrainerData
{
    public class ExportTrainerData : IPlugin
    {
        public string Name => "Export Trainer Data";
        public ISaveFileProvider SaveFileEditor { get; private set; }
        public IPKMView PKMEditor { get; private set; }

        public void Initialize(params object[] args)
        {
            Console.WriteLine($"[Auto Legality Mod] Loading {Name}");
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
            var toolsitems = tools.DropDownItems;
            var modmenusearch = toolsitems.Find("Menu_AutoLegality", false);
            if (modmenusearch.Length == 0)
            {
                var mod = new ToolStripMenuItem("Auto Legality Mod");
                tools.DropDownItems.Insert(0, mod);
                mod.Image = ExportTrainerDataResources.menuautolegality;
                mod.Name = "Menu_AutoLegality";
                var modmenu = mod;
                AddPluginControl(modmenu);
            }
            else
            {
                var modmenu = modmenusearch[0] as ToolStripMenuItem;
                AddPluginControl(modmenu);
            }
        }

        private void AddPluginControl(ToolStripDropDownItem tools)
        {
            var ctrl = new ToolStripMenuItem(Name);
            tools.DropDownItems.Add(ctrl);
            ctrl.Click += new EventHandler(ExportData);
            ctrl.Image = ExportTrainerDataResources.exporttrainerdata;
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

        private void ExportData(object sender, EventArgs e)
        {
            string TID = "23456";
            string SID = "34567";
            string OT = "Archit";
            string Gender = "M";
            string Country = "Canada";
            string SubRegion = "Alberta";
            string ConsoleRegion = "Americas (NA/SA)";
            PKM pk = PKMEditor.PreparePKM();
            try
            {
                TID = pk.TID.ToString();
                SID = pk.SID.ToString();
                OT = pk.OT_Name;
                if (pk.OT_Gender == 1) Gender = "F";
                Country = pk.Country.ToString();
                SubRegion = pk.Region.ToString();
                ConsoleRegion = pk.ConsoleRegion.ToString();
                writeTxtFile(TID, SID, OT, Gender, Country, SubRegion, ConsoleRegion);
                MessageBox.Show("trainerdata.txt Successfully Exported in the same directory as PKHeX");
            }
            catch
            {
                writeTxtFile(TID, SID, OT, Gender, Country, SubRegion, ConsoleRegion);
                MessageBox.Show("Some of the fields were wrongly filled. Exported the default trainerdata.txt");
            }
        }
        private void writeTxtFile(string TID, string SID, string OT, string Gender, string Country, string SubRegion, string ConsoleRegion)
        {
            string[] lines = { "TID:" + TID, "SID:" + SID, "OT:" + OT, "Gender:" + Gender, "Country:" + Country, "SubRegion:" + SubRegion, "3DSRegion:" + ConsoleRegion };
            System.IO.File.WriteAllLines(System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "trainerdata.txt"), lines);
        }
    }
}
