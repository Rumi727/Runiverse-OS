#nullable enable
namespace RuniOS.Sounds
{
    public sealed partial class SoundSystem
    {
        /// <summary>
        /// Occurs immediately before this system releases its owned resources for a reset.<br/>
        /// 이 시스템이 Reset을 위해 소유 리소스를 해제하기 직전에 발생합니다.
        /// </summary>
        /// <remarks>
        /// The lifecycle state has not changed when handlers run. Handlers must not call <see cref="Reset(SoundSystemSettings)"/> or <see cref="Dispose()"/>.<br/>
        /// Handler exceptions are logged and do not stop the reset.
        /// <br/><br/>
        /// 처리기가 실행될 때 lifecycle 상태는 변경되지 않습니다. 처리기에서 <see cref="Reset(SoundSystemSettings)"/> 또는 <see cref="Dispose()"/>를 호출하면 안 됩니다.<br/>
        /// 처리기 예외는 로그로 출력하고 Reset을 계속합니다.
        /// </remarks>
        public event Action<SoundSystem>? onResetting
        {
            add
            {
                lock (resetEventLock)
                    _onResetting += value;
            }
            remove
            {
                lock (resetEventLock)
                    _onResetting -= value;
            }
        }
        Action<SoundSystem>? _onResetting;

        /// <summary>
        /// Occurs immediately after this system successfully completes a reset.<br/>
        /// 이 시스템이 Reset을 성공적으로 완료한 직후 발생합니다.
        /// </summary>
        /// <remarks>
        /// The system is active when handlers run. This event is not raised when native reinitialization fails.<br/>
        /// Handler exceptions are logged and do not change the completed reset state.
        /// <br/><br/>
        /// 처리기가 실행될 때 시스템은 활성 상태입니다. 네이티브 재초기화에 실패하면 이 이벤트는 발생하지 않습니다.<br/>
        /// 처리기 예외는 로그로 출력하며 완료된 Reset 상태를 변경하지 않습니다.
        /// </remarks>
        public event Action<SoundSystem>? onReset
        {
            add
            {
                lock (resetEventLock)
                    _onReset += value;
            }
            remove
            {
                lock (resetEventLock)
                    _onReset -= value;
            }
        }
        Action<SoundSystem>? _onReset;
        readonly object resetEventLock = new();
        bool invokingResettingHandlers;

        /// <summary>
        /// Releases all resources owned by this system and reinitializes its FMOD system.<br/>
        /// 이 시스템이 소유한 모든 리소스를 해제하고 FMOD 시스템을 다시 초기화합니다.
        /// </summary>
        /// <param name="settings">
        /// Settings whose non-<see langword="null"/> properties replace the currently stored initialization settings.<br/>
        /// <see langword="null"/>이 아닌 속성으로 현재 저장된 초기화 설정을 대체할 설정입니다.
        /// </param>
        /// <exception cref="ObjectDisposedException">
        /// Thrown when this sound system has been disposed.<br/>
        /// 이 사운드 시스템이 해제된 경우 발생합니다.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when called while the current thread holds the system native lock, while this lifecycle is already resetting, or from an <see cref="onResetting"/> handler.<br/>
        /// 현재 스레드가 시스템 네이티브 잠금을 보유했거나, 이 lifecycle이 이미 Reset 중이거나, <see cref="onResetting"/> 처리기에서 호출한 경우 발생합니다.
        /// </exception>
        /// <exception cref="FMODException">
        /// Thrown when FMOD cannot close, configure, or initialize the native system.<br/>
        /// FMOD가 네이티브 시스템을 닫거나 설정하거나 초기화하지 못한 경우 발생합니다.
        /// </exception>
        /// <remarks>
        /// Resources released by this method are not recreated. Resource release exceptions are logged and do not stop native reinitialization.<br/>
        /// A <see langword="null"/> property in <paramref name="settings"/> preserves its currently stored value.
        /// <br/><br/>
        /// 이 메서드가 해제한 리소스는 다시 생성하지 않습니다. 리소스 해제 예외는 로그로 출력하고 네이티브 재초기화를 계속합니다.<br/>
        /// <paramref name="settings"/>에서 <see langword="null"/>인 속성은 현재 저장된 값을 유지합니다.
        /// </remarks>
        public void Reset(SoundSystemSettings settings = default)
        {
            ThrowIfSystemLockHeld();

            lock (lifecycleLock)
            {
                if (invokingResettingHandlers)
                    throw new InvalidOperationException("The FMOD sound system lifecycle cannot be changed from an onResetting handler.");

                ThrowIfLifecycleChangeUnavailable();
                InvokeResettingHandlers();
                List<ISoundSystemResource> resources = BeginResourceRelease(LifecycleState.Resetting);
                ReleaseOwnedResources(resources);

                nativeLock.EnterWriteLock();

                try
                {
                    if (nativeInitialized)
                    {
                        native.close().ThrowIfNotOk();
                        nativeInitialized = false;
                    }

                    InitializeNative(settings);
                    queuedDisposals = [];
                    lifecycleState = LifecycleState.Active;
                }
                catch
                {
                    lifecycleState = LifecycleState.Faulted;
                    throw;
                }
                finally
                {
                    nativeLock.ExitWriteLock();
                }

                InvokeResetHandlers();
            }
        }

        void InvokeResettingHandlers()
        {
            Action<SoundSystem>? handlers;
            lock (resetEventLock)
                handlers = _onResetting;

            invokingResettingHandlers = true;

            try
            {
                handlers.SafeInvoke(this);
            }
            finally
            {
                invokingResettingHandlers = false;
            }
        }

        void InvokeResetHandlers()
        {
            Action<SoundSystem>? handlers;
            lock (resetEventLock)
                handlers = _onReset;

            handlers.SafeInvoke(this);
        }
    }
}
