#nullable enable
using System.Threading;

namespace RuniOS.NBS
{
    /// <summary>
    /// Provides the runtime-wide timing settings of NBS playback.<br/>
    /// NBS 재생의 런타임 전역 타이밍 설정을 제공합니다.
    /// </summary>
    public static class NBSPlaybackSettings
    {
        /// <summary>
        /// Gets or sets how long the shared playback worker waits between passes, in seconds. The default is <c>0.1</c>.<br/>
        /// 공유 재생 워커가 순회 사이에 기다리는 시간을 초 단위로 가져오거나 설정합니다. 기본값은 <c>0.1</c>입니다.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the value is not finite or not greater than zero.<br/>
        /// 값이 유한하지 않거나 0보다 크지 않은 경우 발생합니다.
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
        /// Gets or sets how many wall-clock seconds ahead sounds are scheduled on the DSP clock. The default is <c>0.2</c>.<br/>
        /// 소리를 DSP 클록에 몇 초(벽시계 기준) 앞서 예약할지 가져오거나 설정합니다. 기본값은 <c>0.2</c>입니다.
        /// </summary>
        /// <remarks>
        /// With <c>0</c>, each pass starts only the sounds that are due, so their timing depends on <see cref="workerInterval"/>.<br/>
        /// <c>0</c>이면 각 순회는 시작할 때가 된 소리만 시작하므로 타이밍이 <see cref="workerInterval"/>에 좌우됩니다.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the value is negative or not finite.<br/>
        /// 값이 음수이거나 유한하지 않은 경우 발생합니다.
        /// </exception>
        public static double schedulingLookahead
        {
            get => Volatile.Read(ref _schedulingLookahead);
            set
            {
                if (!double.IsFinite(value) || value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "The NBS scheduling lookahead must be finite and non-negative.");

                Volatile.Write(ref _schedulingLookahead, value);
                NBSPlaybackWorker.Signal();
            }
        }
        static double _schedulingLookahead = 0.2;
    }
}
