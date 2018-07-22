using PKHeX.Core;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using pkmn_ntr.Helpers;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace WonderTradeBot
{
    public partial class WonderTradeBot : Form, IPlugin
    {
        private System.Windows.Forms.Timer timer1;
        public string Name => "Wondertrade Gen 7";
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
        private static System.Windows.Forms.NumericUpDown boxDump; // no idea why this is done like this.......
        private static System.Windows.Forms.NumericUpDown slotDump;

        public void Initialize(params object[] args)
        {
            arguments = args;
            Console.WriteLine($"[Auto Legality Mod] Loading {Name}");
            if (args == null)
                return;
            SaveFileEditor = (ISaveFileProvider)Array.Find(args, z => z is ISaveFileProvider);
            SaveFileEditor2 = SaveFileEditor;
            PKMEditor = (IPKMView)Array.Find(args, z => z is IPKMView);
            PKMEditor2 = PKMEditor;
            var menu = (ToolStrip)Array.Find(args, z => z is ToolStrip);
            LoadMenuStrip(menu);
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

        /// <summary>
        /// Secuency of steps done by the bot.
        /// </summary>
        private enum BotState
        {
            StartBot, BackupBoxes, CheckMode, InitializeFC1, InitializeFC2, ReadPoke,
            ReadFolder, WriteFromFolder, WriteLastBox, PressTradeButton, TestTradeMenu,
            PressWTButton, TestWTScreen, PressWTstart, TestBoxes, TouchPoke, CancelTouch,
            TestPoke, StartTrade, ConfirmTrade, TestBoxesOut, WaitForTrade,
            TestTradeFinish, TryFinish, FinishTrade, CollectFC1, CollectFC2, CollectFC3,
            CollectFC4, CollectFC5, DumpAfter, ActionAfter, RestoreBackup, DeletePoke,
            ExitBot
        };

        /// <summary>
        /// All different error or finisish messages the bot can return.
        /// </summary>
        public enum ErrorMessage
        {
            Finished, UserStop, ReadError, WriteError, ButtonError, TouchError,
            StickError, NotInPSS, FestivalPlaza, SVMatch, FilterMatch, NoMatch, SRMatch,
            BattleMatch, Disconnect, NotWTMenu, GeneralError
        };

        // General bot variables
        private bool botworking;
        private bool userstop;
        private BotState botstate;
        private ErrorMessage botresult;
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
        private string wtfolderpath = @Application.StartupPath + "\\Wonder Trade\\";

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
        private uint dialogOff = 0x6747E0; // 1.0: 0x63DD68; 1.1: 0x6747D8;
        private uint dialogIn = 0x00000000; // 1.0: 0x0C;
        private uint dialogOut = 0x41B80000; // 1.0: 0x0B;
        private uint toppkmOff = 0x30000298;
        private bool botWorking;

        #region FCtable
        public static readonly uint[] FCtable = { 6, 16, 31, 61, 101, 151, 211, 281, 361,
            451, 551, 651, 751, 851, 951, 1051, 1151, 1251, 1351, 1451, 1571, 1691, 1811,
            1931, 2051, 2171, 2291, 2411, 2531, 2651, 2801, 2951, 3101, 3251, 3401, 3551,
            3701, 3851, 4001, 4151, 4331, 4511, 4691, 4871, 5051, 5231, 5411, 5591, 5771,
            5951, 6161, 6371, 6581, 6791, 7001, 7211, 7421, 7631, 7841, 8051, 8291, 8531,
            8771, 9011, 9251, 9491, 9731, 9971, 10211, 10451, 10721, 10991, 11261, 11531,
            11801, 12071, 12341, 12611, 12881, 13151, 13421, 13691, 13961, 14231, 14501,
            14771, 15041, 15311, 15581, 15851, 16121, 16391, 16661, 16931, 17201, 17471,
            17741, 18011, 18281, 18551, 18851, 19151, 19451, 19751, 20051, 20351, 20651,
            20951, 21251, 21551, 21851, 22151, 22451, 22751, 23051, 23351, 23651, 23951,
            24251, 24551, 24851, 25151, 25451, 25751, 26051, 26351, 26651, 26951, 27251,
            27551, 27851, 28151, 28451, 28751, 29051, 29351, 29651, 29951, 30251, 30551,
            30851, 31151, 31451, 31751, 32051, 32351, 32651, 32951, 33251, 33551, 33851,
            34151, 34451, 34751, 35051, 35351, 35651, 35951, 36251, 36551, 36851, 37151,
            37451, 37751, 38051, 38351, 38651, 38951, 39251, 39551, 39851, 40151, 40451,
            40751, 41051, 41351, 41651, 41951, 42251, 42551, 42851, 43151, 43451, 43751,
            44051, 44351, 44651, 44951, 45251, 45551, 45851, 46151, 46451, 46751, 47051,
            47351, 47651, 47951, 48251, 48551, 48851, 49151, 49451, 49751, 50051, 50351,
            50651, 50951, 51251, 51551, 51851, 52151, 52451, 52751, 53051, 53351, 53651,
            53951, 54251, 54551, 54851, 55151, 55451, 55751, 56051, 56351, 56651, 56951,
            57251, 57551, 57851, 58151, 58451, 58751, 59051, 59351, 59651, 59951, 60251,
            60551, 60851, 61151, 61451, 61751, 62051, 62351, 62651, 62951, 63251, 63551,
            63851, 64151, 64451, 64751, 65051, 65351, 65651, 65951, 66251, 66551, 66851,
            67151, 67451, 67751, 68051, 68351, 68651, 68951, 69251, 69551, 69851, 70151,
            70451, 70751, 71051, 71351, 71651, 71951, 72251, 72551, 72851, 73151, 73451,
            73751, 74051, 74351, 74651, 74951, 75251, 75551, 75851, 76151, 76451, 76751,
            77051, 77351, 77651, 77951, 78251, 78551, 78851, 79151, 79451, 79751, 80051,
            80351, 80651, 80951, 81251, 81551, 81851, 82151, 82451, 82751, 83051, 83351,
            83651, 83951, 84251, 84551, 84851, 85151, 85451, 85751, 86051, 86351, 86651,
            86951, 87251, 87551, 87851, 88151, 88451, 88751, 89051, 89351, 89651, 89951,
            90251, 90551, 90851, 91151, 91451, 91751, 92051, 92351, 92651, 92951, 93251,
            93551, 93851, 94151, 94451, 94751, 95051, 95351, 95651, 95951, 96251, 96551,
            96851, 97151, 97451, 97751, 98051, 98351, 98651, 98951, 99251, 99551, 99851,
            100151, 100451, 100751, 101051, 101351, 101651, 101951, 102251, 102551,
            102851, 103151, 103451, 103751, 104051, 104351, 104651, 104951, 105251,
            105551, 105851, 106151, 106451, 106751, 107051, 107351, 107651, 107951,
            108251, 108551, 108851, 109151, 109451, 109751, 110051, 110351, 110651,
            110951, 111251, 111551, 111851, 112151, 112451, 112751, 113051, 113351,
            113651, 113951, 114251, 114551, 114851, 115151, 115451, 115751, 116051,
            116351, 116651, 116951, 117251, 117551, 117851, 118151, 118451, 118751,
            119051, 119351, 119651, 119951, 120251, 120551, 120851, 121151, 121451,
            121751, 122051, 122351, 122651, 122951, 123251, 123551, 123851, 124151,
            124451, 124751, 125051, 125351, 125651, 125951, 126251, 126551, 126851,
            127151, 127451, 127751, 128051, 128351, 128651, 128951, 129251, 129551,
            129851, 130151, 130451, 130751, 131051, 131351, 131651, 131951, 132251,
            132551, 132851, 133151, 133451, 133751, 134051, 134351, 134651, 134951,
            135251, 135551, 135851, 136151, 136451, 136751, 137051, 137351, 137651,
            137951, 138251, 138551, 138851, 139151, 139451, 139751, 140051, 140351,
            140651, 140951, 141251, 141551, 141851, 142151, 142451, 142751, 143051,
            143351, 143651, 143951, 144251, 144551, 144851, 145151, 145451, 145751,
            146051, 146351, 146651, 146951, 147251, 147551, 147851, 148151, 148451,
            148751, 149051, 149351, 149651, 149951, 150251, 150551, 150851, 151151,
            151451, 151751, 152051, 152351, 152651, 152951, 153251, 153551, 153851,
            154151, 154451, 154751, 155051, 155351, 155651, 155951, 156251, 156551,
            156851, 157151, 157451, 157751, 158051, 158351, 158651, 158951, 159251,
            159551, 159851, 160151, 160451, 160751, 161051, 161351, 161651, 161951,
            162251, 162551, 162851, 163151, 163451, 163751, 164051, 164351, 164651,
            164951, 165251, 165551, 165851, 166151, 166451, 166751, 167051, 167351,
            167651, 167951, 168251, 168551, 168851, 169151, 169451, 169751, 170051,
            170351, 170651, 170951, 171251, 171551, 171851, 172151, 172451, 172751,
            173051, 173351, 173651, 173951, 174251, 174551, 174851, 175151, 175451,
            175751, 176051, 176351, 176651, 176951, 177251, 177551, 177851, 178151,
            178451, 178751, 179051, 179351, 179651, 179951, 180251, 180551, 180851,
            181151, 181451, 181751, 182051, 182351, 182651, 182951, 183251, 183551,
            183851, 184151, 184451, 184751, 185051, 185351, 185651, 185951, 186251,
            186551, 186851, 187151, 187451, 187751, 188051, 188351, 188651, 188951,
            189251, 189551, 189851, 190151, 190451, 190751, 191051, 191351, 191651,
            191951, 192251, 192551, 192851, 193151, 193451, 193751, 194051, 194351,
            194651, 194951, 195251, 195551, 195851, 196151, 196451, 196751, 197051,
            197351, 197651, 197951, 198251, 198551, 198851, 199151, 199451, 199751,
            200051, 200351, 200651, 200951, 201251, 201551, 201851, 202151, 202451,
            202751, 203051, 203351, 203651, 203951, 204251, 204551, 204851, 205151,
            205451, 205751, 206051, 206351, 206651, 206951, 207251, 207551, 207851,
            208151, 208451, 208751, 209051, 209351, 209651, 209951, 210251, 210551,
            210851, 211151, 211451, 211751, 212051, 212351, 212651, 212951, 213251,
            213551, 213851, 214151, 214451, 214751, 215051, 215351, 215651, 215951,
            216251, 216551, 216851, 217151, 217451, 217751, 218051, 218351, 218651,
            218951, 219251, 219551, 219851, 220151, 220451, 220751, 221051, 221351,
            221651, 221951, 222251, 222551, 222851, 223151, 223451, 223751, 224051,
            224351, 224651, 224951, 225251, 225551, 225851, 226151, 226451, 226751,
            227051, 227351, 227651, 227951, 228251, 228551, 228851, 229151, 229451,
            229751, 230051, 230351, 230651, 230951, 231251, 231551, 231851, 232151,
            232451, 232751, 233051, 233351, 233651, 233951, 234251, 234551, 234851,
            235151, 235451, 235751, 236051, 236351, 236651, 236951, 237251, 237551,
            237851, 238151, 238451, 238751, 239051, 239351, 239651, 239951, 240251,
            240551, 240851, 241151, 241451, 241751, 242051, 242351, 242651, 242951,
            243251, 243551, 243851, 244151, 244451, 244751, 245051, 245351, 245651,
            245951, 246251, 246551, 246851, 247151, 247451, 247751, 248051, 248351,
            248651, 248951, 249251, 249551, 249851, 250151, 250451, 250751, 251051,
            251351, 251651, 251951, 252251, 252551, 252851, 253151, 253451, 253751,
            254051, 254351, 254651, 254951, 255251, 255551, 255851, 256151, 256451,
            256751, 257051, 257351, 257651, 257951, 258251, 258551, 258851, 259151,
            259451, 259751, 260051, 260351, 260651, 260951, 261251, 261551, 261851,
            262151, 262451, 262751, 263051, 263351, 263651, 263951, 264251, 264551,
            264851, 265151, 265451, 265751, 266051, 266351, 266651, 266951, 267251,
            267551, 267851, 268151, 268451, 268751, 269051, 269351, 269651, 269951,
            270251, 270551, 270851, 271151, 271451, 271751, 272051, 272351, 272651,
            272951, 273251, 273551, 273851, 274151, 274451, 274751, 275051, 275351,
            275651, 275951, 276251, 276551, 276851, 277151, 277451, 277751, 278051,
            278351, 278651, 278951, 279251, 279551, 279851, 280151, 280451, 280751,
            281051, 281351, 281651, 281951, 282251, 282551, 282851, 283151, 283451,
            283751, 284051, 284351, 284651, 284951, 285251, 285551, 285851, 286151,
            286451, 286751, 287051, 287351, 287651, 287951, 288251 };
        #endregion FCtable

        /// <summary>
        /// Constructor, changes GUI based on game.
        /// </summary>
        public void Bot_WonderTrade7()
        {
            InitializeComponent();
            RNG = new Random();
            if (SaveFileEditor.SAV.Version == GameVersion.US ||
                SaveFileEditor.SAV.Version == GameVersion.UM)
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
                    pklist = new List<PKM> { };
                    tradeTimer = new Timer();
                    tradeTimer.Interval = 95000; // Trade timeout, 95 s
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

                        case BotState.BackupBoxes:
                            Report("Bot: Backup boxes");
                            waitTaskbool = helper.waitNTRmultiread(LookupTable
                                .BoxOffset, 232 * 30 * 32);
                            if (await waitTaskbool)
                            {
                                attempts = 0;
                                string fileName = "WTBefore-" + DateTime.Now
                                    .ToString("yyyyMMddHHmmss") + ".ek7";
                                backuppath = wtfolderpath + fileName;
                                WriteDataToFile(helper
                                    .lastmultiread, backuppath);
                                botstate = BotState.CheckMode;
                            }
                            else
                            {
                                attempts++;
                                botresult = ErrorMessage.ReadError;
                                botstate = BotState.BackupBoxes;
                            }
                            break;

                        case BotState.CheckMode:
                            if (collectFC.Checked)
                            {
                                botstate = BotState.InitializeFC1;
                            }
                            else if (sourceBox.Checked)
                            {
                                botstate = BotState.ReadPoke;
                            }
                            else
                            {
                                botstate = BotState.ReadFolder;
                            }
                            break;

                        case BotState.InitializeFC1:
                            waitTaskbool = helper.waitNTRread(LookupTable
                                .TrainerTotalFCOffset);
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
                                botresult = ErrorMessage.ReadError;
                                botstate = BotState.InitializeFC1;
                            }
                            break;

                        case BotState.InitializeFC2:
                            waitTaskbool = helper.waitNTRread(LookupTable
                                .TrainerCurrentFCOffset);
                            if (await waitTaskbool)
                            {
                                attempts = 0;
                                currentFC = helper.lastRead;
                                Report("Bot: Current FC: " + currentFC);
                                int i = 0;
                                while (currentTotalFC >= nextFC)
                                {
                                    nextFC = FCtable[i];
                                    i++;
                                }
                                Report("Bot: Points for next level: " + (nextFC -
                                    currentTotalFC));
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
                                botresult = ErrorMessage.ReadError;
                                botstate = BotState.InitializeFC2;
                            }
                            break;

                        case BotState.ReadPoke:
                            Report("Bot: Look for pokemon to trade");
                            waitTaskPKM = helper.waitPokeRead(Box, Slot);
                            WTpoke = await waitTaskPKM;
                            if (WTpoke == null)
                            { // No data or invalid
                                attempts++;
                                botresult = ErrorMessage.ReadError;
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
                                    Report("Bot: Pokémon found - 0x" + currentCHK
                                        .ToString("X4"));
                                    botstate = BotState.WriteLastBox;
                                }
                                else
                                {
                                    Report("Bot: Pokémon cannot be traded, is " +
                                            "illegal or is an egg or have special " +
                                            "ribbons.");
                                    GetNextSlot();
                                }
                            }
                            break;

                        case BotState.ReadFolder:
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
                                            Report("Bot: File " + pkf + " cannot be " +
                                                "traded");
                                        }
                                    }
                                    else
                                    { // Not valid file
                                        Report("Bot: File " + pkf + " is not a valid" +
                                            " pk7 file");
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
                                botresult = ErrorMessage.Finished;
                                botstate = BotState.ExitBot;
                            }
                            break;

                        case BotState.WriteFromFolder:
                            Report("Bot: Write pkm file from list");
                            if (sourceRandom.Checked)
                            { // Select a random file
                                currentfile = RNG.Next() % pklist.Count;
                            }
                            waitTaskbool = helper.waitNTRwrite(
                                GetBoxOffset(LookupTable.BoxOffset, Box, Slot), pklist
                                [currentfile].EncryptedBoxData, pid);
                            if (await waitTaskbool)
                            {
                                UpdateDumpBoxes(Box, Slot);
                                PKMEditor.PopulateFields(pklist[currentfile]);
                                currentCHK = pklist[currentfile].Checksum;
                                if (sourceFolder.Checked)
                                {
                                    currentfile++;
                                    if (currentfile > pklist.Count - 1)
                                    {
                                        currentfile = 0;
                                    }
                                }
                                attempts = 0;
                                botstate = BotState.WriteLastBox;
                            }
                            else
                            {
                                attempts++;
                                botresult = ErrorMessage.WriteError;
                                botstate = BotState.WriteFromFolder;
                            }
                            break;

                        case BotState.WriteLastBox:
                            if (boxchange)
                            {
                                Report("Bot: Set current box");
                                waitTaskbool = helper.waitNTRwrite(LookupTable
                                    .CurrentboxOffset, (uint)GetIndex(Box), pid);
                                if (await waitTaskbool)
                                {
                                    attempts = 0;
                                    boxchange = false;
                                    botstate = isUSUM ? BotState.TestWTScreen : BotState
                                        .PressTradeButton;
                                }
                                else
                                {
                                    attempts++;
                                    botresult = ErrorMessage.WriteError;
                                    botstate = BotState.WriteLastBox;
                                }
                            }
                            else
                            {
                                botstate = isUSUM ? BotState.TestWTScreen : BotState
                                    .PressTradeButton;
                            }
                            break;

                        case BotState.PressTradeButton:
                            Report("Bot: Press Trade Button");
                            waitTaskbool = helper.waittouch(200, 120);
                            if (await waitTaskbool)
                            {
                                botstate = BotState.TestTradeMenu;
                            }
                            else
                            {
                                attempts++;
                                botresult = ErrorMessage.TouchError;
                                botstate = BotState.PressTradeButton;
                            }
                            break;

                        case BotState.TestTradeMenu:
                            Report("Bot: Test if the trademenu is shown");
                            waitTaskbool = helper.timememoryinrange(TrademenuOff,
                                TrademenuIN, TrademenuRange, 100, 5000);
                            if (await waitTaskbool)
                            {
                                attempts = 0;
                                botstate = BotState.PressWTButton;
                            }
                            else
                            {
                                attempts++;
                                botresult = ErrorMessage.ReadError;
                                botstate = BotState.PressTradeButton;
                            }
                            break;

                        case BotState.PressWTButton:
                            Report("Bot: Press Wonder Trade");
                            waitTaskbool = helper.waittouch(160, 160);
                            if (await waitTaskbool)
                                botstate = BotState.TestWTScreen;
                            else
                            {
                                attempts++;
                                botresult = ErrorMessage.TouchError;
                                botstate = BotState.PressWTButton;
                            }
                            break;

                        case BotState.TestWTScreen:
                            Report("Bot: Test if the Wonder Trade screen is shown");
                            waitTaskbool = helper.timememoryinrange(WtscreenOff,
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
                                    botresult = ErrorMessage.NotWTMenu;
                                    botstate = BotState.ExitBot;
                                }
                                else
                                {
                                    attempts++;
                                    botresult = ErrorMessage.ReadError;
                                    botstate = BotState.PressWTButton;
                                }
                            }
                            break;

                        case BotState.PressWTstart:
                            Report("Bot: Press Start");
                            await Task.Delay(4 * delaytime);
                            helper.quickbuton(LookupTable.ButtonA, commandtime);
                            await Task.Delay(commandtime + delaytime);
                            botstate = BotState.TestBoxes;
                            break;

                        case BotState.TestBoxes:
                            Report("Bot: Test if the boxes are shown");
                            waitTaskbool = helper.timememoryinrange(BoxesOff,
                                BoxesIN, BoxesRange, 250, 5000);
                            if (await waitTaskbool)
                            {
                                attempts = 0;
                                botstate = BotState.TouchPoke;
                            }
                            else
                            {
                                attempts++;
                                botresult = ErrorMessage.ReadError;
                                botstate = BotState.PressWTstart;
                            }
                            break;

                        case BotState.TouchPoke:
                            Report("Bot: Touch pokémon");
                            await Task.Delay(4 * delaytime);
                            waitTaskbool = helper.waittouch(LookupTable.pokeposX7
                                [GetIndex(Slot)], LookupTable.pokeposY7[GetIndex(Slot)]);
                            if (await waitTaskbool)
                            {
                                botstate = BotState.TestPoke;
                            }
                            else
                            {
                                attempts++;
                                botresult = ErrorMessage.TouchError;
                                botstate = BotState.TouchPoke;
                            }
                            break;

                        case BotState.TestPoke:
                            Report("Bot: Test if pokemon is selected");
                            waitTaskPKM = helper.waitPokeRead(toppkmOff);
                            validator = await waitTaskPKM;
                            if (validator == null)
                            { // No data or invalid
                                Report("Bot: Error detected or slot is empty");
                                attempts++;
                                botresult = ErrorMessage.ReadError;
                                botstate = BotState.CancelTouch;
                            }
                            else if (validator.Checksum != currentCHK)
                            { // Different poke
                                Report("Bot: Picked incorrect pokemon");
                                attempts++;
                                botresult = ErrorMessage.GeneralError;
                                botstate = BotState.CancelTouch;
                            }
                            else
                            { // Correct pokemon
                                attempts = 0;
                                botstate = BotState.StartTrade;
                            }
                            break;

                        case BotState.CancelTouch:
                            Report("Bot: Cancel selection and check again");
                            waitTaskPKM = helper.waitPokeRead(Box, Slot);
                            WTpoke = await waitTaskPKM;
                            if (WTpoke != null)
                            {
                                currentCHK = WTpoke.Checksum;
                            }
                            waitTaskbool = helper.waitbutton(LookupTable.ButtonB);
                            if (await waitTaskbool)
                            {
                                botstate = BotState.TouchPoke;
                            }
                            else
                            {
                                attempts++;
                                botresult = ErrorMessage.ButtonError;
                                botstate = BotState.TouchPoke;
                            }
                            break;

                        case BotState.StartTrade:
                            Report("Bot: Press Start");
                            waitTaskbool = helper.waitbutton(LookupTable.ButtonA);
                            if (await waitTaskbool)
                            {
                                botstate = BotState.ConfirmTrade;
                            }
                            else
                            {
                                attempts++;
                                botresult = ErrorMessage.ButtonError;
                                botstate = BotState.StartTrade;
                            }
                            break;

                        case BotState.ConfirmTrade:
                            Report("Bot: Press Yes");
                            waitTaskbool = helper.waitbutton(LookupTable.ButtonA);
                            if (await waitTaskbool)
                            {
                                botstate = BotState.TestBoxesOut;
                            }
                            else
                            {
                                attempts++;
                                botresult = ErrorMessage.ButtonError;
                                botstate = BotState.ConfirmTrade;
                            }
                            break;

                        case BotState.TestBoxesOut:
                            Report("Bot: Test if the boxes are not shown");
                            waitTaskbool = helper.timememoryinrange(BoxesOff,
                                BoxesOUT, BoxesRange, 500, 10000);
                            if (await waitTaskbool)
                            {
                                attempts = -40; // Try 50 button presses
                                botstate = BotState.WaitForTrade;
                                tradeTimer.Start();
                            }
                            else
                            {
                                attempts++;
                                botresult = ErrorMessage.ReadError;
                                botstate = BotState.TouchPoke;
                            }
                            break;

                        case BotState.WaitForTrade:
                            Report("Bot: Wait for trade");
                            waitTaskbool = helper.memoryinrange(TrademenuOff,
                                Tradeready, TradereadyRange);
                            if (await waitTaskbool)
                            {
                                tradeTimer.Stop();
                                Report("Bot: Trade detected");
                                await helper.waitPokeRead(Box, Slot);
                                Report("Bot: Wait 30 seconds");
                                await Task.Delay(30000);
                                botstate = BotState.TestTradeFinish;
                            }
                            else if (notradepartner)
                            { // Timeout
                                boxchange = true; // Might fix a couple of errors
                                botstate = BotState.TestTradeFinish;
                            }
                            else
                            {
                                await Task.Delay(8 * delaytime);
                            }
                            break;

                        case BotState.TestTradeFinish:
                            Report("Bot: Test if the trade is finished");
                            if (isUSUM)
                            {
                                waitTaskbool = helper.memoryinrange(WtscreenOff,
                                WtscreenIN, 0x1);
                            }
                            else
                            {
                                waitTaskbool = helper.memoryinrange(WtscreenIN,
                                    TrademenuOUT, TrademenuRange);
                            }
                            if (await waitTaskbool)
                            {
                                attempts = 0;
                                if (collectFC.Checked && !notradepartner)
                                {
                                    botstate = BotState.CollectFC1;
                                }
                                else
                                {
                                    botstate = BotState.FinishTrade;
                                }
                            }
                            else
                            {
                                attempts++;
                                botresult = ErrorMessage.GeneralError;
                                botstate = BotState.TryFinish;
                                if (helper.lastRead == TradeEvoValue &&
                                    !tradeevo)
                                {
                                    Report("Bot: Trade evolution detected, wait 20" +
                                        " seconds");
                                    await Task.Delay(20000);
                                    tradeevo = true;
                                    attempts = -40; // Try 50 button presses.
                                }
                            }
                            break;

                        case BotState.TryFinish:
                            if (!tradeevo)
                            {
                                if (isUSUM)
                                {
                                    Report("Bot: Press A button");
                                    waitTaskbool = helper.waitbutton(LookupTable
                                        .ButtonA);
                                }
                                else
                                {
                                    Report("Bot: Press B button");
                                    waitTaskbool = helper.waitbutton(LookupTable
                                        .ButtonB);
                                }
                            }
                            else
                            {
                                Report("Bot: Trade evolution detected, press A button");
                                waitTaskbool = helper.waitbutton(LookupTable
                                    .ButtonA);
                            }
                            if (await waitTaskbool)
                            {
                                botstate = BotState.TestTradeFinish;
                            }
                            else
                            {
                                attempts++;
                                botresult = ErrorMessage.ButtonError;
                                botstate = BotState.TestTradeFinish;
                            }
                            break;

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

                        case BotState.CollectFC1:
                            Report("Bot: Trigger Dialog");
                            await Task.Delay(4 * delaytime);
                            waitTaskbool = helper.waitbutton(LookupTable.ButtonA);
                            if (await waitTaskbool)
                            {
                                botstate = BotState.CollectFC2;
                            }
                            else
                            {
                                attempts++;
                                botresult = ErrorMessage.ButtonError;
                                botstate = BotState.TestTradeFinish;
                            }
                            break;

                        case BotState.CollectFC2:
                            Report("Bot: Test if dialog has started");
                            waitTaskbool = helper.memoryinrange(dialogOff,
                                dialogIn, 0x010000);
                            if (await waitTaskbool)
                            {
                                attempts = 0;
                                botstate = BotState.CollectFC3;
                            }
                            else
                            {
                                attempts++;
                                botresult = ErrorMessage.ReadError;
                                botstate = BotState.CollectFC1;
                            }
                            break;

                        case BotState.CollectFC3:
                            Report("Bot: Continue dialog");
                            waitTaskbool = helper.waitbutton(LookupTable.ButtonB);
                            if (await waitTaskbool)
                            {
                                botstate = BotState.CollectFC4;
                            }
                            else
                            {
                                attempts++;
                                botresult = ErrorMessage.ButtonError;
                                botstate = BotState.CollectFC3;
                            }
                            break;

                        case BotState.CollectFC4:
                            Report("Bot: Test if dialog has finished");
                            waitTaskbool = helper.memoryinrange(dialogOff,
                                dialogOut, 0x010000);
                            if (await waitTaskbool || helper.lastRead == 0x0D)
                            {
                                attempts = 0;
                                botstate = BotState.CollectFC5;
                            }
                            else
                            {
                                attempts++;
                                botresult = ErrorMessage.ReadError;
                                botstate = BotState.CollectFC3;
                            }
                            break;

                        case BotState.CollectFC5:
                            Report("Bot: Test FC");
                            waitTaskbool = helper.waitNTRread(LookupTable
                                .TrainerTotalFCOffset);
                            if (await waitTaskbool)
                            {
                                attempts = 0;
                                currentTotalFC = helper.lastRead;
                                Report("Bot: Current Total FC: " + currentTotalFC);
                                if (currentTotalFC >= nextFC)
                                {
                                    Report("Bot: Festival Plaza level up");
                                    GetNextSlot();
                                    botresult = ErrorMessage.FestivalPlaza;
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
                                botresult = ErrorMessage.ReadError;
                                botstate = BotState.CollectFC5;
                            }
                            break;

                        case BotState.DumpAfter:
                            if (afterDump.Checked)
                            {
                                Report("Bot: Dump boxes");
                                waitTaskbool = helper.waitNTRmultiread(LookupTable
                                    .BoxOffset, 232 * 30 * 31);
                                if (await waitTaskbool)
                                {
                                    attempts = 0;
                                    string fileName = "WTAfter-" + DateTime.Now.ToString
                                        ("yyyyMMddHHmmss") + ".ek7";
                                    WriteDataToFile(helper
                                        .lastmultiread, wtfolderpath + fileName);
                                    botstate = BotState.ActionAfter;
                                }
                                else
                                {
                                    attempts++;
                                    botresult = ErrorMessage.ReadError;
                                    botstate = BotState.DumpAfter;
                                }
                            }
                            else
                            {
                                botstate = BotState.ActionAfter;
                            }
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
                                botresult = ErrorMessage.Finished;
                                botstate = BotState.ExitBot;
                            }
                            break;

                        case BotState.RestoreBackup:
                            Report("Bot: Restore boxes backup");
                            byte[] restore = File.ReadAllBytes(backuppath);
                            if (restore.Length == 232 * 30 * 32)
                            {
                                waitTaskbool = helper.waitNTRwrite(LookupTable
                                    .BoxOffset, restore, pid);
                                if (await waitTaskbool)
                                {
                                    attempts = 0;
                                    botresult = ErrorMessage.Finished;
                                    botstate = BotState.ExitBot;
                                }
                                else
                                {
                                    attempts++;
                                    botresult = ErrorMessage.WriteError;
                                    botstate = BotState.RestoreBackup;
                                }
                            }
                            else
                            {
                                Report("Bot: Invalid boxes file");
                                botresult = ErrorMessage.GeneralError;
                                botstate = BotState.ExitBot;
                            }
                            break;

                        case BotState.DeletePoke:
                            Report("Bot: Delete traded pokémon");
                            byte[] deletearray = new byte[232 * (int)starttrades];
                            for (int i = 0; i < starttrades; i++)
                            {
                                SaveFileEditor.SAV.BlankPKM.EncryptedBoxData.CopyTo(
                                    deletearray, i * 232);
                            }
                            waitTaskbool = helper.waitNTRwrite(GetBoxOffset(
                                LookupTable.BoxOffset, Box, Slot), deletearray,
                                pid);
                            if (await waitTaskbool)
                            {
                                attempts = 0;
                                botstate = BotState.ExitBot;
                            }
                            else
                            {
                                attempts++;
                                botresult = ErrorMessage.WriteError;
                                botstate = BotState.DeletePoke;
                            }
                            break;

                        case BotState.ExitBot:
                            Report("Bot: Stop Gen 7 Wonder Trade bot");
                            botworking = false;
                            break;

                        default:
                            Report("Bot: Stop Gen 7 Wonder Trade bot");
                            botresult = ErrorMessage.GeneralError;
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
                                await Task.Delay(10 * delaytime);
                                attempts = 0;
                            }
                            else
                            {
                                botresult = ErrorMessage.GeneralError;
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
                botresult = ErrorMessage.GeneralError;
            }
            if (userstop)
            {
                botresult = ErrorMessage.UserStop;
            }
            ShowResult("Wonder Trade bot", botresult);
            Delg.SetText(RunStop, "Start Bot");
            ntrClient.disconnect();
            EnableControls();
            Delg.SetEnabled(RunStop, true);
        }

        /// <summary>
        /// Sets the maximum number of WT possible based on the selected box and slot.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Box_ValueChanged(object sender, EventArgs e)
        {
            Delg.SetMaximum(Trades, LookupTable.GetRemainingSpaces((int)Box.Value,
                (int)Slot.Value));
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
                if (sourceBox.Checked)
                {
                    botstate = BotState.ReadPoke;
                }
                else
                {
                    botstate = BotState.WriteFromFolder;
                }
                attempts = 0;
            }
            else if (runEndless.Checked)
            {
                Delg.SetValue(Box, startbox);
                Delg.SetValue(Slot, startslot);
                Delg.SetValue(Trades, starttrades);
                if (sourceBox.Checked)
                {
                    botstate = BotState.ReadPoke;
                }
                else
                {
                    botstate = BotState.WriteFromFolder;
                }
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
                if (sourceBox.Checked)
                {
                    botstate = BotState.ReadPoke;
                }
                else
                {
                    botstate = BotState.WriteFromFolder;
                }
                attempts = 0;
            }
            else if (runEndless.Checked)
            {
                Delg.SetValue(Box, startbox);
                Delg.SetValue(Slot, startslot);
                Delg.SetValue(Trades, starttrades);
                if (sourceBox.Checked)
                {
                    botstate = BotState.ReadPoke;
                }
                else
                {
                    botstate = BotState.WriteFromFolder;
                }
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
                MessageBox.Show("Stop the bot before closing this window",
                    "Wonder Trade bot", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
            ntrClient.disconnect();
        }

        /// <summary>
        /// Reference for storing bot-related files.
        /// </summary>
        public static string BotFolder
        {
            get
            {
                return Path.Combine(Application.StartupPath, "Bot");
            }
        }

        /// <summary>
        /// Write data to the log.
        /// </summary>
        /// <param name="message">String which will be added to the log</param>
        public static void Report(string message)
        {
            Console.WriteLine(message);
        }

        /// <summary>
        /// Check if a pokémon is legal,
        /// </summary>
        /// <param name="poke">PKM data to check.</param>
        /// <returns>Returns true if the pokémon is legal or if the program is in illegal
        /// mode. Returns false otherwise.</returns>
        public static bool IsLegal(PKM poke)
        {
            LegalityAnalysis Legal = new LegalityAnalysis(poke);
            if (Legal.Parsed)
            {
                return Legal.Valid;
            }
            else
            {
                return true;
            }
        }

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
            if (!IsLegal(poke))
            { // Don't trade illegal pokemon
                return false;
            }

            if (poke.IsEgg)
            { // Don't trade eggs
                return false;
            }

            if (poke.Format == 6)
            {
                var poke6 = new PK6(poke.Data);
                if (poke6.RibbonCountry || poke6.RibbonWorld || poke6.RibbonClassic ||
                    poke6.RibbonPremier || poke6.RibbonEvent || poke6.RibbonBirthday ||
                    poke6.RibbonSpecial || poke6.RibbonSouvenir || poke6.RibbonWishing ||
                    poke6.RibbonChampionBattle || poke6.RibbonChampionRegional ||
                    poke6.RibbonChampionNational || poke6.RibbonChampionWorld)
                { // Check for Special Ribbons
                    return false;
                }
            }
            if (poke.Format == 7)
            {
                var poke7 = new PK7(poke.Data);
                if (poke7.RibbonCountry || poke7.RibbonWorld || poke7.RibbonClassic ||
                    poke7.RibbonPremier || poke7.RibbonEvent || poke7.RibbonBirthday ||
                    poke7.RibbonSpecial || poke7.RibbonSouvenir || poke7.RibbonWishing ||
                    poke7.RibbonChampionBattle || poke7.RibbonChampionRegional ||
                    poke7.RibbonChampionNational || poke7.RibbonChampionWorld)
                { // Check for Special Ribbons
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Gets the RAM offset of a pokémon in the PC.
        /// </summary>
        /// <param name="startOffset">Offset of the first pokémon in the PC.</param>
        /// <param name="boxSource">Box reference.</param>
        /// <param name="slotSource">Slot reference.</param>
        /// <returns>Returns an unsigned integer with the RAM address of the selected PC 
        /// slot</returns>
        public static uint GetBoxOffset(uint startOffset, NumericUpDown boxSource,
            NumericUpDown slotSource)
        {
            return startOffset + (uint)(boxSource.Value - 1) * 30 * 232 +
                (uint)(slotSource.Value - 1) * 232;
        }

        /// <summary>
        /// Gets the zero-indexed value of a NumpericUpDown field.
        /// </summary>
        /// <param name="ctrl"></param>
        /// <returns>Returns the value of the NumericUpDown minus one.</returns>
        public static int GetIndex(NumericUpDown ctrl)
        {
            return (int)ctrl.Value - 1;
        }

        /// <summary>
        /// Compare a pokémon against a list of filters.</summary>
        /// <param name="poke">PKM data to compare.</param>
        /// <param name="filters">Filter list.</param>
        /// <returns>If the pokémon passes all the tests of one of the filters this
        /// method returns the filter position in the list. If no match is found it
        /// returns -1.</returns>
        public static int CheckFilters(PKM poke, DataGridView filters)
        {
            int currentFilter;
            int failedTests;
            int perfectIVs;
            if (filters.Rows.Count > 0)
            {
                currentFilter = 0;
                foreach (DataGridViewRow row in filters.Rows)
                {
                    currentFilter++;
                    Report("\r\nFilter: Analyze pokémon using filter # " + currentFilter);
                    failedTests = 0;
                    perfectIVs = 0;
                    // Test shiny
                    if ((int)row.Cells[0].Value == 1)
                    {
                        if (poke.IsShiny)
                        {
                            Report("Filter: Shiny - PASS");
                        }
                        else
                        {
                            Report("Filter: Shiny - FAIL");
                            failedTests++;
                        }
                    }
                    else
                    {
                        Report("Filter: Shiny - Don't care");
                    }

                    // Test nature
                    if ((int)row.Cells[1].Value < 0 || poke.Nature ==
                        (int)row.Cells[1].Value)
                    {
                        Report("Filter: Nature - PASS");
                    }
                    else
                    {
                        Report("Filter: Nature - FAIL");
                        failedTests++;
                    }

                    // Test Ability
                    if ((int)row.Cells[2].Value < 0 || (poke.Ability - 1) ==
                        (int)row.Cells[2].Value)
                    {
                        Report("Filter: Ability - PASS");
                    }
                    else
                    {
                        Report("Filter: Ability - FAIL");
                        failedTests++;
                    }

                    // Test Hidden Power
                    if ((int)row.Cells[3].Value < 0 || poke.HPType ==
                        (int)row.Cells[3].Value)
                    {
                        Report("Filter: Hidden Power - PASS");
                    }
                    else
                    {
                        Report("Filter: Hidden Power - FAIL");
                        failedTests++;
                    }

                    // Test Gender
                    if ((int)row.Cells[4].Value < 0 || (int)row.Cells[4].Value ==
                        poke.Gender)
                    {
                        Report("Filter: Gender - PASS");
                    }
                    else
                    {
                        Report("Filter: Gender - FAIL");
                        failedTests++;
                    }

                    // Test HP
                    if (IVCheck((int)row.Cells[5].Value, poke.IV_HP,
                        (int)row.Cells[6].Value))
                    {
                        Report("Filter: Hit Points IV - PASS");
                    }
                    else
                    {
                        Report("Filter: Hit Points IV - FAIL");
                        failedTests++;
                    }
                    if (poke.IV_HP == 31)
                    {
                        perfectIVs++;
                    }

                    // Test Atk
                    if (IVCheck((int)row.Cells[7].Value, poke.IV_ATK,
                        (int)row.Cells[8].Value))
                    {
                        Report("Filter: Attack IV - PASS");
                    }
                    else
                    {
                        Report("Filter: Attack IV - FAIL");
                        failedTests++;
                    }
                    if (poke.IV_ATK == 31)
                    {
                        perfectIVs++;
                    }

                    // Test Def
                    if (IVCheck((int)row.Cells[9].Value, poke.IV_DEF,
                        (int)row.Cells[10].Value))
                    {
                        Report("Filter: Defense IV - PASS");
                    }
                    else
                    {
                        Report("Filter: Defense IV - FAIL");
                        failedTests++;
                    }
                    if (poke.IV_DEF == 31)
                    {
                        perfectIVs++;
                    }

                    // Test SpA
                    if (IVCheck((int)row.Cells[11].Value, poke.IV_SPA,
                        (int)row.Cells[12].Value))
                    {
                        Report("Filter: Special Attack IV - PASS");
                    }
                    else
                    {
                        Report("Filter: Special Attack IV - FAIL");
                        failedTests++;
                    }
                    if (poke.IV_SPA == 31)
                    {
                        perfectIVs++;
                    }

                    // Test SpD
                    if (IVCheck((int)row.Cells[13].Value, poke.IV_SPD,
                        (int)row.Cells[14].Value))
                    {
                        Report("Filter: Special Defense IV - PASS");
                    }
                    else
                    {
                        Report("Filter: Special Defense IV - FAIL");
                        failedTests++;
                    }
                    if (poke.IV_SPD == 31)
                    {
                        perfectIVs++;
                    }

                    // Test Spe
                    if (IVCheck((int)row.Cells[15].Value, poke.IV_SPE,
                        (int)row.Cells[16].Value))
                    {
                        Report("Filter: Speed IV - PASS");
                    }
                    else
                    {
                        Report("Filter: Speed IV - FAIL");
                        failedTests++;
                    }
                    if (poke.IV_SPE == 31)
                    {
                        perfectIVs++;
                    }

                    // Test Perfect IVs
                    if (IVCheck((int)row.Cells[17].Value, perfectIVs,
                        (int)row.Cells[18].Value))
                    {
                        Report("Filter: Perfect IVs - PASS");
                    }
                    else
                    {
                        Report("Filter: Perfect IVs - FAIL");
                        failedTests++;
                    }
                    if (failedTests == 0)
                    {
                        return currentFilter;
                    }
                }
                return -1;
            }
            else
            {
                return 1;
            }
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
        public static void ShowResult(string source, ErrorMessage message,
            int[] info = null)
        {
            switch (message)
            {
                case ErrorMessage.Finished:
                    MessageBox.Show("Bot finished sucessfully.", source,
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    break;
                case ErrorMessage.UserStop:
                    MessageBox.Show("Bot stopped by the user.", source,
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    break;
                case ErrorMessage.ReadError:
                    MessageBox.Show("A error ocurred while reading data from the " +
                        "3DS RAM.", source, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    break;
                case ErrorMessage.WriteError:
                    MessageBox.Show("A error ocurred while writting data to the 3DS RAM.",
                        source, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    break;
                case ErrorMessage.ButtonError:
                    MessageBox.Show("A error ocurred while sending Button commands to " +
                        "the 3DS.", source, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    break;
                case ErrorMessage.TouchError:
                    MessageBox.Show("A error ocurred while sending Touch Screen " +
                        "commands to the 3DS.", source, MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    break;
                case ErrorMessage.StickError:
                    MessageBox.Show("A error ocurred while sending Control Stick " +
                        "commands to the 3DS.", source, MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    break;
                case ErrorMessage.NotInPSS:
                    MessageBox.Show("Please go to the PSS menu and try again.", source,
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    break;
                case ErrorMessage.FestivalPlaza:
                    MessageBox.Show("Bot finished due level-up in Festival Plaza.",
                        source, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    break;
                case ErrorMessage.SVMatch:
                    MessageBox.Show($"Finished. A match was found at box {info[0]}, " +
                        $"slot{info[1]} with the ESV/TSV value: {info[2]}.", source,
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    break;
                case ErrorMessage.FilterMatch:
                    MessageBox.Show($"Finished. A match was found at box {info[0]}, " +
                        $"slot {info[1]} using filter #{info[2]}.", source,
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    break;
                case ErrorMessage.NoMatch:
                    MessageBox.Show("Bot finished sucessfuly without finding a match " +
                        "for the current settings.", source, MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    break;
                case ErrorMessage.SRMatch:
                    MessageBox.Show($"Finished. The current pokémon matched filter " +
                        $"#{info[0]} after {info[1]} soft-resets.", source,
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    break;
                case ErrorMessage.BattleMatch:
                    MessageBox.Show($"Finished. The current pokémon matched filter " +
                        $"#{info[0]} after {info[1]} battles.", source,
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    break;
                case ErrorMessage.Disconnect:
                    MessageBox.Show("Connection with the 3DS was lost.", source,
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    break;
                case ErrorMessage.NotWTMenu:
                    MessageBox.Show("Please, go to the Wonder trade screen and try again.",
                        source, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    break;
                case ErrorMessage.GeneralError:
                    MessageBox.Show("A error has ocurred, see log for detals.", source,
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    break;
                default:
                    MessageBox.Show("An unknown error has ocurred, please keep the " +
                        "log and report this error.", source, MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    break;
            }
        }
    }
}
