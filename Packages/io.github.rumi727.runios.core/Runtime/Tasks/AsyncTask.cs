#nullable enable
using R3;
using RuniOS.Texts;
using System.Diagnostics;
using System.Threading;

namespace RuniOS.Tasks
{
    public sealed class AsyncTask : ProgressTask
    {
        public static event Action? asyncTaskAdd = null;
        public static event Action? asyncTaskRemove = null;



        static readonly List<AsyncTask> _asyncTasks = [];
        public static IReadOnlyList<AsyncTask> asyncTasks { get; } = _asyncTasks.AsReadOnly();



        public AsyncTask() : this(Text.empty, Text.empty) { }

        public AsyncTask(Text name) : this(name, Text.empty) { }

        public AsyncTask(Text name, Text description, bool cancellable = false)
        {
            this.name = name;
            this.description = description;
            this.cancellable = cancellable;

            _asyncTasks.Add(this);
            asyncTaskAdd.SafeInvoke();

            runningTimeWatch.Start();
        }



        public override Text name { get; set; } = Text.empty;
        public override Text description { get; set; } = Text.empty;

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
                throw new ObjectDisposedException(nameof(AsyncTask));

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