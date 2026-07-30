namespace RuniOS.Sounds.Processing
{
    public partial class DSP
    {
        protected BoolParameters boolParameters { get; }

        public sealed class BoolParameters
        {
            readonly DSP dsp;

            internal BoolParameters(DSP dsp) => this.dsp = dsp;

            public bool this[int index]
            {
                get => dsp.UseNative((dsp, index) =>
                {
                    dsp.getParameterBool(index, out bool value).ThrowIfNotOk();
                    return value;
                }, index);
                set => dsp.UseNative((FMOD.DSP dsp, (int index, bool value) state) => dsp.setParameterBool(state.index, state.value).ThrowIfNotOk(), (index, value));
            }
        }
    }
}
