using System.Linq;
using System.Windows.Forms;
using PKHeX.Core;

namespace AutoModPlugins
{
    public class LiveHex : AutoModPlugin
    {
        public override string Name => "Open LiveHeX";
        public override int Priority => 1;

        protected override void AddPluginControl(ToolStripDropDownItem modmenu)
        {
            var c1 = new ToolStripMenuItem(Name) { Image = Properties.Resources.wifi };
            c1.Click += (s, e) =>
            {
                if (!(SaveFileEditor.SAV is SAV8SWSH || SaveFileEditor.SAV is SAV7USUM || SaveFileEditor.SAV is SAV7SM || SaveFileEditor.SAV is SAV6AO || SaveFileEditor.SAV is SAV6XY))
                {
                    WinFormsUtil.Error("Must have a 3DS or Switch main line game save file loaded up");
                    return;
                }

                var editor = FirstFormOfType<LiveHexUI>();
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
            while (true)
            {
                var form = FirstFormOfType<LiveHexUI>();
                if (form == null)
                    return;
                form.Close();
            }
        }

        public static T FirstFormOfType<T>() where T : Form => (T)Application.OpenForms.Cast<Form>().FirstOrDefault(form => form is T);
    }
}