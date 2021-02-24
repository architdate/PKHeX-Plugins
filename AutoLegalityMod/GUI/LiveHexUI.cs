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
        public ISaveFileProvider SAV { get; }

        public int ViewIndex => BoxSelect?.SelectedIndex ?? 0;
        public IList<PictureBox> SlotPictureBoxes => throw new InvalidOperationException();
        SaveFile ISlotViewer<PictureBox>.SAV => throw new InvalidOperationException();

        private readonly LiveHeXController Remote;
        private readonly SaveDataEditor<PictureBox> x;

        private readonly InjectorCommunicationType CurrentInjectionType;

#pragma warning disable CA2213 // Disposable fields should be disposed
        private readonly ComboBox? BoxSelect; // this is just us holding a reference; disposal is done by its parent
#pragma warning restore CA2213 // Disposable fields should be disposed

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

        private void SetTrainerData(SaveFile sav, LiveHeXVersion lv)
        {
            // Check and set trainerdata based on ISaveBlock interfaces
            byte[] dest;
            int startofs = 0;
            switch (sav)
            {
                case ISaveBlock8Main s8:
                    dest = s8.MyStatus.Data;
                    startofs = s8.MyStatus.Offset;
                    break;

                case ISaveBlock7Main s7:
                    dest = s7.MyStatus.Data;
                    startofs = s7.MyStatus.Offset;
                    break;

                case ISaveBlock6Main s6:
                    dest = s6.Status.Data;
                    startofs = s6.Status.Offset;
                    break;

                case SAV7b slgpe:
                    dest = slgpe.Blocks.Status.Data;
                    startofs = slgpe.Blocks.Status.Offset;
                    break;

                default:
                    dest = Array.Empty<byte>();
                    break;
            }

            if (dest.Length == 0)
                return;

            var ofs = RamOffsets.GetTrainerBlockOffset(lv);
            var data = Remote.Bot.com.ReadBytes(ofs, RamOffsets.GetTrainerBlockSize(lv));
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
                        com = { IP = TB_IP.Text, Port = int.Parse(TB_Port.Text) }
                    };
                    Remote.Bot.com.Connect();

                    var data = Remote.Bot.ReadSlot(1, 1);
                    var pkm = SAV.SAV.GetDecryptedPKM(data);
                    if (pkm.ChecksumValid)
                    {
                        ConnectionEstablished = true;
                        currver = version;
                        if (Remote.Bot.com is ICommunicatorNX)
                        {
                            groupBox4.Enabled = true;
                            var cblist = GetSortedBlockList().ToArray();
                            if (cblist.Count() > 0)
                            {
                                groupBox5.Enabled = true;
                                CB_BlockName.Items.AddRange(cblist);
                                CB_BlockName.SelectedIndex = 0;
                            }
                        }
                        break;
                    }

                    if (!ConnectionEstablished)
                        Remote.Bot.com.Disconnect();
                }

                if (!ConnectionEstablished)
                {
                    Remote.Bot = new PokeSysBotMini(currver, CurrentInjectionType)
                    {
                        com = { IP = TB_IP.Text, Port = int.Parse(TB_Port.Text) }
                    };
                    Remote.Bot.com.Connect();
                    Text += $" Unknown Version (Forced: {currver})";
                }
                else
                    Text += $" Detected Version: {currver}";

                // Load current box
                Remote.ReadBox(SAV.CurrentBox);

                // Set Trainer Data
                SetTrainerData(SAV.SAV, currver);
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
            var txt = TB_Offset.Text;
            var offset = Util.GetHexValue(txt);
            if (offset.ToString("X8") != txt.ToUpper().PadLeft(8, '0'))
            {
                WinFormsUtil.Alert("Specified offset is not a valid hex string.");
                return;
            }
            try
            {
                var result = Remote.ReadOffset(offset);
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
            var offset = Util.GetHexValue(txt);
            var valid = int.TryParse(RamSize.Text, out int size);
            if (offset.ToString("X8") != txt.ToUpper().PadLeft(8, '0') || !valid)
            {
                WinFormsUtil.Alert("Make sure that the RAM offset is a hex string and the size is a valid integer");
                return;
            }

            try
            {
                var result = Remote.ReadRAM(offset, size);
                using (var form = new SimpleHexEditor(result))
                {
                    var res = form.ShowDialog();
                    if (res == DialogResult.OK)
                    {
                        var modifiedRAM = form.Bytes;
                        Remote.WriteRAM(offset, modifiedRAM);
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

        public IEnumerable<string> GetSortedBlockList()
        {
            var offsets = RamOffsets.GetOffsets(Remote.Bot.Version);
            if (offsets == null)
                return new List<string>();
            var aType = offsets.GetType();
            var props = aType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            var ofType = props.Where(fi => typeof(uint).IsAssignableFrom(fi.FieldType));
            var retval = ofType.ToDictionary(z => z.Name, x => (uint)x.GetValue(offsets));
            return retval.Where(z => z.Value != 0).Select(z => z.Key).OrderBy(z => z);
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
            TB_IP.Visible = TB_Port.Visible = L_IP.Visible = L_Port.Visible = CurrentInjectionType == InjectorCommunicationType.SocketNetwork;
            L_USBState.Visible = CurrentInjectionType == InjectorCommunicationType.USB;
        }

        public ulong GetPointerAddress(SysBotMini sb)
        {
            var ptr = TB_Pointer.Text;
            uint finadd = 0;
            if (!ptr.EndsWith("]"))
                finadd = Util.GetHexValue(ptr.Split('+').Last());
            var jumps = ptr.Replace("main", "").Replace("[", "").Replace("]", "").Split(new[] { "+" }, StringSplitOptions.RemoveEmptyEntries);
            if (jumps.Length == 0)
            {
                WinFormsUtil.Alert("Invalid Pointer");
                return 0;
            }

            var initaddress = Util.GetHexValue(jumps[0].Trim());
            ulong address = BitConverter.ToUInt64(sb.ReadBytesMain(initaddress, 0x8), 0);
            foreach (var j in jumps)
            {
                var val = Util.GetHexValue(j.Trim());
                if (val == initaddress)
                    continue;
                if (val == finadd)
                {
                    address += val;
                    break;
                }
                address = BitConverter.ToUInt64(sb.ReadBytesAbsolute(address + val, 0x8), 0);
            }
            return address;
        }

        private void B_CopyAddress_Click(object sender, EventArgs e)
        {
            if (Remote.Bot.com is not SysBotMini sb)
                return;

            ulong address = GetPointerAddress(sb);
            if (address == 0)
                WinFormsUtil.Alert("No pointer address.");

            Clipboard.SetText(address.ToString("X"));
        }

        private void B_EditPointerData_Click(object sender, EventArgs e)
        {
            if (Remote.Bot.com is not SysBotMini sb)
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
                using (var form = new SimpleHexEditor(result))
                {
                    var res = form.ShowDialog();
                    if (res == DialogResult.OK)
                    {
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
            if (Remote.Bot.com is not SysBotMini sb)
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
            var valid = Remote.ReadBlockFromString(SAV.SAV, txt, out var data);
            if (!valid || data == null)
            {
                WinFormsUtil.Error("Invalid Entry");
                return;
            }
            var sb = (SaveBlock)SAV.SAV.GetType().GetProperty(txt).GetValue(SAV.SAV);
            if (sb is MyItem _)
            {
                var btn = ((ContainerControl)SAV).GetType().GetMethod("B_OpenItemPouch_Click", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(((ContainerControl)SAV), new object[] { sender, e });
                if (!sb.Data.SequenceEqual(data))
                    Remote.WriteBlockFromString(txt, sb.Data);
            }
            else
            {
                using (var form = new SimpleHexEditor(data))
                {
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
                        if (loadgrid)
                            form.Bytes = sb.Data;
                        var modifiedRAM = form.Bytes;
                        Remote.WriteBlockFromString(txt, modifiedRAM);
                    }
                }
            }
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
                    text = text.Substring(2);
                    Clipboard.SetText(text);
                }
            }

            base.WndProc(ref m);
        }
    }
}
