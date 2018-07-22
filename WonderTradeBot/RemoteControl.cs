using System;
using System.Threading.Tasks;
using System.Timers;
using PKHeX.Core;
using System.Windows.Forms;
using System.Collections.Generic;

namespace pkmn_ntr.Helpers
{
    //Objects of this class contains an array for data that have been acquired, a delegate function 
    //to handle them and any additional arguments it might require.
    public class DataReadyWaiting
    {
        public byte[] data;
        public object arguments;
        public delegate void DataHandler(object data_arguments);
        public DataHandler handler;

        public DataReadyWaiting(byte[] data_, DataHandler handler_, object arguments_)
        {
            this.data = data_;
            this.handler = handler_;
            this.arguments = arguments_;
        }
    }
    public class RemoteControl
    {
        // Class variables
        private int maxtimeout = 5000; // Max timeout in ms
        public uint lastRead = 0; // Last read from RAM
        public byte[] lastmultiread;
        public int pid = 0;
        PKM validator;
        private System.Timers.Timer NTRtimer;
        private bool timeout = false;
        public string lastlog;
        public static Dictionary<uint, DataReadyWaiting> waitingForData = new Dictionary<uint, DataReadyWaiting>();

        // Offsets for remote controls
        private uint buttonsOff = 0x10df20;
        private uint touchscrOff = 0x10df24;
        private uint stickOff = 0x10df28;
        private int hid_pid = 0x10;
        public const int BOXSIZE = 30;
        public const int POKEBYTES = 232;
        public const int PARTYBYTES = 260;

        // Class constructor
        public RemoteControl()
        {
            NTRtimer = new System.Timers.Timer(maxtimeout);
            NTRtimer.AutoReset = false;
            NTRtimer.Elapsed += NTRtimer_Tick;
            NTRtimer.Enabled = false;
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
        public async Task<bool> waitbutton(uint key)
        {
            Report("NTR: Send button command 0x" + key.ToString("X3"));
            // Get and send hex coordinates
            byte[] buttonByte = BitConverter.GetBytes(key);
            WonderTradeBot.WonderTradeBot.scriptHelper.write(buttonsOff, buttonByte, hid_pid);
            setTimer(maxtimeout);
            while (!timeout)
            { // Timeout 1
                await Task.Delay(100);
                if (CompareLastLog("finished"))
                {
                    break;
                }
            }
            if (timeout) // If not response, return timeout
            {
                Report("NTR: Button press failed, try to free buttons");
                quickbuton(LookupTable.NoButtons, 250);
                return false;
            }
            else
            { // Free the buttons
                buttonByte = BitConverter.GetBytes(LookupTable.NoButtons);
                WonderTradeBot.WonderTradeBot.scriptHelper.write(buttonsOff, buttonByte, hid_pid);
                setTimer(maxtimeout);
                while (!timeout)
                { // Timeout 2
                    await Task.Delay(100);
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

        public async void quickbuton(uint key, int time)
        {
            Report("NTR: Send button command 0x" + key.ToString("X3") + " during " + time + " ms");
            byte[] buttonByte = BitConverter.GetBytes(key);
            WonderTradeBot.WonderTradeBot.scriptHelper.write(buttonsOff, buttonByte, hid_pid);
            await Task.Delay(time);
            buttonByte = BitConverter.GetBytes(LookupTable.NoButtons);
            WonderTradeBot.WonderTradeBot.scriptHelper.write(buttonsOff, buttonByte, hid_pid);
            Report("NTR: Button command sent, no feedback provided");
        }

        public async Task<bool> waitSoftReset()
        {
            Report("NTR: Send soft-reset command 0xCF7");
            // Get and send hex coordinates
            byte[] buttonByte = BitConverter.GetBytes(0xCF7);
            WonderTradeBot.WonderTradeBot.scriptHelper.write(buttonsOff, buttonByte, hid_pid);
            setTimer(maxtimeout);
            while (!timeout)
            { // Timeout 1
                await Task.Delay(100);
                if (CompareLastLog("patching smdh"))
                {
                    break;
                }
            }
            if (timeout) // If not response, return timeout
            {
                Report("NTR: Button press failed, try to free buttons");
                quickbuton(LookupTable.NoButtons, 250);
                return false;
            }
            else
            { // Free the buttons
                buttonByte = BitConverter.GetBytes(LookupTable.NoButtons);
                WonderTradeBot.WonderTradeBot.scriptHelper.write(buttonsOff, buttonByte, hid_pid);
                setTimer(maxtimeout);
                while (!timeout)
                { // Timeout 2
                    await Task.Delay(100);
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
            WonderTradeBot.WonderTradeBot.scriptHelper.write(buttonsOff, buttonByte, hid_pid);
            await Task.Delay(200);
            buttonByte = BitConverter.GetBytes(LookupTable.NoButtons);
            WonderTradeBot.WonderTradeBot.scriptHelper.write(buttonsOff, buttonByte, hid_pid);
            await Task.Delay(500);
        }

        public async Task ScriptButtonTimed(uint key, int time)
        {
            Report($"Script: Send button command 0x{key.ToString("X3")} during {time} ms");
            byte[] buttonByte = BitConverter.GetBytes(key);
            WonderTradeBot.WonderTradeBot.scriptHelper.write(buttonsOff, buttonByte, hid_pid);
            await Task.Delay(time);
            buttonByte = BitConverter.GetBytes(LookupTable.NoButtons);
            WonderTradeBot.WonderTradeBot.scriptHelper.write(buttonsOff, buttonByte, hid_pid);
            await Task.Delay(500);
        }

        public async Task ScriptButtonHold(uint key)
        {
            Report($"Script: Send and hold button command 0x{key.ToString("X3")}");
            byte[] buttonByte = BitConverter.GetBytes(key);
            WonderTradeBot.WonderTradeBot.scriptHelper.write(buttonsOff, buttonByte, hid_pid);
            await Task.Delay(500);
        }

        public async Task ScriptButtonRelease()
        {
            Report($"Script: Release all buttons");
            byte[] buttonByte = BitConverter.GetBytes(LookupTable.NoButtons);
            WonderTradeBot.WonderTradeBot.scriptHelper.write(buttonsOff, buttonByte, hid_pid);
            await Task.Delay(500);
        }

        // Touch Screen Handler
        public async Task<bool> waittouch(decimal Xcoord, decimal Ycoord)
        {
            Report("NTR: Touch the screen at " + Xcoord.ToString("F0") + "," + Ycoord.ToString("F0"));
            // Get and send hex coordinates
            byte[] buttonByte = BitConverter.GetBytes(gethexcoord(Xcoord, Ycoord));
            WonderTradeBot.WonderTradeBot.scriptHelper.write(touchscrOff, buttonByte, hid_pid);
            setTimer(maxtimeout);
            while (!timeout)
            { // Timeout 1
                await Task.Delay(100);
                if (CompareLastLog("finished"))
                {
                    break;
                }
            }
            if (timeout) // If not response, return timeout
            {
                Report("NTR: Button press failed, try to free the touchscreen");
                freetouch();
                return false;
            }
            else
            {  // Free the touch screen
                buttonByte = BitConverter.GetBytes(LookupTable.NoTouch);
                WonderTradeBot.WonderTradeBot.scriptHelper.write(touchscrOff, buttonByte, hid_pid);
                setTimer(maxtimeout);
                while (!timeout)
                { // Timeout 2
                    await Task.Delay(100);
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

        public async Task<bool> waitholdtouch(decimal Xcoord, decimal Ycoord)
        {
            Report("NTR: Touch the screen and hold at " + Xcoord.ToString("F0") + "," + Ycoord.ToString("F0"));
            // Get and send hex coordinates
            byte[] buttonByte = BitConverter.GetBytes(gethexcoord(Xcoord, Ycoord));
            WonderTradeBot.WonderTradeBot.scriptHelper.write(touchscrOff, buttonByte, hid_pid);
            setTimer(maxtimeout);
            while (!timeout)
            { // Timeout
                await Task.Delay(100);
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

        public async Task<bool> waitfreetouch()
        {
            Report("NTR: Free the touch screen");
            // Get and send hex coordinates
            byte[] buttonByte = BitConverter.GetBytes(LookupTable.NoTouch);
            WonderTradeBot.WonderTradeBot.scriptHelper.write(touchscrOff, buttonByte, hid_pid);
            setTimer(maxtimeout);
            while (!timeout)
            { // Timeout
                await Task.Delay(100);
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

        public async void quicktouch(decimal Xcoord, decimal Ycoord, int time)
        {
            Report("NTR: Touch the screen at " + Xcoord.ToString("F0") + "," + Ycoord.ToString("F0") + " during " + time + " ms");
            byte[] buttonByte = BitConverter.GetBytes(gethexcoord(Xcoord, Ycoord));
            WonderTradeBot.WonderTradeBot.scriptHelper.write(touchscrOff, buttonByte, hid_pid);
            await Task.Delay(time);
            buttonByte = BitConverter.GetBytes(LookupTable.NoTouch);
            WonderTradeBot.WonderTradeBot.scriptHelper.write(touchscrOff, buttonByte, hid_pid);
            Report("NTR: Touch screen command sent, no feedback provided");
        }

        public async void holdtouch(decimal Xcoord, decimal Ycoord)
        {
            Report("NTR: Touch the screen and hold at " + Xcoord.ToString("F0") + "," + Ycoord.ToString("F0"));
            byte[] buttonByte = BitConverter.GetBytes(gethexcoord(Xcoord, Ycoord));
            WonderTradeBot.WonderTradeBot.scriptHelper.write(touchscrOff, buttonByte, hid_pid);
            await Task.Delay(100);
            Report("NTR: Touch screen command sent, no feedback provided");
        }

        public async void freetouch()
        {
            Report("NTR: Free the touch screen");
            byte[] buttonByte = BitConverter.GetBytes(LookupTable.NoTouch);
            WonderTradeBot.WonderTradeBot.scriptHelper.write(touchscrOff, buttonByte, hid_pid);
            await Task.Delay(100);
            Report("NTR: Touch screen command sent, no feedback provided");
        }

        private uint gethexcoord(decimal Xvalue, decimal Yvalue)
        {
            uint hexX = Convert.ToUInt32(Math.Round(Xvalue * 0xFFF / 319));
            uint hexY = Convert.ToUInt32(Math.Round(Yvalue * 0xFFF / 239));
            return 0x01000000 + hexY * 0x1000 + hexX;
        }

        public async Task ScriptTouch(int Xvalue, int Yvalue)
        {
            Report($"Script: Touch screen at {Xvalue}, {Yvalue}");
            byte[] touchByte = BitConverter.GetBytes(gethexcoord(Xvalue, Yvalue));
            WonderTradeBot.WonderTradeBot.scriptHelper.write(touchscrOff, touchByte, hid_pid);
            await Task.Delay(200);
            touchByte = BitConverter.GetBytes(LookupTable.NoTouch);
            WonderTradeBot.WonderTradeBot.scriptHelper.write(touchscrOff, touchByte, hid_pid);
            await Task.Delay(500);
        }

        public async Task ScriptTouchTimed(int Xvalue, int Yvalue, int time)
        {
            Report($"Script: Touch screen at {Xvalue}, {Yvalue} during {time} ms");
            byte[] touchByte = BitConverter.GetBytes(gethexcoord(Xvalue, Yvalue));
            WonderTradeBot.WonderTradeBot.scriptHelper.write(touchscrOff, touchByte, hid_pid);
            await Task.Delay(time);
            touchByte = BitConverter.GetBytes(LookupTable.NoTouch);
            WonderTradeBot.WonderTradeBot.scriptHelper.write(touchscrOff, touchByte, hid_pid);
            await Task.Delay(500);
        }

        public async Task ScriptTouchHold(int Xvalue, int Yvalue)
        {
            Report($"Script: Touch screen and hold at {Xvalue}, {Yvalue}");
            byte[] touchByte = BitConverter.GetBytes(gethexcoord(Xvalue, Yvalue));
            WonderTradeBot.WonderTradeBot.scriptHelper.write(touchscrOff, touchByte, hid_pid);
            await Task.Delay(500);
        }

        public async Task ScriptTouchRelease()
        {
            Report($"Script: Release touch screen");
            byte[] touchByte = BitConverter.GetBytes(LookupTable.NoTouch);
            WonderTradeBot.WonderTradeBot.scriptHelper.write(touchscrOff, touchByte, hid_pid);
            await Task.Delay(500);
        }

        // Control Stick Handler
        public async Task<bool> waitsitck(int Xvalue, int Yvalue)
        {
            Report("NTR: Move Control Stick to " + Xvalue.ToString("D3") + "," + Yvalue.ToString("D3"));
            // Get and send hex coordinates
            byte[] buttonByte = BitConverter.GetBytes(getstickhex(Xvalue, Yvalue));
            WonderTradeBot.WonderTradeBot.scriptHelper.write(stickOff, buttonByte, hid_pid);
            setTimer(maxtimeout);
            while (!timeout)
            { // Timeout 1
                await Task.Delay(100);
                if (CompareLastLog("finished"))
                {
                    break;
                }
            }
            if (timeout) // If not response, return timeout
            {
                Report("NTR: Control stick command failed, try to release it");
                quickstick(0, 0, 250);
                freetouch();
                return false;
            }
            else
            { // Free the control stick
                buttonByte = BitConverter.GetBytes(LookupTable.NoStick);
                WonderTradeBot.WonderTradeBot.scriptHelper.write(stickOff, buttonByte, hid_pid);
                setTimer(maxtimeout);
                while (!timeout)
                { // Timeout 2
                    await Task.Delay(100);
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

        public async void quickstick(int Xvalue, int Yvalue, int time)
        {
            Report("NTR: Move Control Stick to " + Xvalue.ToString("D3") + "," + Yvalue.ToString("D3") + " during " + time + " ms");
            byte[] buttonByte = BitConverter.GetBytes(getstickhex(Xvalue, Yvalue));
            WonderTradeBot.WonderTradeBot.scriptHelper.write(stickOff, buttonByte, hid_pid);
            await Task.Delay(time);
            buttonByte = BitConverter.GetBytes(LookupTable.NoStick);
            WonderTradeBot.WonderTradeBot.scriptHelper.write(stickOff, buttonByte, hid_pid);
            Report("NTR: Control Stick command sent, no feedback provided");
        }

        private uint getstickhex(int Xvalue, int Yvalue)
        {
            uint hexX = Convert.ToUInt32((Xvalue + 100) * 0xFFF / 200);
            uint hexY = Convert.ToUInt32((Yvalue + 100) * 0xFFF / 200);
            if (hexX >= 0x1000) hexX = 0xFFF;
            if (hexY >= 0x1000) hexY = 0xFFF;
            return 0x01000000 + hexY * 0x1000 + hexX;
        }

        public async Task ScriptStick(int Xvalue, int Yvalue)
        {
            Report($"Script: Move and release the control stick to {Xvalue}, {Yvalue}");
            byte[] stickByte = BitConverter.GetBytes(getstickhex(Xvalue, Yvalue));
            WonderTradeBot.WonderTradeBot.scriptHelper.write(stickOff, stickByte, hid_pid);
            await Task.Delay(200);
            stickByte = BitConverter.GetBytes(LookupTable.NoStick);
            WonderTradeBot.WonderTradeBot.scriptHelper.write(stickOff, stickByte, hid_pid);
            await Task.Delay(500);
        }

        public async Task ScriptStickTimed(int Xvalue, int Yvalue, int time)
        {
            Report($"Script: Move the control stick to {Xvalue}, {Yvalue} during {time} ms");
            byte[] stickByte = BitConverter.GetBytes(getstickhex(Xvalue, Yvalue));
            WonderTradeBot.WonderTradeBot.scriptHelper.write(stickOff, stickByte, hid_pid);
            await Task.Delay(time);
            stickByte = BitConverter.GetBytes(LookupTable.NoStick);
            WonderTradeBot.WonderTradeBot.scriptHelper.write(stickOff, stickByte, hid_pid);
            await Task.Delay(500);
        }

        public async Task ScriptStickHold(int Xvalue, int Yvalue)
        {
            Report($"Script: Move and hold the control stick to {Xvalue}, {Yvalue}");
            byte[] stickByte = BitConverter.GetBytes(getstickhex(Xvalue, Yvalue));
            WonderTradeBot.WonderTradeBot.scriptHelper.write(stickOff, stickByte, hid_pid);
            await Task.Delay(500);
        }

        public async Task ScriptStickRelease()
        {
            Report($"Script: Release the control stick");
            byte[] stickByte = BitConverter.GetBytes(LookupTable.NoStick);
            WonderTradeBot.WonderTradeBot.scriptHelper.write(stickOff, stickByte, hid_pid);
            await Task.Delay(500);
        }

        // Memory Read Handler
        private void handleMemoryRead(object args_obj)
        {
            DataReadyWaiting args = (DataReadyWaiting)args_obj;
            lastRead = BitConverter.ToUInt32(args.data, 0);
        }

        private void handlemulitMemoryRead(object args_obj)
        {
            DataReadyWaiting args = (DataReadyWaiting)args_obj;
            lastmultiread = args.data;
        }

        public async Task<bool> waitNTRread(uint address)
        {
            Report("NTR: Read data at address 0x" + address.ToString("X8"));
            lastRead = 0;
            DataReadyWaiting myArgs = new DataReadyWaiting(new byte[0x04], handleMemoryRead, null);
            AddWaitingForData(WonderTradeBot.WonderTradeBot.scriptHelper.data(address, 0x04, pid), myArgs);
            setTimer(maxtimeout);
            while (!timeout)
            {
                await Task.Delay(100);
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

        public async Task<bool> waitNTRmultiread(uint address, uint size)
        {
            Report("NTR: Read " + size + " bytes of data starting at address 0x" + address.ToString("X8"));
            lastmultiread = new byte[] { };
            DataReadyWaiting myArgs = new DataReadyWaiting(new byte[size], handlemulitMemoryRead, null);
            AddWaitingForData(WonderTradeBot.WonderTradeBot.scriptHelper.data(address, size, pid), myArgs);
            setTimer(maxtimeout);
            while (!timeout)
            {
                await Task.Delay(100);
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

        private void handlePokeRead(object args_obj)
        {
            DataReadyWaiting args = (DataReadyWaiting)args_obj;
            if (WonderTradeBot.WonderTradeBot.SaveFileEditor2.SAV.Generation == 6)
            {
                validator = new PK6(PKX.DecryptArray(args.data));
            }
            else
            {
                validator = new PK7(PKX.DecryptArray(args.data));
            }
        }

        public async Task<PKM> waitPokeRead(NumericUpDown boxCtrl, NumericUpDown slotCtrl)
        {
            try
            {
                int box = (int)boxCtrl.Value - 1;
                int slot = (int)slotCtrl.Value - 1;
                Report("NTR: Read pokémon data at box " + (box + 1) + ", slot " + (slot + 1));
                // Get offset
                uint dumpOff = LookupTable.BoxOffset + (Convert.ToUInt32(box * BOXSIZE + slot) * POKEBYTES);
                DataReadyWaiting myArgs = new DataReadyWaiting(new byte[POKEBYTES], handlePokeRead, null);
                WonderTradeBot.WonderTradeBot.UpdateDumpBoxes(box, slot);
                AddWaitingForData(WonderTradeBot.WonderTradeBot.scriptHelper.data(dumpOff, POKEBYTES, pid), myArgs);
                setTimer(maxtimeout);
                while (!timeout)
                {
                    await Task.Delay(100);
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

        public async Task<PKM> waitPokeRead(uint offset)
        {
            try
            {
                Report("NTR: Read pokémon data at offset 0x" + offset.ToString("X8"));
                DataReadyWaiting myArgs = new DataReadyWaiting(new byte[POKEBYTES], handlePokeRead, null);
                AddWaitingForData(WonderTradeBot.WonderTradeBot.scriptHelper.data(offset, POKEBYTES, pid), myArgs);
                setTimer(maxtimeout);
                while (!timeout)
                {
                    await Task.Delay(100);
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

        public async Task<PKM> waitPartyRead(uint slot)
        {
            try
            {
                Report("NTR: Read pokémon data at party slot " + slot);
                DataReadyWaiting myArgs = new DataReadyWaiting(new byte[PARTYBYTES], handlePokeRead, null);
                uint offset = LookupTable.PartyOffset + 484 * (slot - 1);
                AddWaitingForData(WonderTradeBot.WonderTradeBot.scriptHelper.data(offset, PARTYBYTES, pid), myArgs);
                setTimer(maxtimeout);
                while (!timeout)
                {
                    await Task.Delay(100);
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

        public async Task<bool> memoryinrange(uint address, uint value, uint range)
        {
            Report("NTR: Read data at address 0x" + address.ToString("X8"));
            Report("NTR: Expected value 0x" + value.ToString("X8") + " to 0x" + (value + range - 1).ToString("X8"));
            lastRead = value + range;
            DataReadyWaiting myArgs = new DataReadyWaiting(new byte[0x04], handleMemoryRead, null);
            AddWaitingForData(WonderTradeBot.WonderTradeBot.scriptHelper.data(address, 0x04, pid), myArgs);
            setTimer(maxtimeout);
            while (!timeout)
            {
                await Task.Delay(100);
                if (CompareLastLog("finished"))
                {
                    break;
                }
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

        public async Task<bool> timememoryinrange(uint address, uint value, uint range, int tick, int maxtime)
        {
            Report("NTR: Read data at address 0x" + address.ToString("X8") + " during " + maxtime + " ms");
            Report("NTR: Expected value 0x" + value.ToString("X8") + " to 0x" + (value + range - 1).ToString("X8"));
            int readcount = 0;
            setTimer(maxtime);
            while (!timeout || readcount < 5)
            { // Ask for data
                lastRead = value + range;
                DataReadyWaiting myArgs = new DataReadyWaiting(new byte[0x04], handleMemoryRead, null);
                AddWaitingForData(WonderTradeBot.WonderTradeBot.scriptHelper.data(address, 0x04, pid), myArgs);
                // Wait for data
                while (!timeout)
                {
                    await Task.Delay(100);
                    if (CompareLastLog("finished"))
                    {
                        break;
                    }
                    if (timeout && readcount < 5)
                    {
                        Report("NTR: Restarting timeout");
                        setTimer(maxtimeout);
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
                    await Task.Delay(tick);
                }
                if (timeout && readcount < 5)
                {
                    Report("NTR: Restarting timeout");
                    setTimer(maxtimeout);
                }
                readcount++;
            }
            Report("NTR: Read failed or outside of range");
            return false;
        }

        // Memory Write handler
        public async Task<bool> waitNTRwrite(uint address, uint data, int pid)
        {
            Report("NTR: Write value 0x" + data.ToString("X8") + " at address 0x" + address.ToString("X8"));
            byte[] command = BitConverter.GetBytes(data);
            WonderTradeBot.WonderTradeBot.scriptHelper.write(address, command, pid);
            setTimer(maxtimeout);
            while (!timeout)
            { // Timeout 1
                await Task.Delay(100);
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

        public async Task<bool> waitNTRwrite(uint address, byte[] data, int pid)
        {
            Report("NTR: Write " + data.Length + " bytes at address 0x" + address.ToString("X8"));
            WonderTradeBot.WonderTradeBot.scriptHelper.write(address, data, pid);
            setTimer(maxtimeout);
            while (!timeout)
            { // Timeout 1
                await Task.Delay(100);
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
        private void setTimer(int time)
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
