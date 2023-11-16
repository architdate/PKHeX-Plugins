using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PKHeX.Core.AutoMod
{
    public static class GlyphLegality
    {
        private static readonly Dictionary<char, char> CharDictionary;

        static GlyphLegality()
        {
            CharDictionary = [];
            const string full =
                "アイウエオカキクケコサシスセソタチツテトナニヌネノハヒフヘホマミムメモヤユヨラリルレロワヲンッァィゥェォャュョ゙゚ー０１２３４５６７８９ＡＢＣＤＥＦＧＨＩＪＫＬＭＮＯＰＱＲＳＴＵＶＷＸＹＺａｂｃｄｅｆｇｈｉｊｋｌｍｎｏｐｑｒｓｔｕｖｗｘｙｚ～！＠＃＄％＾＆＊（）＿＋－＝｛｝［］｜＼：；＂＇＜＞，．？／";
            const string half =
                "ｱｲｳｴｵｶｷｸｹｺｻｼｽｾｿﾀﾁﾂﾃﾄﾅﾆﾇﾈﾉﾊﾋﾌﾍﾎﾏﾐﾑﾒﾓﾔﾕﾖﾗﾘﾙﾚﾛﾜｦﾝｯｧｨｩｪｫｬｭｮﾞﾟｰ0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz~!@#$%^&*()_+-={}[]|\\:;\"'<>,.?/";
            for (int i = 0; i < full.Length; i++)
                CharDictionary.Add(half[i], full[i]);
        }

        public static bool ContainsFullWidth(string val) =>
            val.Any(z => CharDictionary.ContainsValue(z));

        public static bool ContainsHalfWidth(string val) =>
            val.Any(z => CharDictionary.ContainsKey(z));

        public static string StringConvert(string val, StringConversionType type) =>
            type switch
            {
                StringConversionType.HalfWidth => val.Normalize(NormalizationForm.FormKC),
                StringConversionType.FullWidth
                    => string.Concat(
                        val.Select(c => CharDictionary.ContainsKey(c) ? CharDictionary[c] : c)
                    ),
                _ => val,
            };
    }

    public enum StringConversionType
    {
        HalfWidth,
        FullWidth,
    }
}
