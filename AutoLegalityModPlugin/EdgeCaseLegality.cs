using System.Collections.Generic;
using System.Linq;

using PKHeX.Core;

/// <summary>
/// Covers everything that is not covered by / Cannot currently be covered by Auto Legality Mod generic legality formula
/// AKA Hardcoding certain things that I cannot fix without restructuring everything I have done so far :[
/// </summary>

namespace PKHeX.AutoLegality
{
    public class EdgeCaseLegality
    {
        public EncounterStatic[] BWEntreeForest = MarkG5DreamWorld(BW_DreamWorld);
        public EncounterStatic[] B2W2EntreeForest = MarkG5DreamWorld(B2W2_DreamWorld);
        public EncounterStatic[] USUMEdgeEnc = MarkEncountersGeneration(USUMEdgeEncounters, 7);

        ///<summary>
        ///Gen 5 Dreamworld wack events
        ///</summary>

        public static EncounterStatic[] DreamWorld_Common =
        {
            // Pleasant forest
            new EncounterStatic { Species=019, Level = 10, Moves = new[]{098, 382, 231}, },	//Rattata
            new EncounterStatic { Species=043, Level = 10, Moves = new[]{230, 298, 202}, },	//Oddish
            new EncounterStatic { Species=069, Level = 10, Moves = new[]{022, 235, 402}, },	//Bellsprout
            new EncounterStatic { Species=077, Level = 10, Moves = new[]{033, 037, 257}, },	//Ponyta
            new EncounterStatic { Species=083, Level = 10, Moves = new[]{210, 355, 348}, },	//Farfetch'd
            new EncounterStatic { Species=084, Level = 10, Moves = new[]{045, 175, 355}, },	//Doduo
            new EncounterStatic { Species=102, Level = 10, Moves = new[]{140, 235, 202}, },	//Exeggcute
            new EncounterStatic { Species=108, Level = 10, Moves = new[]{122, 214, 431}, },	//Lickitung
            new EncounterStatic { Species=114, Level = 10, Moves = new[]{079, 073, 402}, },	//Tangela
            new EncounterStatic { Species=115, Level = 10, Moves = new[]{252, 068, 409}, },	//Kangaskhan
            new EncounterStatic { Species=161, Level = 10, Moves = new[]{010, 203, 343}, },	//Sentret
            new EncounterStatic { Species=179, Level = 10, Moves = new[]{084, 115, 351}, },	//Mareep
            new EncounterStatic { Species=191, Level = 10, Moves = new[]{072, 230, 414}, },	//Sunkern
            new EncounterStatic { Species=234, Level = 10, Moves = new[]{033, 050, 285}, },	//Stantler
            new EncounterStatic { Species=261, Level = 10, Moves = new[]{336, 305, 399}, },	//Poochyena
            new EncounterStatic { Species=283, Level = 10, Moves = new[]{145, 056, 202}, },	//Surskit
            new EncounterStatic { Species=399, Level = 10, Moves = new[]{033, 401, 290}, },	//Bidoof
            new EncounterStatic { Species=403, Level = 10, Moves = new[]{268, 393, 400}, },	//Shinx
            new EncounterStatic { Species=431, Level = 10, Moves = new[]{252, 372, 290}, },	//Glameow
            new EncounterStatic { Species=054, Level = 10, Moves = new[]{346, 227, 362}, },	//Psyduck
            new EncounterStatic { Species=058, Level = 10, Moves = new[]{044, 034, 203}, },	//Growlithe
            new EncounterStatic { Species=123, Level = 10, Moves = new[]{098, 226, 366}, },	//Scyther
            new EncounterStatic { Species=128, Level = 10, Moves = new[]{099, 231, 431}, },	//Tauros
            new EncounterStatic { Species=183, Level = 10, Moves = new[]{111, 453, 008}, },	//Marill
            new EncounterStatic { Species=185, Level = 10, Moves = new[]{175, 205, 272}, },	//Sudowoodo
            new EncounterStatic { Species=203, Level = 10, Moves = new[]{093, 243, 285}, },	//Girafarig
            new EncounterStatic { Species=241, Level = 10, Moves = new[]{111, 174, 231}, },	//Miltank
            new EncounterStatic { Species=263, Level = 10, Moves = new[]{033, 271, 387}, },	//Zigzagoon
            new EncounterStatic { Species=427, Level = 10, Moves = new[]{193, 252, 409}, },	//Buneary
            new EncounterStatic { Species=037, Level = 10, Moves = new[]{046, 257, 399}, },	//Vulpix
            new EncounterStatic { Species=060, Level = 10, Moves = new[]{095, 054, 214}, },	//Poliwag
            new EncounterStatic { Species=177, Level = 10, Moves = new[]{101, 297, 202}, },	//Natu
            new EncounterStatic { Species=239, Level = 10, Moves = new[]{084, 238, 393}, },	//Elekid
            new EncounterStatic { Species=300, Level = 10, Moves = new[]{193, 321, 445}, },	//Skitty
            // Windskept Sky
            new EncounterStatic { Species=016, Level = 10, Moves = new[]{016, 211, 290}, },	//Pidgey
            new EncounterStatic { Species=021, Level = 10, Moves = new[]{064, 185, 211}, },	//Spearow
            new EncounterStatic { Species=041, Level = 10, Moves = new[]{048, 095, 162}, },	//Zubat
            new EncounterStatic { Species=142, Level = 10, Moves = new[]{044, 372, 446}, },	//Aerodactyl
            new EncounterStatic { Species=165, Level = 10, Moves = new[]{004, 450, 009}, },	//Ledyba
            new EncounterStatic { Species=187, Level = 10, Moves = new[]{235, 227, 340}, },	//Hoppip
            new EncounterStatic { Species=193, Level = 10, Moves = new[]{098, 364, 202}, },	//Yanma
            new EncounterStatic { Species=198, Level = 10, Moves = new[]{064, 109, 355}, },	//Murkrow
            new EncounterStatic { Species=207, Level = 10, Moves = new[]{028, 364, 366}, },	//Gligar
            new EncounterStatic { Species=225, Level = 10, Moves = new[]{217, 420, 264}, },	//Delibird
            new EncounterStatic { Species=276, Level = 10, Moves = new[]{064, 203, 413}, },	//Taillow
            new EncounterStatic { Species=397, Level = 14, Moves = new[]{017, 297, 366}, },	//Staravia
            new EncounterStatic { Species=227, Level = 10, Moves = new[]{064, 065, 355}, },	//Skarmory
            new EncounterStatic { Species=357, Level = 10, Moves = new[]{016, 073, 318}, },	//Tropius
            // Sparkling Sea
            new EncounterStatic { Species=086, Level = 10, Moves = new[]{029, 333, 214}, },	//Seel
            new EncounterStatic { Species=090, Level = 10, Moves = new[]{110, 112, 196}, },	//Shellder
            new EncounterStatic { Species=116, Level = 10, Moves = new[]{145, 190, 362}, },	//Horsea
            new EncounterStatic { Species=118, Level = 10, Moves = new[]{064, 060, 352}, },	//Goldeen
            new EncounterStatic { Species=129, Level = 10, Moves = new[]{150, 175, 340}, },	//Magikarp
            new EncounterStatic { Species=138, Level = 10, Moves = new[]{044, 330, 196}, },	//Omanyte
            new EncounterStatic { Species=140, Level = 10, Moves = new[]{071, 175, 446}, },	//Kabuto
            new EncounterStatic { Species=170, Level = 10, Moves = new[]{086, 133, 351}, },	//Chinchou
            new EncounterStatic { Species=194, Level = 10, Moves = new[]{055, 034, 401}, },	//Wooper
            new EncounterStatic { Species=211, Level = 10, Moves = new[]{040, 453, 290}, },	//Qwilfish
            new EncounterStatic { Species=223, Level = 10, Moves = new[]{199, 350, 362}, },	//Remoraid
            new EncounterStatic { Species=226, Level = 10, Moves = new[]{048, 243, 314}, },	//Mantine
            new EncounterStatic { Species=320, Level = 10, Moves = new[]{055, 214, 340}, },	//Wailmer
            new EncounterStatic { Species=339, Level = 10, Moves = new[]{189, 214, 209}, },	//Barboach
            new EncounterStatic { Species=366, Level = 10, Moves = new[]{250, 445, 392}, },	//Clamperl
            new EncounterStatic { Species=369, Level = 10, Moves = new[]{055, 214, 414}, },	//Relicanth
            new EncounterStatic { Species=370, Level = 10, Moves = new[]{204, 300, 196}, },	//Luvdisc
            new EncounterStatic { Species=418, Level = 10, Moves = new[]{346, 163, 352}, },	//Buizel
            new EncounterStatic { Species=456, Level = 10, Moves = new[]{213, 186, 352}, },	//Finneon
            new EncounterStatic { Species=072, Level = 10, Moves = new[]{048, 367, 202}, },	//Tentacool
            new EncounterStatic { Species=318, Level = 10, Moves = new[]{044, 037, 399}, },	//Carvanha
            new EncounterStatic { Species=341, Level = 10, Moves = new[]{106, 232, 283}, },	//Corphish
            new EncounterStatic { Species=345, Level = 10, Moves = new[]{051, 243, 202}, },	//Lileep
            new EncounterStatic { Species=347, Level = 10, Moves = new[]{010, 446, 440}, },	//Anorith
            new EncounterStatic { Species=349, Level = 10, Moves = new[]{150, 445, 243}, },	//Feebas
            new EncounterStatic { Species=131, Level = 10, Moves = new[]{109, 032, 196}, },	//Lapras
            new EncounterStatic { Species=147, Level = 10, Moves = new[]{086, 352, 225}, },	//Dratini
            // Spooky Mannor
            new EncounterStatic { Species=092, Level = 10, Moves = new[]{095, 050, 482}, },	//Gastly
            new EncounterStatic { Species=096, Level = 10, Moves = new[]{095, 427, 409}, },	//Drowzee
            new EncounterStatic { Species=122, Level = 10, Moves = new[]{112, 298, 285}, },	//Mr. Mime
            new EncounterStatic { Species=167, Level = 10, Moves = new[]{040, 527, 450}, },	//Spinarak
            new EncounterStatic { Species=200, Level = 10, Moves = new[]{149, 194, 517}, },	//Misdreavus
            new EncounterStatic { Species=228, Level = 10, Moves = new[]{336, 364, 399}, },	//Houndour
            new EncounterStatic { Species=325, Level = 10, Moves = new[]{149, 285, 278}, },	//Spoink
            new EncounterStatic { Species=353, Level = 10, Moves = new[]{101, 194, 220}, },	//Shuppet
            new EncounterStatic { Species=355, Level = 10, Moves = new[]{050, 220, 271}, },	//Duskull
            new EncounterStatic { Species=358, Level = 10, Moves = new[]{035, 095, 304}, },	//Chimecho
            new EncounterStatic { Species=434, Level = 10, Moves = new[]{103, 492, 389}, },	//Stunky
            new EncounterStatic { Species=209, Level = 10, Moves = new[]{204, 370, 038}, },	//Snubbull
            new EncounterStatic { Species=235, Level = 10, Moves = new[]{166, 445, 214}, },	//Smeargle
            new EncounterStatic { Species=313, Level = 10, Moves = new[]{148, 271, 366}, },	//Volbeat
            new EncounterStatic { Species=314, Level = 10, Moves = new[]{204, 313, 366}, },	//Illumise
            new EncounterStatic { Species=063, Level = 10, Moves = new[]{100, 285, 356}, },	//Abra
            // Rugged Mountain
            new EncounterStatic { Species=066, Level = 10, Moves = new[]{067, 418, 270}, },	//Machop
            new EncounterStatic { Species=081, Level = 10, Moves = new[]{319, 278, 356}, },	//Magnemite
            new EncounterStatic { Species=109, Level = 10, Moves = new[]{123, 399, 482}, },	//Koffing
            new EncounterStatic { Species=218, Level = 10, Moves = new[]{052, 517, 257}, },	//Slugma
            new EncounterStatic { Species=246, Level = 10, Moves = new[]{044, 399, 446}, },	//Larvitar
            new EncounterStatic { Species=324, Level = 10, Moves = new[]{052, 090, 446}, },	//Torkoal
            new EncounterStatic { Species=328, Level = 10, Moves = new[]{044, 324, 202}, },	//Trapinch
            new EncounterStatic { Species=331, Level = 10, Moves = new[]{071, 298, 009}, },	//Cacnea
            new EncounterStatic { Species=412, Level = 10, Moves = new[]{182, 450, 173}, },	//Burmy
            new EncounterStatic { Species=449, Level = 10, Moves = new[]{044, 254, 276}, },	//Hippopotas
            new EncounterStatic { Species=240, Level = 10, Moves = new[]{052, 009, 257}, },	//Magby
            new EncounterStatic { Species=322, Level = 10, Moves = new[]{052, 034, 257}, },	//Numel
            new EncounterStatic { Species=359, Level = 10, Moves = new[]{364, 224, 276}, },	//Absol
            new EncounterStatic { Species=453, Level = 10, Moves = new[]{040, 409, 441}, },	//Croagunk
            new EncounterStatic { Species=236, Level = 10, Moves = new[]{252, 364, 183}, },	//Tyrogue
            new EncounterStatic { Species=371, Level = 10, Moves = new[]{044, 349, 200}, },	//Bagon
            // Icy Cave
            new EncounterStatic { Species=027, Level = 10, Moves = new[]{028, 068, 162}, },	//Sandshrew
            new EncounterStatic { Species=074, Level = 10, Moves = new[]{111, 446, 431}, },	//Geodude
            new EncounterStatic { Species=095, Level = 10, Moves = new[]{020, 446, 431}, },	//Onix
            new EncounterStatic { Species=100, Level = 10, Moves = new[]{268, 324, 363}, },	//Voltorb
            new EncounterStatic { Species=104, Level = 10, Moves = new[]{125, 195, 067}, },	//Cubone
            new EncounterStatic { Species=293, Level = 10, Moves = new[]{253, 283, 428}, },	//Whismur
            new EncounterStatic { Species=304, Level = 10, Moves = new[]{106, 283, 457}, },	//Aron
            new EncounterStatic { Species=337, Level = 10, Moves = new[]{093, 414, 236}, },	//Lunatone
            new EncounterStatic { Species=338, Level = 10, Moves = new[]{093, 428, 234}, },	//Solrock
            new EncounterStatic { Species=343, Level = 10, Moves = new[]{229, 356, 428}, },	//Baltoy
            new EncounterStatic { Species=459, Level = 10, Moves = new[]{075, 419, 202}, },	//Snover
            new EncounterStatic { Species=050, Level = 10, Moves = new[]{028, 251, 446}, },	//Diglett
            new EncounterStatic { Species=215, Level = 10, Moves = new[]{269, 008, 067}, },	//Sneasel
            new EncounterStatic { Species=361, Level = 10, Moves = new[]{181, 311, 352}, },	//Snorunt
            new EncounterStatic { Species=220, Level = 10, Moves = new[]{316, 246, 333}, },	//Swinub
            new EncounterStatic { Species=443, Level = 10, Moves = new[]{082, 200, 203}, },	//Gible
            // Dream Park
            new EncounterStatic { Species=046, Level = 10, Moves = new[]{078, 440, 235}, },	//Paras
            new EncounterStatic { Species=204, Level = 10, Moves = new[]{120, 390, 356}, },	//Pineco
            new EncounterStatic { Species=265, Level = 10, Moves = new[]{040, 450, 173}, },	//Wurmple
            new EncounterStatic { Species=273, Level = 10, Moves = new[]{074, 331, 492}, },	//Seedot
            new EncounterStatic { Species=287, Level = 10, Moves = new[]{281, 400, 389}, },	//Slakoth
            new EncounterStatic { Species=290, Level = 10, Moves = new[]{141, 203, 400}, },	//Nincada
            new EncounterStatic { Species=311, Level = 10, Moves = new[]{086, 435, 324}, },	//Plusle
            new EncounterStatic { Species=312, Level = 10, Moves = new[]{086, 435, 324}, },	//Minun
            new EncounterStatic { Species=316, Level = 10, Moves = new[]{139, 151, 202}, },	//Gulpin
            new EncounterStatic { Species=352, Level = 10, Moves = new[]{185, 285, 513}, },	//Kecleon
            new EncounterStatic { Species=401, Level = 10, Moves = new[]{522, 283, 253}, },	//Kricketot
            new EncounterStatic { Species=420, Level = 10, Moves = new[]{073, 505, 331}, },	//Cherubi
            new EncounterStatic { Species=455, Level = 10, Moves = new[]{044, 476, 380}, },	//Carnivine
            new EncounterStatic { Species=023, Level = 10, Moves = new[]{040, 251, 399}, },	//Ekans
            new EncounterStatic { Species=175, Level = 10, Moves = new[]{118, 381, 253}, },	//Togepi
            new EncounterStatic { Species=190, Level = 10, Moves = new[]{010, 252, 007}, },	//Aipom
            new EncounterStatic { Species=285, Level = 10, Moves = new[]{078, 331, 264}, },	//Shroomish
            new EncounterStatic { Species=315, Level = 10, Moves = new[]{074, 079, 129}, },	//Roselia
            new EncounterStatic { Species=113, Level = 10, Moves = new[]{045, 068, 270}, },	//Chansey
            new EncounterStatic { Species=127, Level = 10, Moves = new[]{011, 370, 382}, },	//Pinsir
            new EncounterStatic { Species=133, Level = 10, Moves = new[]{028, 204, 129}, },	//Eevee
            new EncounterStatic { Species=143, Level = 10, Moves = new[]{133, 007, 278}, },	//Snorlax
            new EncounterStatic { Species=214, Level = 10, Moves = new[]{030, 175, 264}, },	//Heracross
            // Pokémon Café Forest
            new EncounterStatic { Species=061, Level = 25, Moves = new[]{240, 114, 352}, },	//Poliwhirl
            new EncounterStatic { Species=133, Level = 10, Moves = new[]{270, 204, 129}, },	//Eevee
            new EncounterStatic { Species=235, Level = 10, Moves = new[]{166, 445, 214}, },	//Smeargle
            new EncounterStatic { Species=412, Level = 10, Moves = new[]{182, 450, 173}, },	//Burmy
            //PGL
            new EncounterStatic { Species=212, Level = 10, Moves = new[]{211}, Gender = 0, }, //Scizor
            new EncounterStatic { Species=445, Level = 48, Gender = 0, },                     //Garchomp
            new EncounterStatic { Species=149, Level = 55, Moves = new[]{245}, Gender = 0, }, //Dragonite
            new EncounterStatic { Species=248, Level = 55, Moves = new[]{069}, Gender = 0, }, //Tyranitar
            new EncounterStatic { Species=376, Level = 45, Moves = new[]{038}, Gender = 2, }, //Metagross
        };

        static EncounterStatic[] BW_DreamWorld = DreamWorld_Common.Concat(new[]
        {
            // Pleasant forest
            new EncounterStatic { Species=029, Level = 10, Moves = new[]{010, 389, 162}, },	//Nidoran (F)
            new EncounterStatic { Species=032, Level = 10, Moves = new[]{064, 068, 162}, },	//Nidoran (M)
            new EncounterStatic { Species=174, Level = 10, Moves = new[]{047, 313, 270}, },	//Igglybuff  
            new EncounterStatic { Species=187, Level = 10, Moves = new[]{235, 270, 331}, },	//Hoppip     
            new EncounterStatic { Species=270, Level = 10, Moves = new[]{071, 073, 352}, },	//Lotad      
            new EncounterStatic { Species=276, Level = 10, Moves = new[]{064, 119, 366}, },	//Taillow    
            new EncounterStatic { Species=309, Level = 10, Moves = new[]{086, 423, 324}, },	//Electrike  
            new EncounterStatic { Species=351, Level = 10, Moves = new[]{052, 466, 352}, },	//Castform   
            new EncounterStatic { Species=417, Level = 10, Moves = new[]{098, 343, 351}, },	//Pachirisu  
            // Windskept Sky
            new EncounterStatic { Species=012, Level = 10, Moves = new[]{093, 355, 314}, },	//Butterfree 
            new EncounterStatic { Species=163, Level = 10, Moves = new[]{193, 101, 278}, },	//Hoothoot   
            new EncounterStatic { Species=278, Level = 10, Moves = new[]{055, 239, 351}, },	//Wingull     
            new EncounterStatic { Species=333, Level = 10, Moves = new[]{064, 297, 355}, },	//Swablu      
            new EncounterStatic { Species=425, Level = 10, Moves = new[]{107, 095, 285}, },	//Drifloon    
            new EncounterStatic { Species=441, Level = 10, Moves = new[]{119, 417, 272}, },	//Chatot      
            // Sparkling Sea
            new EncounterStatic { Species=079, Level = 10, Moves = new[]{281, 335, 362}, },	//Slowpoke    
            new EncounterStatic { Species=098, Level = 10, Moves = new[]{011, 133, 290}, },	//Krabby      
            new EncounterStatic { Species=119, Level = 33, Moves = new[]{352, 214, 203}, },	//Seaking     
            new EncounterStatic { Species=120, Level = 10, Moves = new[]{055, 278, 196}, },	//Staryu      
            new EncounterStatic { Species=222, Level = 10, Moves = new[]{145, 109, 446}, },	//Corsola     
            new EncounterStatic { Species=422, Level = 10, Moves = new[]{189, 281, 290}, Form = 0 },	//Shellos
            new EncounterStatic { Species=422, Level = 10, Moves = new[]{189, 281, 290}, Form = 1 },
            // Spooky Mannor
            new EncounterStatic { Species=202, Level = 15, Moves = new[]{243, 204, 227}, },	//Wobbuffet   
            new EncounterStatic { Species=238, Level = 10, Moves = new[]{186, 445, 285}, },	//Smoochum    
            new EncounterStatic { Species=303, Level = 10, Moves = new[]{313, 424, 008}, }, //Mawile      
            new EncounterStatic { Species=307, Level = 10, Moves = new[]{096, 409, 203}, },	//Meditite    
            new EncounterStatic { Species=436, Level = 10, Moves = new[]{095, 285, 356}, },	//Bronzor     
            new EncounterStatic { Species=052, Level = 10, Moves = new[]{010, 095, 290}, },	//Meowth      
            new EncounterStatic { Species=479, Level = 10, Moves = new[]{086, 351, 324}, },	//Rotom       
            new EncounterStatic { Species=280, Level = 10, Moves = new[]{093, 194, 270}, },	//Ralts       
            new EncounterStatic { Species=302, Level = 10, Moves = new[]{193, 389, 180}, },	//Sableye     
            new EncounterStatic { Species=442, Level = 10, Moves = new[]{180, 220, 196}, },	//Spiritomb   
            // Rugged Mountain
            new EncounterStatic { Species=056, Level = 10, Moves = new[]{067, 179, 009}, },	//Mankey      
            new EncounterStatic { Species=111, Level = 10, Moves = new[]{030, 068, 038}, },	//Rhyhorn     
            new EncounterStatic { Species=231, Level = 10, Moves = new[]{175, 484, 402}, },	//Phanpy      
            new EncounterStatic { Species=451, Level = 10, Moves = new[]{044, 097, 401}, },	//Skorupi     
            new EncounterStatic { Species=216, Level = 10, Moves = new[]{313, 242, 264}, },	//Teddiursa   
            new EncounterStatic { Species=296, Level = 10, Moves = new[]{292, 270, 008}, },	//Makuhita    
            new EncounterStatic { Species=327, Level = 10, Moves = new[]{383, 252, 276}, },	//Spinda      
            new EncounterStatic { Species=374, Level = 10, Moves = new[]{036, 428, 442}, },	//Beldum      
            new EncounterStatic { Species=447, Level = 10, Moves = new[]{203, 418, 264}, },	//Riolu       
            // Icy Cave
            new EncounterStatic { Species=173, Level = 10, Moves = new[]{227, 312, 214}, },	//Cleffa      
            new EncounterStatic { Species=213, Level = 10, Moves = new[]{227, 270, 504}, },	//Shuckle     
            new EncounterStatic { Species=299, Level = 10, Moves = new[]{033, 446, 246}, },	//Nosepass    
            new EncounterStatic { Species=363, Level = 10, Moves = new[]{181, 090, 401}, },	//Spheal      
            new EncounterStatic { Species=408, Level = 10, Moves = new[]{029, 442, 007}, },	//Cranidos    
            new EncounterStatic { Species=206, Level = 10, Moves = new[]{111, 277, 446}, },	//Dunsparce   
            new EncounterStatic { Species=410, Level = 10, Moves = new[]{182, 068, 090}, },	//Shieldon    
            // Dream Park
            new EncounterStatic { Species=048, Level = 10, Moves = new[]{050, 226, 285}, }, //Venonat     
            new EncounterStatic { Species=088, Level = 10, Moves = new[]{139, 114, 425}, },	//Grimer      
            new EncounterStatic { Species=415, Level = 10, Moves = new[]{016, 366, 314}, },	//Combee      
            new EncounterStatic { Species=015, Level = 10, Moves = new[]{031, 314, 210}, },	//Beedrill    
            new EncounterStatic { Species=335, Level = 10, Moves = new[]{098, 458, 067}, },	//Zangoose    
            new EncounterStatic { Species=336, Level = 10, Moves = new[]{044, 034, 401}, },	//Seviper    
            // PGL
            new EncounterStatic { Species=134, Level = 10, Gender = 0, }, //Vaporeon
            new EncounterStatic { Species=135, Level = 10, Gender = 0, }, //Jolteon
            new EncounterStatic { Species=136, Level = 10, Gender = 0, }, //Flareon
            new EncounterStatic { Species=196, Level = 10, Gender = 0, }, //Espeon
            new EncounterStatic { Species=197, Level = 10, Gender = 0, }, //Umbreon
            new EncounterStatic { Species=470, Level = 10, Gender = 0, }, //Leafeon
            new EncounterStatic { Species=471, Level = 10, Gender = 0, }, //Glaceon
            new EncounterStatic { Species=001, Level = 10, Gender = 0, }, //Bulbasaur
            new EncounterStatic { Species=004, Level = 10, Gender = 0, }, //Charmander
            new EncounterStatic { Species=007, Level = 10, Gender = 0, }, //Squirtle
            new EncounterStatic { Species=453, Level = 10, Gender = 0, }, //Croagunk
            new EncounterStatic { Species=387, Level = 10, Gender = 0, }, //Turtwig
            new EncounterStatic { Species=390, Level = 10, Gender = 0, }, //Chimchar
            new EncounterStatic { Species=393, Level = 10, Gender = 0, }, //Piplup
            new EncounterStatic { Species=493, Level = 100, Shiny = Shiny.Never },             //Arceus
            new EncounterStatic { Species=252, Level = 10, Gender = 0, }, //Treecko
            new EncounterStatic { Species=255, Level = 10, Gender = 0, }, //Torchic
            new EncounterStatic { Species=258, Level = 10, Gender = 0, }, //Mudkip
            new EncounterStatic { Species=468, Level = 10, Moves = new[]{217}, Gender = 0, }, //Togekiss
            new EncounterStatic { Species=473, Level = 34, Gender = 0, }, //Mamoswine
            new EncounterStatic { Species=137, Level = 10 },              //Porygon
            new EncounterStatic { Species=384, Level = 50 },              //Rayquaza
            new EncounterStatic { Species=354, Level = 37, Moves = new[]{538}, Gender = 1, }, //Banette
            new EncounterStatic { Species=453, Level = 10, Moves = new[]{398}, Gender = 0, }, //Croagunk
            new EncounterStatic { Species=334, Level = 35, Moves = new[]{206}, Gender = 0,},  //Altaria
            new EncounterStatic { Species=242, Level = 10 },              //Blissey
            new EncounterStatic { Species=448, Level = 10, Moves = new[]{418}, Gender = 0, }, //Lucario
            new EncounterStatic { Species=189, Level = 27, Moves = new[]{206}, Gender = 0, }, //Jumpluff 
        }).ToArray();

        static EncounterStatic[] B2W2_DreamWorld = DreamWorld_Common.Concat(new[]
        {
            // Pleasant forest
            new EncounterStatic { Species=535, Level = 10, Moves = new[]{496, 414, 352}, },	//Tympole    
            new EncounterStatic { Species=546, Level = 10, Moves = new[]{073, 227, 388}, },	//Cottonee   
            new EncounterStatic { Species=548, Level = 10, Moves = new[]{079, 204, 230}, },	//Petilil    
            new EncounterStatic { Species=588, Level = 10, Moves = new[]{203, 224, 450}, },	//Karrablast 
            new EncounterStatic { Species=616, Level = 10, Moves = new[]{051, 226, 227}, },	//Shelmet    
            new EncounterStatic { Species=545, Level = 30, Moves = new[]{342, 390, 276}, },	//Scolipede  
            // Windskept Sky
            new EncounterStatic { Species=519, Level = 10, Moves = new[]{016, 095, 234}, },	//Pidove      
            new EncounterStatic { Species=561, Level = 10, Moves = new[]{095, 500, 257}, },	//Sigilyph    
            new EncounterStatic { Species=580, Level = 10, Moves = new[]{432, 362, 382}, },	//Ducklett    
            new EncounterStatic { Species=587, Level = 10, Moves = new[]{098, 403, 204}, },	//Emolga      
            // Sparkling Sea
            new EncounterStatic { Species=550, Level = 10, Moves = new[]{029, 097, 428}, Form = 0 },//Basculin
            new EncounterStatic { Species=550, Level = 10, Moves = new[]{029, 097, 428}, Form = 1 },
            new EncounterStatic { Species=594, Level = 10, Moves = new[]{392, 243, 220}, },	//Alomomola   
            new EncounterStatic { Species=618, Level = 10, Moves = new[]{189, 174, 281}, },	//Stunfisk    
            new EncounterStatic { Species=564, Level = 10, Moves = new[]{205, 175, 334}, },	//Tirtouga    
            // Spooky Mannor
            new EncounterStatic { Species=605, Level = 10, Moves = new[]{377, 112, 417}, },	//Elgyem      
            new EncounterStatic { Species=624, Level = 10, Moves = new[]{210, 427, 389}, },	//Pawniard    
            new EncounterStatic { Species=596, Level = 36, Moves = new[]{486, 050, 228}, },	//Galvantula  
            new EncounterStatic { Species=578, Level = 32, Moves = new[]{105, 286, 271}, },	//Duosion     
            new EncounterStatic { Species=622, Level = 10, Moves = new[]{205, 007, 009}, },	//Golett 
            // Rugged Mountain
            new EncounterStatic { Species=631, Level = 10, Moves = new[]{510, 257, 202}, },	//Heatmor     
            new EncounterStatic { Species=632, Level = 10, Moves = new[]{210, 203, 422}, },	//Durant      
            new EncounterStatic { Species=556, Level = 10, Moves = new[]{042, 073, 191}, },	//Maractus    
            new EncounterStatic { Species=558, Level = 34, Moves = new[]{157, 068, 400}, },	//Crustle     
            new EncounterStatic { Species=553, Level = 40, Moves = new[]{242, 068, 212}, },	//Krookodile  
            // Icy Cave
            new EncounterStatic { Species=529, Level = 10, Moves = new[]{229, 319, 431}, },	//Drilbur     
            new EncounterStatic { Species=621, Level = 10, Moves = new[]{044, 424, 389}, },	//Druddigon   
            new EncounterStatic { Species=525, Level = 25, Moves = new[]{479, 174, 484}, },	//Boldore     
            new EncounterStatic { Species=583, Level = 35, Moves = new[]{429, 420, 286}, },	//Vanillish   
            new EncounterStatic { Species=600, Level = 38, Moves = new[]{451, 356, 393}, },	//Klang       
            new EncounterStatic { Species=610, Level = 10, Moves = new[]{082, 068, 400}, },	//Axew        
            // Dream Park
            new EncounterStatic { Species=531, Level = 10, Moves = new[]{270, 227, 281}, },	//Audino      
            new EncounterStatic { Species=538, Level = 10, Moves = new[]{020, 008, 276}, },	//Throh       
            new EncounterStatic { Species=539, Level = 10, Moves = new[]{249, 009, 530}, },	//Sawk        
            new EncounterStatic { Species=559, Level = 10, Moves = new[]{067, 252, 409}, },	//Scraggy     
            new EncounterStatic { Species=533, Level = 25, Moves = new[]{067, 183, 409}, },	//Gurdurr   
            // PGL
            new EncounterStatic { Species=575, Level = 32, Moves = new[]{243}, Gender = 0, }, //Gothorita
            new EncounterStatic { Species=025, Level = 10, Moves = new[]{029}, Gender = 0, }, //Pikachu
            new EncounterStatic { Species=511, Level = 10, Moves = new[]{437}, Gender = 0, }, //Pansage
            new EncounterStatic { Species=513, Level = 10, Moves = new[]{257}, Gender = 0, }, //Pansear
            new EncounterStatic { Species=515, Level = 10, Moves = new[]{056}, Gender = 0, }, //Panpour
            new EncounterStatic { Species=387, Level = 10, Moves = new[]{254}, Gender = 0, }, //Turtwig
            new EncounterStatic { Species=390, Level = 10, Moves = new[]{252}, Gender = 0, }, //Chimchar
            new EncounterStatic { Species=393, Level = 10, Moves = new[]{297}, Gender = 0, }, //Piplup
            new EncounterStatic { Species=575, Level = 32, Moves = new[]{286}, Gender = 0, }, //Gothorita  
        }).ToArray();

        static EncounterStatic[] USUMEdgeEncounters = new EncounterStatic[]
        {
            new EncounterStatic // Pikachu (Pretty Wing), should probably be a fake mystery gift as it has OT details
            {
                Gift = true, Species = 25, Level = 21, Location = 40005, Form = 7, HeldItem = 571, Ability = 1,
                Fateful = true, RibbonWishing = true, Relearn = new[] {85, 98, 87, 231}, Nature = Nature.Hardy,
            },
        };

        private static EncounterStatic[] MarkEncountersGeneration(EncounterStatic[] t, int Generation)
        {
            foreach (EncounterStatic s in t)
            {
                s.Generation = Generation;
            }
            return t;
        }

        private static EncounterStatic[] MarkG5DreamWorld(EncounterStatic[] t)
        {
            foreach (EncounterStatic s in t)
            {
                s.Location = 75;  //Entree Forest
                s.Ability = PersonalTable.B2W2.GetAbilities(s.Species, s.Form)[2] == 0 ? 1 : 4; // Check if has HA
                s.Shiny = Shiny.Never;
            }

            // Split encounters with multiple permitted special moves -- a pkm can only be obtained with 1 of the special moves!
            var list = new List<EncounterStatic>();
            foreach (EncounterStatic s in t)
            {
                if (s.Moves == null || s.Moves.Length <= 1) // no special moves
                {
                    list.Add(s);
                    continue;
                }

                var loc = s.Location;
                for (int i = 0; i < s.Moves.Length; i++)
                {
                    var clone = Cloner(s, loc);
                    clone.Moves = new[] { s.Moves[i] };
                    list.Add(clone);
                }
            }
            t = list.ToArray();
            return t;
        }
        internal static EncounterStatic Cloner(EncounterStatic s, int location)
        {
            var result = CloneObject(s);
            result.Location = location;
            return result;
        }
        internal static EncounterStatic CloneObject(EncounterStatic s)
        {
            if (s == null) return null;
            System.Reflection.MethodInfo inst = s.GetType().GetMethod("MemberwiseClone",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (inst != null)
                return (EncounterStatic)inst.Invoke(s, null);
            else
                return null;
        }
        internal static EncounterStatic[] GetStaticEncounters(IEnumerable<EncounterStatic> source, GameVersion game)
        {
            return source.Where(s => s.Version.Contains(game)).ToArray();
        }
    }
}
