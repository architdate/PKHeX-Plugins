using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows.Forms;
using AutoModPlugins.Properties;
using PKHeX.Core;

namespace AutoModPlugins
{
    public class GPSSPlugin : AutoModPlugin
    {
        public override string Name => "GPSS Tools";
        public override int Priority => 2;
        public static string Url => _settings.GPSSBaseURL;

        protected override void AddPluginControl(ToolStripDropDownItem modmenu)
        {
            var ctrl = new ToolStripMenuItem(Name) {Name = "Menu_GPSSPlugin", Image = Resources.flagbrew};
            var c1 = new ToolStripMenuItem("Upload to GPSS") {Image = Resources.uploadgpss};
            var c2 = new ToolStripMenuItem("Import from GPSS URL") {Image = Resources.mgdbdownload};
            c1.Click += GPSSUpload;
            c1.Name = "Menu_UploadtoGPSS";
            c2.Click += GPSSDownload;
            c2.Name = "Menu_ImportfromGPSSURL";

            ctrl.DropDownItems.Add(c1);
            ctrl.DropDownItems.Add(c2);
            modmenu.DropDownItems.Add(ctrl);
        }

        private async void GPSSUpload(object? sender, EventArgs e)
        {
            var pk = PKMEditor.PreparePKM();
            byte[] rawdata = pk.Data;
            try
            {
                var response = await PKHeX.Core.Enhancements.NetUtil.GPSSPost(rawdata, SaveFileEditor.SAV.Generation, Url);

                var content = await response.Content.ReadAsStringAsync();
                var decoded = JsonSerializer.Deserialize<JsonNode>(content);
                var error = (string)decoded["error"];
                var msg = "";
                var copyToClipboard = false;
                // TODO set proper status codes on FlagBrew side - Allen;
                if (response.IsSuccessStatusCode)
                {
                    if (error != null && error != "no errors")
                    {
                        switch (error)
                        {
                            case "your pokemon is being held for manual review":
                                msg = $"Your pokemon was uploaded to GPSS, however it is being held for manual review. Once approved it will be available at https://{Url}/gpss/{decoded["code"]} (copied to clipboard)";
                                copyToClipboard = true;
                                break;
                            case "Your Pokemon is already uploaded":
                                msg = $"Your pokemon was already uploaded to GPSS, and is available at https://{Url}/gpss/{decoded["code"]} (copied to clipboard)";
                                copyToClipboard = true;
                                break;
                            default:
                                msg = $"Could not upload your Pokemon to GPSS, please try again later or ask Allen if something seems wrong.\n Error details: {decoded["code"]}";
                                break;
                        }
                    }
                    else
                    {
                        msg = $"Pokemon added to the GPSS database. Here is your URL (has been copied to the clipboard):\n https://{Url}/gpss/{decoded["code"]}";
                        copyToClipboard = true;
                    }
                } else if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
                {
                    msg = "Uploading to GPSS is currently disabled, please try again later, or check the FlagBrew discord for more information.";
                } else
                {
                    msg = $"Uploading to GPSS returned an unexpected status code {response.StatusCode}\nError details (if any returned from server): {error}";
                }
                

                if (copyToClipboard)
                {
                    Clipboard.SetText($"https://{Url}/gpss/{decoded["code"]}");
                }
                WinFormsUtil.Alert(msg);
            } catch (Exception ex)
            {
                WinFormsUtil.Alert($"Something went wrong uploading to GPSS.\nError details: {ex.Message}");
            }
        }

        private void GPSSDownload(object? sender, EventArgs e)
        {
            if (Clipboard.ContainsText())
            {
                var txt = Clipboard.GetText();
                if (!txt.Contains("/gpss/"))
                {
                    WinFormsUtil.Error("Invalid URL or incorrect data in the clipboard");
                    return;
                }

                if (!long.TryParse(txt.Split('/')[^1], out long code))
                {
                    WinFormsUtil.Error("Invalid URL (wrong code)");
                    return;
                }

                var pkbytes = PKHeX.Core.Enhancements.NetUtil.GPSSDownload(code, Url);
                if (pkbytes == null)
                {
                    WinFormsUtil.Error("GPSS Download failed");
                    return;
                }
                var pkm = EntityFormat.GetFromBytes(pkbytes, EntityContext.None);
                if (pkm == null || !LoadPKM(pkm))
                {
                    WinFormsUtil.Error("Error parsing PKM bytes. Make sure the pokemon is valid and can exist in this generation.");
                    return;
                }
                WinFormsUtil.Alert("GPSS Pokemon loaded to PKM Editor");
            }
        }

        private bool LoadPKM(PKM pk)
        {
            var result = EntityConverter.ConvertToType(pk, SaveFileEditor.SAV.PKMType, out _);
            if (result == null)
                return false;
            PKMEditor.PopulateFields(result);
            return true;
        }
    }
}
