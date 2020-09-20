using System;
using System.Windows.Forms;
using AutoModPlugins.Properties;
using PKHeX.Core;

namespace AutoModPlugins
{
    public class GPSSPlugin : AutoModPlugin
    {
        public override string Name => "GPSS Tools";
        public override int Priority => 2;
        public static string Url => AutoLegality.Default.GPSSBaseURL;

        protected override void AddPluginControl(ToolStripDropDownItem modmenu)
        {
            var ctrl = new ToolStripMenuItem(Name) { Image = Resources.flagbrew };
            ctrl.Name = "Menu_GPSSPlugin";

            var c1 = new ToolStripMenuItem("Upload to GPSS") { Image = Resources.uploadgpss };
            c1.Click += GPSSUpload;
            c1.Name = "Menu_UploadtoGPSS";
            var c2 = new ToolStripMenuItem("Import from GPSS URL") { Image = Resources.mgdbdownload };
            c2.Click += GPSSDownload;
            c2.Name = "Menu_ImportfromGPSSURL";

            ctrl.DropDownItems.Add(c1);
            ctrl.DropDownItems.Add(c2);
            modmenu.DropDownItems.Add(ctrl);
        }

        private void GPSSUpload(object sender, EventArgs e)
        {
            var pk = PKMEditor.PreparePKM();
            byte[] rawdata = pk.Data;
            var postval = PKHeX.Core.Enhancements.NetUtil.GPSSPost(rawdata, Url);
            Clipboard.SetText(postval);
            WinFormsUtil.Alert(postval);
        }

        private void GPSSDownload(object sender, EventArgs e)
        {
            if (Clipboard.ContainsText())
            {
                var txt = Clipboard.GetText();
                if (!txt.Contains("/gpss/"))
                {
                    WinFormsUtil.Error("Invalid URL or incorrect data in the clipboard");
                    return;
                }

                if (!long.TryParse(txt.Split('/')[txt.Split('/').Length - 1], out long code))
                {
                    WinFormsUtil.Error("Invalid URL (wrong code)");
                    return;
                }

                var pkbytes = PKHeX.Core.Enhancements.NetUtil.GPSSDownload(code, Url);
                var pkm = PKMConverter.GetPKMfromBytes(pkbytes);
                if (!LoadPKM(pkm))
                {
                    WinFormsUtil.Error("Error parsing PKM bytes. Make sure the pokemon is valid and can exist in this generation.");
                    return;
                }
                WinFormsUtil.Alert("GPSS Pokemon loaded to PKM Editor");
            }
        }

        private bool LoadPKM(PKM pk)
        {
            pk = PKMConverter.ConvertToType(pk, SaveFileEditor.SAV.PKMType, out _);
            if (pk == null)
                return false;
            PKMEditor.PopulateFields(pk);
            return true;
        }
    }
}
