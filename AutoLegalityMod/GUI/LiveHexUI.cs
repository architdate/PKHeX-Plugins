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
            SetInjectionTypeView();

            // add an event to the editor
            // ReSharper disable once SuspiciousTypeConversion.Global
            BoxSelect = ((Control)sav).Controls.Find("CB_BoxSelect", true).FirstOrDefault() as ComboBox;
            if (BoxSelect != null)
            {
                BoxSelect.SelectedIndexChanged += ChangeBox;
                Closing += (s, e) => BoxSelect.SelectedIndexChanged -= ChangeBox;
            }

            var type = sav.GetType();
            var fields = type.GetTypeInfo().DeclaredFields;
            var test = fields.First(z => z.Name == "EditEnv");
            x = (SaveDataEditor<PictureBox>)test.GetValue(sav);
            x.Slots.Publisher.Subscribers.Add(this);

            TB_Port.Text = Remote.Bot.com.Port.ToString();
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

        private void ChangeBox(object sender, EventArgs e)
        {
            if (checkBox1.Checked && Remote.Bot.Connected)
                Remote.ChangeBox(ViewIndex);
        }

        private void B_Connect_Click(object sender, EventArgs e)
        {
            try
            {
                // Enable controls
                B_Connect.Enabled = TB_IP.Enabled = TB_Port.Enabled = false;
                groupBox1.Enabled = groupBox2.Enabled = groupBox3.Enabled = true;
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
#pragma warning disable CA1031 // Do not catch general exception types
            // Console might be disconnected...
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                WinFormsUtil.Error(ex.Message);
            }
        }

        private void LiveHeXUI_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (Remote.Bot.Connected)
                Remote.Bot.com.Disconnect();
            x.Slots.Publisher.Subscribers.Remove(this);

            AutoLegality.Default.LatestIP = TB_IP.Text;
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
            if (offset.ToString("X16") != txt.ToUpper().PadLeft(16, '0') && !readPointer)
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
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
            {
                WinFormsUtil.Error("Unable to load data from the specified offset.", ex.Message);
            }
#pragma warning restore CA1031 // Do not catch general exception types
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
                if (Remote.Bot.com is not ICommunicatorNX cnx) result = Remote.ReadRAM(offset, size);
                else
                {
                    if (RB_Main.Checked) result = cnx.ReadBytesMain(offset, size);
                    else if (RB_Absolute.Checked) result = cnx.ReadBytesAbsolute(offset, size);
                    else result = Remote.ReadRAM(offset, size);
                }
                bool blockview = (ModifierKeys & Keys.Control) == Keys.Control;
                PKM? pkm = null;
                if (blockview)
                {
                    pkm = SAV.SAV.GetDecryptedPKM(result);
                    if (!pkm.ChecksumValid || pkm == null)
                        blockview = false;
                }
                using (var form = new SimpleHexEditor(result))
                {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                    var loadgrid = blockview && ReflectUtil.GetPropertiesCanWritePublicDeclared(pkm.GetType()).Count() > 1;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                    if (loadgrid)
                    {
                        form.PG_BlockView.Visible = true;
                        form.PG_BlockView.SelectedObject = pkm;
                    }
                    var res = form.ShowDialog();
                    if (res == DialogResult.OK)
                    {
                        if (loadgrid && pkm != null)
                        {
                            var pkmbytes = RamOffsets.WriteBoxData(Remote.Bot.Version) ? pkm.EncryptedBoxData : pkm.EncryptedPartyData;
                            if (pkmbytes.Count() == Remote.Bot.SlotSize)
                                form.Bytes = pkmbytes;
                            else
                            {
                                form.Bytes = result;
                                WinFormsUtil.Error("Size mismatch. Please report this issue on the discord server.");
                            }
                        }
                        var modifiedRAM = form.Bytes;
                        if (Remote.Bot.com is not ICommunicatorNX nx) Remote.WriteRAM(offset, modifiedRAM);
                        else
                        {
                            if (RB_Main.Checked) nx.WriteBytesMain(modifiedRAM, offset);
                            else if (RB_Absolute.Checked) nx.WriteBytesAbsolute(modifiedRAM, offset);
                            else Remote.WriteRAM(offset, modifiedRAM);
                        }
                    }
                }
                Debug.WriteLine("RAM Modified");
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
            {
                WinFormsUtil.Error("Unable to load data from the specified offset.", ex.Message);
            }
#pragma warning restore CA1031 // Do not catch general exception types
        }

        public IEnumerable<string> GetSortedBlockList(LiveHeXVersion lv)
        {
            if (LPBasic.SupportedVersions.Contains(lv))
            {
                var offsets = RamOffsets.GetOffsets(Remote.Bot.Version);
                if (offsets == null)
                    return new List<string>();
                var aType = offsets.GetType();
                var props = aType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                var ofType = props.Where(fi => typeof(uint).IsAssignableFrom(fi.FieldType));
                var retval = ofType.ToDictionary(z => z.Name, field => (uint)field.GetValue(offsets));
                return retval.Where(z => z.Value != 0).Select(z => z.Key).OrderBy(z => z);
            }
            if (LPBDSP.SupportedVersions.Contains(lv))
            {
                return LPBDSP.FunctionMap.Keys.OrderBy(z => z);
            }
            return new List<string>();
        }

        public void NotifySlotOld(ISlotInfo previous) { }

        public void NotifySlotChanged(ISlotInfo slot, SlotTouchType type, PKM pkm)
        {
            if (!checkBox2.Checked || !Remote.Bot.Connected)
                return;
            if (slot is not SlotInfoBox b)
                return;
            if (!type.IsContentChange())
                return;
            int box = b.Box;
            int slotpkm = b.Slot;
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
            var ptr = TB_Pointer.Text;
            var address = InjectionUtil.GetPointerAddress(sb, ptr, false);
            if (address == 0)
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
                    if (!pkm.ChecksumValid || pkm == null)
                        blockview = false;
                }
                using (var form = new SimpleHexEditor(result))
                {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                    var loadgrid = blockview && ReflectUtil.GetPropertiesCanWritePublicDeclared(pkm.GetType()).Count() > 1;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                    if (loadgrid)
                    {
                        form.PG_BlockView.Visible = true;
                        form.PG_BlockView.SelectedObject = pkm;
                    }
                    var res = form.ShowDialog();
                    if (res == DialogResult.OK)
                    {
                        if (loadgrid && pkm != null)
                        {
                            var pkmbytes = RamOffsets.WriteBoxData(Remote.Bot.Version) ? pkm.EncryptedBoxData : pkm.EncryptedPartyData;
                            if (pkmbytes.Count() == Remote.Bot.SlotSize)
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
#pragma warning disable CA1031 // Do not catch general exception types
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
            bool valid = false;
            byte[]? data = null;
            if (LPBasic.SupportedVersions.Contains(version))
                valid = LPBasic.ReadBlockFromString(Remote.Bot, SAV.SAV, txt, out data);
            if (LPBDSP.SupportedVersions.Contains(version))
                valid = LPBDSP.ReadBlockFromString(Remote.Bot, SAV.SAV, txt, out data);
            if (!valid || data == null)
            {
                WinFormsUtil.Error("Invalid Entry");
                return;
            }

            object? sb = null;
            if (LPBasic.SupportedVersions.Contains(version))
            {
                var allblocks = SAV.SAV.GetType().GetProperty("Blocks").GetValue(SAV.SAV);
                var blockprop = allblocks.GetType().GetProperty(txt);
                if (allblocks is SCBlockAccessor scba && blockprop == null)
                {
                    var key = allblocks.GetType().GetField(txt, BindingFlags.NonPublic | BindingFlags.Static).GetValue(allblocks);
                    sb = scba.GetBlock((uint)key);
                }
                else
                {
                    sb = blockprop.GetValue(allblocks);
                }
            }
            if (LPBDSP.SupportedVersions.Contains(version))
                sb = SAV.SAV.GetType().GetProperty(txt).GetValue(SAV.SAV);

            if (sb == null)
            {
                WinFormsUtil.Error("Error fetching Block");
                return;
            }

            // Verify if sb is a valid block type
            if (sb is not SCBlock && sb is not SaveBlock)
                return;

            if (sb.IsSpecialBlock(Remote.Bot.Version, out var v))
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

                var blockdata = GetBlockDataRaw(sb, data);
                if (!blockdata.SequenceEqual(data))
                {
                    if (LPBasic.SupportedVersions.Contains(version))
                        LPBasic.WriteBlockFromString(Remote.Bot, txt, blockdata);
                    if (LPBDSP.SupportedVersions.Contains(version))
                        LPBDSP.WriteBlockFromString(Remote.Bot, txt, blockdata, sb);
                }
                return;
            }
            using var form = new SimpleHexEditor(data);
            var props = ReflectUtil.GetPropertiesCanWritePublicDeclared(sb.GetType());
            var loadgrid = props.Count() > 1 && ModifierKeys != Keys.Control;
            if (loadgrid)
            {
                form.PG_BlockView.Visible = true;
                form.PG_BlockView.SelectedObject = sb;
            }
            var res = form.ShowDialog();
            if (res == DialogResult.OK)
            {
                var blockdata = GetBlockDataRaw(sb, data);
                if (loadgrid)
                    form.Bytes = blockdata;
                var modifiedRAM = form.Bytes;
                if (LPBasic.SupportedVersions.Contains(version))
                    LPBasic.WriteBlockFromString(Remote.Bot, txt, modifiedRAM);
                if (LPBDSP.SupportedVersions.Contains(version))
                    LPBDSP.WriteBlockFromString(Remote.Bot, txt, modifiedRAM, sb);
            }
        }

        private static byte[] GetBlockDataRaw(object sb, byte[] data) => sb switch
        {
            SCBlock sc => sc.Data,
            SaveBlock sv => sv.Data,
            _ => data,
        };
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
