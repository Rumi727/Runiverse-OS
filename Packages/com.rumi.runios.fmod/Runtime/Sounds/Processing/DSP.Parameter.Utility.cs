#nullable enable
using System.Runtime.InteropServices;

namespace RuniOS.Sounds.Processing
{
    public abstract partial class DSP
    {
        private protected T GetDataParameter<T>(int index) where T : struct => UseNative(dsp =>
        {
            dsp.getParameterData(index, out IntPtr data, out uint length).ThrowIfNotOk();
            if (length < (uint)Marshal.SizeOf<T>())
                throw new InvalidOperationException($"FMOD returned an invalid {typeof(T).Name} parameter.");

            return Marshal.PtrToStructure<T>(data);
        });

        private protected void SetDataParameter<T>(int index, T value) where T : struct
        {
            int length = Marshal.SizeOf<T>();
            IntPtr data = Marshal.AllocHGlobal(length);
            bool initialized = false;

            try
            {
                Marshal.StructureToPtr(value, data, false);
                initialized = true;
                byte[] bytes = new byte[length];
                Marshal.Copy(data, bytes, 0, length);
                dataParameters[index] = bytes;
            }
            finally
            {
                if (initialized)
                    Marshal.DestroyStructure<T>(data);

                Marshal.FreeHGlobal(data);
            }
        }

        protected bool GetBooleanDataParameter(int index) => UseNative(dsp =>
        {
            dsp.getParameterData(index, out IntPtr data, out uint length).ThrowIfNotOk();
            return length >= sizeof(int) && Marshal.ReadInt32(data) != 0;
        });

        protected void SetBooleanDataParameter(int index, bool value) => dataParameters[index] = BitConverter.GetBytes(value ? 1 : 0);

        protected (float linear, float additive) GetOverallGainDataParameter(int index) => UseNative(dsp =>
        {
            dsp.getParameterData(index, out IntPtr data, out uint length).ThrowIfNotOk();
            if (length < (uint)Marshal.SizeOf<FMOD.DSP_PARAMETER_OVERALLGAIN>())
                throw new InvalidOperationException("FMOD returned an invalid overall gain parameter.");

            FMOD.DSP_PARAMETER_OVERALLGAIN gain = Marshal.PtrToStructure<FMOD.DSP_PARAMETER_OVERALLGAIN>(data);
            return (gain.linear_gain, gain.linear_gain_additive);
        });

        protected float[] GetDynamicResponseDataParameter(int index) => UseNative(dsp =>
        {
            dsp.getParameterData(index, out IntPtr data, out uint length).ThrowIfNotOk();
            if (length < (uint)Marshal.SizeOf<FMOD.DSP_PARAMETER_DYNAMIC_RESPONSE>())
                throw new InvalidOperationException("FMOD returned an invalid dynamic response parameter.");

            FMOD.DSP_PARAMETER_DYNAMIC_RESPONSE response = Marshal.PtrToStructure<FMOD.DSP_PARAMETER_DYNAMIC_RESPONSE>(data);
            int count = Math.Min(Math.Max(response.numchannels, 0), response.rms.Length);
            float[] result = new float[count];
            Array.Copy(response.rms, result, count);
            return result;
        });

        protected float[][] GetFFTSpectrumDataParameter(int index) => UseNative(dsp =>
        {
            dsp.getParameterData(index, out IntPtr data, out uint length).ThrowIfNotOk();
            if (length < (uint)Marshal.SizeOf<FMOD.DSP_PARAMETER_FFT>())
                throw new InvalidOperationException("FMOD returned an invalid FFT spectrum parameter.");

            return Marshal.PtrToStructure<FMOD.DSP_PARAMETER_FFT>(data).spectrum;
        });

        private protected static DSP3DAttributes ToDSP3DAttributes(FMOD.ATTRIBUTES_3D attributes) => new(
            ToUnityVector(attributes.position),
            ToUnityVector(attributes.velocity),
            ToUnityVector(attributes.forward),
            ToUnityVector(attributes.up));

        private protected static FMOD.ATTRIBUTES_3D ToFMOD3DAttributes(DSP3DAttributes attributes) => new()
        {
            position = ToFMODVector(attributes.position),
            velocity = ToFMODVector(attributes.velocity),
            forward = ToFMODVector(attributes.forward),
            up = ToFMODVector(attributes.up)
        };

        static Vector3 ToUnityVector(FMOD.VECTOR vector) => new(vector.x, vector.y, vector.z);

        static FMOD.VECTOR ToFMODVector(Vector3 vector) => new() { x = vector.x, y = vector.y, z = vector.z };
    }
}
