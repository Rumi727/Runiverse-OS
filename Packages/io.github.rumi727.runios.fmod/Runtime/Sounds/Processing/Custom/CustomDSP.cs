#nullable enable
using FMOD;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace RuniOS.Sounds.Processing.Custom
{
    /// <summary>
    /// Base class for FMOD DSPs implemented by managed overrides.<br/>
    /// 관리형 override로 구현하는 FMOD DSP의 기반 클래스입니다.
    /// </summary>
    /// <remarks>
    /// Callback methods can run on user or mixer threads. Audio buffer spans remain valid only for current callback.<br/>
    /// 콜백 메서드는 user 또는 mixer thread에서 실행할 수 있습니다. 오디오 버퍼 Span은 현재 콜백 동안에만 유효합니다.
    /// </remarks>
    [Obsolete("CustomDSP has not been tested and is quite complex!")]
    public abstract class CustomDSP : DSP
    {
        internal static readonly DSP_CREATE_CALLBACK createCallback = OnNativeCreate;
        internal static readonly DSP_RELEASE_CALLBACK releaseCallback = OnNativeRelease;
        internal static readonly DSP_RESET_CALLBACK resetCallback = OnNativeReset;
        internal static readonly DSP_SETPOSITION_CALLBACK setPositionCallback = OnNativeSetPosition;
        internal static readonly DSP_READ_CALLBACK readCallback = OnNativeRead;
        internal static readonly DSP_PROCESS_CALLBACK processCallback = OnNativeProcess;
        internal static readonly DSP_SHOULDIPROCESS_CALLBACK shouldProcessCallback = OnNativeShouldProcess;
        internal static readonly DSP_SETPARAM_FLOAT_CALLBACK setFloatParameterCallback = OnNativeSetFloatParameter;
        internal static readonly DSP_SETPARAM_INT_CALLBACK setIntParameterCallback = OnNativeSetIntParameter;
        internal static readonly DSP_SETPARAM_BOOL_CALLBACK setBoolParameterCallback = OnNativeSetBoolParameter;
        internal static readonly DSP_GETPARAM_FLOAT_CALLBACK getFloatParameterCallback = OnNativeGetFloatParameter;
        internal static readonly DSP_GETPARAM_INT_CALLBACK getIntParameterCallback = OnNativeGetIntParameter;
        internal static readonly DSP_GETPARAM_BOOL_CALLBACK getBoolParameterCallback = OnNativeGetBoolParameter;

        [ThreadStatic] static CustomDSP? currentCallbackDSP;
        [ThreadStatic] static DSP_STATE currentCallbackState;

        GCHandle callbackHandle;
        CustomDSPParameterStorage? parameterStorage;

        /// <summary>
        /// Initializes a custom DSP before a <see cref="SoundSystem"/> creates its native state.<br/>
        /// <see cref="SoundSystem"/>이 네이티브 상태를 만들기 전 Custom DSP를 초기화합니다.
        /// </summary>
        protected CustomDSP() { }

        internal sealed override DSP_TYPE type => DSP_TYPE.UNKNOWN;

        /// <summary>
        /// Gets current FMOD system sample rate during a callback.<br/>
        /// 콜백 중 현재 FMOD 시스템 sample rate를 가져옵니다.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when accessed outside a Custom DSP callback.<br/>
        /// Custom DSP 콜백 밖에서 접근한 경우 발생합니다.
        /// </exception>
        protected int sampleRate
        {
            get
            {
                DSP_STATE state = GetCallbackState();
                int result = 0;
                state.functions.getsamplerate(ref state, ref result).ThrowIfNotOk();
                return result;
            }
        }

        /// <summary>
        /// Gets maximum mixer block size during a callback.<br/>
        /// 콜백 중 최대 mixer block 크기를 가져옵니다.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when accessed outside a Custom DSP callback.<br/>
        /// Custom DSP 콜백 밖에서 접근한 경우 발생합니다.
        /// </exception>
        protected uint blockSize
        {
            get
            {
                DSP_STATE state = GetCallbackState();
                uint result = 0;
                state.functions.getblocksize(ref state, ref result).ThrowIfNotOk();
                return result;
            }
        }

        /// <summary>
        /// Gets mixer and output speaker modes during a callback.<br/>
        /// 콜백 중 mixer와 output speaker mode를 가져옵니다.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when accessed outside a Custom DSP callback.<br/>
        /// Custom DSP 콜백 밖에서 접근한 경우 발생합니다.
        /// </exception>
        protected (SPEAKERMODE mixer, SPEAKERMODE output) speakerModes
        {
            get
            {
                DSP_STATE state = GetCallbackState();
                int mixer = 0;
                int output = 0;
                state.functions.getspeakermode(ref state, ref mixer, ref output).ThrowIfNotOk();
                return ((SPEAKERMODE)mixer, (SPEAKERMODE)output);
            }
        }

        /// <summary>
        /// Gets current DSP clock and valid signal range during a callback.<br/>
        /// 콜백 중 현재 DSP clock과 유효한 signal 범위를 가져옵니다.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when accessed outside a Custom DSP callback.<br/>
        /// Custom DSP 콜백 밖에서 접근한 경우 발생합니다.
        /// </exception>
        protected (ulong clock, uint offset, uint length) dspClock
        {
            get
            {
                DSP_STATE state = GetCallbackState();
                state.functions.getclock(ref state, out ulong clock, out uint offset, out uint length).ThrowIfNotOk();
                return (clock, offset, length);
            }
        }

        /// <summary>
        /// Copies current listener attributes into <paramref name="destination"/> during a callback.<br/>
        /// 콜백 중 현재 listener attribute를 <paramref name="destination"/>으로 복사합니다.
        /// </summary>
        /// <param name="destination">
        /// Destination span with capacity for <see cref="CONSTANTS.MAX_LISTENERS"/> listeners.<br/>
        /// <see cref="CONSTANTS.MAX_LISTENERS"/>개의 listener를 담을 수 있는 destination Span입니다.
        /// </param>
        /// <returns>
        /// Number of copied listener attributes.<br/>
        /// 복사한 listener attribute 수를 반환합니다.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="destination"/> cannot hold every FMOD listener.<br/>
        /// <paramref name="destination"/>이 모든 FMOD listener를 담을 수 없는 경우 발생합니다.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when called outside a Custom DSP callback.<br/>
        /// Custom DSP 콜백 밖에서 호출한 경우 발생합니다.
        /// </exception>
        protected unsafe int CopyListenerAttributes(Span<ATTRIBUTES_3D> destination)
        {
            if (destination.Length < CONSTANTS.MAX_LISTENERS)
                throw new ArgumentException($"The destination span must contain at least {CONSTANTS.MAX_LISTENERS} elements.", nameof(destination));

            DSP_STATE state = GetCallbackState();
            int count = 0;
            fixed (ATTRIBUTES_3D* pointer = destination)
                state.functions.getlistenerattributes(ref state, ref count, (IntPtr)pointer).ThrowIfNotOk();

            return count;
        }

        /// <summary>
        /// Runs when FMOD creates native DSP state.<br/>
        /// FMOD가 네이티브 DSP 상태를 만들 때 실행합니다.
        /// </summary>
        protected virtual void OnCreate() { }

        /// <summary>
        /// Runs immediately before FMOD frees native DSP state.<br/>
        /// FMOD가 네이티브 DSP 상태를 해제하기 직전에 실행합니다.
        /// </summary>
        protected virtual void OnRelease() { }

        /// <summary>
        /// Runs when FMOD resets DSP state.<br/>
        /// FMOD가 DSP 상태를 초기화할 때 실행합니다.
        /// </summary>
        protected virtual void OnReset() { }

        /// <summary>
        /// Runs when FMOD changes this DSP playback position.<br/>
        /// FMOD가 이 DSP의 재생 위치를 변경할 때 실행합니다.
        /// </summary>
        /// <param name="position">
        /// New position in PCM frames.<br/>
        /// PCM frame 단위의 새 위치입니다.
        /// </param>
        protected virtual void OnSetPosition(uint position) { }

        /// <summary>
        /// Declares the FMOD parameters supported by this DSP.<br/>
        /// 이 DSP가 지원하는 FMOD parameter를 선언합니다.
        /// </summary>
        /// <param name="parameters">
        /// Builder used only while the DSP native description is being created.<br/>
        /// DSP native description 생성 중에만 사용하는 builder입니다.
        /// </param>
        /// <remarks>
        /// FMOD invokes parameter getters and setters on the user thread. Derived DSPs must synchronize parameter state shared with audio callbacks.<br/>
        /// FMOD는 parameter getter와 setter를 user thread에서 호출합니다. 파생 DSP는 audio callback과 공유하는 parameter 상태를 동기화해야 합니다.
        /// </remarks>
        protected virtual void OnConfigureParameters(CustomDSPParameterBuilder parameters) { }

        /// <summary>
        /// Runs when FMOD sets a floating-point parameter.<br/>
        /// FMOD가 부동 소수점 parameter를 설정할 때 실행합니다.
        /// </summary>
        /// <param name="index">
        /// Index returned from <see cref="CustomDSPParameterBuilder.AddFloat"/>.<br/>
        /// <see cref="CustomDSPParameterBuilder.AddFloat"/>가 반환한 index입니다.
        /// </param>
        /// <param name="value">
        /// Value requested by FMOD.<br/>
        /// FMOD가 요청한 값입니다.
        /// </param>
        protected virtual void OnSetFloatParameter(int index, float value) => throw UnsupportedParameter(index);

        /// <summary>
        /// Gets a floating-point parameter value for FMOD.<br/>
        /// FMOD에 반환할 부동 소수점 parameter 값을 가져옵니다.
        /// </summary>
        /// <param name="index">
        /// Index returned from <see cref="CustomDSPParameterBuilder.AddFloat"/>.<br/>
        /// <see cref="CustomDSPParameterBuilder.AddFloat"/>가 반환한 index입니다.
        /// </param>
        /// <returns>
        /// Current value of the parameter.<br/>
        /// 현재 parameter 값을 반환합니다.
        /// </returns>
        protected virtual float OnGetFloatParameter(int index) => throw UnsupportedParameter(index);

        /// <summary>
        /// Runs when FMOD sets an integer parameter.<br/>
        /// FMOD가 정수 parameter를 설정할 때 실행합니다.
        /// </summary>
        /// <param name="index">
        /// Index returned from <see cref="CustomDSPParameterBuilder.AddInt"/>.<br/>
        /// <see cref="CustomDSPParameterBuilder.AddInt"/>가 반환한 index입니다.
        /// </param>
        /// <param name="value">
        /// Value requested by FMOD.<br/>
        /// FMOD가 요청한 값입니다.
        /// </param>
        protected virtual void OnSetIntParameter(int index, int value) => throw UnsupportedParameter(index);

        /// <summary>
        /// Gets an integer parameter value for FMOD.<br/>
        /// FMOD에 반환할 정수 parameter 값을 가져옵니다.
        /// </summary>
        /// <param name="index">
        /// Index returned from <see cref="CustomDSPParameterBuilder.AddInt"/>.<br/>
        /// <see cref="CustomDSPParameterBuilder.AddInt"/>가 반환한 index입니다.
        /// </param>
        /// <returns>
        /// Current value of the parameter.<br/>
        /// 현재 parameter 값을 반환합니다.
        /// </returns>
        protected virtual int OnGetIntParameter(int index) => throw UnsupportedParameter(index);

        /// <summary>
        /// Runs when FMOD sets a boolean parameter.<br/>
        /// FMOD가 Boolean parameter를 설정할 때 실행합니다.
        /// </summary>
        /// <param name="index">
        /// Index returned from <see cref="CustomDSPParameterBuilder.AddBool"/>.<br/>
        /// <see cref="CustomDSPParameterBuilder.AddBool"/>가 반환한 index입니다.
        /// </param>
        /// <param name="value">
        /// Value requested by FMOD.<br/>
        /// FMOD가 요청한 값입니다.
        /// </param>
        protected virtual void OnSetBoolParameter(int index, bool value) => throw UnsupportedParameter(index);

        /// <summary>
        /// Gets a boolean parameter value for FMOD.<br/>
        /// FMOD에 반환할 Boolean parameter 값을 가져옵니다.
        /// </summary>
        /// <param name="index">
        /// Index returned from <see cref="CustomDSPParameterBuilder.AddBool"/>.<br/>
        /// <see cref="CustomDSPParameterBuilder.AddBool"/>가 반환한 index입니다.
        /// </param>
        /// <returns>
        /// Current value of the parameter.<br/>
        /// 현재 parameter 값을 반환합니다.
        /// </returns>
        protected virtual bool OnGetBoolParameter(int index) => throw UnsupportedParameter(index);

        static InvalidOperationException UnsupportedParameter(int index) => new($"The Custom DSP does not implement parameter index {index}.");

        internal int parameterCount => parameterStorage?.count ?? 0;
        internal IntPtr parameterDescriptions => parameterStorage?.pointerArray ?? IntPtr.Zero;

        internal void PrepareParameters()
        {
            if (parameterStorage != null)
                throw new InvalidOperationException("The Custom DSP parameters have already been prepared.");

            CustomDSPParameterBuilder builder = new();
            try
            {
                OnConfigureParameters(builder);
                parameterStorage = builder.BuildStorage();
            }
            finally
            {
                builder.Seal();
            }
        }

        internal IntPtr AllocateCallbackHandle()
        {
            if (callbackHandle.IsAllocated)
                throw new InvalidOperationException("The Custom DSP callback handle has already been allocated.");

            callbackHandle = GCHandle.Alloc(this);
            return GCHandle.ToIntPtr(callbackHandle);
        }

        internal void AbortNativeCreation()
        {
            ReleaseParameterStorage();
            FreeCallbackHandle();
        }

        protected override void OnNativeReleaseAccepted()
        {
            ReleaseParameterStorage();
            FreeCallbackHandle();
        }

        void ReleaseParameterStorage()
        {
            parameterStorage?.Dispose();
            parameterStorage = null;
        }

        void FreeCallbackHandle()
        {
            if (callbackHandle.IsAllocated)
                callbackHandle.Free();
        }

        DSP_STATE GetCallbackState()
        {
            if (!ReferenceEquals(currentCallbackDSP, this))
                throw new InvalidOperationException("The Custom DSP callback state is only available during this DSP callback.");

            return currentCallbackState;
        }

        [AOT.MonoPInvokeCallback(typeof(DSP_CREATE_CALLBACK))]
        static RESULT OnNativeCreate(ref DSP_STATE dspState)
        {
            if (!TryGetDescriptionInstance(ref dspState, out CustomDSP? dsp))
                return RESULT.ERR_INTERNAL;

            dspState.plugindata = GCHandle.ToIntPtr(dsp.callbackHandle);
            using CallbackStateScope scope = new(dsp, dspState);
            return Invoke(dsp.OnCreate);
        }

        [AOT.MonoPInvokeCallback(typeof(DSP_RELEASE_CALLBACK))]
        static RESULT OnNativeRelease(ref DSP_STATE dspState)
        {
            if (!TryGetInstance(ref dspState, out CustomDSP? dsp))
                return RESULT.ERR_INTERNAL;

            using CallbackStateScope scope = new(dsp, dspState);
            return Invoke(dsp.OnRelease);
        }

        [AOT.MonoPInvokeCallback(typeof(DSP_RESET_CALLBACK))]
        static RESULT OnNativeReset(ref DSP_STATE dspState)
        {
            if (!TryGetInstance(ref dspState, out CustomDSP? dsp))
                return RESULT.ERR_INTERNAL;

            using CallbackStateScope scope = new(dsp, dspState);
            return Invoke(dsp.OnReset);
        }

        [AOT.MonoPInvokeCallback(typeof(DSP_SETPOSITION_CALLBACK))]
        static RESULT OnNativeSetPosition(ref DSP_STATE dspState, uint position)
        {
            if (!TryGetInstance(ref dspState, out CustomDSP? dsp))
                return RESULT.ERR_INTERNAL;

            using CallbackStateScope scope = new(dsp, dspState);
            return Invoke(() => dsp.OnSetPosition(position));
        }

        [AOT.MonoPInvokeCallback(typeof(DSP_SHOULDIPROCESS_CALLBACK))]
        static RESULT OnNativeShouldProcess(ref DSP_STATE dspState, bool inputIdle, uint frameCount, CHANNELMASK inputMask, int inputChannels, SPEAKERMODE speakerMode)
        {
            if (!TryGetInstance(ref dspState, out CustomDSP? dsp) || dsp is not CustomReadDSP readDSP)
                return RESULT.ERR_INTERNAL;

            using CallbackStateScope scope = new(dsp, dspState);
            return Invoke(() => readDSP.ShouldProcess(inputIdle, frameCount, inputMask, inputChannels, speakerMode));
        }

        [AOT.MonoPInvokeCallback(typeof(DSP_READ_CALLBACK))]
        static unsafe RESULT OnNativeRead(ref DSP_STATE dspState, IntPtr input, IntPtr output, uint frameCount, int inputChannels, ref int outputChannels)
        {
            if (!TryGetInstance(ref dspState, out CustomDSP? dsp) || dsp is not CustomReadDSP readDSP)
            {
                Clear(output, frameCount, Math.Max(inputChannels, 0));
                return RESULT.ERR_INTERNAL;
            }

            outputChannels = inputChannels;
            using CallbackStateScope scope = new(dsp, dspState);
            try
            {
                int sampleCount = checked((int)(frameCount * (uint)inputChannels));
                readDSP.Read(new ReadOnlySpan<float>(input.ToPointer(), sampleCount), new Span<float>(output.ToPointer(), sampleCount), frameCount, inputChannels);
                return RESULT.OK;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Clear(output, frameCount, Math.Max(inputChannels, 0));
                return RESULT.ERR_INTERNAL;
            }
        }

        [AOT.MonoPInvokeCallback(typeof(DSP_PROCESS_CALLBACK))]
        static RESULT OnNativeProcess(ref DSP_STATE dspState, uint frameCount, ref DSP_BUFFER_ARRAY inputs, ref DSP_BUFFER_ARRAY outputs, bool inputIdle, DSP_PROCESS_OPERATION operation)
        {
            if (!TryGetInstance(ref dspState, out CustomDSP? dsp))
            {
                Clear(outputs, frameCount);
                return RESULT.ERR_INTERNAL;
            }

            using CallbackStateScope scope = new(dsp, dspState);
            try
            {
                return dsp switch
                {
                    CustomGeneratorDSP generator => ProcessGenerator(generator, ref outputs, frameCount, operation),
                    CustomProcessDSP processor => ProcessInputs(processor, ref inputs, ref outputs, frameCount, inputIdle, operation),
                    _ => ProcessUnsupported(ref outputs, frameCount),
                };
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Clear(outputs, frameCount);
                return RESULT.ERR_INTERNAL;
            }
        }

        [AOT.MonoPInvokeCallback(typeof(DSP_SETPARAM_FLOAT_CALLBACK))]
        static RESULT OnNativeSetFloatParameter(ref DSP_STATE dspState, int index, float value)
        {
            if (!TryGetInstance(ref dspState, out CustomDSP? dsp))
                return RESULT.ERR_INTERNAL;

            using CallbackStateScope scope = new(dsp, dspState);
            return Invoke(() => dsp.OnSetFloatParameter(index, value));
        }

        [AOT.MonoPInvokeCallback(typeof(DSP_SETPARAM_INT_CALLBACK))]
        static RESULT OnNativeSetIntParameter(ref DSP_STATE dspState, int index, int value)
        {
            if (!TryGetInstance(ref dspState, out CustomDSP? dsp))
                return RESULT.ERR_INTERNAL;

            using CallbackStateScope scope = new(dsp, dspState);
            return Invoke(() => dsp.OnSetIntParameter(index, value));
        }

        [AOT.MonoPInvokeCallback(typeof(DSP_SETPARAM_BOOL_CALLBACK))]
        static RESULT OnNativeSetBoolParameter(ref DSP_STATE dspState, int index, bool value)
        {
            if (!TryGetInstance(ref dspState, out CustomDSP? dsp))
                return RESULT.ERR_INTERNAL;

            using CallbackStateScope scope = new(dsp, dspState);
            return Invoke(() => dsp.OnSetBoolParameter(index, value));
        }

        [AOT.MonoPInvokeCallback(typeof(DSP_GETPARAM_FLOAT_CALLBACK))]
        static RESULT OnNativeGetFloatParameter(ref DSP_STATE dspState, int index, ref float value, IntPtr valueString)
        {
            if (!TryGetInstance(ref dspState, out CustomDSP? dsp))
                return RESULT.ERR_INTERNAL;

            using CallbackStateScope scope = new(dsp, dspState);
            RESULT result = Invoke(() => dsp.OnGetFloatParameter(index), out float resultValue);
            if (result == RESULT.OK)
                value = resultValue;
            return result;
        }

        [AOT.MonoPInvokeCallback(typeof(DSP_GETPARAM_INT_CALLBACK))]
        static RESULT OnNativeGetIntParameter(ref DSP_STATE dspState, int index, ref int value, IntPtr valueString)
        {
            if (!TryGetInstance(ref dspState, out CustomDSP? dsp))
                return RESULT.ERR_INTERNAL;

            using CallbackStateScope scope = new(dsp, dspState);
            RESULT result = Invoke(() => dsp.OnGetIntParameter(index), out int resultValue);
            if (result == RESULT.OK)
                value = resultValue;
            return result;
        }

        [AOT.MonoPInvokeCallback(typeof(DSP_GETPARAM_BOOL_CALLBACK))]
        static RESULT OnNativeGetBoolParameter(ref DSP_STATE dspState, int index, ref bool value, IntPtr valueString)
        {
            if (!TryGetInstance(ref dspState, out CustomDSP? dsp))
                return RESULT.ERR_INTERNAL;

            using CallbackStateScope scope = new(dsp, dspState);
            RESULT result = Invoke(() => dsp.OnGetBoolParameter(index), out bool resultValue);
            if (result == RESULT.OK)
                value = resultValue;
            return result;
        }

        static unsafe RESULT ProcessGenerator(CustomGeneratorDSP dsp, ref DSP_BUFFER_ARRAY outputs, uint frameCount, DSP_PROCESS_OPERATION operation)
        {
            if (operation == DSP_PROCESS_OPERATION.PROCESS_QUERY)
                return outputs.numbuffers == 1 ? RESULT.OK : RESULT.ERR_DSP_FORMAT;

            int channelCount = outputs.numchannels;
            if (channelCount <= 0 || outputs.buffer == IntPtr.Zero)
                return RESULT.ERR_DSP_FORMAT;

            int sampleCount = checked((int)(frameCount * (uint)channelCount));
            dsp.Generate(new Span<float>(outputs.buffer.ToPointer(), sampleCount), frameCount, channelCount);
            return RESULT.OK;
        }

        static unsafe RESULT ProcessInputs(CustomProcessDSP dsp, ref DSP_BUFFER_ARRAY inputs, ref DSP_BUFFER_ARRAY outputs, uint frameCount, bool inputIdle, DSP_PROCESS_OPERATION operation)
        {
            if (inputs.numbuffers < 2 || outputs.numbuffers != 1)
                return RESULT.ERR_DSP_FORMAT;

            int channelCount = GetChannelCount(inputs, 0);
            if (channelCount <= 0 || !HaveMatchingFormats(inputs, channelCount))
                return RESULT.ERR_DSP_FORMAT;

            if (operation == DSP_PROCESS_OPERATION.PROCESS_QUERY)
            {
                outputs.numchannels = channelCount;
                CopyChannelMask(inputs, ref outputs);
                outputs.speakermode = inputs.speakermode;
                return RESULT.OK;
            }

            if (outputs.buffer == IntPtr.Zero)
                return RESULT.ERR_DSP_FORMAT;

            int sampleCount = checked((int)(frameCount * (uint)channelCount));
            dsp.Process(new CustomDSPInputBuffers(inputs, frameCount), new Span<float>(outputs.buffer.ToPointer(), sampleCount), frameCount, channelCount, inputIdle);
            return RESULT.OK;
        }

        static RESULT ProcessUnsupported(ref DSP_BUFFER_ARRAY outputs, uint frameCount)
        {
            Clear(outputs, frameCount);
            return RESULT.ERR_INTERNAL;
        }

        static bool HaveMatchingFormats(DSP_BUFFER_ARRAY buffers, int channelCount)
        {
            int channelMask = GetChannelMask(buffers, 0);
            for (int index = 1; index < buffers.numbuffers; index++)
            {
                if (GetChannelCount(buffers, index) != channelCount || GetChannelMask(buffers, index) != channelMask)
                    return false;
            }

            return true;
        }

        static int GetChannelCount(DSP_BUFFER_ARRAY buffers, int index) => buffers.buffernumchannels == IntPtr.Zero ? 0 : Marshal.ReadInt32(buffers.buffernumchannels, sizeof(int) * index);

        static int GetChannelMask(DSP_BUFFER_ARRAY buffers, int index) => buffers.bufferchannelmask == IntPtr.Zero ? 0 : Marshal.ReadInt32(buffers.bufferchannelmask, sizeof(int) * index);

        static void CopyChannelMask(DSP_BUFFER_ARRAY input, ref DSP_BUFFER_ARRAY output)
        {
            if (input.bufferchannelmask == IntPtr.Zero || output.bufferchannelmask == IntPtr.Zero)
                return;

            Marshal.WriteInt32(output.bufferchannelmask, GetChannelMask(input, 0));
        }

        static RESULT Invoke(Action action)
        {
            try
            {
                action.Invoke();
                return RESULT.OK;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return RESULT.ERR_INTERNAL;
            }
        }

        static RESULT Invoke(Func<RESULT> func)
        {
            try
            {
                return func.Invoke();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return RESULT.ERR_INTERNAL;
            }
        }

        static RESULT Invoke<T>(Func<T> func, out T value)
        {
            try
            {
                value = func.Invoke();
                return RESULT.OK;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                value = default!;
                return RESULT.ERR_INTERNAL;
            }
        }

        static bool TryGetDescriptionInstance(ref DSP_STATE dspState, [NotNullWhen(true)] out CustomDSP? dsp)
        {
            DSP_GETUSERDATA_FUNC? getUserData = dspState.functions.getuserdata;
            if (getUserData == null || getUserData(ref dspState, out IntPtr userData) != RESULT.OK)
            {
                dsp = null;
                return false;
            }

            return TryGetInstance(userData, out dsp);
        }

        static bool TryGetInstance(ref DSP_STATE dspState, [NotNullWhen(true)] out CustomDSP? dsp) => TryGetInstance(dspState.plugindata, out dsp);

        static bool TryGetInstance(IntPtr handle, [NotNullWhen(true)] out CustomDSP? dsp)
        {
            if (handle == IntPtr.Zero)
            {
                dsp = null;
                return false;
            }

            try
            {
                dsp = GCHandle.FromIntPtr(handle).Target as CustomDSP;
                return dsp != null;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                dsp = null;
                return false;
            }
        }

        static unsafe void Clear(IntPtr buffer, uint frameCount, int channelCount)
        {
            if (buffer == IntPtr.Zero || channelCount <= 0)
                return;

            new Span<float>(buffer.ToPointer(), (frameCount * channelCount).ClampToInt()).Clear();
        }

        static void Clear(DSP_BUFFER_ARRAY buffers, uint frameCount)
        {
            for (int index = 0; index < buffers.numbuffers; index++)
            {
                int channelCount = GetChannelCount(buffers, index);
                IntPtr buffer = buffers.buffers == IntPtr.Zero ? IntPtr.Zero : Marshal.ReadIntPtr(buffers.buffers, IntPtr.Size * index);
                Clear(buffer, frameCount, channelCount);
            }
        }

        readonly struct CallbackStateScope : IDisposable
        {
            readonly CustomDSP? previousDSP;
            readonly DSP_STATE previousState;

            public CallbackStateScope(CustomDSP dsp, DSP_STATE state)
            {
                previousDSP = currentCallbackDSP;
                previousState = currentCallbackState;
                currentCallbackDSP = dsp;
                currentCallbackState = state;
            }

            public void Dispose()
            {
                currentCallbackDSP = previousDSP;
                currentCallbackState = previousState;
            }
        }
    }

    /// <summary>
    /// Provides input buffers for one <see cref="CustomProcessDSP.Process"/> callback.<br/>
    /// 한 <see cref="CustomProcessDSP.Process"/> 콜백의 입력 버퍼를 제공합니다.
    /// </summary>
    /// <remarks>
    /// All spans expire when current DSP callback returns.<br/>
    /// 모든 Span은 현재 DSP 콜백이 반환되면 만료됩니다.
    /// </remarks>
    public readonly ref struct CustomDSPInputBuffers
    {
        readonly DSP_BUFFER_ARRAY buffers;
        readonly uint frameCount;

        internal CustomDSPInputBuffers(DSP_BUFFER_ARRAY buffers, uint frameCount)
        {
            this.buffers = buffers;
            this.frameCount = frameCount;
        }

        /// <summary>
        /// Gets input buffer count.<br/>
        /// 입력 버퍼 개수를 가져옵니다.
        /// </summary>
        public int count => buffers.numbuffers;

        /// <summary>
        /// Gets samples of input buffer at <paramref name="index"/>.<br/>
        /// <paramref name="index"/> 위치의 입력 버퍼 샘플을 가져옵니다.
        /// </summary>
        public ReadOnlySpan<float> this[int index]
        {
            get
            {
                if ((uint)index >= (uint)buffers.numbuffers || buffers.buffers == IntPtr.Zero || buffers.buffernumchannels == IntPtr.Zero)
                    throw new ArgumentOutOfRangeException(nameof(index));

                IntPtr buffer = Marshal.ReadIntPtr(buffers.buffers, IntPtr.Size * index);
                int channelCount = Marshal.ReadInt32(buffers.buffernumchannels, sizeof(int) * index);

                unsafe
                {
                    return new ReadOnlySpan<float>(buffer.ToPointer(), (frameCount * channelCount).ClampToInt());
                }
            }
        }
    }
}
