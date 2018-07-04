﻿using System;
using System.Windows.Forms;
using PKHeX.Core;
using AutoLegalityMod;

namespace PGLRentalLegality
{
    public class PGLRentalLegality : IPlugin
    {
        public string Name => "Import PGL QR code";
        public int Priority => 1;
        public ISaveFileProvider SaveFileEditor { get; private set; }
        public IPKMView PKMEditor { get; private set; }
        public object[] Arguments { get; private set; }

        public void Initialize(params object[] args)
        {
            Arguments = args;
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
                mod.Image = PGLRentalLegalityResources.menuautolegality;
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
            ctrl.Click += PGLShowdownSet;
            ctrl.Image = PGLRentalLegalityResources.pglqrcode;
            ctrl.ShortcutKeys = (Keys.Alt | Keys.Q);
        }

        private void PGLShowdownSet(object sender, EventArgs e)
        {
            if (!Clipboard.ContainsImage()) return;
            var rentalTeam = new QRParser().decryptQRCode(Clipboard.GetImage());
            string data = "";
            foreach (QRPoke p in rentalTeam.team)
            {
                data += p.ToShowdownFormat(false) + Environment.NewLine + Environment.NewLine;
            }
            Clipboard.SetText(data.TrimEnd());
            AutomaticLegality.ImportModded();
            MessageBox.Show("Exported OwO","Alert");
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
    }
}
