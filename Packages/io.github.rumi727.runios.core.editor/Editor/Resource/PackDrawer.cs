#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.IO;
using System.Collections.Immutable;
using System.Text.RegularExpressions;
using UnityEngine.UIElements;

namespace RuniOS.Editor.Resource
{
    public abstract class PackDrawer(ImmutableArray<IONode> targets)
    {
        public ImmutableArray<IONode> targets { get; } = targets;
        public IONode? target { get; } = targets.Length == 1 ? targets[0] : null;

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
        
        /// <summary>
        /// Prepares the drawer, such as loading the targets. The drawer is drawn while this is running, so drawing must handle data that is not loaded yet.<br/>
        /// 대상을 불러오는 등 드로어를 준비합니다. 이 작업이 진행되는 동안에도 드로어를 그리므로, 아직 불러오지 않은 데이터를 그릴 수 있어야 합니다.
        /// </summary>
        /// <returns>
        /// An asynchronous operation that completes when the drawer is ready.<br/>
        /// 드로어가 준비되면 완료되는 비동기 작업입니다.
        /// </returns>
        protected internal virtual UniTask OnEnable() => UniTask.CompletedTask;
        protected internal virtual void OnDisable() { }

        /// <summary>
        /// Creates the UI Toolkit element that edits the targets. Return <see langword="null"/> to draw the targets with <see cref="OnGUI"/> instead.<br/>
        /// 대상을 편집하는 UI Toolkit 요소를 만듭니다. <see langword="null"/>을 반환하면 대신 <see cref="OnGUI"/>로 대상을 그립니다.
        /// </summary>
        /// <param name="isDebug">
        /// <see langword="true"/> if the inspector is in debug mode. The element is created again when the mode changes.<br/>
        /// 인스펙터가 디버그 모드이면 <see langword="true"/>입니다. 모드가 바뀌면 요소를 다시 만듭니다.
        /// </param>
        /// <returns>
        /// The element that edits the targets, or <see langword="null"/> to use <see cref="OnGUI"/>.<br/>
        /// 대상을 편집하는 요소를 반환하고, <see cref="OnGUI"/>를 쓰려면 <see langword="null"/>을 반환합니다.
        /// </returns>
        protected internal virtual VisualElement? CreateInspectorGUI(bool isDebug = false) => null;

        protected internal abstract void OnGUI(bool isDebug = false);

        protected void SetDirty() => isDirty = true;

        /// <summary>
        /// Writes the changes to the targets and clears <see cref="isDirty"/>.<br/>
        /// Overrides call the base method after writing, so <see cref="isDirty"/> stays <see langword="true"/> when writing fails.
        /// <br/><br/>
        /// 변경 사항을 대상에 쓰고 <see cref="isDirty"/>를 해제합니다.<br/>
        /// 재정의할 때는 쓴 뒤에 기반 메서드를 호출하므로, 쓰기에 실패하면 <see cref="isDirty"/>가 <see langword="true"/>로 남습니다.
        /// </summary>
        /// <returns>
        /// An asynchronous operation that completes when the changes are written.<br/>
        /// 변경 사항을 다 쓰면 완료되는 비동기 작업입니다.
        /// </returns>
        public virtual UniTask SaveChanges()
        {
            isDirty = false;
            return UniTask.CompletedTask;
        }

        /// <summary>
        /// Discards the changes, restores the values of the targets, and clears <see cref="isDirty"/>.<br/>
        /// 변경 사항을 버리고 대상의 값을 되돌린 뒤 <see cref="isDirty"/>를 해제합니다.
        /// </summary>
        /// <returns>
        /// An asynchronous operation that completes when the values are restored.<br/>
        /// 값을 다 되돌리면 완료되는 비동기 작업입니다.
        /// </returns>
        public virtual UniTask DiscardChanges()
        {
            isDirty = false;
            return UniTask.CompletedTask;
        }

        protected internal virtual bool HasPreviewGUI() => false;
        protected internal virtual GUIContent GetPreviewTitle() => new GUIContent("Preview");
        protected internal virtual void OnPreviewGUI(Rect r, IONode node, GUIStyle background) { }
        protected internal virtual void OnInteractivePreviewGUI(Rect r, IONode node, GUIStyle background) => OnPreviewGUI(r, node, background);
        protected internal virtual void OnPreviewSettings() { }

        /// <summary>
        /// Creates the UI Toolkit element that previews <paramref name="node"/>. Return <see langword="null"/> to draw the preview with <see cref="OnInteractivePreviewGUI"/> instead.<br/>
        /// This is used only when <see cref="HasPreviewGUI"/> returns <see langword="true"/> and there is a single target. Several targets are always drawn as a grid with <see cref="OnInteractivePreviewGUI"/>.
        /// <br/><br/>
        /// <paramref name="node"/>를 미리 보여주는 UI Toolkit 요소를 만듭니다. <see langword="null"/>을 반환하면 대신 <see cref="OnInteractivePreviewGUI"/>로 미리보기를 그립니다.<br/>
        /// <see cref="HasPreviewGUI"/>가 <see langword="true"/>를 반환하고 대상이 하나일 때만 사용합니다. 대상이 여러 개면 항상 <see cref="OnInteractivePreviewGUI"/>로 격자를 그립니다.
        /// </summary>
        /// <param name="node">
        /// The target to preview.<br/>
        /// 미리 볼 대상입니다.
        /// </param>
        /// <returns>
        /// The element that previews <paramref name="node"/>, or <see langword="null"/> to use <see cref="OnInteractivePreviewGUI"/>.<br/>
        /// <paramref name="node"/>를 미리 보여주는 요소를 반환하고, <see cref="OnInteractivePreviewGUI"/>를 쓰려면 <see langword="null"/>을 반환합니다.
        /// </returns>
        protected internal virtual VisualElement? CreatePreview(IONode node) => null;

        protected static bool IsMatch(RuniPath path, string folderName, IPatternMatcher matcher)
        {
            if (!Regex.IsMatch(path.value, $"^assets/.*/{folderName}/.*", RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture))
                return false;

            return matcher.IsMatch(path);
        }

        public void Repaint() => repaintAction?.Invoke();
    }
}