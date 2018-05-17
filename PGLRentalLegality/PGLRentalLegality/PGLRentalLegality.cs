using System;
using System.Windows.Forms;
using PKHeX.Core;

namespace PGLRentalLegality
{
    public class PGLRentalLegality : IPlugin
    {
        public string Name => nameof(PGLRentalLegality);
        public ISaveFileProvider SaveFileEditor { get; private set; }
        public IPKMView PKMEditor { get; private set; }

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
            ctrl.Click += new EventHandler(PGLShowdownSet);
        }

        private void PGLShowdownSet(object sender, EventArgs e)
        {
            if (!Clipboard.ContainsImage()) return;
            PKHeX.WinForms.Misc.RentalTeam rentalTeam = new PKHeX.WinForms.Misc.QRParser().decryptQRCode(Clipboard.GetImage());
            string data = "";
            foreach (PKHeX.WinForms.Misc.Pokemon p in rentalTeam.team)
            {
                data += p.ToShowdownFormat(false) + "\n\r";
            }

            Clipboard.SetText(data.TrimEnd());
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
