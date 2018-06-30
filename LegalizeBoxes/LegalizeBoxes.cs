using System;
using System.Collections.Generic;
using PKHeX.Core;
using AutoLegalityMod;
using System.Windows.Forms;

namespace LegalizeBoxes
{
    public class LegalizeBoxes : IPlugin
    {
        public string Name => "Legalize Active Pokemon";
        public ISaveFileProvider SaveFileEditor { get; private set; }
        public IPKMView PKMEditor { get; private set; }
        public object[] arguments;
        public ToolStripMenuItem ModMenu;

        public void Initialize(params object[] args)
        {
            arguments = args;
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
                mod.Image = LegalizeBoxesResources.menuautolegality;
                mod.Name = "Menu_AutoLegality";
                var modmenu = mod;
                ModMenu = modmenu;
                AddPluginControl(modmenu);
            }
            else
            {
                var modmenu = modmenusearch[0] as ToolStripMenuItem;
                ModMenu = modmenu;
                AddPluginControl(modmenu);
            }
        }

        private void AddPluginControl(ToolStripDropDownItem tools)
        {
            var ctrl = new ToolStripMenuItem(Name);
            tools.DropDownItems.Add(ctrl);
            ctrl.Click += new EventHandler(Legalize);
            ctrl.Image = LegalizeBoxesResources.legalizeboxes;
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

        private void Legalize(object sender, EventArgs e)
        {
            AutoLegalityMod.AutoLegalityMod alm = new AutoLegalityMod.AutoLegalityMod();
            alm.Initialize(arguments);
            alm.SAV = SaveFileEditor.SAV;
            IList<PKM> BoxData = SaveFileEditor.SAV.BoxData;
            for (int i = 0; i < 30; i++)
            {
                PKM illegalPK = PKMEditor.PreparePKM();
                bool box = false;
                if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
                {
                    box = true;
                }
                if (box) illegalPK = BoxData[SaveFileEditor.CurrentBox * SaveFileEditor.SAV.BoxSlotCount + i];
                if (illegalPK.Species > 0 && !new LegalityAnalysis(illegalPK).Valid)
                {
                    ShowdownSet Set = new ShowdownSet(ShowdownSet.GetShowdownText(illegalPK));
                    bool resetForm = false;
                    if (Set.Form != null)
                    {
                        if (Set.Form.Contains("Mega") || Set.Form == "Primal" || Set.Form == "Busted") resetForm = true;
                    }
                    PKM legal;
                    PKM APIGenerated = SaveFileEditor.SAV.BlankPKM;
                    bool satisfied = false;
                    try { APIGenerated = alm.APILegality(illegalPK, Set, out satisfied); }
                    catch { satisfied = false; }
                    if (!satisfied)
                    {
                        BruteForce b = new BruteForce();
                        b.SAV = SaveFileEditor.SAV;
                        legal = b.LoadShowdownSetModded_PKSM(illegalPK, Set, resetForm, illegalPK.TID, illegalPK.SID, illegalPK.OT_Name, illegalPK.OT_Gender);
                    }
                    else legal = APIGenerated;
                    legal = alm.SetTrainerData(illegalPK.OT_Name, illegalPK.TID, illegalPK.SID, illegalPK.OT_Gender, legal, satisfied);
                    if (box) BoxData[SaveFileEditor.CurrentBox * SaveFileEditor.SAV.BoxSlotCount + i] = legal;
                    else
                    {
                        PKMEditor.PopulateFields(legal);
                        MessageBox.Show("Legalized Active Pokemon.");
                        return;
                    }
                }
            }
            SaveFileEditor.SAV.BoxData = BoxData;
            SaveFileEditor.ReloadSlots();
            ModMenu.DropDownItems.Remove(alm.menuinstance);
            MessageBox.Show("Legalized Box Pokemon");
        }
    }
}
