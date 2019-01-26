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
        public int Priority => 1;
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
                mod.Image = LegalizeBoxesResources.menuautolegality;
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
            ctrl.Click += Legalize;
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
            bool box = (Control.ModifierKeys & Keys.Control) == Keys.Control;
            AutomaticLegality.PKMEditor = PKMEditor;
            AutomaticLegality.SaveFileEditor = SaveFileEditor;
            API.SAV = SaveFileEditor.SAV;
            IList<PKM> BoxData = SaveFileEditor.SAV.BoxData;
            for (int i = 0; i < 30; i++)
            {
                PKM illegalPK = PKMEditor.PreparePKM();

                if (box && BoxData.Count > (SaveFileEditor.CurrentBox * SaveFileEditor.SAV.BoxSlotCount) + i)
                    illegalPK = BoxData[(SaveFileEditor.CurrentBox * SaveFileEditor.SAV.BoxSlotCount) + i];

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
                    bool satisfied;
                    try { APIGenerated = API.APILegality(illegalPK, Set, out satisfied); }
                    catch { satisfied = false; }

                    var trainer = illegalPK.GetTrainerData();
                    if (!satisfied)
                    {
                        BruteForce b = new BruteForce { SAV = SaveFileEditor.SAV };
                        legal = b.LoadShowdownSetModded_PKSM(illegalPK, Set, resetForm, trainer);
                    }
                    else
                    {
                        legal = APIGenerated;
                    }

                    AutoLegalityMod.AutoLegalityMod.SetTrainerData(legal, trainer, satisfied);

                    if (box)
                    {
                        BoxData[(SaveFileEditor.CurrentBox * SaveFileEditor.SAV.BoxSlotCount) + i] = legal;
                    }
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
            MessageBox.Show("Legalized Box Pokemon");
        }
    }
}
