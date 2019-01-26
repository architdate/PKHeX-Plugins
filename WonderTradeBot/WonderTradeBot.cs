using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using PKHeX.Core;
using pkmn_ntr.Helpers;

namespace WonderTradeBot
{
    public partial class WonderTradeBot : Form, IPlugin
    {
        private readonly Timer timer1 = new Timer();
        private static NumericUpDown boxDump; // no idea why this is done like this.......
        private static NumericUpDown slotDump;
        string IPlugin.Name { get; } = "Wondertrade Gen 7";

        public int Priority => 1;
        public ISaveFileProvider SaveFileEditor { get; private set; }
        public static ISaveFileProvider SaveFileEditor2 { get; private set; }
        public IPKMView PKMEditor { get; private set; }
        public static IPKMView PKMEditor2 { get; private set; }
        public object[] arguments;
        public static RemoteControl helper;
        public static ScriptHelper scriptHelper;
        public static NTR ntrClient;
        public int pid; // apparently pokemon pid? wtf lol

        public void Initialize(params object[] args)
        {
            arguments = args;
            Debug.WriteLine($"[Auto Legality Mod] Loading {Name}");
            if (args == null)
                return;
            SaveFileEditor = (ISaveFileProvider)Array.Find(args, z => z is ISaveFileProvider);
            SaveFileEditor2 = SaveFileEditor;
            PKMEditor = (IPKMView)Array.Find(args, z => z is IPKMView);
            PKMEditor2 = PKMEditor;
            var menu = (ToolStrip)Array.Find(args, z => z is ToolStrip);
            LoadMenuStrip(menu);
            boxDump = Box;
            slotDump = Slot;
        }

        private void LoadMenuStrip(ToolStrip menuStrip)
        {
            var items = menuStrip.Items;
            var tools = items.Find("Menu_Tools", false)[0] as ToolStripDropDownItem;
            var toolsitems = tools.DropDownItems;
            var modmenusearch = toolsitems.Find("Menu_AutoLegality", false);
            if (modmenusearch.Length == 0)
            {
                var mod = new ToolStripMenuItem("Auto Legality Mod");
                tools.DropDownItems.Insert(0, mod);
                mod.Name = "Menu_AutoLegality";
                var modmenu = mod;
                AddPluginControl(modmenu);
            }
            else
            {
                var modmenu = modmenusearch[0] as ToolStripMenuItem;
                AddPluginControl(modmenu);
            }
        }

        private void AddPluginControl(ToolStripMenuItem tools)
        {
            var ctrl = new ToolStripMenuItem(Name);
            tools.DropDownItems.Add(ctrl);
            // Add click function
        }

        public void NotifySaveLoaded()
        {
            Console.WriteLine($"{Name} was notified that a Save File was just loaded.");
        }

        public bool TryLoadFile(string filePath)
        {
            Console.WriteLine($"{Name} was provided with the file path, but chose to do nothing with it.");
            return false; // no action taken
        }

        // General bot variables
        private bool botworking;
        private bool userstop;
        private BotState botstate;
        private BotErrorMessage botresult;
        private int attempts;
        private int maxreconnect;
        private Task<bool> waitTaskbool;
        private Task<PKM> waitTaskPKM;

        // Class bot variables
        private bool boxchange;
        private bool notradepartner;
        private bool tradeevo;
        private bool isUSUM;
        private decimal startbox;
        private decimal startslot;
        private decimal starttrades;
        private int currentfile;
        private List<PKM> pklist;
        private PKM WTpoke;
        private PKM validator;
        private Random RNG;
        private string backuppath;
        private string[] pkfiles;
        private Timer tradeTimer;
        private uint currentTotalFC;
        private uint currentFC;
        private uint nextFC;
        private ushort currentCHK;

        // Class constants
        private readonly int commandtime = 250;
        private readonly int delaytime = 250;
        private static readonly string wtfolderpath = Path.Combine(Application.StartupPath, "Wonder Trade");

        // Data offsets
        private uint TrademenuOff
        {
            get
            {
                switch (SaveFileEditor.SAV.Version)
                {
                    case GameVersion.SN:
                    case GameVersion.MN:
                        return 0x6749DC; // 1.0: 0x672790; 1.1: 0x6749D4;
                    case GameVersion.US:
                        return 0x6A62E6; // 1.1: 0x6A6264;
                    case GameVersion.UM:
                        return 0x6A62E6; // 1.1: 0x6A6268;
                    default:
                        return 0;
                }
            }
        }

        private uint TrademenuIN
        {
            get
            {
                switch (SaveFileEditor.SAV.Version)
                {
                    case GameVersion.SN:
                    case GameVersion.MN:
                        return 0xC2D00000; // 1.0 :0x41200000;
                    case GameVersion.US:
                    case GameVersion.UM:
                        return 0xC2D0; // 1.1: 0xC30E0000;
                    default:
                        return 0;
                }
            }
        }

        private uint TrademenuOUT
        {
            get
            {
                switch (SaveFileEditor.SAV.Version)
                {
                    case GameVersion.SN:
                    case GameVersion.MN:
                        return 0xC2C00000; // 1.0: 0x41B80000;
                    case GameVersion.US:
                    case GameVersion.UM:
                        return 0x3E98; // 1.1: 0xC30B0000;
                    default:
                        return 0;
                }
            }
        }

        private uint TrademenuRange
        {
            get
            {
                switch (SaveFileEditor.SAV.Version)
                {
                    case GameVersion.SN:
                    case GameVersion.MN:
                        return 0x100000; // 1.0: 0x41B80000;
                    case GameVersion.US:
                    case GameVersion.UM:
                        return 0x1; // 1.1: 0x10000;
                    default:
                        return 0;
                }
            }
        }

        private uint Tradeready
        {
            get
            {
                switch (SaveFileEditor.SAV.Version)
                {
                    case GameVersion.SN:
                    case GameVersion.MN:
                        return 0x3F800000;
                    case GameVersion.US:
                    case GameVersion.UM:
                        return 0x3F80; // 1.1: 0xBF800000;
                    default:
                        return 0;
                }
            }
        }

        private uint TradereadyRange
        {
            get
            {
                switch (SaveFileEditor.SAV.Version)
                {
                    case GameVersion.SN:
                    case GameVersion.MN:
                        return 0x100000;
                    case GameVersion.US:
                    case GameVersion.UM:
                        return 0x1;
                    default:
                        return 0;
                }
            }
        }

        private uint TradeEvoValue
        {
            get
            {
                switch (SaveFileEditor.SAV.Version)
                {
                    case GameVersion.SN:
                    case GameVersion.MN:
                        return 0x100000;
                    case GameVersion.US:
                    case GameVersion.UM:
                        return 0x1;
                    default:
                        return 0;
                }
            }
        }

        private uint WtscreenOff
        {
            get
            {
                switch (SaveFileEditor.SAV.Version)
                {
                    case GameVersion.SN:
                    case GameVersion.MN:
                        return 0x6703F0; // 1.0: 0x10F1D0; 1.1: 0x6703E8;
                    case GameVersion.US:
                        return 0x6A62B2; // 1.1: 0x10FCB8; //return 0x672180;
                    case GameVersion.UM:
                        return 0x6A62B2; // 1.1: 0x10FCBC; //return 0x672184;
                    default:
                        return 0;
                }
            }
        }

        private uint WtscreenIN
        {
            get
            {
                switch (SaveFileEditor.SAV.Version)
                {
                    case GameVersion.SN:
                    case GameVersion.MN:
                        return 0x00000000; // 1.0: 0x520000;
                    case GameVersion.US:
                    case GameVersion.UM:
                        return 0; // return 0x006E0D64;
                    default:
                        return 0;
                }
            }
        }

        //private uint wtscreenOUT = 0x01; // 1.0 0x720000; 0x00 USUM
        private uint WtscreenRange
        {
            get
            {
                switch (SaveFileEditor.SAV.Version)
                {
                    case GameVersion.SN:
                    case GameVersion.MN:
                        return 0x1; // 1.0: 0x520000;
                    case GameVersion.US:
                    case GameVersion.UM:
                        return 0x41; // return 0x10;
                    default:
                        return 0;
                }
            }
        }

        private uint BoxesOff
        {
            get
            {
                switch (SaveFileEditor.SAV.Version)
                {
                    case GameVersion.SN:
                    case GameVersion.MN:
                        return 0x674828; //1.0: 0x10F1A0; 1.1: 0x674820;
                    case GameVersion.US:
                        return 0x6A6132; // return 0x66EA24;
                    case GameVersion.UM:
                        return 0x006A6132; // return 0x66EA28;
                    default:
                        return 0;
                }
            }
        }

        private uint BoxesIN
        {
            get
            {
                switch (SaveFileEditor.SAV.Version)
                {
                    case GameVersion.SN:
                    case GameVersion.MN:
                        return 0x42210000; // 1.0: 0x6F0000;
                    case GameVersion.US:
                    case GameVersion.UM:
                        return 0x4221; // return 0x47;
                    default:
                        return 0;
                }
            }
        }

        private uint BoxesOUT
        {
            get
            {
                switch (SaveFileEditor.SAV.Version)
                {
                    case GameVersion.SN:
                    case GameVersion.MN:
                        return 0x42220000; // 1.0: 0x520000;
                    case GameVersion.US:
                    case GameVersion.UM:
                        return 0x4222; // return 0x45;
                    default:
                        return 0;
                }
            }
        }

        private uint BoxesRange
        {
            get
            {
                switch (SaveFileEditor.SAV.Version)
                {
                    case GameVersion.SN:
                    case GameVersion.MN:
                        return 0x10000; // 1.0: 0x520000;
                    case GameVersion.US:
                    case GameVersion.UM:
                        return 0x1;
                    default:
                        return 0;
                }
            }
        }

        //private uint boxesviewOff = 0x672D04;
        //private uint boxesviewIN = 0x00000000;
        //private uint boxesviewOUT = 0x41000000;

        private const uint dialogOff = 0x6747E0; // 1.0: 0x63DD68; 1.1: 0x6747D8;

        private const uint dialogIn = 0x00000000; // 1.0: 0x0C;

        private const uint dialogOut = 0x41B80000; // 1.0: 0x0B;

        private const uint toppkmOff = 0x30000298;

        private bool botWorking;

        /// <summary>
        /// Constructor, changes GUI based on game.
        /// </summary>
        public void Bot_WonderTrade7()
        {
            InitializeComponent();
            RNG = new Random();
            if (GameVersion.USUM.Contains(SaveFileEditor.SAV.Version))
            {
                isUSUM = true;
                collectFC.Visible = false;
            }
        }

        /// <summary>
        /// Start or stop the bot.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RunStop_Click(object sender, EventArgs e)
        {
            DisableControls();
            if (botworking)
            { // Stop bot
                Delg.SetEnabled(RunStop, false);
                Delg.SetText(RunStop, "Start Bot");
                botworking = false;
                userstop = true;
            }
            else
            { // Run bot
                DialogResult dialogResult = MessageBox.Show("This scirpt will try to " +
                    "Wonder Trade " + Trades.Value + " pokémon, starting from the slot "
                    + Slot.Value + " of box " + Box.Value + ". Remember to read the " +
                    "wiki for this bot in GitHub before starting.\r\n\r\nDo you want " +
                    "to continue?", "Wonder Trade Bot", MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);
                if (dialogResult == DialogResult.Yes && Trades.Value > 0)
                {
                    // Configure GUI
                    Delg.SetText(RunStop, "Stop Bot");
                    if (isUSUM)
                    {
                        collectFC.Checked = false;
                    }
                    // Initialize variables
                    botworking = true;
                    userstop = false;
                    botstate = BotState.StartBot;
                    attempts = 0;
                    maxreconnect = 10;
                    boxchange = true;
                    notradepartner = false;
                    tradeevo = false;
                    currentfile = 0;
                    pklist = new List<PKM>();
                    tradeTimer = new Timer
                    {
                        Interval = 95000 // Trade timeout, 95 s
                    };
                    tradeTimer.Tick += TradeTimer_Tick;
                    startbox = Box.Value;
                    startslot = Slot.Value;
                    starttrades = Trades.Value;
                    // Run the bot
                    RunBot();
                }
                else
                {
                    EnableControls();
                }
            }
        }

        /// <summary>
        /// Disables the controls in the form.
        /// </summary>
        private void DisableControls()
        {
            Delg.SetEnabled(Box, false);
            Delg.SetEnabled(Slot, false);
            Delg.SetEnabled(Trades, false);
            Delg.SetEnabled(WTSource, false);
            Delg.SetEnabled(WTAfter, false);
            Delg.SetEnabled(collectFC, false);
            Delg.SetEnabled(runEndless, false);
        }

        /// <summary>
        /// Enables the controls in the form.
        /// </summary>
        private void EnableControls()
        {
            Delg.SetEnabled(Box, true);
            Delg.SetEnabled(Slot, true);
            Delg.SetEnabled(Trades, true);
            Delg.SetEnabled(WTSource, true);
            Delg.SetEnabled(WTAfter, true);
            Delg.SetEnabled(collectFC, true);
            Delg.SetEnabled(runEndless, true);
        }

        public void SetBotMode(bool state)
        {
            botWorking = state;
            if (state)
            {
                timer1.Interval = 500;
            }
            else
            {
                timer1.Interval = 1000;
            }
        }

        public void WriteDataToFile(byte[] data, string path)
        {
            try
            {
                File.WriteAllBytes(path, data);
            }
            catch (Exception ex)
            {
                MessageBox.Show("A error has ocurred:\r\n\r\n" + ex.Message);
            }
        }

        /// <summary>
        /// Bot procedure.
        /// </summary>
        public async void RunBot()
        {
            try
            {
                SetBotMode(true);
                while (botworking)
                {
                    switch (botstate)
                    {
                        case BotState.StartBot:
                            botstate = BotState.BackupBoxes;
                            break;

                        case BotState.CheckMode:
                            if (collectFC.Checked)
                                botstate = BotState.InitializeFC1;
                            else if (sourceBox.Checked)
                                botstate = BotState.ReadPoke;
                            else
                                botstate = BotState.ReadFolder;
                            break;

                        case BotState.BackupBoxes: await BotBackupBoxes().ConfigureAwait(false); break;
                        case BotState.InitializeFC1: await BotInitializeFC1().ConfigureAwait(false); break;
                        case BotState.InitializeFC2: await BotInitializeFC2().ConfigureAwait(false); break;
                        case BotState.ReadPoke: await BotReadPoke().ConfigureAwait(false); break;
                        case BotState.ReadFolder: BotReadFolder(); break;
                        case BotState.WriteFromFolder: await BotWriteFolder().ConfigureAwait(false); break;
                        case BotState.WriteLastBox: await BotWriteLastBox().ConfigureAwait(false); break;
                        case BotState.PressTradeButton: await BotPressTradeButton().ConfigureAwait(false); break;
                        case BotState.TestTradeMenu: await BotTestTradeMenu().ConfigureAwait(false); break;
                        case BotState.PressWTButton: await BotPressWTButton().ConfigureAwait(false); break;
                        case BotState.TestWTScreen: await BotTestWTScreen().ConfigureAwait(false); break;
                        case BotState.PressWTstart: await BotPressWTStart().ConfigureAwait(false); break;
                        case BotState.TestBoxes: await BotTestBoxes().ConfigureAwait(false); break;
                        case BotState.TouchPoke: await BotTouchPoke().ConfigureAwait(false); break;
                        case BotState.TestPoke: await BotTestPoke().ConfigureAwait(false); break;
                        case BotState.CancelTouch: await BotCancelTouch().ConfigureAwait(false); break;
                        case BotState.StartTrade: await BotStartTrade().ConfigureAwait(false); break;
                        case BotState.ConfirmTrade: await BotConfirmTrade().ConfigureAwait(false); break;
                        case BotState.TestBoxesOut: await BotTestBoxesOut().ConfigureAwait(false); break;
                        case BotState.WaitForTrade: await BotWaitForTrade().ConfigureAwait(false); break;
                        case BotState.TestTradeFinish: await BotTestTradeFinish().ConfigureAwait(false); break;
                        case BotState.TryFinish: await BotTryFinish().ConfigureAwait(false); break;
                        case BotState.CollectFC1: await BotCollectFC1().ConfigureAwait(false); break;
                        case BotState.CollectFC2: await BotCollectFC2().ConfigureAwait(false); break;
                        case BotState.CollectFC3: await BotCollectFC3().ConfigureAwait(false); break;
                        case BotState.CollectFC4: await BotCollectFC4().ConfigureAwait(false); break;
                        case BotState.CollectFC5: await BotCollectFC5().ConfigureAwait(false); break;
                        case BotState.DumpAfter: await BotDumpAfter().ConfigureAwait(false); break;
                        case BotState.RestoreBackup: await BotRestoreBackup().ConfigureAwait(false); break;
                        case BotState.DeletePoke: await BotDeletePoke().ConfigureAwait(false); break;

                        case BotState.FinishTrade:
                            if (notradepartner)
                            {
                                RestartSlot();
                            }
                            else
                            {
                                GetNextSlot();
                            }
                            notradepartner = false;
                            tradeevo = false;
                            break;

                        case BotState.ActionAfter:
                            if (afterRestore.Checked)
                            {
                                botstate = BotState.RestoreBackup;
                            }
                            else if (afterDelete.Checked)
                            {
                                botstate = BotState.DeletePoke;
                            }
                            else
                            {
                                botresult = BotErrorMessage.Finished;
                                botstate = BotState.ExitBot;
                            }
                            break;

                        case BotState.ExitBot:
                            Report("Bot: Stop Gen 7 Wonder Trade bot");
                            botworking = false;
                            break;

                        default:
                            Report("Bot: Stop Gen 7 Wonder Trade bot");
                            botresult = BotErrorMessage.GeneralError;
                            botworking = false;
                            break;
                    }
                    if (attempts > 10)
                    { // Too many attempts
                        if (maxreconnect > 0)
                        {
                            Report("Bot: Try reconnection to fix error");
                            maxreconnect--;
                            if (await waitTaskbool)
                            {
                                await Task.Delay(10 * delaytime).ConfigureAwait(false);
                                attempts = 0;
                            }
                            else
                            {
                                botresult = BotErrorMessage.GeneralError;
                                botworking = false;
                            }
                        }
                        else
                        {
                            Report("Bot: Maximum number of reconnection attempts " +
                                "reached");
                            Report("Bot: STOP Gen 7 Wonder Trade bot");
                            botworking = false;
                        }
                    }
                }
                tradeTimer.Stop();
            }
            catch (Exception ex)
            {
                tradeTimer.Stop();
                Report("Bot: Exception detected:");
                Report(ex.Source);
                Report(ex.Message);
                Report(ex.StackTrace);
                Report("Bot: STOP Gen 7 Wonder Trade bot");
                MessageBox.Show(ex.Message);
                botworking = false;
                botresult = BotErrorMessage.GeneralError;
            }
            if (userstop)
            {
                botresult = BotErrorMessage.UserStop;
            }
            ShowResult("Wonder Trade bot", botresult);
            Delg.SetText(RunStop, "Start Bot");
            ntrClient.Disconnect();
            EnableControls();
            Delg.SetEnabled(RunStop, true);
        }

        private async Task BotDeletePoke()
        {
            Report("Bot: Delete traded pokémon");
            byte[] deletearray = new byte[232 * (int)starttrades];
            for (int i = 0; i < starttrades; i++)
            {
                SaveFileEditor.SAV.BlankPKM.EncryptedBoxData.CopyTo(deletearray, i * 232);
            }
            waitTaskbool = helper.WaitWriteNTR(GetBoxOffset(LookupTable.BoxOffset, Box, Slot), deletearray, pid);
            if (await waitTaskbool)
            {
                attempts = 0;
                botstate = BotState.ExitBot;
            }
            else
            {
                attempts++;
                botresult = BotErrorMessage.WriteError;
                botstate = BotState.DeletePoke;
            }
        }

        private async Task BotRestoreBackup()
        {
            Report("Bot: Restore boxes backup");
            byte[] restore = File.ReadAllBytes(backuppath);
            if (restore.Length == 232 * 30 * 32)
            {
                waitTaskbool = helper.WaitWriteNTR(LookupTable.BoxOffset, restore, pid);
                if (await waitTaskbool)
                {
                    attempts = 0;
                    botresult = BotErrorMessage.Finished;
                    botstate = BotState.ExitBot;
                }
                else
                {
                    attempts++;
                    botresult = BotErrorMessage.WriteError;
                    botstate = BotState.RestoreBackup;
                }
            }
            else
            {
                Report("Bot: Invalid boxes file");
                botresult = BotErrorMessage.GeneralError;
                botstate = BotState.ExitBot;
            }
        }

        private async Task BotDumpAfter()
        {
            if (afterDump.Checked)
            {
                Report("Bot: Dump boxes");
                waitTaskbool = helper.WaitReadNTRMulti(LookupTable.BoxOffset, 232 * 30 * 31);
                if (await waitTaskbool)
                {
                    attempts = 0;
                    string fileName = $"WTAfter-{DateTime.Now:yyyyMMddHHmmss}.ek7";
                    WriteDataToFile(helper
                        .lastmultiread, wtfolderpath + fileName);
                    botstate = BotState.ActionAfter;
                }
                else
                {
                    attempts++;
                    botresult = BotErrorMessage.ReadError;
                    botstate = BotState.DumpAfter;
                }
            }
            else
            {
                botstate = BotState.ActionAfter;
            }
        }

        private async Task BotCollectFC5()
        {
            Report("Bot: Test FC");
            waitTaskbool = helper.WaitReadNTR(LookupTable.TrainerTotalFCOffset);
            if (await waitTaskbool)
            {
                attempts = 0;
                currentTotalFC = helper.lastRead;
                Report($"Bot: Current Total FC: {currentTotalFC}");
                if (currentTotalFC >= nextFC)
                {
                    Report("Bot: Festival Plaza level up");
                    GetNextSlot();
                    botresult = BotErrorMessage.FestivalPlaza;
                    botstate = BotState.ExitBot;
                }
                else
                {
                    botstate = BotState.FinishTrade;
                }
            }
            else
            {
                attempts++;
                botresult = BotErrorMessage.ReadError;
                botstate = BotState.CollectFC5;
            }
        }

        private async Task BotCollectFC4()
        {
            Report("Bot: Test if dialog has finished");
            waitTaskbool = helper.IsMemoryInRange(dialogOff, dialogOut, 0x010000);
            if (await waitTaskbool || helper.lastRead == 0x0D)
            {
                attempts = 0;
                botstate = BotState.CollectFC5;
            }
            else
            {
                attempts++;
                botresult = BotErrorMessage.ReadError;
                botstate = BotState.CollectFC3;
            }
        }

        private async Task BotCollectFC3()
        {
            Report("Bot: Continue dialog");
            waitTaskbool = helper.ButtonWait(LookupTable.ButtonB);
            if (await waitTaskbool)
            {
                botstate = BotState.CollectFC4;
            }
            else
            {
                attempts++;
                botresult = BotErrorMessage.ButtonError;
                botstate = BotState.CollectFC3;
            }
        }

        private async Task BotCollectFC2()
        {
            Report("Bot: Test if dialog has started");
            waitTaskbool = helper.IsMemoryInRange(dialogOff, dialogIn, 0x010000);
            if (await waitTaskbool)
            {
                attempts = 0;
                botstate = BotState.CollectFC3;
            }
            else
            {
                attempts++;
                botresult = BotErrorMessage.ReadError;
                botstate = BotState.CollectFC1;
            }
        }

        private async Task BotCollectFC1()
        {
            Report("Bot: Trigger Dialog");
            await Task.Delay(4 * delaytime).ConfigureAwait(false);
            waitTaskbool = helper.ButtonWait(LookupTable.ButtonA);
            if (await waitTaskbool)
            {
                botstate = BotState.CollectFC2;
            }
            else
            {
                attempts++;
                botresult = BotErrorMessage.ButtonError;
                botstate = BotState.TestTradeFinish;
            }
        }

        private async Task BotTryFinish()
        {
            if (!tradeevo)
            {
                if (isUSUM)
                {
                    Report("Bot: Press A button");
                    waitTaskbool = helper.ButtonWait(LookupTable.ButtonA);
                }
                else
                {
                    Report("Bot: Press B button");
                    waitTaskbool = helper.ButtonWait(LookupTable.ButtonB);
                }
            }
            else
            {
                Report("Bot: Trade evolution detected, press A button");
                waitTaskbool = helper.ButtonWait(LookupTable.ButtonA);
            }
            if (await waitTaskbool)
            {
                botstate = BotState.TestTradeFinish;
            }
            else
            {
                attempts++;
                botresult = BotErrorMessage.ButtonError;
                botstate = BotState.TestTradeFinish;
            }
        }

        private async Task BotTestTradeFinish()
        {
            Report("Bot: Test if the trade is finished");
            if (isUSUM)
            {
                waitTaskbool = helper.IsMemoryInRange(WtscreenOff, WtscreenIN, 0x1);
            }
            else
            {
                waitTaskbool = helper.IsMemoryInRange(WtscreenIN, TrademenuOUT, TrademenuRange);
            }

            if (await waitTaskbool)
            {
                attempts = 0;
                botstate = collectFC.Checked && !notradepartner ? BotState.CollectFC1 : BotState.FinishTrade;
            }
            else
            {
                attempts++;
                botresult = BotErrorMessage.GeneralError;
                botstate = BotState.TryFinish;
                if (helper.lastRead == TradeEvoValue && !tradeevo)
                {
                    Report("Bot: Trade evolution detected, wait 20" +
                        " seconds");
                    await Task.Delay(20000).ConfigureAwait(false);
                    tradeevo = true;
                    attempts = -40; // Try 50 button presses.
                }
            }
        }

        private async Task BotWaitForTrade()
        {
            Report("Bot: Wait for trade");
            waitTaskbool = helper.IsMemoryInRange(TrademenuOff, Tradeready, TradereadyRange);
            if (await waitTaskbool)
            {
                tradeTimer.Stop();
                Report("Bot: Trade detected");
                await helper.WaitReadPoke((int)Box.Value - 1, (int)Slot.Value - 1).ConfigureAwait(false);
                Report("Bot: Wait 30 seconds");
                await Task.Delay(30000).ConfigureAwait(false);
                botstate = BotState.TestTradeFinish;
            }
            else if (notradepartner)
            { // Timeout
                boxchange = true; // Might fix a couple of errors
                botstate = BotState.TestTradeFinish;
            }
            else
            {
                await Task.Delay(8 * delaytime).ConfigureAwait(false);
            }
        }

        private async Task BotTestBoxesOut()
        {
            Report("Bot: Test if the boxes are not shown");
            waitTaskbool = helper.IsTimeMemoryInRange(BoxesOff, BoxesOUT, BoxesRange, 500, 10000);
            if (await waitTaskbool)
            {
                attempts = -40; // Try 50 button presses
                botstate = BotState.WaitForTrade;
                tradeTimer.Start();
            }
            else
            {
                attempts++;
                botresult = BotErrorMessage.ReadError;
                botstate = BotState.TouchPoke;
            }
        }

        private async Task BotConfirmTrade()
        {
            Report("Bot: Press Yes");
            waitTaskbool = helper.ButtonWait(LookupTable.ButtonA);
            if (await waitTaskbool)
            {
                botstate = BotState.TestBoxesOut;
            }
            else
            {
                attempts++;
                botresult = BotErrorMessage.ButtonError;
                botstate = BotState.ConfirmTrade;
            }
        }

        private async Task BotStartTrade()
        {
            Report("Bot: Press Start");
            waitTaskbool = helper.ButtonWait(LookupTable.ButtonA);
            if (await waitTaskbool)
            {
                botstate = BotState.ConfirmTrade;
            }
            else
            {
                attempts++;
                botresult = BotErrorMessage.ButtonError;
                botstate = BotState.StartTrade;
            }
        }

        private async Task BotCancelTouch()
        {
            Report("Bot: Cancel selection and check again");
            waitTaskPKM = helper.WaitReadPoke((int)Box.Value - 1, (int)Slot.Value - 1);
            WTpoke = await waitTaskPKM;
            if (WTpoke != null)
                currentCHK = WTpoke.Checksum;

            waitTaskbool = helper.ButtonWait(LookupTable.ButtonB);
            if (await waitTaskbool)
            {
                botstate = BotState.TouchPoke;
            }
            else
            {
                attempts++;
                botresult = BotErrorMessage.ButtonError;
                botstate = BotState.TouchPoke;
            }
        }

        private async Task BotTestPoke()
        {
            Report("Bot: Test if pokemon is selected");
            waitTaskPKM = helper.WaitReadPoke(toppkmOff);
            validator = await waitTaskPKM;
            if (validator == null)
            { // No data or invalid
                Report("Bot: Error detected or slot is empty");
                attempts++;
                botresult = BotErrorMessage.ReadError;
                botstate = BotState.CancelTouch;
            }
            else if (validator.Checksum != currentCHK)
            { // Different poke
                Report("Bot: Picked incorrect pokemon");
                attempts++;
                botresult = BotErrorMessage.GeneralError;
                botstate = BotState.CancelTouch;
            }
            else
            { // Correct pokemon
                attempts = 0;
                botstate = BotState.StartTrade;
            }
        }

        private async Task BotTouchPoke()
        {
            Report("Bot: Touch pokémon");
            await Task.Delay(4 * delaytime).ConfigureAwait(false);
            var slot = (uint)(Slot.Value - 1);
            waitTaskbool = helper.TouchWait(LookupTable.pokeposX7[slot], LookupTable.pokeposY7[slot]);
            if (await waitTaskbool)
            {
                botstate = BotState.TestPoke;
            }
            else
            {
                attempts++;
                botresult = BotErrorMessage.TouchError;
                botstate = BotState.TouchPoke;
            }
        }

        private async Task BotTestBoxes()
        {
            Report("Bot: Test if the boxes are shown");
            waitTaskbool = helper.IsTimeMemoryInRange(BoxesOff,
                BoxesIN, BoxesRange, 250, 5000);
            if (await waitTaskbool)
            {
                attempts = 0;
                botstate = BotState.TouchPoke;
            }
            else
            {
                attempts++;
                botresult = BotErrorMessage.ReadError;
                botstate = BotState.PressWTstart;
            }
        }

        private async Task BotPressWTStart()
        {
            Report("Bot: Press Start");
            await Task.Delay(4 * delaytime).ConfigureAwait(false);
            helper.ButtonQuick(LookupTable.ButtonA, commandtime);
            await Task.Delay(commandtime + delaytime).ConfigureAwait(false);
            botstate = BotState.TestBoxes;
        }

        private async Task BotWriteFolder()
        {
            Report("Bot: Write pkm file from list");
            if (sourceRandom.Checked)
            { // Select a random file
                currentfile = RNG.Next() % pklist.Count;
            }
            waitTaskbool = helper.WaitWriteNTR(GetBoxOffset(LookupTable.BoxOffset, Box, Slot), pklist[currentfile].EncryptedBoxData, pid);
            if (await waitTaskbool)
            {
                UpdateDumpBoxes(Box, Slot);
                PKMEditor.PopulateFields(pklist[currentfile]);
                currentCHK = pklist[currentfile].Checksum;
                if (sourceFolder.Checked)
                {
                    currentfile++;
                    if (currentfile > pklist.Count - 1)
                        currentfile = 0;
                }
                attempts = 0;
                botstate = BotState.WriteLastBox;
            }
            else
            {
                attempts++;
                botresult = BotErrorMessage.WriteError;
                botstate = BotState.WriteFromFolder;
            }
        }

        private void BotReadFolder()
        {
            Report("Bot: Reading Wonder Trade folder");
            pkfiles = Directory.GetFiles(wtfolderpath, "*.pk7");
            if (pkfiles.Length > 0)
            {
                foreach (string pkf in pkfiles)
                {
                    byte[] temp = File.ReadAllBytes(pkf);
                    if (temp.Length == 232)
                    {
                        PK7 pkmn = new PK7(temp);
                        if (IsTradeable(pkmn))
                        { // Legal pkm
                            Report("Bot: Valid PK7 file");
                            pklist.Add(pkmn);
                        }
                        else
                        { // Illegal pkm
                            Report($"Bot: File {pkf} cannot be traded");
                        }
                    }
                    else
                    { // Not valid file
                        Report($"Bot: File {pkf} is not a valid pk7 file");
                    }
                }
            }
            if (pklist.Count > 0)
            {
                botstate = BotState.WriteFromFolder;
            }
            else
            {
                Report("Bot: No files detected");
                botresult = BotErrorMessage.Finished;
                botstate = BotState.ExitBot;
            }
        }

        private async Task BotReadPoke()
        {
            Report("Bot: Look for pokemon to trade");
            waitTaskPKM = helper.WaitReadPoke((int)Box.Value - 1, (int)Slot.Value - 1);
            WTpoke = await waitTaskPKM;
            if (WTpoke == null)
            { // No data or invalid
                attempts++;
                botresult = BotErrorMessage.ReadError;
                botstate = BotState.ReadPoke;
            }
            else if (WTpoke.Species == 0)
            { // Empty space
                Report("Bot: Empty slot");
                attempts = 0;
                GetNextSlot();
            }
            else
            { // Valid pkm, check legality
                attempts = 0;
                if (IsTradeable(WTpoke))
                {
                    currentCHK = WTpoke.Checksum;
                    Report($"Bot: Pokémon found - 0x{currentCHK:X4}");
                    botstate = BotState.WriteLastBox;
                }
                else
                {
                    Report("Bot: Pokémon cannot be traded, is illegal or is an egg or have special ribbons.");
                    GetNextSlot();
                }
            }
        }

        private async Task BotInitializeFC2()
        {
            waitTaskbool = helper.WaitReadNTR(LookupTable.TrainerCurrentFCOffset);
            if (await waitTaskbool)
            {
                attempts = 0;
                currentFC = helper.lastRead;
                Report($"Bot: Current FC: {currentFC}");
                int i = 0;
                while (currentTotalFC >= nextFC)
                {
                    nextFC = FriendCodes.Table[i];
                    i++;
                }
                Report($"Bot: Points for next level: {nextFC - currentTotalFC}");
                if (sourceBox.Checked)
                {
                    botstate = BotState.ReadPoke;
                }
                else
                {
                    botstate = BotState.ReadFolder;
                }
            }
            else
            {
                attempts++;
                botresult = BotErrorMessage.ReadError;
                botstate = BotState.InitializeFC2;
            }
        }

        private async Task BotInitializeFC1()
        {
            waitTaskbool = helper.WaitReadNTR(LookupTable.TrainerTotalFCOffset);
            if (await waitTaskbool)
            {
                attempts = 0;
                currentTotalFC = helper.lastRead;
                Report("Bot: Current Total FC: " + currentTotalFC);
                botstate = BotState.InitializeFC2;
            }
            else
            {
                attempts++;
                botresult = BotErrorMessage.ReadError;
                botstate = BotState.InitializeFC1;
            }
        }

        private async Task BotBackupBoxes()
        {
            Report("Bot: Backup boxes");
            waitTaskbool = helper.WaitReadNTRMulti(LookupTable.BoxOffset, 232 * 30 * 32);
            if (await waitTaskbool)
            {
                attempts = 0;
                string fileName = $"WTBefore-{DateTime.Now:yyyyMMddHHmmss}.ek7";
                backuppath = wtfolderpath + fileName;
                WriteDataToFile(helper.lastmultiread, backuppath);
                botstate = BotState.CheckMode;
            }
            else
            {
                attempts++;
                botresult = BotErrorMessage.ReadError;
                botstate = BotState.BackupBoxes;
            }
        }

        private async Task BotTestWTScreen()
        {
            Report("Bot: Test if the Wonder Trade screen is shown");
            waitTaskbool = helper.IsTimeMemoryInRange(WtscreenOff,
                WtscreenIN, WtscreenRange, 250, 5000);
            if (await waitTaskbool)
            {
                attempts = 0;
                botstate = BotState.PressWTstart;
            }
            else
            {
                if (isUSUM)
                {
                    botresult = BotErrorMessage.NotWTMenu;
                    botstate = BotState.ExitBot;
                }
                else
                {
                    attempts++;
                    botresult = BotErrorMessage.ReadError;
                    botstate = BotState.PressWTButton;
                }
            }
        }

        private async Task BotPressWTButton()
        {
            Report("Bot: Press Wonder Trade");
            waitTaskbool = helper.TouchWait(160, 160);
            if (await waitTaskbool)
            {
                botstate = BotState.TestWTScreen;
            }
            else
            {
                attempts++;
                botresult = BotErrorMessage.TouchError;
                botstate = BotState.PressWTButton;
            }
        }

        private async Task BotTestTradeMenu()
        {
            Report("Bot: Test if the trademenu is shown");
            waitTaskbool = helper.IsTimeMemoryInRange(TrademenuOff, TrademenuIN, TrademenuRange, 100, 5000);
            if (await waitTaskbool)
            {
                attempts = 0;
                botstate = BotState.PressWTButton;
            }
            else
            {
                attempts++;
                botresult = BotErrorMessage.ReadError;
                botstate = BotState.PressTradeButton;
            }
        }

        private async Task BotPressTradeButton()
        {
            Report("Bot: Press Trade Button");
            waitTaskbool = helper.TouchWait(200, 120);
            if (await waitTaskbool)
            {
                botstate = BotState.TestTradeMenu;
            }
            else
            {
                attempts++;
                botresult = BotErrorMessage.TouchError;
                botstate = BotState.PressTradeButton;
            }
        }

        private async Task BotWriteLastBox()
        {
            if (boxchange)
            {
                Report("Bot: Set current box");
                waitTaskbool = helper.WaitWriteNTR(LookupTable.CurrentboxOffset, (uint)(Box.Value - 1), pid);
                if (await waitTaskbool)
                {
                    attempts = 0;
                    boxchange = false;
                    botstate = isUSUM ? BotState.TestWTScreen : BotState.PressTradeButton;
                }
                else
                {
                    attempts++;
                    botresult = BotErrorMessage.WriteError;
                    botstate = BotState.WriteLastBox;
                }
            }
            else
            {
                botstate = isUSUM ? BotState.TestWTScreen : BotState.PressTradeButton;
            }
        }

        /// <summary>
        /// Sets the maximum number of WT possible based on the selected box and slot.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Box_ValueChanged(object sender, EventArgs e)
        {
            Delg.SetMaximum(Trades, LookupTable.GetRemainingSpaces((int)Box.Value, (int)Slot.Value));
        }

        /// <summary>
        /// Sets the reference to the next slot in the PC.
        /// </summary>
        private void GetNextSlot()
        {
            if (Slot.Value == 30)
            {
                Delg.SetValue(Box, Box.Value + 1);
                Delg.SetValue(Slot, 1);
                boxchange = true;
            }
            else
            {
                Delg.SetValue(Slot, Slot.Value + 1);
            }
            Delg.SetValue(Trades, Trades.Value - 1);
            if (Trades.Value > 0)
            {
                botstate = sourceBox.Checked ? BotState.ReadPoke : BotState.WriteFromFolder;
                attempts = 0;
            }
            else if (runEndless.Checked)
            {
                Delg.SetValue(Box, startbox);
                Delg.SetValue(Slot, startslot);
                Delg.SetValue(Trades, starttrades);
                botstate = sourceBox.Checked ? BotState.ReadPoke : BotState.WriteFromFolder;
                attempts = 0;
            }
            else
            {
                botstate = BotState.DumpAfter;
            }
        }

        /// <summary>
        /// Reloads the current slot if there is no trade partner.
        /// </summary>
        private void RestartSlot()
        {
            if (Trades.Value > 0)
            {
                botstate = sourceBox.Checked ? BotState.ReadPoke : BotState.WriteFromFolder;
                attempts = 0;
            }
            else if (runEndless.Checked)
            {
                Delg.SetValue(Box, startbox);
                Delg.SetValue(Slot, startslot);
                Delg.SetValue(Trades, starttrades);
                botstate = sourceBox.Checked ? BotState.ReadPoke : BotState.WriteFromFolder;
                attempts = 0;
            }
            else
            {
                botstate = BotState.DumpAfter;
            }
        }

        /// <summary>
        /// Waiting time for a trade.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TradeTimer_Tick(object sender, EventArgs e)
        {
            tradeTimer.Stop();
            Report("Bot: Trade timed out");
            attempts = -40;
            notradepartner = true;
        }

        /// <summary>
        /// Prevents user from closing the form while a bot is running.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Bot_WonderTrade7_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (botworking)
            {
                MessageBox.Show("Stop the bot before closing this window", "Wonder Trade bot", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                e.Cancel = true;
            }
        }

        /// <summary>
        /// Enables controls in the Main Form.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Bot_WonderTrade7_FormClosed(object sender, FormClosedEventArgs e)
        {
            ntrClient.Disconnect();
        }

        /// <summary>
        /// Reference for storing bot-related files.
        /// </summary>
        public static string BotFolder { get; } = Path.Combine(Application.StartupPath, "Bot");

        /// <summary>
        /// Write data to the log.
        /// </summary>
        /// <param name="message">String which will be added to the log</param>
        public static void Report(string message) => Console.WriteLine(message);

        /// <summary>
        /// Check if a pokémon can be traded via Wonder Trade, checks legality,
        /// egg and ribbons.
        /// </summary>
        /// <param name="poke">PKM data to check.</param>
        /// <returns>Returns false if the pokémon is an egg or has special ribbons. It
        /// also returns false if the pokémon is illegal, except when the program is in
        /// illegal mode.</returns>
        public static bool IsTradeable(PKM poke)
        {
            if (!new LegalityAnalysis(poke).Valid) // Don't trade illegal pokemon
                return false;

            if (poke.IsEgg) // Don't trade eggs
                return false;

            if (HasUntradableRibbon(poke))
                return false;

            return true;
        }

        private static bool HasUntradableRibbon(PKM poke)
        {
            switch (poke)
            {
                case PK6 pk6:
                    return pk6.RibbonCountry || pk6.RibbonWorld || pk6.RibbonClassic || pk6.RibbonPremier
                           || pk6.RibbonEvent || pk6.RibbonBirthday || pk6.RibbonSpecial || pk6.RibbonSouvenir
                           || pk6.RibbonWishing || pk6.RibbonChampionBattle || pk6.RibbonChampionRegional
                           || pk6.RibbonChampionNational || pk6.RibbonChampionWorld;
                case PK7 pk7:
                    return pk7.RibbonCountry || pk7.RibbonWorld || pk7.RibbonClassic || pk7.RibbonPremier
                           || pk7.RibbonEvent || pk7.RibbonBirthday || pk7.RibbonSpecial || pk7.RibbonSouvenir
                           || pk7.RibbonWishing || pk7.RibbonChampionBattle || pk7.RibbonChampionRegional
                           || pk7.RibbonChampionNational || pk7.RibbonChampionWorld;
                default:
                    return false;
            }
        }

        private static uint GetBoxOffset(uint startOffset, uint box, uint slot)
        {
            return startOffset + (232 * ((box * 30) + slot));
        }

        /// <summary>
        /// Gets the RAM offset of a pokémon in the PC.
        /// </summary>
        /// <param name="startOffset">Offset of the first pokémon in the PC.</param>
        /// <param name="boxSource">Box reference.</param>
        /// <param name="slotSource">Slot reference.</param>
        /// <returns>Returns an unsigned integer with the RAM address of the selected PC
        /// slot</returns>
        public static uint GetBoxOffset(uint startOffset, NumericUpDown boxSource, NumericUpDown slotSource)
        {
            uint box = (uint)(boxSource.Value - 1);
            uint slot = (uint)(slotSource.Value - 1);
            return GetBoxOffset(startOffset, box, slot);
        }

        /// <summary>
        /// Compare a pokémon against a list of filters.</summary>
        /// <param name="poke">PKM data to compare.</param>
        /// <param name="filters">Filter list.</param>
        /// <returns>
        /// If the pokémon passes all the tests of one of the filters this method returns the filter position in the list.
        /// If no match is found it  returns -1.
        /// </returns>
        public static int CheckFilters(PKM poke, DataGridView filters)
        {
            if (filters.Rows.Count <= 0)
                return 1;

            for (var filter = 0; filter < filters.Rows.Count; filter++)
            {
                DataGridViewRow row = filters.Rows[filter];
                bool failedTests = GetPassesTests(poke, filter, row);
                if (failedTests)
                    return filter;
            }
            return -1;
        }

        private static bool GetPassesTests(PKM pk, int currentFilter, DataGridViewRow row)
        {
            Report($"{Environment.NewLine}Filter: Analyze pokémon using filter # {currentFilter}");

            // Test shiny
            var shiny = (int)row.Cells[0].Value;
            if (shiny == 1)
            {
                if (pk.IsShiny)
                {
                    Report("Filter: Shiny - PASS");
                }
                else
                {
                    Report("Filter: Shiny - FAIL");
                    return false;
                }
            }
            else
            {
                Report("Filter: Shiny - Don't care");
            }

            // Test nature
            int nature = (int) row.Cells[1].Value;
            if (nature < 0 || pk.Nature == nature)
            {
                Report("Filter: Nature - PASS");
            }
            else
            {
                Report("Filter: Nature - FAIL");
                return false;
            }

            // Test Ability
            int ability = (int) row.Cells[2].Value;
            if (ability < 0 || (pk.Ability - 1) == ability)
            {
                Report("Filter: Ability - PASS");
            }
            else
            {
                Report("Filter: Ability - FAIL");
                return false;
            }

            // Test Hidden Power
            int power = (int) row.Cells[3].Value;
            if (power < 0 || pk.HPType == power)
            {
                Report("Filter: Hidden Power - PASS");
            }
            else
            {
                Report("Filter: Hidden Power - FAIL");
                return false;
            }

            // Test Gender
            int gender = (int) row.Cells[4].Value;
            if (gender < 0 || pk.Gender == gender)
            {
                Report("Filter: Gender - PASS");
            }
            else
            {
                Report("Filter: Gender - FAIL");
                return false;
            }

            // Test HP
            int hp1 = (int) row.Cells[5].Value;
            int hp2 = (int) row.Cells[6].Value;
            if (IVCheck(hp1, pk.IV_HP, hp2))
            {
                Report("Filter: Hit Points IV - PASS");
            }
            else
            {
                Report("Filter: Hit Points IV - FAIL");
                return false;
            }

            // Test Atk
            if (IVCheck((int)row.Cells[7].Value, pk.IV_ATK, (int)row.Cells[8].Value))
            {
                Report("Filter: Attack IV - PASS");
            }
            else
            {
                Report("Filter: Attack IV - FAIL");
                return false;
            }

            // Test Def
            if (IVCheck((int)row.Cells[9].Value, pk.IV_DEF,
                (int)row.Cells[10].Value))
            {
                Report("Filter: Defense IV - PASS");
            }
            else
            {
                Report("Filter: Defense IV - FAIL");
                return false;
            }

            // Test SpA
            if (IVCheck((int)row.Cells[11].Value, pk.IV_SPA, (int)row.Cells[12].Value))
            {
                Report("Filter: Special Attack IV - PASS");
            }
            else
            {
                Report("Filter: Special Attack IV - FAIL");
                return false;
            }

            // Test SpD
            if (IVCheck((int)row.Cells[13].Value, pk.IV_SPD, (int)row.Cells[14].Value))
            {
                Report("Filter: Special Defense IV - PASS");
            }
            else
            {
                Report("Filter: Special Defense IV - FAIL");
                return false;
            }

            // Test Spe
            if (IVCheck((int)row.Cells[15].Value, pk.IV_SPE, (int)row.Cells[16].Value))
            {
                Report("Filter: Speed IV - PASS");
            }
            else
            {
                Report("Filter: Speed IV - FAIL");
                return false;
            }

            // Test Perfect IVs
            int perfectIVs = pk.IVs.Count(z => z == pk.MaxIV);
            if (IVCheck((int)row.Cells[17].Value, perfectIVs, (int)row.Cells[18].Value))
            {
                Report("Filter: Perfect IVs - PASS");
            }
            else
            {
                Report("Filter: Perfect IVs - FAIL");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Compares a numeric value againt other using a determined logic.
        /// </summary>
        /// <param name="targetNumber">Number reference.</param>
        /// <param name="sourceNumber">Number to compare.</param>
        /// <param name="logic">Logic to be applied.</param>
        /// <returns>Returns true if the target number passes the test.</returns>
        private static bool IVCheck(int targetNumber, int sourceNumber, int logic)
        {
            switch (logic)
            {
                case 0: // Greater or equal
                    return sourceNumber >= targetNumber;
                case 1: // Greater
                    return sourceNumber > targetNumber;
                case 2: // Equal
                    return sourceNumber == targetNumber;
                case 3: // Less
                    return sourceNumber < targetNumber;
                case 4: // Less or equal
                    return sourceNumber <= targetNumber;
                case 5: // Different
                    return sourceNumber != targetNumber;
                case 6: // Even
                    return sourceNumber % 2 == 0;
                case 7: // Odd
                    return sourceNumber % 2 == 1;
                default:
                    return true;
            }
        }

        public static void UpdateDumpBoxes(int box, int slot)
        {
            Delg.SetValue(boxDump, box + 1);
            Delg.SetValue(slotDump, slot + 1);
        }

        public void UpdateDumpBoxes(NumericUpDown box, NumericUpDown slot)
        {
            Delg.SetValue(boxDump, box.Value);
            Delg.SetValue(slotDump, slot.Value);
        }

        /// <summary>
        /// Shows a message box with the result of a bot execution.
        /// </summary>
        /// <param name="source">Bot name.</param>
        /// <param name="message">Message to be displayed.</param>
        /// <param name="info">Additional informaiton.</param>
        public static void ShowResult(string source, BotErrorMessage message, int[] info = null)
        {
            string userMessage = message.FormatString(info);
            var icon = GetIcon(message);
            MessageBox.Show(userMessage, source, MessageBoxButtons.OK);
        }

        private static MessageBoxIcon GetIcon(BotErrorMessage message)
        {
            switch (message)
            {
                case BotErrorMessage.Finished:
                case BotErrorMessage.UserStop:
                case BotErrorMessage.FestivalPlaza:
                case BotErrorMessage.SVMatch:
                case BotErrorMessage.FilterMatch:
                case BotErrorMessage.NoMatch:
                case BotErrorMessage.SRMatch:
                case BotErrorMessage.BattleMatch:
                    return MessageBoxIcon.Information;

                case BotErrorMessage.NotInPSS:
                    return MessageBoxIcon.Warning;

                default:
                    return MessageBoxIcon.Error;
            }
        }
    }
}
