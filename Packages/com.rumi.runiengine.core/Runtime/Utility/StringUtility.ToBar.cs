#nullable enable
namespace RuniEngine
{
    public static partial class StringUtility
    {
        static ToBarSetting defaultToBarSetting = new ToBarSetting();

        /// <summary>
        /// (value = 5, max = 10, length = 10) = "■■■■■□□□□□"
        /// </summary>
        /// <param name="value"></param>
        /// <param name="max"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static string ToBar(this int value, int max, int length, ToBarSetting? setting) => ToBar((double)value, max, length, setting);

        /// <summary>
        /// (value = 5.5, max = 10, length = 10) = "■■■■■▣□□□□"
        /// </summary>
        /// <param name="value"></param>
        /// <param name="max"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static string ToBar(this float value, float max, int length, ToBarSetting? setting) => ToBar((double)value, max, length, setting);

        /// <summary>
        /// (value = 5.5, max = 10, length = 10) = "■■■■■▣□□□□"
        /// </summary>
        /// <param name="value"></param>
        /// <param name="max"></param>
        /// <param name="length"></param>
        /// <returns></returns>
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

        public struct ToBarSetting
        {
            public ToBarSetting(string fill = "■", string half = "▣", string empty = "□")
            {
                this.fill = fill;
                this.half = half;
                this.empty = empty;
            }

            public string fill { get; set; }
            public string half { get; set; }
            public string empty { get; set; }
        }
    }
}
