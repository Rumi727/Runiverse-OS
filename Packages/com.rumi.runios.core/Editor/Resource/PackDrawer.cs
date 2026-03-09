#nullable enable
using RuniOS.IO;

namespace RuniOS.Editor.Resource
{
    public abstract class PackDrawer
    {
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
        
        public abstract bool IsMatch(IEnumerable<FilePath> relativePaths);
        
        public virtual void OnEnable(IEnumerable<FilePath> relativePaths) { }
        public virtual void OnDisable() { }
        
        public abstract void OnGUI(IEnumerable<FilePath> relativePaths, bool isDebug = false);

        protected void SetDirty() => isDirty = true;

        public virtual void SaveChanges() => isDirty = false;
        
        public virtual void DiscardChanges() => isDirty = false;
    }
}