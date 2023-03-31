using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using AutoModPlugins.GUI;
using AutoModPlugins.Properties;
using PKHeX.Core;
using PKHeX.Core.Injection;

namespace AutoModPlugins
{
    public partial class LiveHeXUI : Form, ISlotViewer<PictureBox>
    {
        private ISaveFileProvider SAV { get; }

        public int ViewIndex => BoxSelect?.SelectedIndex ?? 0;
        public IList<PictureBox> SlotPictureBoxes => throw new InvalidOperationException();
        SaveFile ISlotViewer<PictureBox>.SAV => throw new InvalidOperationException();

        private readonly LiveHeXController Remote;
        private readonly SaveDataEditor<PictureBox> x;

        private readonly InjectorCommunicationType CurrentInjectionType;

        private readonly ComboBox? BoxSelect; // this is just us holding a reference; disposal is done by its parent

        public LiveHeXUI(ISaveFileProvider sav, IPKMView editor)
        {
            SAV = sav;
            CurrentInjectionType = AutoLegality.Default.USBBotBasePreferred ? InjectorCommunicationType.USB : InjectorCommunicationType.SocketNetwork;
            Remote = new LiveHeXController(sav, editor, CurrentInjectionType);

            InitializeComponent();
            this.TranslateInterface(WinFormsTranslator.CurrentLanguage);

            TB_IP.Text = AutoLegality.Default.LatestIP;
            TB_Port.Text = AutoLegality.Default.LatestPort;
            SetInjectionTypeView();

            // add an event to the editor
            // ReSharper disable once SuspiciousTypeConversion.Global
            BoxSelect = ((Control)sav).Controls.Find("CB_BoxSelect", true).FirstOrDefault() as ComboBox;
            if (BoxSelect != null)
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

        private void SetTrainerData(SaveFile sav)
        {
            // Check and set trainerdata based on ISaveBlock interfaces
            byte[] dest;
            int startofs = 0;

            //byte[] config;
            //int configstart = 0;

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

                    //configstart = sbdsp.Config.Offset;
                    //config = sbdsp.Config.Data;
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
            if (data == null)
                return;
            data.CopyTo(dest, startofs);
        }
        
        private static bool ChangeBoxAlreadyCalled = false;

        private void ChangeBox(object? sender, EventArgs e)
        {
            if (ChangeBoxAlreadyCalled)
                return;
            ChangeBoxAlreadyCalled = true;
            if (checkBox1.Checked && Remote.Bot.Connected)
                Remote.ChangeBox(ViewIndex);
            ChangeBoxAlreadyCalled = false;
        }

        private void B_Connect_Click(object sender, EventArgs e)
        {
            try
            {
                // Enable controls
                var ConnectionEstablished = false;
                var validversions = RamOffsets.GetValidVersions(SAV.SAV).Reverse().ToArray();
                var currver = validversions[0];
                foreach (var version in validversions)
                {
                    Remote.Bot = new PokeSysBotMini(version, CurrentInjectionType)
                    {
                        com = { IP = TB_IP.Text, Port = int.Parse(TB_Port.Text) },
                    };
                    Remote.Bot.com.Connect();

                    var data = Remote.Bot.ReadSlot(1, 1);
                    var pkm = SAV.SAV.GetDecryptedPKM(data);
                    if (pkm.ChecksumValid)
                    {
                        ConnectionEstablished = true;
                        currver = version;
                        if (Remote.Bot.com is IPokeBlocks)
                        {
                            var cblist = GetSortedBlockList(version).ToArray();
                            if (cblist.Length > 0)
                            {
                                groupBox5.Enabled = true;
                                CB_BlockName.Items.AddRange(cblist);
                                CB_BlockName.SelectedIndex = 0;
                            }
                        }
                        break;
                    }
                }

                if (!ConnectionEstablished)
                {
                    Remote.Bot = new PokeSysBotMini(currver, CurrentInjectionType)
                    {
                        com = { IP = TB_IP.Text, Port = int.Parse(TB_Port.Text) },
                    };
                    Remote.Bot.com.Connect();
                    Text += $" Unknown Version (Forced: {currver})";
                }
                else
                {
                    Text += $" Detected Version: {currver}";
                }

                if (Remote.Bot.com is ICommunicatorNX)
                    groupBox4.Enabled = groupBox6.Enabled = true;

                // Load current box
                Remote.ReadBox(SAV.CurrentBox);

                // Set Trainer Data
                SetTrainerData(SAV.SAV);
            }
            // Console might be disconnected...
            catch (Exception ex)
            {
                WinFormsUtil.Error(ex.Message);
                return;
            }
            B_Connect.Enabled = TB_IP.Enabled = TB_Port.Enabled = false;
            groupBox1.Enabled = groupBox2.Enabled = groupBox3.Enabled = true;
        }

        private void LiveHeXUI_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (Remote.Bot.Connected)
                Remote.Bot.com.Disconnect();
            x.Slots.Publisher.Subscribers.Remove(this);

            AutoLegality.Default.LatestIP = TB_IP.Text;
            AutoLegality.Default.LatestPort = TB_Port.Text;
            AutoLegality.Default.Save();
        }

        private void B_ReadCurrent_Click(object sender, EventArgs e) => Remote.ReadBox(SAV.CurrentBox);
        private void B_WriteCurrent_Click(object sender, EventArgs e) => Remote.WriteBox(SAV.CurrentBox);
        private void B_ReadSlot_Click(object sender, EventArgs e) => Remote.ReadActiveSlot((int)NUD_Box.Value - 1, (int)NUD_Slot.Value - 1);
        private void B_WriteSlot_Click(object sender, EventArgs e) => Remote.WriteActiveSlot((int)NUD_Box.Value - 1, (int)NUD_Slot.Value - 1);

        private void B_ReadOffset_Click(object sender, EventArgs e)
        {
            bool readPointer = (ModifierKeys & Keys.Control) == Keys.Control;
            var txt = TB_Offset.Text;
            var offset = readPointer && Remote.Bot.com is ICommunicatorNX nx ? nx.GetPointerAddress(TB_Pointer.Text) : Util.GetHexValue64(txt);
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
                WinFormsUtil.Alert("Make sure that the RAM offset is a hex string and the size is a valid integer");
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
                    {
                        form.Bytes = pkmbytes;
                    }
                    else
                    {
                        form.Bytes = result;
                        WinFormsUtil.Error("Size mismatch. Please report this issue on the discord server.");
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

        private static IEnumerable<string> GetSortedBlockList(LiveHeXVersion lv)
        {
            if (LPBasic.SupportedVersions.Contains(lv))
            {
                if (!LPBasic.SCBlocks.ContainsKey(lv))
                    return new List<string>();
                return LPBasic.SCBlocks[lv].Select(z => z.Display).Distinct().OrderBy(z => z);
            }
            if (LPBDSP.SupportedVersions.Contains(lv))
            {
                var save_blocks = LPBDSP.FunctionMap.Keys;
                var custom_blocks = LPBDSP.types.Select(t => t.Name);
                return save_blocks.Concat(custom_blocks).OrderBy(z => z);
            }
            if (LPPointer.SupportedVersions.Contains(lv))
            {
                var save_blocks = LPPointer.SCBlocks[lv].Select(z => z.Display).Distinct();
                return save_blocks.OrderBy(z => z);
            }
            return new List<string>();
        }

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

        public ISlotInfo GetSlotData(PictureBox view) => throw new InvalidOperationException();
        public int GetViewIndex(ISlotInfo slot) => -1;

        private void SetInjectionTypeView()
        {
            TB_IP.Visible = L_IP.Visible = CurrentInjectionType == InjectorCommunicationType.SocketNetwork;
            L_USBState.Visible = CurrentInjectionType == InjectorCommunicationType.USB;
        }

        public ulong GetPointerAddress(ICommunicatorNX sb)
        {
            var ptr = TB_Pointer.Text.Trim();
            var address = sb.GetPointerAddress(ptr, false);
            if (address == InjectionUtil.INVALID_PTR)
                WinFormsUtil.Alert("Invalid Pointer");
            return address;
        }

        private void B_CopyAddress_Click(object sender, EventArgs e)
        {
            if (Remote.Bot.com is not ICommunicatorNX sb)
                return;

            ulong address = GetPointerAddress(sb);
            if (address == 0)
                WinFormsUtil.Alert("No pointer address.");
            ulong heap = sb.GetHeapBase();
            address -= heap;

            Clipboard.SetText(address.ToString("X"));
            bool getDetails = (ModifierKeys & Keys.Control) == Keys.Control;
            if (getDetails) Clipboard.SetText($"Absolute Address: {address + heap:X}\nHeap Address: {address:X}\nHeap Base: {heap:X}");
        }

        private void B_EditPointerData_Click(object sender, EventArgs e)
        {
            if (Remote.Bot.com is not ICommunicatorNX sb)
                return;

            ulong address = GetPointerAddress(sb);
            if (address == 0)
                WinFormsUtil.Alert("No pointer address.");

            var valid = int.TryParse(RamSize.Text, out int size);
            if (!valid)
            {
                WinFormsUtil.Alert("Make sure that the size is a valid integer");
                return;
            }

            try
            {
                var result = sb.ReadBytesAbsolute(address, size);
                bool blockview = (ModifierKeys & Keys.Control) == Keys.Control;
                PKM? pkm = null;
                if (blockview)
                {
                    pkm = SAV.SAV.GetDecryptedPKM(result);
                    if (!pkm.ChecksumValid)
                        blockview = false;
                }
                using (var form = new SimpleHexEditor(result, Remote.Bot, address, RWMethod.Absolute))
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
                            {
                                form.Bytes = pkmbytes;
                            }
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
                WinFormsUtil.Alert("No pointer address.");

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
            if (!valid || data == null)
            {
                WinFormsUtil.Error("Invalid Entry");
                return;
            }

            valid = TryGetObjectInSave(version, SAV.SAV, txt, data[0], out var sb);

            if (!valid && LPBDSP.SupportedVersions.Contains(version))
            {
                WinFormsUtil.Error("Error fetching Block");
                return;
            }

            var write = false;
            if (txt.IsSpecialBlock(Remote.Bot.Version, out var v) && v != null)
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

                if (sb is SCBlock scb && scb.Data.SequenceEqual(data[0]))
                    return;
                write = true;
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
                    if (o != null)
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

            if (LPBasic.SupportedVersions.Contains(version))
                LPBasic.WriteBlocksFromSAV(Remote.Bot, txt, SAV.SAV);
            if (LPBDSP.SupportedVersions.Contains(version))
#pragma warning disable CS8604 // Possible null reference argument.
                LPBDSP.WriteBlockFromString(Remote.Bot, txt, GetBlockDataRaw(sb, data[0]), sb);
#pragma warning restore CS8604 // Possible null reference argument.
            if (LPPointer.SupportedVersions.Contains(version))
                LPPointer.WriteBlocksFromSAV(Remote.Bot, txt, SAV.SAV);
        }

        private static byte[] GetBlockDataRaw(object sb, byte[] data) => sb switch
        {
            SCBlock sc => sc.Data,
            IDataIndirect sv => sv.Data,
            _ => data,
        };

        private static bool ReadBlock(PokeSysBotMini bot, SaveFile sav, string display, out List<byte[]>? data)
        {
            var version = bot.Version;
            bool valid = false;
            data = null;
            if (LPBasic.SupportedVersions.Contains(version))
                valid = LPBasic.ReadBlockFromString(bot, sav, display, out data);
            if (LPBDSP.SupportedVersions.Contains(version))
                valid = LPBDSP.ReadBlockFromString(bot, sav, display, out data);
            if (LPPointer.SupportedVersions.Contains(version))
                valid = LPPointer.ReadBlockFromString(bot, sav, display, out data);
            return valid;
        }

        private static bool TryGetObjectInSave(LiveHeXVersion version, SaveFile sav, string display, byte[]? customdata, out object? sb)
        {
            sb = null;
            if (LPBDSP.SupportedVersions.Contains(version))
            {
                var prop = sav.GetType().GetProperty(display);
                if (prop != null)
                    sb = prop.GetValue(sav);
                else
                    sb = Activator.CreateInstance(LPBDSP.types.First(t => t.Name == display), customdata);
            }
            else
            {
                var subblocks = Array.Empty<BlockData>();
                if (LPBasic.SupportedVersions.Contains(version))
                    subblocks = LPBasic.SCBlocks[version].Where(z => z.Display == display).ToArray();
                if (LPPointer.SupportedVersions.Contains(version))
                    subblocks = LPPointer.SCBlocks[version].Where(z => z.Display == display).ToArray();
                if (subblocks.Length != 1)
                    return false;

                // Check for SCBlocks or SaveBlocks based on name. (SCBlocks will invoke the hex editor, SaveBlocks will invoke a property grid
                var props = sav.GetType().GetProperty("Blocks");
                if (props is null)
                    return false;

                var allblocks = props.GetValue(sav) ?? throw new Exception("Blocks not present.");
                var blockprop = allblocks.GetType().GetProperty(subblocks[0].Name);
                if (allblocks is SCBlockAccessor scba && blockprop == null)
                    sb = scba.GetBlock(subblocks[0].SCBKey);
                else
                    sb = blockprop?.GetValue(allblocks);
            }

            return sb != null;
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
