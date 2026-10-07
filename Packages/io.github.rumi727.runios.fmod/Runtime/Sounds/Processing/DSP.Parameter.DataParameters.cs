namespace RuniOS.Sounds.Processing
{
    public partial class DSP
    {
        protected DataParameters dataParameters { get; }

        public sealed class DataParameters
        {
            readonly DSP dsp;

            internal DataParameters(DSP dsp) => this.dsp = dsp;

            public Span<byte> this[int index]
            {
                get
                {
                    dsp.nativeLock.EnterReadLock();

                    try
                    {
                        dsp.ThrowIfDisposedUnsafe();
                        dsp.native.getParameterData(index, out IntPtr pointer, out uint length).ThrowIfNotOk();

                        unsafe
                        {
                            return new Span<byte>(pointer.ToPointer(), length.ClampToInt());
                        }
                    }
                    finally
                    {
                        dsp.nativeLock.ExitReadLock();
                    }
                }
                set => dsp.UseNative((FMOD.DSP dsp, (int index, byte[] value) state) => dsp.setParameterData(state.index, state.value).ThrowIfNotOk(), (index, value.ToArray()));
            }
        }
    }
}
