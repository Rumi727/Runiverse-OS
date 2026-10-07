namespace RuniOS.Sounds.Processing
{
    public partial class DSP
    {
        protected IntParameters intParameters { get; }

        public sealed class IntParameters
        {
            readonly DSP dsp;

            internal IntParameters(DSP dsp) => this.dsp = dsp;

            public int this[int index]
            {
                get => dsp.UseNative((dsp, index) =>
                {
                    dsp.getParameterInt(index, out int value).ThrowIfNotOk();
                    return value;
                }, index);
                set => dsp.UseNative((FMOD.DSP dsp, (int index, int value) state) => dsp.setParameterInt(state.index, state.value).ThrowIfNotOk(), (index, value));
            }
        }
    }
}
