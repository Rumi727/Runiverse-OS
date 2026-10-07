#nullable enable
using R3;
using RuniOS.Texts;

namespace RuniOS.Tasks
{
    public abstract class ProgressTask : IProgress<float>, IDisposable
    {
        public abstract Text name { get; set; }
        public abstract Text description { get; set; }
        
        public abstract ReactiveProperty<float> progress { get; }
        public abstract bool cancellable { get; set; }

        public bool isRunning => !isDisposed;

        public abstract event Action? cancelEvent;

        public abstract bool isDisposed { get; protected set; }

        public abstract double runningTime { get; }
        
        public abstract void Dispose();
        
        void IProgress<float>.Report(float value) => progress.Value = value;
    }
}