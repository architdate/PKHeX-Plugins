using System;
using System.Diagnostics;
using System.Windows.Forms;
using AutoModPlugins.Properties;
using PKHeX.Core;
using PKHeX.Core.AutoMod;

namespace AutoModPlugins
{
    public class LegalizeBoxes : AutoModPlugin
    {
        public override string Name => "Legalize Active Pokemon";
        public override int Priority => 1;

        protected override void AddPluginControl(ToolStripDropDownItem modmenu)
        {
            var ctrl = new ToolStripMenuItem(Name) { Image = Resources.legalizeboxes };
            ctrl.Click += Legalize;
            ctrl.Name = "Menu_LeaglizeBoxes";
            modmenu.DropDownItems.Add(ctrl);
        }

        private void Legalize(object? sender, EventArgs e)
        {
            try
            {
                var box = (Control.ModifierKeys & Keys.Control) == Keys.Control;
                if (!box)
                {
                    LegalizeActive();
                    return;
                }

                var all = (Control.ModifierKeys & Keys.Shift) == Keys.Shift;
                if (!all)
                    LegalizeCurrent();
                else
                    LegalizeAllBoxes();
            }
            catch (MissingMethodException)
            {
                var errorstr = "The PKHeX-Plugins version does not match the PKHeX version. \nRefer to the Wiki for how to fix this error.\n\n" +
                              $"The current ALM Version is {ALMVersion.Versions.AlmVersionCurrent}\n" +
                              $"The current PKHeX Version is {ALMVersion.Versions.CoreVersionCurrent}";

                var res = WinFormsUtil.ALMErrorBasic(errorstr);
                if (res == DialogResult.Retry)
                    Process.Start(new ProcessStartInfo { FileName = "https://github.com/architdate/PKHeX-Plugins/wiki/Installing-PKHeX-Plugins", UseShellExecute = true });
            }
        }

        private void LegalizeCurrent()
        {
            var sav = SaveFileEditor.SAV;
            var count = sav.LegalizeBox(sav.CurrentBox);
            if (count <= 0) // failed to modify anything
                return;
            SaveFileEditor.ReloadSlots();
            WinFormsUtil.Alert($"Legalized {count} Pokémon in Current Box!");
        }

        private void LegalizeAllBoxes()
        {
            var sav = SaveFileEditor.SAV;
            var count = sav.LegalizeBoxes();
            if (count <= 0) // failed to modify anything
                return;
            SaveFileEditor.ReloadSlots();
            WinFormsUtil.Alert($"Legalized {count} Pokémon across all boxes!");
        }

        private void LegalizeActive()
        {
            var pk = PKMEditor.PreparePKM();
            var la = new LegalityAnalysis(pk);
            if (la.Valid)
                return; // already valid, don't modify it

            var sav = SaveFileEditor.SAV;
            var result = sav.Legalize(pk);

            // let's double check

            la = new LegalityAnalysis(result);
            if (!la.Valid)
            {
                var res = WinFormsUtil.ALMErrorBasic(
                    "Unable to make the Active Pokemon legal!\n\n" +
                    "No legal Pokémon matches the provided traits.\n\n" +
                    "Visit the Wiki to learn how to import Showdown Sets.");

                if (res == DialogResult.Retry)
                    Process.Start(new ProcessStartInfo { FileName = "https://github.com/architdate/PKHeX-Plugins/wiki/Getting-Started-with-Auto-Legality-Mod", UseShellExecute = true });
                return;
            }

            PKMEditor.PopulateFields(result);
            WinFormsUtil.Alert("Legalized Active Pokemon!");
        }
    }
}
