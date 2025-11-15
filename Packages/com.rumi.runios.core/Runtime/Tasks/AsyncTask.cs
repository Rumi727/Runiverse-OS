#nullable enable
using R3;
using RuniOS.Localizations;
using System.Diagnostics;
using System.Threading;

namespace RuniOS.Tasks
{
    public sealed class AsyncTask : ProgressTask
    {
        public static event Action? asyncTaskAdd = null;
        public static event Action? asyncTaskRemove = null;



        static readonly List<AsyncTask> _asyncTasks = new List<AsyncTask>();
        public static IReadOnlyList<AsyncTask> asyncTasks { get; } = _asyncTasks.AsReadOnly();



        public AsyncTask() : this(Localization.empty, Localization.empty) { }

        public AsyncTask(Localization name) : this(name, Localization.empty) { }

        public AsyncTask(Localization name, Localization description, bool cancellable = false)
        {
            this.name = name;
            this.description = description;
            this.cancellable = cancellable;

            _asyncTasks.Add(this);
            asyncTaskAdd.SafeInvoke();

            runningTimeWatch.Start();
        }



        public override Localization name { get; set; } = Localization.empty;
        public override Localization description { get; set; } = Localization.empty;

        public override ReactiveProperty<float> progress { get; } = new();

        public override bool cancellable { get; set; } = false;

        public override event Action? cancelEvent = null;

        public override bool isDisposed { get; protected set; } = false;

        public override double runningTime => runningTimeWatch.Elapsed.TotalSeconds;
        readonly Stopwatch runningTimeWatch = new Stopwatch();



        readonly CancellationTokenSource cancelTokenSource = new CancellationTokenSource();
        public CancellationToken cancelToken => cancelTokenSource.Token;



        public override void Dispose()
        {
            if (isDisposed)
                throw new ObjectDisposedException(GetType().Name);

            cancelEvent.SafeInvoke();
            cancelEvent = null;

            _asyncTasks.Remove(this);
            asyncTaskRemove.SafeInvoke();

            cancelTokenSource.Cancel();

            isDisposed = true;
            runningTimeWatch.Stop();
            
            progress.Dispose();
        }
    }
}