#nullable enable
using System.Threading;
using Unity.Scripting.LifecycleManagement;

namespace RuniOS.Sounds
{
    static partial class NoteBlockPlaybackWorker
    {
        static readonly object gate = new object();
        static readonly HashSet<NoteBlockSource> players = [];
        static readonly AutoResetEvent wakeEvent = new AutoResetEvent(false);

        static Thread? thread;
        static bool stopping;

        public static void Register(NoteBlockSource player)
        {
            lock (gate)
            {
                players.Add(player);
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

        public static void Unregister(NoteBlockSource player)
        {
            lock (gate)
                players.Remove(player);

            wakeEvent.Set();
        }

        public static void Signal() => wakeEvent.Set();

        [OnCodeUnloading]
        static void OnCodeUnloading()
        {
            Thread? worker;
            lock (gate)
            {
                stopping = true;
                players.Clear();
                worker = thread;
            }

            wakeEvent.Set();
            if (worker != null && worker != Thread.CurrentThread)
                worker.Join(TimeSpan.FromSeconds(1));
        }

        static void Run()
        {
            while (true)
            {
                NoteBlockSource[] snapshot;
                lock (gate)
                {
                    if (stopping)
                        break;

                    snapshot = players.ToArray();
                }

                foreach (NoteBlockSource player in snapshot)
                {
                    try
                    {
                        player.WorkerUpdate();
                    }
                    catch (Exception exception)
                    {
                        Debug.LogException(exception);
                    }
                }

                if (snapshot.Length == 0)
                    wakeEvent.WaitOne();
                else
                    wakeEvent.WaitOne(TimeSpan.FromSeconds(NoteBlockPlaybackSettings.workerInterval));
            }

            lock (gate)
                thread = null;
        }
    }
}
