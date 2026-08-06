#nullable enable
using RuniOS.IO;
using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace RuniOS.Editor.Resource
{
    public abstract class PackDrawer(ImmutableArray<PackDrawer.PathPair> targets)
    {
        public record struct PathPair(PhysicalPath rootPath, RuniPath relativePath);

        public ImmutableArray<PathPair> targets { get; } = targets;
        public PathPair? target { get; } = targets.Length == 1 ? targets[0] : null;

        public bool isEnabled { get; internal set; } = false;

        public virtual string? targetTitle => null;
        public abstract string targetTypeName { get; }

        public virtual int order => 0;

        public virtual bool needsApplyRevert => false;

        public bool isDirty
        {
            get => _isDirty;
            set
            {
                if (_isDirty == value)
                    return;
                
                _isDirty = value;
                onDirtyStateChanged?.Invoke();
            }
        }
        bool _isDirty;
        
        public event Action? onDirtyStateChanged;

        internal Action? repaintAction;
        
        public abstract bool IsMatch(IEnumerable<RuniPath> relativePaths);
        
        protected internal virtual void OnEnable() { }
        protected internal virtual void OnDisable() { }

        protected internal abstract void OnGUI(bool isDebug = false);

        protected void SetDirty() => isDirty = true;

        public virtual void SaveChanges() => isDirty = false;
        
        public virtual void DiscardChanges() => isDirty = false;

        public virtual Texture2D? RenderStaticPreview(PhysicalPath rootPath, RuniPath relativePath, int width, int height) => null;

        protected internal virtual bool HasPreviewGUI() => false;
        protected internal virtual GUIContent GetPreviewTitle() => new GUIContent("Preview");
        protected internal virtual void OnPreviewGUI(Rect r, PhysicalPath rootPath, RuniPath relativePath, GUIStyle background) { }
        protected internal virtual void OnInteractivePreviewGUI(Rect r, PhysicalPath rootPath, RuniPath relativePath, GUIStyle background) => OnPreviewGUI(r, rootPath, relativePath, background);
        protected internal virtual void OnPreviewSettings() { }

        protected static bool IsMatch(RuniPath path, string folderName, WildcardPatterns patterns)
        {
            if (!Regex.IsMatch(path.value, $"^assets/.*/{folderName}/.*", RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture))
                return false;

            return patterns.IsMatch(path);
        }

        public void Repaint() => repaintAction?.Invoke();
    }
}