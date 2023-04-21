using System.Windows.Forms;
using AutoModPlugins.Properties;
using PKHeX.Core;
using PKHeX.Core.Injection;

namespace AutoModPlugins
{
    public class LiveHeX : AutoModPlugin
    {
        public override string Name => "Open LiveHeX";
        public override int Priority => 1;

        protected override void AddPluginControl(ToolStripDropDownItem modmenu)
        {
            var c1 = new ToolStripMenuItem(Name) { Image = Resources.wifi };
            c1.Click += (_, _) =>
            {
                var sav = SaveFileEditor.SAV;
                if (!RamOffsets.IsLiveHeXSupported(sav))
                {
                    WinFormsUtil.Error("Must have a 3DS or Switch main line game save file currently loaded.");
                    return;
                }

                var editor = WinFormsUtil.FirstFormOfType<LiveHeXUI>();
                if (editor == null)
                {
                    editor = new LiveHeXUI(SaveFileEditor, PKMEditor, _settings);
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
            var forms = WinFormsUtil.FormsOfType<LiveHeXUI>();
            foreach (var form in forms)
                form.Close();
        }
    }
}
