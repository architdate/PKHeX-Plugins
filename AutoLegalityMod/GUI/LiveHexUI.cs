using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using AutoModPlugins.GUI;
using PKHeX.Core;
using PKHeX.Core.Injection;

namespace AutoModPlugins
{
    public partial class LiveHeXUI : Form, ISlotViewer<PictureBox>
    {
        private ISaveFileProvider SAV { get; }
        private static GameVersion SAV_Version = GameVersion.Unknown;

        public int ViewIndex => BoxSelect?.SelectedIndex ?? 0;
        public IList<PictureBox> SlotPictureBoxes => throw new InvalidOperationException();
        SaveFile ISlotViewer<PictureBox>.SAV => throw new InvalidOperationException();

        private readonly LiveHeXController Remote;
        private readonly SaveDataEditor<PictureBox> x;
        private readonly PluginSettings _settings;

        private readonly InjectorCommunicationType CurrentInjectionType;

        private readonly ComboBox? BoxSelect; // this is just us holding a reference; disposal is done by its parent

        public LiveHeXUI(ISaveFileProvider sav, IPKMView editor, PluginSettings settings)
        {
            SAV = sav;
            if (SAV.SAV.Version != GameVersion.Invalid)
                SAV_Version = sav.SAV.Version;

            _settings = settings;
            CurrentInjectionType = _settings.USBBotBasePreferred ? InjectorCommunicationType.USB : InjectorCommunicationType.SocketNetwork;
            Remote = new LiveHeXController(sav, editor, CurrentInjectionType, _settings.UseCachedPointers);

            InitializeComponent();
            this.TranslateInterface(WinFormsTranslator.CurrentLanguage);

            TB_IP.Text = _settings.LatestIP;
            var default_port = RamOffsets.IsSwitchTitle(sav.SAV) ? 6000 : 8000; // default port for loaded save
            TB_Port.Text = int.Parse(_settings.LatestPort) is 6000 or 8000 ? default_port.ToString() : _settings.LatestPort;
            SetInjectionTypeView();

            // add an event to the editor
            // ReSharper disable once SuspiciousTypeConversion.Global
            BoxSelect = ((Control)sav).Controls.Find("CB_BoxSelect", true).FirstOrDefault() as ComboBox;
            if (BoxSelect is not null)
            {
                BoxSelect.SelectedIndexChanged += ChangeBox;
                Closing += (_, _) => BoxSelect.SelectedIndexChanged -= ChangeBox;
            }

            var type = sav.GetType();
            var fields = type.GetTypeInfo().DeclaredFields;
            var test = fields.First(z => z.Name == "EditEnv");
            x = (SaveDataEditor<PictureBox>)(test.GetValue(sav) ?? new Exception("Error with LiveHeXUI init."));
            x.Slots.Publisher.Subscribers.Add(this);

            CenterToParent();
        }

        public ISlotInfo GetSlotData(PictureBox view) => throw new InvalidOperationException();
        public int GetViewIndex(ISlotInfo slot) => -1;
        public void NotifySlotOld(ISlotInfo previous) { }

        public void NotifySlotChanged(ISlotInfo slot, SlotTouchType type, PKM pkm)
        {
            if (!checkBox2.Checked || !Remote.Bot.Connected)
                return;

            if (slot is not SlotInfoBox(var box, var slotpkm))
                return;

            if (!type.IsContentChange())
                return;

            Remote.Bot.SendSlot(RamOffsets.WriteBoxData(Remote.Bot.Version) ? pkm.EncryptedBoxData : pkm.EncryptedPartyData, box, slotpkm);
        }

        private void SetTrainerData(SaveFile sav)
        {
            // Check and set trainerdata based on ISaveBlock interfaces
            byte[] dest;
            int startofs = 0;

            Func<PokeSysBotMini, byte[]?> tdata;
            switch (sav)
            {
                case ISaveBlock8SWSH s8:
                    dest = s8.MyStatus.Data;
                    startofs = s8.MyStatus.Offset;
                    tdata = LPBasic.GetTrainerData;
                    break;

                case ISaveBlock7Main s7:
                    dest = s7.MyStatus.Data;
                    startofs = s7.MyStatus.Offset;
                    tdata = LPBasic.GetTrainerData;
                    break;

                case ISaveBlock6Main s6:
                    dest = s6.Status.Data;
                    startofs = s6.Status.Offset;
                    tdata = LPBasic.GetTrainerData;
                    break;

                case SAV7b slgpe:
                    dest = slgpe.Blocks.Status.Data;
                    startofs = slgpe.Blocks.Status.Offset;
                    tdata = LPLGPE.GetTrainerData;
                    break;

                case SAV8BS sbdsp:
                    dest = sbdsp.MyStatus.Data;
                    startofs = sbdsp.MyStatus.Offset;
                    tdata = LPBDSP.GetTrainerData;
                    break;

                case SAV8LA sbla:
                    dest = sbla.MyStatus.Data;
                    startofs = sbla.MyStatus.Offset;
                    tdata = LPPointer.GetTrainerDataLA;
                    break;

                case SAV9SV s9sv:
                    dest = s9sv.MyStatus.Data;
                    startofs = s9sv.MyStatus.Offset;
                    tdata = LPPointer.GetTrainerDataSV;
                    break;

                default:
                    dest = Array.Empty<byte>();
                    tdata = LPBasic.GetTrainerData;
                    break;
            }

            if (dest.Length == 0)
                return;

            var data = tdata(Remote.Bot);
            if (data is null)
                return;

            data.CopyTo(dest, startofs);
        }

        private void ChangeBox(object? sender, EventArgs e)
        {
            if (CB_ReadBox.Checked && Remote.Bot.Connected)
                Remote.ChangeBox(ViewIndex);
        }

        private void B_Connect_Click(object sender, EventArgs e)
        {
            try
            {
                Text = "LiveHeXUI";
                var communicator = RamOffsets.GetCommunicator(SAV.SAV, CurrentInjectionType);
                communicator.IP = TB_IP.Text;
                communicator.Port = int.Parse(TB_Port.Text);
                communicator.Connect();

                var (validation, msg, lv) = (LiveHeXValidation.None, "", LiveHeXVersion.Unknown);
                string gameVer = "0", gameName = "";

                var versions = RamOffsets.GetValidVersions(SAV.SAV).Reverse().ToArray();
                if (communicator is not ICommunicatorNX nx)
                    (validation, msg, lv) = Connect_NTR(communicator, versions);
                else
                    (validation, msg, lv) = Connect_Switch(nx, versions, ref gameVer, ref gameName);

                var currVer = lv is LiveHeXVersion.Unknown ? RamOffsets.GetValidVersions(SAV.SAV).Reverse().ToArray()[0] : lv;
                bool validated = ConnectionValidated(Remote.Bot, gameVer, currVer, validation, msg);
                if (!validated && !_settings.EnableDevMode)
                    return;

                Text = $"Detected: {gameName} ({gameVer})";
                if (_settings.EnableDevMode && lv is LiveHeXVersion.Unknown)
                    Text += " [Forced DevMode]";

                if (Remote.Bot.com is IPokeBlocks)
                {
                    var cblist = GetSortedBlockList(currVer).ToArray();
                    if (cblist.Length > 0)
                    {
                        groupBox5.Enabled = true;
                        CB_BlockName.Items.AddRange(cblist);
                        CB_BlockName.SelectedIndex = 0;
                    }
                }

                if (Remote.Bot.com is ICommunicatorNX)
                    groupBox4.Enabled = groupBox6.Enabled = true;

                if (lv is not LiveHeXVersion.Unknown)
                {
                    // Load current box
                    Remote.ReadBox(SAV.CurrentBox);

                    // Set Trainer Data
                    SetTrainerData(SAV.SAV);
                }
            }
            // Console might be disconnected...
            catch (Exception ex)
            {
                var errorstr = $"{ex.Message}\n\n" +
                                "Click the \"Wiki\" button to troubleshoot.";

                var error = WinFormsUtil.ALMErrorBasic(errorstr);
                error.ShowDialog();

                var res = error.DialogResult;
                if (res == DialogResult.Retry)
                    Process.Start(new ProcessStartInfo { FileName = "https://github.com/architdate/PKHeX-Plugins/wiki/FAQ-and-Troubleshooting#troubleshooting", UseShellExecute = true });
                return;
            }

            B_Connect.Enabled = B_Connect.Visible = TB_IP.Enabled = TB_Port.Enabled = false;
            B_Disconnect.Enabled = B_Disconnect.Visible = groupBox1.Enabled = groupBox2.Enabled = groupBox3.Enabled = true;
        }

        private (LiveHeXValidation, string, LiveHeXVersion) Connect_NTR(ICommunicator com, LiveHeXVersion[] versions)
        {
            foreach (var version in versions)
            {
                Remote.Bot = new PokeSysBotMini(version, com, _settings.UseCachedPointers);
                var data = Remote.Bot.ReadSlot(0, 0);
                var pkm = SAV.SAV.GetDecryptedPKM(data);
                bool valid = pkm.Species <= pkm.MaxSpeciesID && pkm.ChecksumValid &&
                        ((pkm.Species == 0 && pkm.EncryptionConstant == 0) || (pkm.Species > 0 && pkm.Language != (int)LanguageID.Hacked && pkm.Language != (int)LanguageID.UNUSED_6));
                if (valid)
                    return (LiveHeXValidation.None, "", version);
            }

            var saveName = GameInfo.GetVersionName((GameVersion)SAV.SAV.Game);
            var msg = $"Could not find a compatible game version while establishing an NTR connection.\n" +
                      $"Save file loaded: Pokémon {saveName}";
            return (LiveHeXValidation.GameVersion, msg, LiveHeXVersion.Unknown);
        }

        private (LiveHeXValidation, string, LiveHeXVersion) Connect_Switch(ICommunicatorNX nx, LiveHeXVersion[] versions, ref string gameVer, ref string gameName)
        {
            var botbaseVer = nx.GetBotbaseVersion();
            var version = decimal.TryParse(botbaseVer, CultureInfo.InvariantCulture, out var v) ? v : 0;
            if (version < InjectionBase.BotbaseVersion && !_settings.EnableDevMode)
            {
                var msg = $"Incompatible {(nx.Protocol is InjectorCommunicationType.SocketNetwork ? "sys-botbase" : "usb-botbase")} version.\n" +
                          $"Expected version {InjectionBase.BotbaseVersion} or greater, and current version is {version}.\n\n" +
                          $"Please download and install the latest version by clicking the \"Update\" button.";

                return (LiveHeXValidation.Botbase, msg, LiveHeXVersion.Unknown);
            }

            var titleID = nx.GetTitleID();
            gameName = nx.GetGameInfo("name");
            gameVer = nx.GetGameInfo("version").Trim();

            var compatible = InjectionBase.SaveCompatibleWithTitle(SAV.SAV, titleID);
            var lv = compatible ? InjectionBase.GetVersionFromTitle(titleID, gameVer) : LiveHeXVersion.Unknown;
            if (!compatible && !_settings.EnableDevMode)
            {
                var saveName = GameInfo.GetVersionName(SAV_Version);
                var msg = $"Detected game: {gameName} ({gameVer})\n" +
                          $"Save file loaded: Pokémon {saveName}\n\n" +
                          $"Have you selected the correct blank save in PKHeX?";

                if (lv is not LiveHeXVersion.Unknown)
                    gameVer = lv.ToString();
                return (LiveHeXValidation.BlankSAV, msg, LiveHeXVersion.Unknown);
            }

            if (lv is LiveHeXVersion.Unknown && !_settings.EnableDevMode)
            {
                var msg = $"Unsupported version for {gameName}\n\n" +
                          $"Latest supported version is {versions.First()}.\n" +
                          $"Earliest supported version is {versions.Last()}.\n" +
                          $"Detected version is {gameVer}.";
                return (LiveHeXValidation.GameVersion, msg, lv);
            }

            var connect_ver = lv is LiveHeXVersion.Unknown ? RamOffsets.GetValidVersions(SAV.SAV).Reverse().ToArray()[0] : lv;
            Remote.Bot = new PokeSysBotMini(connect_ver, nx, _settings.UseCachedPointers);
            if (lv is LiveHeXVersion.Unknown && _settings.EnableDevMode)
                return (LiveHeXValidation.None, "", lv);

            var data = Remote.Bot.ReadSlot(0, 0);
            PKM? pkm = null;
            try
            {
                pkm = SAV.SAV.GetDecryptedPKM(data);
            }
            catch {}

            bool valid = pkm is not null && pkm.Species <= pkm.MaxSpeciesID && pkm.ChecksumValid &&
                        ((pkm.Species == 0 && pkm.EncryptionConstant == 0) || (pkm.Species > 0 && pkm.Language != (int)LanguageID.Hacked && pkm.Language != (int)LanguageID.UNUSED_6));
            if (!_settings.EnableDevMode && !valid && InjectionBase.CheckRAMShift(Remote.Bot, out string err))
                return (LiveHeXValidation.RAMShift, err, lv);
            return (LiveHeXValidation.None, "", lv);
        }

        private void B_Disconnect_Click(object sender, EventArgs e)
        {
            if (!Remote.Bot.com.Connected)
                return;

            try
            {
                B_Connect.Enabled = B_Connect.Visible = TB_IP.Enabled = TB_Port.Enabled = true;
                B_Disconnect.Enabled = B_Disconnect.Visible = groupBox1.Enabled = groupBox2.Enabled = groupBox3.Enabled = false;
                CB_BlockName.Items.Clear();
                Text = "LiveHeXUI";

                if (Remote.Bot.com is ICommunicatorNX)
                    groupBox4.Enabled = groupBox6.Enabled = groupBox5.Enabled = false;

                Remote.Bot.com.Disconnect();
            }
            catch (Exception ex)
            {
                var error = WinFormsUtil.ALMErrorBasic(ex.Message);
                error.ShowDialog();
            }
        }

        private void LiveHeXUI_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (Remote.Bot.Connected)
                Remote.Bot.com.Disconnect();

            x.Slots.Publisher.Subscribers.Remove(this);
            _settings.LatestIP = TB_IP.Text;
            _settings.LatestPort = TB_Port.Text;
            _settings.Save();
        }

        private void B_ReadCurrent_Click(object sender, EventArgs e) => Remote.ReadBox(SAV.CurrentBox);
        private void B_WriteCurrent_Click(object sender, EventArgs e) => Remote.WriteBox(SAV.CurrentBox);
        private void B_ReadSlot_Click(object sender, EventArgs e) => Remote.ReadActiveSlot((int)NUD_Box.Value - 1, (int)NUD_Slot.Value - 1);
        private void B_WriteSlot_Click(object sender, EventArgs e) => Remote.WriteActiveSlot((int)NUD_Box.Value - 1, (int)NUD_Slot.Value - 1);

        private void B_ReadOffset_Click(object sender, EventArgs e)
        {
            bool readPointer = (ModifierKeys & Keys.Control) == Keys.Control;
            var txt = TB_Offset.Text;
            var offset = readPointer && Remote.Bot.com is ICommunicatorNX nx ? Remote.Bot.GetCachedPointer(nx, TB_Pointer.Text) : Util.GetHexValue64(txt);
            if ((offset.ToString("X16") != txt.ToUpper().PadLeft(16, '0') && !readPointer) || offset == InjectionUtil.INVALID_PTR)
            {
                WinFormsUtil.Alert("Specified offset is not a valid hex string.");
                return;
            }
            try
            {
                var method = RWMethod.Heap;
                if (RB_Main.Checked) method = RWMethod.Main;
                if (RB_Absolute.Checked) method = RWMethod.Absolute;
                var result = Remote.ReadOffset(offset, method);
                if (!result)
                    WinFormsUtil.Alert("No valid data is located at the specified offset.");
            }
            catch (Exception ex)
            {
                WinFormsUtil.Error("Unable to load data from the specified offset.", ex.Message);
            }
        }

        private RWMethod GetRWMethod()
        {
            if (RB_Main.Checked)
                return RWMethod.Main;
            if (RB_Absolute.Checked)
                return RWMethod.Absolute;
            return RWMethod.Heap;
        }

        private void B_ReadRAM_Click(object sender, EventArgs e)
        {
            var txt = RamOffset.Text;
            var offset = Util.GetHexValue64(txt);
            var valid = int.TryParse(RamSize.Text, out int size);
            if (offset.ToString("X16") != txt.ToUpper().PadLeft(16, '0') || !valid)
            {
                WinFormsUtil.Alert("Make sure that the RAM offset is a hex string and the size is a valid integer.");
                return;
            }

            try
            {
                byte[] result;
                if (Remote.Bot.com is not ICommunicatorNX cnx)
                    result = Remote.ReadRAM(offset, size);
                else if (RB_Main.Checked)
                    result = cnx.ReadBytesMain(offset, size);
                else if (RB_Absolute.Checked)
                    result = cnx.ReadBytesAbsolute(offset, size);
                else
                    result = Remote.ReadRAM(offset, size);

                bool blockview = (ModifierKeys & Keys.Control) == Keys.Control;
                PKM? pkm = null;
                if (blockview)
                {
                    pkm = SAV.SAV.GetDecryptedPKM(result);
                    if (!pkm.ChecksumValid)
                        blockview = false;
                }

                using var form = new SimpleHexEditor(result, Remote.Bot, offset, GetRWMethod());
                var loadgrid = blockview && ReflectUtil.GetPropertiesCanWritePublicDeclared(pkm!.GetType()).Count() > 1;
                if (loadgrid)
                {
                    form.PG_BlockView.Visible = true;
                    form.PG_BlockView.SelectedObject = pkm;
                }

                var res = form.ShowDialog();
                if (res != DialogResult.OK)
                    return;

                if (loadgrid)
                {
                    PKM pk = pkm!;
                    var pkmbytes = RamOffsets.WriteBoxData(Remote.Bot.Version) ? pk.EncryptedBoxData : pk.EncryptedPartyData;
                    if (pkmbytes.Length == Remote.Bot.SlotSize)
                        form.Bytes = pkmbytes;
                    else
                    {
                        form.Bytes = result;
                        WinFormsUtil.Error("Size mismatch. Please report this issue on the Discord server.");
                    }
                }

                var modifiedRAM = form.Bytes;
                if (Remote.Bot.com is not ICommunicatorNX nx)
                    Remote.WriteRAM(offset, modifiedRAM);
                else if (RB_Main.Checked)
                    nx.WriteBytesMain(modifiedRAM, offset);
                else if (RB_Absolute.Checked)
                    nx.WriteBytesAbsolute(modifiedRAM, offset);
                else
                    Remote.WriteRAM(offset, modifiedRAM);

                Debug.WriteLine("RAM Modified");
            }
            catch (Exception ex)
            {
                WinFormsUtil.Error("Unable to load data from the specified offset.", ex.Message);
            }
        }

        private IEnumerable<string> GetSortedBlockList(LiveHeXVersion lv)
        {
            if (Remote.Bot.Injector is LPBasic)
            {
                if (!LPBasic.SCBlocks.ContainsKey(lv))
                    return new List<string>();

                var blks = LPBasic.SCBlocks[lv].Select(z => z.Display).Distinct().OrderBy(z => z);
                return blks;
            }

            if (Remote.Bot.Injector is LPBDSP)
            {
                var save_blocks = LPBDSP.FunctionMap.Keys;
                var custom_blocks = LPBDSP.types.Select(t => t.Name);
                var blks = save_blocks.Concat(custom_blocks).OrderBy(z => z);
                return blks;
            }

            if (Remote.Bot.Injector is LPPointer)
            {
                var save_blocks = LPPointer.SCBlocks[lv].Select(z => z.Display).Distinct();
                var blks = save_blocks.OrderBy(z => z);
                return blks;
            }

            return new List<string>();
        }

        private void SetInjectionTypeView()
        {
            TB_IP.Visible = L_IP.Visible = CurrentInjectionType == InjectorCommunicationType.SocketNetwork;
            L_USBState.Visible = CurrentInjectionType == InjectorCommunicationType.USB;
        }

        public ulong GetPointerAddress(ICommunicatorNX sb)
        {
            var ptr = TB_Pointer.Text.Contains("[key]") ? TB_Pointer.Text.Replace("[key]", "").Trim() : TB_Pointer.Text.Trim();
            var address = Remote.Bot.GetCachedPointer(sb, ptr, false);
            return address;
        }

        private void B_CopyAddress_Click(object sender, EventArgs e)
        {
            if (Remote.Bot.com is not ICommunicatorNX sb)
                return;

            ulong address = GetPointerAddress(sb);
            if (address == 0)
            {
                WinFormsUtil.Alert("No pointer address.");
                return;
            }

            ulong heap = sb.GetHeapBase();
            address -= heap;

            Clipboard.SetText(address.ToString("X"));
            bool getDetails = (ModifierKeys & Keys.Control) == Keys.Control;
            if (getDetails)
                Clipboard.SetText($"Absolute Address: {address + heap:X}\nHeap Address: {address:X}\nHeap Base: {heap:X}");
        }

        private void B_EditPointerData_Click(object sender, EventArgs e)
        {
            if (Remote.Bot.com is not ICommunicatorNX sb)
                return;

            ulong address;
            int size;
            uint keyval = 0;

            var blk_key = TB_Pointer.Text.Contains("[key]");
            if (!blk_key)
            {
                address = GetPointerAddress(sb);
                if (address == 0)
                {
                    WinFormsUtil.Alert("No pointer address.");
                    return;
                }

                var valid = int.TryParse(RamSize.Text, out size);
                if (!valid)
                {
                    WinFormsUtil.Alert("Make sure that the size is a valid integer.");
                    return;
                }
            }
            else
            {
                var key = TB_Pointer.Text.Replace("[key]", "").Trim();
                if (!uint.TryParse(key, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out keyval))
                {
                    WinFormsUtil.Alert("Key must be in a hexadecimal format.");
                    return;
                }

                if (keyval == 0)
                {
                    WinFormsUtil.Alert("Make sure that the provided key is valid.");
                    return;
                }

                try
                {
                    (address, size) = ReadKey(Remote.Bot, keyval);
                }
                catch (Exception ex)
                {
                    WinFormsUtil.Alert(ex.Message);
                    return;
                }
            }

            try
            {
                var header = 0;
                byte[] result = sb.ReadBytesAbsolute(address, size);
                if (blk_key)
                {
                    bool typeView = (ModifierKeys & Keys.Alt) == Keys.Alt;
                    var block = SCBlock.ReadFromOffset(result, keyval, ref header);

                    if (block.Type.IsBoolean())
                    {
                        WinFormsUtil.Alert($"SCBlock is set to {block.Type}.");
                        return;
                    }

                    if (typeView)
                        WinFormsUtil.Alert($"Block type is {block.Type}.");

                    result = block.Data;
                }

                bool blockview = (ModifierKeys & Keys.Control) == Keys.Control;
                PKM? pkm = null;
                if (blockview)
                {
                    pkm = SAV.SAV.GetDecryptedPKM(result);
                    if (!pkm.ChecksumValid)
                        blockview = false;
                }

                using (var form = new SimpleHexEditor(result, Remote.Bot, address, RWMethod.Absolute, blk_key, keyval, header))
                {
                    var loadgrid = blockview && ReflectUtil.GetPropertiesCanWritePublicDeclared(pkm!.GetType()).Count() > 1;
                    if (loadgrid)
                    {
                        form.PG_BlockView.Visible = true;
                        form.PG_BlockView.SelectedObject = pkm;
                    }

                    var res = form.ShowDialog();
                    if (res == DialogResult.OK)
                    {
                        if (loadgrid)
                        {
                            PKM pk = pkm!;
                            var pkmbytes = RamOffsets.WriteBoxData(Remote.Bot.Version) ? pk.EncryptedBoxData : pk.EncryptedPartyData;
                            if (pkmbytes.Length == Remote.Bot.SlotSize)
                                form.Bytes = pkmbytes;
                            else
                            {
                                form.Bytes = result;
                                WinFormsUtil.Error("Size mismatch. Please report this issue on the discord server.");
                            }
                        }
                        var modifiedRAM = form.Bytes;
                        sb.WriteBytesAbsolute(modifiedRAM, address);
                    }
                }
                Debug.WriteLine("RAM Modified");
            }
            catch (Exception ex)
            {
                WinFormsUtil.Error("Unable to load data from the specified offset.", ex.Message);
            }
        }

        private void B_ReadPointer_Click(object sender, EventArgs e)
        {
            if (Remote.Bot.com is not ICommunicatorNX sb)
                return;

            ulong address = GetPointerAddress(sb);
            if (address == 0)
            {
                WinFormsUtil.Alert("No pointer address.");
                return;
            }

            var size = Remote.Bot.SlotSize;
            var data = sb.ReadBytesAbsolute(address, size);
            var pkm = SAV.SAV.GetDecryptedPKM(data);

            // Since data might not actually exist at the user-specified offset, double check that the pkm data is valid.
            if (pkm.ChecksumValid)
                Remote.Editor.PopulateFields(pkm);
        }

        private void B_EditBlock_Click(object sender, EventArgs e)
        {
            var txt = CB_BlockName.Text;
            var version = Remote.Bot.Version;

            var valid = ReadBlock(Remote.Bot, SAV.SAV, txt, out var data);
            if (!valid || data is null)
            {
                WinFormsUtil.Error("Invalid Entry");
                return;
            }

            var objects = new List<object>();
            for (var i = 0; i < data.Count; i++)
            {
                var obj = data[i];
                valid = TryGetObjectInSave(version, SAV.SAV, txt, i, obj, out var blk);
                if (!valid || blk == null)
                {
                    WinFormsUtil.Error("Error fetching Block");
                    return;
                }
                objects.Add(blk);
            }
            var sb = objects[0];

            var write = false;
            if (txt.IsSpecialBlock(Remote.Bot, out var v) && v is not null)
            {
                // ReSharper disable once SuspiciousTypeConversion.Global
                var cc = (ContainerControl)SAV;

                // Set sender
                var s = txt switch
                {
                    "Raid" => cc.Controls.Find("B_Raids", true)[0],
                    "RaidArmor" => cc.Controls.Find("B_RaidArmor", true)[0],
                    _ => sender,
                };

                // Invoke function
                cc.GetType().GetMethod(v, BindingFlags.NonPublic | BindingFlags.Instance)?.Invoke(cc, new[] { s, e });

                for (var i = 0; i <  objects.Count; i++)
                {
                    if (objects[i] is not SCBlock scb)
                        write = true;
                    else if (!scb.Data.SequenceEqual(data[i]))
                        write = true;
                    if (write)
                        break;
                }
            }
            else if (sb is SCBlock || sb is IDataIndirect || sb is ICustomBlock)
            {
                // Must be single block output
                using var form = new SimpleHexEditor(data[0]);
                if (sb is IDataIndirect || sb is ICustomBlock)
                {
                    var props = ReflectUtil.GetPropertiesCanWritePublicDeclared(sb.GetType());
                    if (props.Count() > 1 && ModifierKeys != Keys.Control)
                    {
                        form.PG_BlockView.Visible = true;
                        form.PG_BlockView.SelectedObject = sb;
                    }
                }
                else
                {
                    var o = SCBlockMetadata.GetEditableBlockObject((SCBlock)sb);
                    if (o is not null)
                    {
                        form.PG_BlockView.Visible = true;
                        form.PG_BlockView.SelectedObject = o;
                    }
                }
                var res = form.ShowDialog();
                write = res == DialogResult.OK;
            }

            if (!write)
                return;

            if (Remote.Bot.Injector is LPBDSP)
                Remote.Bot.Injector.WriteBlockFromString(Remote.Bot, txt, GetBlockDataRaw(sb!, data[0]), sb!);
            else
                Remote.Bot.Injector.WriteBlocksFromSAV(Remote.Bot, txt, SAV.SAV);
        }

        private static byte[] GetBlockDataRaw(object sb, byte[] data) => sb switch
        {
            SCBlock sc => sc.Data,
            IDataIndirect sv => sv.Data,
            _ => data,
        };

        private static bool ReadBlock(PokeSysBotMini bot, SaveFile sav, string display, out List<byte[]>? data)
        {
            return bot.Injector.ReadBlockFromString(bot, sav, display, out data);
        }

        private static (ulong Offset, int Length) ReadKey(PokeSysBotMini bot, uint keyval)
        {
            var version = bot.Version;
            string sbptr = LPPointer.GetSaveBlockPointer(version);

            if (sbptr.Length == 0)
                throw new Exception($"Pointer is not documented for searching block keys in {version}.");

            if (bot.com is not ICommunicatorNX nx)
                throw new Exception("Remote connection type is unable to read data from absolute offsets.");

            var ofs = bot.SearchSaveKey(sbptr, keyval);
            if (ofs == 0)
                throw new Exception($"Unable to find block key 0x{keyval:X8}");

            var dt = nx.ReadBytesAbsolute(ofs + 8, 8);
            ofs = BitConverter.ToUInt64(dt);

            var headerAfterKey = nx.ReadBytesAbsolute(ofs, 6);
            int size = SCBlock.GetTotalLength(headerAfterKey, keyval);
            return (ofs, size);
        }

        private bool TryGetObjectInSave(LiveHeXVersion version, SaveFile sav, string display, int index, byte[]? customdata, out object? sb)
        {
            sb = null;
            if (Remote.Bot.Injector is LPBDSP)
            {
                var prop = sav.GetType().GetProperty(display);
                if (prop is not null)
                    sb = prop.GetValue(sav);
                else
                    sb = Activator.CreateInstance(LPBDSP.types.First(t => t.Name == display), customdata);
            }
            else
            {
                var subblocks = Remote.Bot.Injector switch
                {
                    LPBasic => LPBasic.SCBlocks[version].Where(z => z.Display == display).ToArray(),
                    LPPointer => LPPointer.SCBlocks[version].Where(z => z.Display == display).ToArray(),
                    _ => Array.Empty<BlockData>(),
                };

                if (subblocks.Length == 0)
                    return false;

                // Check for SCBlocks or SaveBlocks based on name. (SCBlocks will invoke the hex editor, SaveBlocks will invoke a property grid
                var props = sav.GetType().GetProperty("Blocks");
                if (props is null)
                    return false;

                var allblocks = props.GetValue(sav) ?? throw new Exception("Blocks not present.");
                var blockprop = allblocks.GetType().GetProperty(subblocks[index].Name);
                if (allblocks is SCBlockAccessor scba && blockprop is null)
                    sb = scba.GetBlock(subblocks[index].SCBKey);
                else
                    sb = blockprop?.GetValue(allblocks);
            }

            return sb is not null;
        }

        private bool ConnectionValidated(PokeSysBotMini psb, string gameVer, LiveHeXVersion version, LiveHeXValidation validation, string msg)
        {
            if (psb.com is not ICommunicatorNX nx)
            {
                if (msg != "")
                {
                    var error = WinFormsUtil.ALMErrorBasic(msg);
                    error.ShowDialog();

                    var res = error.DialogResult;
                    if (res == DialogResult.Retry)
                        Process.Start(new ProcessStartInfo { FileName = "https://github.com/architdate/PKHeX-Plugins/wiki/FAQ-and-Troubleshooting#troubleshooting", UseShellExecute = true });
                    return false;
                }
                return true;
            }

            var url = validation switch
            {
                LiveHeXValidation.Botbase when nx.Protocol is InjectorCommunicationType.SocketNetwork => "https://github.com/olliz0r/sys-botbase/releases/latest",
                LiveHeXValidation.Botbase when nx.Protocol is InjectorCommunicationType.USB => "https://github.com/Koi-3088/usb-botbase/releases/latest",
                LiveHeXValidation.BlankSAV => "https://github.com/architdate/PKHeX-Plugins/wiki/FAQ-and-Troubleshooting#pkhex-plugins-is-telling-me-that-the-detected-game-does-not-match-the-current-save-file-the-top-of-the-window-says-forced-for-the-game-version",
                LiveHeXValidation.GameVersion => "https://github.com/architdate/PKHeX-Plugins/wiki/FAQ-and-Troubleshooting#pkhex-plugins-is-telling-me-that-the-detected-game-does-not-match-the-current-save-file-the-top-of-the-window-says-forced-for-the-game-version",
                LiveHeXValidation.RAMShift => "https://github.com/architdate/PKHeX-Plugins/wiki/FAQ-and-Troubleshooting#pkhex-plugins-is-telling-me-that-a-possible-ram-shift-is-detected",
                _ => "https://github.com/architdate/PKHeX-Plugins/wiki/FAQ-and-Troubleshooting#troubleshooting",
            };

            switch (validation)
            {
                case LiveHeXValidation.Botbase:
                    {
                        var error = WinFormsUtil.ALMErrorBasic(msg, true);
                        error.ShowDialog();

                        var res = error.DialogResult;
                        if (res == DialogResult.Retry)
                            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
                        return false;
                    };
                case LiveHeXValidation.BlankSAV or LiveHeXValidation.GameVersion:
                    {
                        Remote.Bot = new PokeSysBotMini(version, nx, _settings.UseCachedPointers);
                        Text += $" SAV/Version (Detected: {gameVer} | Forced: {version})";

                        var error = WinFormsUtil.ALMErrorBasic(msg);
                        error.ShowDialog();

                        var res = error.DialogResult;
                        if (res == DialogResult.Retry)
                            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
                        return false;
                    };
                case LiveHeXValidation.RAMShift:
                    {
                        Text += $" Possible RAM Shift | Detected: {version}";
                        var error = WinFormsUtil.ALMErrorBasic(msg);
                        error.ShowDialog();

                        var res = error.DialogResult;
                        if (res == DialogResult.Retry)
                            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
                        return false;
                    };
            };

            return true;
        }
    }

    internal class HexTextBox : TextBox
    {
        private const int WM_PASTE = 0x0302;

        protected override void WndProc(ref Message m)
        {
            Debug.WriteLine(m.Msg);
            if (m.Msg == WM_PASTE)
            {
                var text = Clipboard.GetText();
                if (text.StartsWith("0x"))
                {
                    text = text[2..];
                    Clipboard.SetText(text);
                }
            }

            base.WndProc(ref m);
        }
    }
}
