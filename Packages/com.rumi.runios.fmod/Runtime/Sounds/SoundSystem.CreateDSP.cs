#nullable enable
using System.Text;
using RuniOS.Sounds.Processing;
using RuniOS.Sounds.Processing.Custom;

namespace RuniOS.Sounds
{
    public sealed partial class SoundSystem
    {
        /// <summary>
        /// Creates a DSP of type <typeparamref name="T"/>.<br/>
        /// <typeparamref name="T"/> 형식의 DSP를 생성합니다.
        /// </summary>
        /// <typeparam name="T">
        /// DSP type to create.<br/>
        /// 생성할 DSP 형식입니다.
        /// </typeparam>
        /// <returns>
        /// The initialized DSP.<br/>
        /// 초기화된 DSP를 반환합니다.
        /// </returns>
        public T CreateDSP<T>() where T : DSP => (T)CreateDSP(typeof(T));

        /// <summary>
        /// Creates the specified <paramref name="type"/>.<br/>
        /// 지정한 <paramref name="type"/>의 DSP를 생성합니다.
        /// </summary>
        /// <param name="type">
        /// Non-abstract type derived from <see cref="DSP"/>.<br/>
        /// <see cref="DSP"/>를 상속한 추상이 아닌 형식입니다.
        /// </param>
        /// <returns>
        /// The initialized DSP.<br/>
        /// 초기화된 DSP를 반환합니다.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="type"/> is <see langword="null"/>.<br/>
        /// <paramref name="type"/>이 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="type"/> is not a non-abstract <see cref="DSP"/> type.<br/>
        /// <paramref name="type"/>이 추상이 아닌 <see cref="DSP"/> 형식이 아닌 경우 발생합니다.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Thrown when this sound system has been disposed.<br/>
        /// 이 사운드 시스템이 해제된 경우 발생합니다.
        /// </exception>
        public DSP CreateDSP(Type type)
        {
            nativeLock.EnterReadLock();

            try
            {
                ThrowIfUnavailableUnsafe();

                if (type == null)
                    throw new ArgumentNullException(nameof(type));

                if (!typeof(DSP).IsAssignableFrom(type) || type.IsAbstract)
                    throw new ArgumentException($"The type must be a non-abstract {nameof(DSP)} type.", nameof(type));

                if (Activator.CreateInstance(type, nonPublic: true) is not DSP dsp)
                    throw new ArgumentException($"Unable to create DSP of type {type.Name}.", nameof(type));

                FMOD.DSP nativeDSP = CreateNativeDSP(dsp);
                dsp.Initialize(this, nativeDSP);

                return dsp;
            }
            finally
            {
                nativeLock.ExitReadLock();
            }
        }

#pragma warning disable CS0618 // 형식 또는 멤버는 사용되지 않습니다.
        FMOD.DSP CreateNativeDSP(DSP dsp)
        {
            if (dsp is not CustomDSP customDSP)
            {
                native.createDSPByType(dsp.type, out FMOD.DSP nativeDSP).ThrowIfNotOk();
                return nativeDSP;
            }

            IntPtr callbackHandle = customDSP.AllocateCallbackHandle();

            try
            {
                customDSP.PrepareParameters();

                FMOD.DSP_DESCRIPTION description = new()
                {
                    pluginsdkversion = FMOD.VERSION.number,
                    name = CreateDSPName(customDSP.GetType().Name),
                    version = 1,
                    numoutputbuffers = 1,
                    create = CustomDSP.createCallback,
                    release = CustomDSP.releaseCallback,
                    reset = CustomDSP.resetCallback,
                    setposition = CustomDSP.setPositionCallback,
                    userdata = callbackHandle,
                };

                if (customDSP.parameterCount > 0)
                {
                    description.numparameters = customDSP.parameterCount;
                    description.paramdesc = customDSP.parameterDescriptions;
                    description.setparameterfloat = CustomDSP.setFloatParameterCallback;
                    description.setparameterint = CustomDSP.setIntParameterCallback;
                    description.setparameterbool = CustomDSP.setBoolParameterCallback;
                    description.getparameterfloat = CustomDSP.getFloatParameterCallback;
                    description.getparameterint = CustomDSP.getIntParameterCallback;
                    description.getparameterbool = CustomDSP.getBoolParameterCallback;
                }

                switch (customDSP)
                {
                    case CustomGeneratorDSP:
                    {
                        description.numinputbuffers = 0;
                        description.process = CustomDSP.processCallback;
                        break;
                    }
                    case CustomReadDSP:
                    {
                        description.numinputbuffers = 1;
                        description.read = CustomDSP.readCallback;
                        description.shouldiprocess = CustomDSP.shouldProcessCallback;
                        break;
                    }
                    case CustomProcessDSP processDSP:
                    {
                        if (processDSP.numInputBuffers < 2)
                            throw new ArgumentOutOfRangeException(nameof(dsp), "CustomProcessDSP requires at least two input buffers.");

                        description.numinputbuffers = processDSP.numInputBuffers;
                        description.process = CustomDSP.processCallback;
                        break;
                    }
                    default:
                        throw new ArgumentException($"Unsupported Custom DSP type: {customDSP.GetType().FullName}", nameof(dsp));
                }

                native.createDSP(ref description, out FMOD.DSP customNativeDSP).ThrowIfNotOk();
                return customNativeDSP;
            }
            catch
            {
                customDSP.AbortNativeCreation();
                throw;
            }
        }
#pragma warning restore CS0618 // 형식 또는 멤버는 사용되지 않습니다.

        static byte[] CreateDSPName(string name)
        {
            byte[] encodedName = Encoding.UTF8.GetBytes(name);
            byte[] result = new byte[32];
            Array.Copy(encodedName, result, Math.Min(encodedName.Length, result.Length - 1));
            return result;
        }
    }
}
