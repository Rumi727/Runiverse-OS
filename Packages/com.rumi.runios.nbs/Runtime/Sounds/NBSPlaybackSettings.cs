#nullable enable
using System.Threading;

namespace RuniOS.Sounds
{
    /// <summary>
    /// Provides runtime-wide timing settings for the shared NBS playback worker.<br/>
    /// 공유 NBS 재생 워커의 런타임 전역 타이밍 설정을 제공합니다.
    /// </summary>
    public static class NBSPlaybackSettings
    {
        /// <summary>
        /// Gets or sets how long the worker waits after each scan, in seconds. The default is <c>0.002</c> seconds.<br/>
        /// 워커가 각 순회 후 대기할 시간을 초 단위로 가져오거나 설정합니다. 기본값은 <c>0.002</c>초입니다.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the value is not finite or is not greater than zero.<br/>
        /// 값이 유한하지 않거나 0보다 크지 않으면 발생합니다.
        /// </exception>
        public static double workerInterval
        {
            get => Volatile.Read(ref _workerInterval);
            set
            {
                if (!double.IsFinite(value) || value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "The NBS worker interval must be finite and greater than zero.");

                Volatile.Write(ref _workerInterval, value);
                NBSPlaybackWorker.Signal();
            }
        }
        static double _workerInterval = 0.1;

        /// <summary>
        /// Gets or sets how many seconds ahead FMOD DSP starts are reserved. Zero selects immediate worker-pulse playback. The default is <c>0.05</c> seconds.<br/>
        /// FMOD DSP 시작을 몇 초 앞서 예약할지 가져오거나 설정합니다. 0이면 워커 펄스 기반 즉시 재생을 선택합니다. 기본값은 <c>0.05</c>초입니다.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the value is negative or not finite.<br/>
        /// 값이 음수이거나 유한하지 않으면 발생합니다.
        /// </exception>
        public static double schedulingLookahead
        {
            get => Volatile.Read(ref _schedulingLookahead);
            set
            {
                if (!double.IsFinite(value) || value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Scheduling lookahead must be finite and non-negative.");

                if (Volatile.Read(ref _schedulingLookahead).Approximately(value))
                    return;

                Volatile.Write(ref _schedulingLookahead, value);
                Interlocked.Increment(ref _schedulingRevision);
                NBSPlaybackWorker.Signal();
            }
        }
        static double _schedulingLookahead = 0.2;

        /// <summary>
        /// Gets or sets how late a note may be before it is skipped, in seconds. The default is <c>0.05</c> seconds.<br/>
        /// 음이 생략되기 전까지 허용할 지연 시간을 초 단위로 가져오거나 설정합니다. 기본값은 <c>0.05</c>초입니다.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the value is negative or not finite.<br/>
        /// 값이 음수이거나 유한하지 않으면 발생합니다.
        /// </exception>
        public static double lateTolerance
        {
            get => Volatile.Read(ref _lateTolerance);
            set
            {
                if (!double.IsFinite(value) || value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Late tolerance must be finite and non-negative.");

                Volatile.Write(ref _lateTolerance, value);
                NBSPlaybackWorker.Signal();
            }
        }
        static double _lateTolerance = 0.05;

        internal static long schedulingRevision => Interlocked.Read(ref _schedulingRevision);
        static long _schedulingRevision;
    }
}
