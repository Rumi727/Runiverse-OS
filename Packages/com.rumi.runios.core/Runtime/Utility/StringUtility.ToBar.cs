#nullable enable
namespace RuniOS.Utility
{
    public static partial class StringUtility
    {
        static readonly ToBarSetting defaultToBarSetting = new ToBarSetting();

        /// <summary>
        /// (value = 5, max = 10, length = 10) = "■■■■■□□□□□"
        /// </summary>
        public static string ToBar(this int value, int max, int length, ToBarSetting? setting) => ToBar((double)value, max, length, setting);

        /// <summary>
        /// (value = 5.5, max = 10, length = 10) = "■■■■■▣□□□□"
        /// </summary>
        public static string ToBar(this float value, float max, int length, ToBarSetting? setting) => ToBar((double)value, max, length, setting);

        /// <summary>
        /// (value = 5.5, max = 10, length = 10) = "■■■■■▣□□□□"
        /// </summary>
        public static string ToBar(this double value, double max, int length, ToBarSetting? setting)
        {
            string text = "";
            setting ??= defaultToBarSetting;

            for (double i = 0.5; i < length + 0.5; i++)
            {
                if (value / max >= i / length)
                    text += setting.Value.fill;
                else
                {
                    if (value / max >= (i - 0.5) / length)
                        text += setting.Value.half;
                    else
                        text += setting.Value.empty;
                }
            }
            return text;
        }

        public struct ToBarSetting(string fill = "■", string half = "▣", string empty = "□")
        {
            public string fill { get; set; } = fill;
            public string half { get; set; } = half;
            public string empty { get; set; } = empty;
        }
    }
}