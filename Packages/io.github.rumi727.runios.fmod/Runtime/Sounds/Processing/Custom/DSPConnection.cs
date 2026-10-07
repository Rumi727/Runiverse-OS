#nullable enable
using FMOD;
using System.Threading;

namespace RuniOS.Sounds.Processing.Custom
{
    /// <summary>
    /// Owns one input connection between two FMOD DSPs.<br/>
    /// 두 FMOD DSP 사이의 입력 연결 하나를 소유합니다.
    /// </summary>
    /// <remarks>
    /// This token does not own either endpoint. Disposing it removes only its connection.<br/>
    /// Sound-system shutdown invalidates endpoint handles, after which disposal only completes managed state.<br/><br/>
    /// 이 토큰은 어느 endpoint도 소유하지 않습니다. 해제하면 자기 연결만 제거합니다.<br/>
    /// 사운드 시스템 종료는 endpoint 핸들을 무효화하며, 그 뒤 해제는 관리 상태만 완료합니다.
    /// </remarks>
    [Obsolete("CustomDSP has not been tested and is quite complex!")]
    public sealed class DSPConnection : IDisposable
    {
        internal DSPConnection(DSP input, DSP output, FMOD.DSPConnection native)
        {
            this.input = input;
            this.output = output;
            this.native = native;
        }

        readonly DSP input;
        readonly DSP output;
        FMOD.DSPConnection native;
        int _isDisposed;

        /// <summary>
        /// Gets whether this connection token has been disposed.<br/>
        /// 이 연결 토큰이 해제되었는지 여부를 가져옵니다.
        /// </summary>
        public bool isDisposed => Volatile.Read(ref _isDisposed) != 0;

        /// <summary>
        /// Removes this connection when its endpoint handles are still valid.<br/>
        /// endpoint 핸들이 아직 유효하면 이 연결을 제거합니다.
        /// </summary>
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _isDisposed, 1) != 0)
                return;

            if (input.system.isDisposed || input.isDisposed || output.isDisposed)
            {
                native.clearHandle();
                return;
            }

            try
            {
                DSP.UseNativePair(output, input, (outputNative, inputNative) =>
                {
                    RESULT result = outputNative.disconnectFrom(inputNative, native);
                    if (result != RESULT.OK && result != RESULT.ERR_DSP_NOTFOUND && result != RESULT.ERR_INVALID_HANDLE)
                        result.ThrowIfNotOk();
                });
            }
            catch (ObjectDisposedException) when (input.system.isDisposed || input.isDisposed || output.isDisposed)
            {
                // 시스템 종료 또는 endpoint 해제와 경합한 경우 native 호출 없이 관리 상태만 완료합니다.
            }
            finally
            {
                native.clearHandle();
            }
        }
    }
}
