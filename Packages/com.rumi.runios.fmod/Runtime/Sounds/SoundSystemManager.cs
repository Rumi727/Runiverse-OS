#nullable enable
using System.Threading;
using Unity.Scripting.LifecycleManagement;

namespace RuniOS.Sounds
{
    sealed partial class SoundSystemManager
    {
        const int updateIntervalMilliseconds = 10;
        static readonly object updateThreadLock = new();
        static readonly AutoResetEvent updateWake = new(false);

        static Thread? thread;
        static volatile bool stop;

        [OnCodeLoaded]
        static void OnCodeLoaded()
        {
            thread = new Thread(UpdateLoop)
            {
                IsBackground = true,
                Name = "Runiverse OS FMOD Update"
            };
            thread.Start();
        }

        static void UpdateLoop()
        {
            while (!stop)
            {
                try
                {
                    SoundSystem.main.Execute(system => system.Update());
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }

                updateWake.WaitOne(updateIntervalMilliseconds);
            }
        }

        [OnCodeUnloading]
        static void OnCodeUnloading()
        {
            stop = true;
            updateWake.Set();

            if (thread != null && thread != Thread.CurrentThread)
                thread.Join();

            SoundSystem.main.Dispose();
        }
    }
}
