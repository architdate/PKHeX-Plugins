using System;
using System.Threading.Tasks;
using System.Timers;
using PKHeX.Core;
using System.Collections.Generic;

namespace pkmn_ntr.Helpers
{
    //Objects of this class contains an array for data that have been acquired, a delegate function
    //to handle them and any additional arguments it might require.
    public class RemoteControl
    {
        // Class variables
        private readonly int maxtimeout = 5000; // Max timeout in ms
        public uint lastRead = 0; // Last read from RAM
        public byte[] lastmultiread;
        public int pid = 0;
        private PKM validator;
        private readonly Timer NTRtimer;
        private bool timeout = false;
        public string lastlog;
        public static Dictionary<uint, DataReadyWaiting> waitingForData = new Dictionary<uint, DataReadyWaiting>();

        // Offsets for remote controls
        private readonly uint buttonsOff = 0x10df20;
        private readonly uint touchscrOff = 0x10df24;
        private readonly uint stickOff = 0x10df28;
        private readonly int hid_pid = 0x10;
        public const int BOXSIZE = 30;
        public const int POKEBYTES = 232;
        public const int PARTYBYTES = 260;

        // Class constructor
        public RemoteControl()
        {
            NTRtimer = new Timer(maxtimeout)
            {
                AutoReset = false,
                Enabled = false
            };
            NTRtimer.Elapsed += NTRtimer_Tick;
        }

        // Log Handler
        private void WriteLastLog(string str)
        {
            lastlog = str;
        }

        private bool CompareLastLog(string str)
        {
            return lastlog.Contains(str);
        }

        private void Report(string log)
        {
            Console.WriteLine(log);
        }

        // Button Handler
        public async Task<bool> ButtonWait(uint key)
        {
            Report("NTR: Send button command 0x" + key.ToString("X3"));
            // Get and send hex coordinates
            byte[] buttonByte = BitConverter.GetBytes(key);
            WonderTradeBot.WonderTradeBot.scriptHelper.WriteData(buttonsOff, buttonByte, hid_pid);
            SetTimer(maxtimeout);
            while (!timeout)
            { // Timeout 1
                await Task.Delay(100).ConfigureAwait(false);
                if (CompareLastLog("finished"))
                {
                    break;
                }
            }
            if (timeout) // If not response, return timeout
            {
                Report("NTR: Button press failed, try to free buttons");
                ButtonQuick(LookupTable.NoButtons, 250);
                return false;
            }
            else
            { // Free the buttons
                buttonByte = BitConverter.GetBytes(LookupTable.NoButtons);
                WonderTradeBot.WonderTradeBot.scriptHelper.WriteData(buttonsOff, buttonByte, hid_pid);
                SetTimer(maxtimeout);
                while (!timeout)
                { // Timeout 2
                    await Task.Delay(100).ConfigureAwait(false);
                    if (CompareLastLog("finished"))
                    {
                        break;
                    }
                }
                if (timeout) // If not response, return timeout
                {
                    Report("NTR: Button release failed");
                    return false;
                }
                else // Return sucess
                {
                    NTRtimer.Stop();
                    Report("NTR: Button command sent correctly");
                    return true;
                }
            }
        }

        public async void ButtonQuick(uint key, int time)
        {
            Report("NTR: Send button command 0x" + key.ToString("X3") + " during " + time + " ms");
            byte[] buttonByte = BitConverter.GetBytes(key);
            WonderTradeBot.WonderTradeBot.scriptHelper.WriteData(buttonsOff, buttonByte, hid_pid);
            await Task.Delay(time).ConfigureAwait(false);
            buttonByte = BitConverter.GetBytes(LookupTable.NoButtons);
            WonderTradeBot.WonderTradeBot.scriptHelper.WriteData(buttonsOff, buttonByte, hid_pid);
            Report("NTR: Button command sent, no feedback provided");
        }

        public async Task<bool> SoftResetWait()
        {
            Report("NTR: Send soft-reset command 0xCF7");
            // Get and send hex coordinates
            byte[] buttonByte = BitConverter.GetBytes(0xCF7);
            WonderTradeBot.WonderTradeBot.scriptHelper.WriteData(buttonsOff, buttonByte, hid_pid);
            SetTimer(maxtimeout);
            while (!timeout)
            { // Timeout 1
                await Task.Delay(100).ConfigureAwait(false);
                if (CompareLastLog("patching smdh"))
                {
                    break;
                }
            }
            if (timeout) // If not response, return timeout
            {
                Report("NTR: Button press failed, try to free buttons");
                ButtonQuick(LookupTable.NoButtons, 250);
                return false;
            }
            else
            { // Free the buttons
                buttonByte = BitConverter.GetBytes(LookupTable.NoButtons);
                WonderTradeBot.WonderTradeBot.scriptHelper.WriteData(buttonsOff, buttonByte, hid_pid);
                SetTimer(maxtimeout);
                while (!timeout)
                { // Timeout 2
                    await Task.Delay(100).ConfigureAwait(false);
                    if (CompareLastLog("finished") || CompareLastLog("patching smdh"))
                    {
                        break;
                    }
                }
                if (timeout) // If not response, return timeout
                {
                    Report("NTR: Button release failed");
                    return false;
                }
                else // Return sucess
                {
                    NTRtimer.Stop();
                    Report("NTR: Soft-reset command sent correctly");
                    return true;
                }
            }
        }

        public async Task ScriptButton(uint key)
        {
            Report($"Script: Send button command 0x{key.ToString("X3")}");
            byte[] buttonByte = BitConverter.GetBytes(key);
            WonderTradeBot.WonderTradeBot.scriptHelper.WriteData(buttonsOff, buttonByte, hid_pid);
            await Task.Delay(200).ConfigureAwait(false);
            buttonByte = BitConverter.GetBytes(LookupTable.NoButtons);
            WonderTradeBot.WonderTradeBot.scriptHelper.WriteData(buttonsOff, buttonByte, hid_pid);
            await Task.Delay(500).ConfigureAwait(false);
        }

        public async Task ScriptButtonTimed(uint key, int time)
        {
            Report($"Script: Send button command 0x{key.ToString("X3")} during {time} ms");
            byte[] buttonByte = BitConverter.GetBytes(key);
            WonderTradeBot.WonderTradeBot.scriptHelper.WriteData(buttonsOff, buttonByte, hid_pid);
            await Task.Delay(time).ConfigureAwait(false);
            buttonByte = BitConverter.GetBytes(LookupTable.NoButtons);
            WonderTradeBot.WonderTradeBot.scriptHelper.WriteData(buttonsOff, buttonByte, hid_pid);
            await Task.Delay(500).ConfigureAwait(false);
        }

        public async Task ScriptButtonHold(uint key)
        {
            Report($"Script: Send and hold button command 0x{key.ToString("X3")}");
            byte[] buttonByte = BitConverter.GetBytes(key);
            WonderTradeBot.WonderTradeBot.scriptHelper.WriteData(buttonsOff, buttonByte, hid_pid);
            await Task.Delay(500).ConfigureAwait(false);
        }

        public async Task ScriptButtonRelease()
        {
            Report($"Script: Release all buttons");
            byte[] buttonByte = BitConverter.GetBytes(LookupTable.NoButtons);
            WonderTradeBot.WonderTradeBot.scriptHelper.WriteData(buttonsOff, buttonByte, hid_pid);
            await Task.Delay(500).ConfigureAwait(false);
        }

        // Touch Screen Handler
        public async Task<bool> TouchWait(decimal Xcoord, decimal Ycoord)
        {
            Report("NTR: Touch the screen at " + Xcoord.ToString("F0") + "," + Ycoord.ToString("F0"));
            // Get and send hex coordinates
            byte[] buttonByte = BitConverter.GetBytes(GetHexCoord(Xcoord, Ycoord));
            WonderTradeBot.WonderTradeBot.scriptHelper.WriteData(touchscrOff, buttonByte, hid_pid);
            SetTimer(maxtimeout);
            while (!timeout)
            { // Timeout 1
                await Task.Delay(100).ConfigureAwait(false);
                if (CompareLastLog("finished"))
                {
                    break;
                }
            }
            if (timeout) // If not response, return timeout
            {
                Report("NTR: Button press failed, try to free the touchscreen");
                TouchFree();
                return false;
            }
            else
            {  // Free the touch screen
                buttonByte = BitConverter.GetBytes(LookupTable.NoTouch);
                WonderTradeBot.WonderTradeBot.scriptHelper.WriteData(touchscrOff, buttonByte, hid_pid);
                SetTimer(maxtimeout);
                while (!timeout)
                { // Timeout 2
                    await Task.Delay(100).ConfigureAwait(false);
                    if (CompareLastLog("finished"))
                    {
                        break;
                    }
                }
                if (timeout) // If not response, return timeout
                {
                    Report("NTR: Touch screen release failed");
                    return false;
                }
                else // Return sucess
                {
                    NTRtimer.Stop();
                    Report("NTR: Touch screen command sent correctly");
                    return true;
                }
            }
        }

        public async Task<bool> TouchWaitHold(decimal Xcoord, decimal Ycoord)
        {
            Report("NTR: Touch the screen and hold at " + Xcoord.ToString("F0") + "," + Ycoord.ToString("F0"));
            // Get and send hex coordinates
            byte[] buttonByte = BitConverter.GetBytes(GetHexCoord(Xcoord, Ycoord));
            WonderTradeBot.WonderTradeBot.scriptHelper.WriteData(touchscrOff, buttonByte, hid_pid);
            SetTimer(maxtimeout);
            while (!timeout)
            { // Timeout
                await Task.Delay(100).ConfigureAwait(false);
                if (CompareLastLog("finished"))
                {
                    break;
                }
            }
            if (timeout) // If not response, return timeout
            {
                Report("NTR: Button press failed");
                return false;
            }
            else // Return sucess
            {
                NTRtimer.Stop();
                Report("NTR: Touch screen command sent correctly");
                return true;
            }
        }

        public async Task<bool> TouchWaitFree()
        {
            Report("NTR: Free the touch screen");
            // Get and send hex coordinates
            byte[] buttonByte = BitConverter.GetBytes(LookupTable.NoTouch);
            WonderTradeBot.WonderTradeBot.scriptHelper.WriteData(touchscrOff, buttonByte, hid_pid);
            SetTimer(maxtimeout);
            while (!timeout)
            { // Timeout
                await Task.Delay(100).ConfigureAwait(false);
                if (CompareLastLog("finished"))
                {
                    break;
                }
            }
            if (timeout) // If not response, return timeout
            {
                Report("NTR: Button press failed");
                return false;
            }
            else // Return sucess
            {
                NTRtimer.Stop();
                Report("NTR: Touch screen command sent correctly");
                return true;
            }
        }

        public async void TouchQuick(decimal x, decimal y, int time)
        {
            Report($"NTR: Touch the screen at {x:F0},{y:F0} during {time} ms");
            byte[] buttonByte = BitConverter.GetBytes(GetHexCoord(x, y));
            WonderTradeBot.WonderTradeBot.scriptHelper.WriteData(touchscrOff, buttonByte, hid_pid);
            await Task.Delay(time).ConfigureAwait(false);
            buttonByte = BitConverter.GetBytes(LookupTable.NoTouch);
            WonderTradeBot.WonderTradeBot.scriptHelper.WriteData(touchscrOff, buttonByte, hid_pid);
            Report("NTR: Touch screen command sent, no feedback provided");
        }

        public async void TouchHold(decimal x, decimal y)
        {
            Report($"NTR: Touch the screen and hold at {x:F0},{y:F0}");
            byte[] buttonByte = BitConverter.GetBytes(GetHexCoord(x, y));
            WonderTradeBot.WonderTradeBot.scriptHelper.WriteData(touchscrOff, buttonByte, hid_pid);
            await Task.Delay(100).ConfigureAwait(false);
            Report("NTR: Touch screen command sent, no feedback provided");
        }

        public async void TouchFree()
        {
            Report("NTR: Free the touch screen");
            byte[] buttonByte = BitConverter.GetBytes(LookupTable.NoTouch);
            WonderTradeBot.WonderTradeBot.scriptHelper.WriteData(touchscrOff, buttonByte, hid_pid);
            await Task.Delay(100).ConfigureAwait(false);
            Report("NTR: Touch screen command sent, no feedback provided");
        }

        private uint GetHexCoord(decimal Xvalue, decimal Yvalue)
        {
            uint hexX = Convert.ToUInt32(Math.Round(Xvalue * 0xFFF / 319));
            uint hexY = Convert.ToUInt32(Math.Round(Yvalue * 0xFFF / 239));
            return 0x01000000 + (hexY * 0x1000) + hexX;
        }

        public async Task ScriptTouch(int Xvalue, int Yvalue)
        {
            Report($"Script: Touch screen at {Xvalue}, {Yvalue}");
            byte[] touchByte = BitConverter.GetBytes(GetHexCoord(Xvalue, Yvalue));
            WonderTradeBot.WonderTradeBot.scriptHelper.WriteData(touchscrOff, touchByte, hid_pid);
            await Task.Delay(200).ConfigureAwait(false);
            touchByte = BitConverter.GetBytes(LookupTable.NoTouch);
            WonderTradeBot.WonderTradeBot.scriptHelper.WriteData(touchscrOff, touchByte, hid_pid);
            await Task.Delay(500).ConfigureAwait(false);
        }

        public async Task ScriptTouchTimed(int Xvalue, int Yvalue, int time)
        {
            Report($"Script: Touch screen at {Xvalue}, {Yvalue} during {time} ms");
            byte[] touchByte = BitConverter.GetBytes(GetHexCoord(Xvalue, Yvalue));
            WonderTradeBot.WonderTradeBot.scriptHelper.WriteData(touchscrOff, touchByte, hid_pid);
            await Task.Delay(time).ConfigureAwait(false);
            touchByte = BitConverter.GetBytes(LookupTable.NoTouch);
            WonderTradeBot.WonderTradeBot.scriptHelper.WriteData(touchscrOff, touchByte, hid_pid);
            await Task.Delay(500).ConfigureAwait(false);
        }

        public async Task ScriptTouchHold(int Xvalue, int Yvalue)
        {
            Report($"Script: Touch screen and hold at {Xvalue}, {Yvalue}");
            byte[] touchByte = BitConverter.GetBytes(GetHexCoord(Xvalue, Yvalue));
            WonderTradeBot.WonderTradeBot.scriptHelper.WriteData(touchscrOff, touchByte, hid_pid);
            await Task.Delay(500).ConfigureAwait(false);
        }

        public async Task ScriptTouchRelease()
        {
            Report($"Script: Release touch screen");
            byte[] touchByte = BitConverter.GetBytes(LookupTable.NoTouch);
            WonderTradeBot.WonderTradeBot.scriptHelper.WriteData(touchscrOff, touchByte, hid_pid);
            await Task.Delay(500).ConfigureAwait(false);
        }

        // Control Stick Handler
        public async Task<bool> StickWait(int Xvalue, int Yvalue)
        {
            Report("NTR: Move Control Stick to " + Xvalue.ToString("D3") + "," + Yvalue.ToString("D3"));
            // Get and send hex coordinates
            byte[] buttonByte = BitConverter.GetBytes(GetStickHex(Xvalue, Yvalue));
            WonderTradeBot.WonderTradeBot.scriptHelper.WriteData(stickOff, buttonByte, hid_pid);
            SetTimer(maxtimeout);
            while (!timeout)
            { // Timeout 1
                await Task.Delay(100).ConfigureAwait(false);
                if (CompareLastLog("finished"))
                {
                    break;
                }
            }
            if (timeout) // If not response, return timeout
            {
                Report("NTR: Control stick command failed, try to release it");
                StickQuick(0, 0, 250);
                TouchFree();
                return false;
            }
            else
            { // Free the control stick
                buttonByte = BitConverter.GetBytes(LookupTable.NoStick);
                WonderTradeBot.WonderTradeBot.scriptHelper.WriteData(stickOff, buttonByte, hid_pid);
                SetTimer(maxtimeout);
                while (!timeout)
                { // Timeout 2
                    await Task.Delay(100).ConfigureAwait(false);
                    if (CompareLastLog("finished"))
                    {
                        break;
                    }
                }
                if (timeout) // If not response, return timeout
                {
                    Report("NTR: Control Stick release failed");
                    return false;
                }
                else // Return sucess
                {
                    NTRtimer.Stop();
                    Report("NTR: Control Stick command sent correctly");
                    return true;
                }
            }
        }

        public async void StickQuick(int Xvalue, int Yvalue, int time)
        {
            Report("NTR: Move Control Stick to " + Xvalue.ToString("D3") + "," + Yvalue.ToString("D3") + " during " + time + " ms");
            byte[] buttonByte = BitConverter.GetBytes(GetStickHex(Xvalue, Yvalue));
            WonderTradeBot.WonderTradeBot.scriptHelper.WriteData(stickOff, buttonByte, hid_pid);
            await Task.Delay(time).ConfigureAwait(false);
            buttonByte = BitConverter.GetBytes(LookupTable.NoStick);
            WonderTradeBot.WonderTradeBot.scriptHelper.WriteData(stickOff, buttonByte, hid_pid);
            Report("NTR: Control Stick command sent, no feedback provided");
        }

        private uint GetStickHex(int Xvalue, int Yvalue)
        {
            uint hexX = Convert.ToUInt32((Xvalue + 100) * 0xFFF / 200);
            uint hexY = Convert.ToUInt32((Yvalue + 100) * 0xFFF / 200);
            if (hexX >= 0x1000) hexX = 0xFFF;
            if (hexY >= 0x1000) hexY = 0xFFF;
            return 0x01000000 + (hexY * 0x1000) + hexX;
        }

        public async Task ScriptStick(int x, int y)
        {
            Report($"Script: Move and release the control stick to {x}, {y}");
            byte[] stickByte = BitConverter.GetBytes(GetStickHex(x, y));
            WonderTradeBot.WonderTradeBot.scriptHelper.WriteData(stickOff, stickByte, hid_pid);
            await Task.Delay(200).ConfigureAwait(false);
            stickByte = BitConverter.GetBytes(LookupTable.NoStick);
            WonderTradeBot.WonderTradeBot.scriptHelper.WriteData(stickOff, stickByte, hid_pid);
            await Task.Delay(500).ConfigureAwait(false);
        }

        public async Task ScriptStickTimed(int x, int y, int time)
        {
            Report($"Script: Move the control stick to {x}, {y} during {time} ms");
            byte[] stickByte = BitConverter.GetBytes(GetStickHex(x, y));
            WonderTradeBot.WonderTradeBot.scriptHelper.WriteData(stickOff, stickByte, hid_pid);
            await Task.Delay(time).ConfigureAwait(false);
            stickByte = BitConverter.GetBytes(LookupTable.NoStick);
            WonderTradeBot.WonderTradeBot.scriptHelper.WriteData(stickOff, stickByte, hid_pid);
            await Task.Delay(500).ConfigureAwait(false);
        }

        public async Task ScriptStickHold(int x, int y)
        {
            Report($"Script: Move and hold the control stick to {x}, {y}");
            byte[] stickByte = BitConverter.GetBytes(GetStickHex(x, y));
            WonderTradeBot.WonderTradeBot.scriptHelper.WriteData(stickOff, stickByte, hid_pid);
            await Task.Delay(500).ConfigureAwait(false);
        }

        public async Task ScriptStickRelease()
        {
            Report($"Script: Release the control stick");
            byte[] stickByte = BitConverter.GetBytes(LookupTable.NoStick);
            WonderTradeBot.WonderTradeBot.scriptHelper.WriteData(stickOff, stickByte, hid_pid);
            await Task.Delay(500).ConfigureAwait(false);
        }

        // Memory Read Handler
        private void HandleMemoryRead(object args_obj)
        {
            DataReadyWaiting args = (DataReadyWaiting)args_obj;
            lastRead = BitConverter.ToUInt32(args.Data, 0);
        }

        private void HandleMemoryReadMulti(object args_obj)
        {
            DataReadyWaiting args = (DataReadyWaiting)args_obj;
            lastmultiread = args.Data;
        }

        public async Task<bool> WaitReadNTR(uint address)
        {
            Report("NTR: Read data at address 0x" + address.ToString("X8"));
            lastRead = 0;
            DataReadyWaiting myArgs = new DataReadyWaiting(new byte[0x04], HandleMemoryRead, null);
            AddWaitingForData(WonderTradeBot.WonderTradeBot.scriptHelper.ReadData(address, 0x04, pid), myArgs);
            SetTimer(maxtimeout);
            while (!timeout)
            {
                await Task.Delay(100).ConfigureAwait(false);
                if (CompareLastLog("finished"))
                {
                    break;
                }
            }
            if (timeout)
            {
                Report("NTR: Read failed");
                return false;
            }
            else
            {
                return true;
            }
        }

        public async Task<bool> WaitReadNTRMulti(uint address, uint size)
        {
            Report("NTR: Read " + size + " bytes of data starting at address 0x" + address.ToString("X8"));
            lastmultiread = new byte[] { };
            DataReadyWaiting myArgs = new DataReadyWaiting(new byte[size], HandleMemoryReadMulti, null);
            AddWaitingForData(WonderTradeBot.WonderTradeBot.scriptHelper.ReadData(address, size, pid), myArgs);
            SetTimer(maxtimeout);
            while (!timeout)
            {
                await Task.Delay(100).ConfigureAwait(false);
                if (CompareLastLog("finished"))
                {
                    break;
                }
            }
            if (timeout)
            {
                Report("NTR: Read failed");
                return false;
            }
            else
            {
                NTRtimer.Stop();
                return true;
            }
        }

        public void AddWaitingForData(uint newkey, DataReadyWaiting newvalue)
        {
            if (waitingForData.ContainsKey(newkey))
            {
                return;
            }

            waitingForData.Add(newkey, newvalue);
        }

        private void HandleReadPoke(object args_obj)
        {
            DataReadyWaiting args = (DataReadyWaiting)args_obj;
            if (WonderTradeBot.WonderTradeBot.SaveFileEditor2.SAV.Generation == 6)
                validator = new PK6(PKX.DecryptArray(args.Data));
            else
                validator = new PK7(PKX.DecryptArray(args.Data));
        }

        public async Task<PKM> WaitReadPoke(int box, int slot)
        {
            try
            {
                Report("NTR: Read pokémon data at box " + (box + 1) + ", slot " + (slot + 1));
                // Get offset
                uint dumpOff = LookupTable.BoxOffset + (Convert.ToUInt32((box * BOXSIZE) + slot) * POKEBYTES);
                DataReadyWaiting myArgs = new DataReadyWaiting(new byte[POKEBYTES], HandleReadPoke, null);
                WonderTradeBot.WonderTradeBot.UpdateDumpBoxes(box, slot);
                AddWaitingForData(WonderTradeBot.WonderTradeBot.scriptHelper.ReadData(dumpOff, POKEBYTES, pid), myArgs);
                SetTimer(maxtimeout);
                while (!timeout)
                {
                    await Task.Delay(100).ConfigureAwait(false);
                    if (CompareLastLog("finished"))
                    {
                        break;
                    }
                }
                if (timeout)
                { // No read
                    Report("NTR: Read failed");
                    return null;
                }
                if (validator.ChecksumValid && validator.Species > 0 && validator.Species <= WonderTradeBot.WonderTradeBot.SaveFileEditor2.SAV.MaxSpeciesID)
                { // Valid pokemon
                    NTRtimer.Stop();
                    lastRead = validator.Checksum;
                    WonderTradeBot.WonderTradeBot.PKMEditor2.PopulateFields(validator);
                    Report("NTR: Read sucessful - PID 0x" + validator.PID.ToString("X8"));
                    return validator;
                }
                else if (validator.ChecksumValid && validator.Species == 0)
                { // Empty slot
                    NTRtimer.Stop();
                    Report("NTR: Empty pokémon data");
                    return WonderTradeBot.WonderTradeBot.SaveFileEditor2.SAV.BlankPKM;
                }
                else
                { // Invalid pokémon
                    NTRtimer.Stop();
                    Report("NTR: Invalid pokémon data");
                    return null;
                }
            }
            catch (Exception ex)
            {
                NTRtimer.Stop();
                Report("NTR: Read failed with exception:");
                Report(ex.Message);
                return null; // No data received
            }
        }

        public async Task<PKM> WaitReadPoke(uint offset)
        {
            try
            {
                Report("NTR: Read pokémon data at offset 0x" + offset.ToString("X8"));
                DataReadyWaiting myArgs = new DataReadyWaiting(new byte[POKEBYTES], HandleReadPoke, null);
                AddWaitingForData(WonderTradeBot.WonderTradeBot.scriptHelper.ReadData(offset, POKEBYTES, pid), myArgs);
                SetTimer(maxtimeout);
                while (!timeout)
                {
                    await Task.Delay(100).ConfigureAwait(false);
                    if (CompareLastLog("finished"))
                    {
                        break;
                    }
                }
                if (timeout)
                { // No read
                    Report("NTR: Read failed");
                    return null;
                }
                if (validator.ChecksumValid && validator.Species > 0 && validator.Species <= WonderTradeBot.WonderTradeBot.SaveFileEditor2.SAV.MaxSpeciesID)
                { // Valid pokemon
                    NTRtimer.Stop();
                    lastRead = validator.Checksum;
                    WonderTradeBot.WonderTradeBot.PKMEditor2.PopulateFields(validator);
                    Report("NTR: Read sucessful - PID 0x" + validator.PID.ToString("X8"));
                    return validator;
                }
                else if (validator.ChecksumValid && validator.Species == 0)
                { // Empty slot
                    NTRtimer.Stop();
                    Report("NTR: Empty pokémon data");
                    return WonderTradeBot.WonderTradeBot.SaveFileEditor2.SAV.BlankPKM;
                }
                else
                { // Invalid pokémon
                    NTRtimer.Stop();
                    Report("NTR: Invalid pokémon data");
                    return null;
                }
            }
            catch (Exception ex)
            {
                NTRtimer.Stop();
                Report("NTR: Read failed with exception:");
                Report(ex.Message);
                return null; // No data received
            }
        }

        public async Task<PKM> WaitReadParty(uint slot)
        {
            try
            {
                Report("NTR: Read pokémon data at party slot " + slot);
                DataReadyWaiting myArgs = new DataReadyWaiting(new byte[PARTYBYTES], HandleReadPoke, null);
                uint offset = LookupTable.PartyOffset + (484 * (slot - 1));
                AddWaitingForData(WonderTradeBot.WonderTradeBot.scriptHelper.ReadData(offset, PARTYBYTES, pid), myArgs);
                SetTimer(maxtimeout);
                while (!timeout)
                {
                    await Task.Delay(100).ConfigureAwait(false);
                    if (CompareLastLog("finished"))
                        break;
                }
                if (timeout)
                { // No read
                    Report("NTR: Read failed");
                    return null;
                }
                if (validator.ChecksumValid && validator.Species > 0 && validator.Species <= WonderTradeBot.WonderTradeBot.SaveFileEditor2.SAV.MaxSpeciesID)
                { // Valid pokemon
                    NTRtimer.Stop();
                    lastRead = validator.Checksum;
                    WonderTradeBot.WonderTradeBot.PKMEditor2.PopulateFields(validator);
                    Report("NTR: Read sucessful - PID 0x" + validator.PID.ToString("X8"));
                    return validator;
                }
                else if (validator.ChecksumValid && validator.Species == 0)
                { // Empty slot
                    NTRtimer.Stop();
                    Report("NTR: Empty pokémon data");
                    return WonderTradeBot.WonderTradeBot.SaveFileEditor2.SAV.BlankPKM;
                }
                else
                { // Invalid pokémon
                    NTRtimer.Stop();
                    Report("NTR: Invalid pokémon data");
                    return null;
                }
            }
            catch (Exception ex)
            {
                NTRtimer.Stop();
                Report("NTR: Read failed with exception:");
                Report(ex.Message);
                return null; // No data received
            }
        }

        public async Task<bool> IsMemoryInRange(uint address, uint value, uint range)
        {
            Report($"NTR: Read data at address 0x{address:X8}");
            Report($"NTR: Expected value 0x{value:X8} to 0x{value + range - 1:X8}");
            lastRead = value + range;
            DataReadyWaiting myArgs = new DataReadyWaiting(new byte[0x04], HandleMemoryRead, null);
            AddWaitingForData(WonderTradeBot.WonderTradeBot.scriptHelper.ReadData(address, 0x04, pid), myArgs);
            SetTimer(maxtimeout);
            while (!timeout)
            {
                await Task.Delay(100).ConfigureAwait(false);
                if (CompareLastLog("finished"))
                    break;
            }
            if (!timeout)
            { // Data received
                if (lastRead >= value && lastRead < value + range)
                {
                    NTRtimer.Stop();
                    Report("NTR: Value in range: YES");
                    return true;
                }
                else
                {
                    Report("NTR: Value in range: NO");
                    return false;
                }
            }
            else // No data received
            {
                Report("NTR: Read failed");
                return false;
            }
        }

        public async Task<bool> IsTimeMemoryInRange(uint address, uint value, uint range, int tick, int maxtime)
        {
            Report($"NTR: Read data at address 0x{address:X8} during {maxtime} ms");
            Report($"NTR: Expected value 0x{value:X8} to 0x{value + range - 1:X8}");
            int readcount = 0;
            SetTimer(maxtime);
            while (!timeout || readcount < 5)
            { // Ask for data
                lastRead = value + range;
                DataReadyWaiting myArgs = new DataReadyWaiting(new byte[0x04], HandleMemoryRead, null);
                AddWaitingForData(WonderTradeBot.WonderTradeBot.scriptHelper.ReadData(address, 0x04, pid), myArgs);
                // Wait for data
                while (!timeout)
                {
                    await Task.Delay(100).ConfigureAwait(false);
                    if (CompareLastLog("finished"))
                    {
                        break;
                    }
                    if (timeout && readcount < 5)
                    {
                        Report("NTR: Restarting timeout");
                        SetTimer(maxtimeout);
                        break;
                    }
                }
                if (lastRead >= value && lastRead < value + range)
                {
                    NTRtimer.Stop();
                    Report("NTR: Value in range: YES");
                    return true;
                }
                else
                {
                    Report("NTR: Value in range: No");
                    await Task.Delay(tick).ConfigureAwait(false);
                }
                if (timeout && readcount < 5)
                {
                    Report("NTR: Restarting timeout");
                    SetTimer(maxtimeout);
                }
                readcount++;
            }
            Report("NTR: Read failed or outside of range");
            return false;
        }

        // Memory Write handler
        public async Task<bool> WaitWriteNTR(uint address, uint data, int pid)
        {
            Report($"NTR: Write value 0x{data:X8} at address 0x{address:X8}");
            byte[] command = BitConverter.GetBytes(data);
            WonderTradeBot.WonderTradeBot.scriptHelper.WriteData(address, command, pid);
            SetTimer(maxtimeout);
            while (!timeout)
            {
                // Timeout 1
                await Task.Delay(100).ConfigureAwait(false);
                if (CompareLastLog("finished"))
                {
                    break;
                }
            }
            if (!timeout)
            {
                NTRtimer.Stop();
                Report("NTR: Write sucessful");
                return true;
            }
            else
            {
                Report("NTR: Write failed");
                return false;
            }
        }

        public async Task<bool> WaitWriteNTR(uint address, byte[] data, int pid)
        {
            Report($"NTR: Write {data.Length} bytes at address 0x{address:X8}");
            WonderTradeBot.WonderTradeBot.scriptHelper.WriteData(address, data, pid);
            SetTimer(maxtimeout);
            while (!timeout)
            {
                // Timeout 1
                await Task.Delay(100).ConfigureAwait(false);
                if (CompareLastLog("finished"))
                {
                    break;
                }
            }
            if (!timeout)
            {
                NTRtimer.Stop();
                Report("NTR: Write sucessful");
                return true;
            }
            else
            {
                Report("NTR: Write failed");
                return false;
            }
        }

        // Timer
        private void SetTimer(int time)
        {
            WriteLastLog("");
            timeout = false;
            NTRtimer.Interval = time;
            NTRtimer.Start();
        }

        private void NTRtimer_Tick(object sender, ElapsedEventArgs e)
        {
            Report("NTR: Command timed out");
            timeout = true;
        }
    }
}
