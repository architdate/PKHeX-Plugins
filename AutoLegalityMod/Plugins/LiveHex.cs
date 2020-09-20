using System.Windows.Forms;
using AutoModPlugins.Properties;
using PKHeX.Core;
using PKHeX.Core.Injection;

namespace AutoModPlugins
{
    public class LiveHex : AutoModPlugin
    {
        public override string Name => "Open LiveHeX";
        public override int Priority => 1;

        protected override void AddPluginControl(ToolStripDropDownItem modmenu)
        {
            var c1 = new ToolStripMenuItem(Name) { Image = Resources.wifi };
            c1.Click += (s, e) =>
            {
                var sav = SaveFileEditor.SAV;
                if (!RamOffsets.IsLiveHexSupported(sav))
                {
                    WinFormsUtil.Error("Must have a 3DS or Switch main line game save file currently loaded.");
                    return;
                }

                var editor = WinFormsUtil.FirstFormOfType<LiveHexUI>();
                if (editor == null)
                {
                    editor = new LiveHexUI(SaveFileEditor, PKMEditor);
                    editor.Show();
                }
                else
                {
                    editor.Focus();
                    // WinFormsUtil.Alert("LiveHeX already open!");
                }
            };
            c1.Name = "Menu_LiveHeX";
            modmenu.DropDownItems.Add(c1);
        }

        public override void NotifySaveLoaded()
        {
            if (SaveFileEditor.SAV is SAV8SWSH)
                return;

            // close any windows & connections to force disconnect
            var forms = WinFormsUtil.FormsOfType<LiveHexUI>();
            foreach (var form in forms)
                form.Close();
        }
    }
}
