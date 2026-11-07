#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.Editor.Localizations;
using RuniOS.Editor.UIElements.Resource;
using RuniOS.IO;
using RuniOS.UIElements;
using System.Collections.Immutable;
using UnityEngine.UIElements;

namespace RuniOS.Editor.Resource
{
    /// <summary>
    /// Edits the selection of <see cref="ResourcePackBrowserWindow"/> with the matching <see cref="PackDrawer"/>.<br/>
    /// <see cref="ResourcePackBrowserWindow"/>의 선택을 맞는 <see cref="PackDrawer"/>로 편집합니다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// While the drawer has unsaved changes, changing the selection or moving or deleting a target in the browser asks whether to save, cancel, or discard them.<br/>
    /// When a target disappears for another reason, the unsaved changes can only be discarded, because saving would write to a location that no longer exists.
    /// </para>
    /// <br/><br/>
    /// <para>
    /// 드로어에 적용하지 않은 변경 사항이 있는 동안 브라우저에서 선택을 바꾸거나 대상을 옮기거나 지우면 저장할지, 취소할지, 버릴지 묻습니다.<br/>
    /// 다른 이유로 대상이 사라지면, 저장하면 더 이상 없는 위치에 쓰게 되므로 변경 사항을 버리는 것만 할 수 있습니다.
    /// </para>
    /// </remarks>
    public sealed class ResourcePackInspectorWindow : EditorWindow, IHasCustomMenu
    {
        [SerializeField] List<ResourcePackNodeReference> targetReferences = [];
        [SerializeField] bool isDebug;

        ImmutableArray<IONode> targets = [];
        PackDrawer? drawer;

        PackInspector? inspector;
        VisualElement? missingTargetsBanner;

        // 더티인 채로 대상이 사라져, 변경 사항을 저장할 수 없는 상태입니다.
        bool targetsMissing;

        // ReSharper disable once Unity.IncorrectMethodSignature
        [MenuItem("RuniOS/Resource Pack Inspector")]
        public static ResourcePackInspectorWindow GetWindow() => GetWindow<ResourcePackInspectorWindow>();

        void OnEnable()
        {
            EditorLocalization.onLanguageUpdate += UpdateTexts;
            UpdateTexts();

            ResourcePackBrowserWindow.selectionChanging += OnSelectionChanging;
            ResourcePackBrowserWindow.selectionChanged += OnSelectionChanged;
            ResourcePackBrowserWindow.moving += OnTargetsChanging;
            ResourcePackBrowserWindow.moved += OnMoved;
            ResourcePackBrowserWindow.deleting += OnTargetsChanging;
            ResourcePackBrowserWindow.deleted += OnDeleted;

            EditorApplication.projectChanged += OnProjectChanged;

            // 브라우저 창과 관계없이 되살리도록 이 창이 보여주던 대상을 직접 저장해 둡니다. 저장해 둔 대상이 없으면 브라우저의 선택을 따릅니다.
            if (targetReferences.Count > 0)
            {
                ImmutableArray<IONode>.Builder nodes = ImmutableArray.CreateBuilder<IONode>(targetReferences.Count);
                foreach (ResourcePackNodeReference reference in targetReferences)
                {
                    if (reference.TryResolve(out IONode node))
                        nodes.Add(node);
                }

                SetTargets(nodes.ToImmutable());
            }
            else
                SetTargets(ResourcePackBrowserWindow.selection);

            ValidateTargets().Forget();
        }

        void OnDisable()
        {
            ResourcePackBrowserWindow.selectionChanging -= OnSelectionChanging;
            ResourcePackBrowserWindow.selectionChanged -= OnSelectionChanged;
            ResourcePackBrowserWindow.moving -= OnTargetsChanging;
            ResourcePackBrowserWindow.moved -= OnMoved;
            ResourcePackBrowserWindow.deleting -= OnTargetsChanging;
            ResourcePackBrowserWindow.deleted -= OnDeleted;

            EditorApplication.projectChanged -= OnProjectChanged;
            EditorLocalization.onLanguageUpdate -= UpdateTexts;

            ReleaseDrawer();
        }

        void UpdateTexts()
        {
            titleContent = new GUIContent(GetTextOrKey("runios-editor:resource_pack_inspector.title"));
            saveChangesMessage = GetTextOrKey("runios-editor:resource_pack_inspector.unsaved_changes.message");
        }

        void CreateGUI()
        {
            missingTargetsBanner = new VisualElement { name = "missing-targets-banner" };
            {
                HelpBox message = new HelpBox { messageType = HelpBoxMessageType.Warning };
                missingTargetsBanner.Add(message);

                Button discard = new Button(DiscardChanges);
                missingTargetsBanner.Add(discard);

                missingTargetsBanner.AddManipulator(new LocalizationManipulator(() =>
                {
                    message.text = GetTextOrKey("runios-editor:resource_pack_inspector.targets_missing.message");
                    discard.text = GetTextOrKey("runios-editor:resource_pack_inspector.unsaved_changes.discard");
                }));
            }
            rootVisualElement.Add(missingTargetsBanner);

            inspector = new PackInspector { drawer = drawer, isDebug = isDebug };
            // Unity 인스펙터처럼, 미리보기에서 더블클릭한 대상만 선택합니다.
            inspector.previewTargetActivated += x => ResourcePackBrowserWindow.SetSelection([x]);
            rootVisualElement.Add(inspector);

            SetTargetsMissing(targetsMissing);
        }

        void OnFocus() => ValidateTargets().Forget();

        void OnProjectChanged() => ValidateTargets().Forget();

        void IHasCustomMenu.AddItemsToMenu(GenericMenu menu) => menu.AddItem(new GUIContent(GetTextOrKey("runios-editor:resource_pack_inspector.debug")), isDebug, () =>
        {
            isDebug = !isDebug;
            if (inspector != null)
                inspector.isDebug = isDebug;
        });

        void SetTargets(ImmutableArray<IONode> nodes)
        {
            if (IsSameTargets(nodes))
                return;

            ReleaseDrawer();

            targets = nodes;

            targetReferences.Clear();
            foreach (IONode node in nodes)
            {
                if (ResourcePackNodeReference.TryCreate(node, out ResourcePackNodeReference reference))
                    targetReferences.Add(reference);
            }

            // 드로어를 만들다 실패하면 드로어 없이 보여줍니다.
            try
            {
                drawer = nodes.IsEmpty ? null : PackInspectorSystem.CreateDrawer(nodes);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                drawer = null;
            }

            if (drawer != null)
            {
                drawer.onDirtyStateChanged += SyncDirtyState;
                EnableDrawer(drawer).Forget();
            }

            if (inspector != null)
                inspector.drawer = drawer;

            SetTargetsMissing(false);
            SyncDirtyState();
        }

        // 저장을 시작하고 바로 다음 대상으로 넘어가므로, 새 드로어는 진행 중인 저장이 끝난 뒤에 대상을 불러옵니다.
        UniTask pendingSave = UniTask.CompletedTask;

        void StartSave(PackDrawer drawer) => pendingSave = Save(drawer).Preserve();

        static async UniTask Save(PackDrawer drawer)
        {
            try
            {
                await drawer.SaveChanges();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        async UniTaskVoid EnableDrawer(PackDrawer drawer)
        {
            await pendingSave;

            // 기다리는 동안 다른 대상으로 바뀌었으면 준비하지 않습니다.
            if (this.drawer != drawer)
                return;

            await drawer.OnEnable();
        }

        async UniTaskVoid Discard(PackDrawer drawer)
        {
            await drawer.DiscardChanges();

            if (targetsMissing)
                ValidateTargets().Forget();
        }

        void ReleaseDrawer()
        {
            if (drawer == null)
                return;

            drawer.onDirtyStateChanged -= SyncDirtyState;
            drawer.OnDisable();

            drawer = null;

            if (inspector != null)
                inspector.drawer = null;
        }

        void SyncDirtyState() => hasUnsavedChanges = drawer?.isDirty ?? false;

        void SetTargetsMissing(bool value)
        {
            targetsMissing = value;

            if (inspector != null)
                inspector.canApply = !value;

            if (missingTargetsBanner != null)
                missingTargetsBanner.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // 순서와 관계없이 같은 대상인지 확인합니다.
        bool IsSameTargets(ImmutableArray<IONode> nodes)
        {
            if (nodes.Length != targets.Length)
                return false;

            foreach (IONode node in nodes)
            {
                if (!targets.Contains(node))
                    return false;
            }

            return true;
        }

        bool OnSelectionChanging(ImmutableArray<IONode> nodes) => IsSameTargets(nodes) || ConfirmLeave();

        // 새로 고침처럼 취소할 수 없는 선택 변경이 더티일 때 오면, 편집 내용을 잃지 않도록 지금 대상을 유지합니다.
        void OnSelectionChanged(ImmutableArray<IONode> nodes)
        {
            if (drawer is { isDirty: true })
                return;

            SetTargets(nodes);
        }

        bool OnTargetsChanging(ImmutableArray<IONode> nodes) => !targets.Any(x => nodes.Any(y => IsSameOrDescendant(x, y))) || ConfirmLeave();

        void OnMoved(ImmutableArray<(IONode source, IONode destination)> moves) => SetTargets([..targets.Select(x => GetMovedNode(x, moves))]);

        void OnDeleted(ImmutableArray<IONode> nodes) => SetTargets([..targets.Where(x => !nodes.Any(y => IsSameOrDescendant(x, y)))]);

        // 더티가 아니면 바로 진행합니다. 대상이 사라졌으면 저장할 수 없으므로 버릴지만 묻습니다.
        bool ConfirmLeave()
        {
            if (drawer is not { isDirty: true })
                return true;

            string title = titleContent.text;
            string cancel = GetTextOrKey("runios-editor:resource_pack_inspector.unsaved_changes.cancel");
            string discard = GetTextOrKey("runios-editor:resource_pack_inspector.unsaved_changes.discard");

            if (targetsMissing)
            {
                if (!EditorUtility.DisplayDialog(title, GetTextOrKey("runios-editor:resource_pack_inspector.targets_missing.message"), discard, cancel))
                    return false;

                drawer.DiscardChanges().Forget();
                return true;
            }

            // 저장과 버리기는 끝나기를 기다리지 않고 진행합니다. 버린 드로어는 곧 해제되고, 저장은 새 드로어가 기다립니다.
            string save = GetTextOrKey("runios-editor:resource_pack_inspector.unsaved_changes.save");
            switch (EditorUtility.DisplayDialogComplex(title, saveChangesMessage, save, cancel, discard))
            {
                case 0:
                    StartSave(drawer);
                    return true;
                case 2:
                    drawer.DiscardChanges().Forget();
                    return true;
                default:
                    return false;
            }
        }

        // 브라우저가 알려 주지 않는 바깥의 변경도 있으므로, 창을 볼 때와 프로젝트가 바뀔 때 대상이 아직 있는지 확인합니다.
        async UniTaskVoid ValidateTargets()
        {
            ImmutableArray<IONode> checkingTargets = targets;

            ImmutableArray<IONode>.Builder existingTargets = ImmutableArray.CreateBuilder<IONode>(checkingTargets.Length);
            foreach (IONode target in checkingTargets)
            {
                if (await target.provider.DirectoryExists(target.path) || await target.provider.FileExists(target.path))
                    existingTargets.Add(target);
            }

            // 확인하는 동안 대상이 바뀌었으면 새 대상은 따로 확인합니다.
            if (checkingTargets != targets)
                return;

            if (existingTargets.Count == checkingTargets.Length)
                SetTargetsMissing(false);
            else if (drawer is { isDirty: true })
                SetTargetsMissing(true);
            else
                SetTargets(existingTargets.ToImmutable());
        }

        public override void SaveChanges()
        {
            // 대상이 사라졌으면 저장하지 않고 더티로 남겨 둡니다.
            if (targetsMissing)
                return;

            // Unity는 창을 닫을 때 이 메서드를 동기로 부르므로, 저장이 끝나기를 기다리지 않고 닫습니다.
            if (drawer != null)
                StartSave(drawer);

            base.SaveChanges();
        }

        public override void DiscardChanges()
        {
            if (drawer != null)
                Discard(drawer).Forget();

            base.DiscardChanges();
        }

        static IONode GetMovedNode(IONode node, ImmutableArray<(IONode source, IONode destination)> moves)
        {
            foreach ((IONode source, IONode destination) in moves)
            {
                if (IsSameOrDescendant(node, source) && node.path.TryGetRelativePath(source.path, out RuniPath relativePath))
                    return destination.CreateInRoot(relativePath.IsEmpty() ? destination.path : destination.path / relativePath);
            }

            return node;
        }

        static bool IsSameOrDescendant(IONode node, IONode ancestor) => node.provider.IsSameTarget(ancestor.provider) && node.path.StartsWith(ancestor.path);
    }
}
