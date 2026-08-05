#nullable enable
using RuniOS.IO;
using System.Text.RegularExpressions;

namespace RuniOS.Editor.Resource
{
    public abstract class PackDrawer
    {
        public bool isEnabled { get; internal set; } = false;

        public virtual string? title => null;

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
        
        public virtual void OnEnable(PhysicalPath rootPath, IEnumerable<RuniPath> relativePaths) { }
        public virtual void OnDisable() { }

        public abstract void OnGUI(PhysicalPath rootPath, IEnumerable<RuniPath> relativePaths, bool isDebug = false);

        protected void SetDirty() => isDirty = true;

        public virtual void SaveChanges() => isDirty = false;
        
        public virtual void DiscardChanges() => isDirty = false;

        protected static bool IsMatch(RuniPath path, string folderName, WildcardPatterns patterns)
        {
            if (!Regex.IsMatch(path.value, $"^assets/.*/{folderName}/.*", RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture))
                return false;

            return patterns.IsMatch(path);
        }

        public void Repaint() => repaintAction?.Invoke();
    }
}