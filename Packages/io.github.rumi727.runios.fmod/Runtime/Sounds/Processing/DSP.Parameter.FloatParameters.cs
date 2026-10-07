namespace RuniOS.Sounds.Processing
{
    public partial class DSP
    {
        protected FloatParameters floatParameters { get; }

        public sealed class FloatParameters
        {
            readonly DSP dsp;

            internal FloatParameters(DSP dsp) => this.dsp = dsp;

            public float this[int index]
            {
                get => dsp.UseNative((dsp, index) =>
                {
                    dsp.getParameterFloat(index, out float value).ThrowIfNotOk();
                    return value;
                }, index);
                set => dsp.UseNative((FMOD.DSP dsp, (int index, float value) state) => dsp.setParameterFloat(state.index, state.value).ThrowIfNotOk(), (index, value));
            }
        }
    }
}
