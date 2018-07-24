using PKHeX.Core;
using System;
using System.Linq;
using System.Windows.Forms;

namespace LivingDex
{
    public class LivingDex : IPlugin
    {
        public string Name => "Generate Living Dex";
        public int Priority => 1;
        public ISaveFileProvider SaveFileEditor { get; private set; }
        public IPKMView PKMEditor { get; private set; }
        public object[] arguments;

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
                mod.Image = LivingDexResources.menuautolegality;
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
            ctrl.Click += GenLivingDex;
            ctrl.Image = LivingDexResources.livingdex;
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

        private void GenLivingDex(object sender, EventArgs e)
        {
            var bd = SaveFileEditor.SAV.BoxData;

            var tr = SaveFileEditor.SAV;
            for (int i = 1; i <= tr.MaxSpeciesID; i++)
            {
                var pk = SaveFileEditor.SAV.BlankPKM;
                pk.Species = i;
                pk.Gender = pk.GetSaneGender();
                if (i == 678)
                    pk.AltForm = pk.Gender;
                var f = EncounterMovesetGenerator.GeneratePKMs(pk, tr).FirstOrDefault();
                if (f != null)
                {
                    int abilityretain = f.AbilityNumber >> 1;
                    f.Species = i;
                    f.Gender = f.GetSaneGender();
                    if (i == 678)
                        f.AltForm = f.Gender;
                    f.CurrentLevel = 100;
                    f.Nickname = PKX.GetSpeciesNameGeneration(f.Species, f.Language, SaveFileEditor.SAV.Generation);
                    f.IsNicknamed = false;
                    SetSuggestedMoves(f);
                    f.AbilityNumber = abilityretain;
                    f.RefreshAbility(abilityretain);
                    bd[i] = PKMConverter.ConvertToType(f, SaveFileEditor.SAV.PKMType, out _);
                }
            }
            SaveFileEditor.SAV.BoxData = bd;
            SaveFileEditor.ReloadSlots();
        }

        private void SetSuggestedMoves(PKM pkm, bool random = false)
        {
            int[] m = pkm.GetMoveSet(random);
            if (m?.Any(z => z != 0) != true)
            {
                return;
            }

            if (pkm.Moves.SequenceEqual(m))
                return;

            pkm.SetMoves(m);
        }
    }
}
