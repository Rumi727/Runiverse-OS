#nullable enable
using System.Threading;
using Unity.Scripting.LifecycleManagement;

namespace RuniOS.NBS
{
    /// <summary>
    /// Runs the background thread that every <see cref="NBSVoiceScheduler{TTransport}"/> shares to schedule sounds ahead of time.<br/>
    /// 모든 <see cref="NBSVoiceScheduler{TTransport}"/>가 소리를 미리 예약하기 위해 공유하는 백그라운드 스레드를 실행합니다.
    /// </summary>
    public static partial class NBSPlaybackWorker
    {
        /// <summary>Is implemented by the schedulers that the worker drives.</summary>
        internal interface IClient
        {
            void Tick();
        }

        static readonly Lock gate = new();
        static readonly List<IClient> clients = [];
        static readonly AutoResetEvent wakeEvent = new AutoResetEvent(false);
        static Thread? thread;
        static bool stopping;

        /// <summary>
        /// Wakes the worker so it runs a pass immediately.<br/>
        /// 워커를 깨워 즉시 한 번 순회하게 합니다.
        /// </summary>
        public static void Signal() => wakeEvent.Set();

        internal static void Register(IClient client)
        {
            lock (gate)
            {
                clients.Add(client);
                stopping = false;
                if (thread is not { IsAlive: true })
                {
                    thread = new Thread(Run)
                    {
                        IsBackground = true,
                        Name = "Runiverse OS NBS Playback"
                    };
                    thread.Start();
                }
            }

            wakeEvent.Set();
        }

        internal static void Unregister(IClient client)
        {
            lock (gate)
                clients.Remove(client);
        }

        [OnCodeUnloading]
        static void OnCodeUnloading()
        {
            Thread? worker;
            lock (gate)
            {
                stopping = true;
                clients.Clear();
                worker = thread;
            }

            wakeEvent.Set();
            if (worker != null && worker != Thread.CurrentThread)
                worker.Join(TimeSpan.FromSeconds(1));
        }

        static void Run()
        {
            List<IClient> snapshot = [];
            while (true)
            {
                lock (gate)
                {
                    if (stopping)
                    {
                        thread = null;
                        return;
                    }

                    snapshot.Clear();
                    snapshot.AddRange(clients);
                }

                for (int i = 0; i < snapshot.Count; i++)
                {
                    try
                    {
                        snapshot[i].Tick();
                    }
                    catch (Exception exception)
                    {
                        Debug.LogException(exception);
                    }
                }

                if (snapshot.Count == 0)
                    wakeEvent.WaitOne();
                else
                    wakeEvent.WaitOne(TimeSpan.FromSeconds(NBSPlaybackSettings.workerInterval));
            }
        }
    }
}
