#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.IO;
using RuniOS.Resource;
using System.Collections.Immutable;
using UnityEditorInternal;

namespace RuniOS.Editor.Resource
{
    public sealed class NamespacePackDrawer(ImmutableArray<IONode> targets) : PackDrawer(targets)
    {
        public override string targetTypeName => targetTitle;
        public override string targetTitle => GetTextOrKey("runios-editor:gui.namespace");

        public override bool needsApplyRevert => true;

        public override bool IsMatch(IEnumerable<RuniPath> relativePaths) => relativePaths.Any() && relativePaths.All(x => x == ResourcePack.assetsFolderName);

        // 마지막으로 읽어 온 네임스페이스 목록이며, 되돌리기는 다시 읽지 않고 이 목록으로 돌아갑니다.
        List<string> loadedNameSpaces = [];

        List<string> nameSpaces = [];
        int orgCount;

        ReorderableList? reorderableList;

        protected internal override UniTask OnEnable() => Load();

        // 모든 대상의 하위 폴더 이름을 겹치지 않게 모읍니다.
        async UniTask Load()
        {
            List<string> names = [];
            foreach (IONode target in targets)
            {
                if (!await target.dir.Exists())
                    continue;

                await foreach (IOEntry entry in target.dir.GetDirectories())
                {
                    string name = entry.path.GetFileName();
                    if (!names.Contains(name))
                        names.Add(name);
                }
            }

            loadedNameSpaces = names;
            nameSpaces = [..names];
            orgCount = names.Count;

            Repaint();
        }

        protected internal override void OnGUI(bool isDebug = false)
        {
            GUILayout.Label(TrTempContent("runios-editor:pack_drawer.namespace.title"), RuniStyles.largeLabel);

            List<string> nameSpaces = this.nameSpaces;

            reorderableList ??= new ReorderableList(nameSpaces, typeof(string), false, false, true, true)
            { multiSelect = true };

            reorderableList.list = nameSpaces;

            reorderableList.drawElementCallback = (rect, index, _, _) =>
            {
                EditorGUI.BeginChangeCheck();

                string label = string.Format(GetTextOrKey("runios-editor:gui.element.index"), index);
                
                EditorGUI.BeginDisabledGroup(index < orgCount);
                string oldText = nameSpaces[index]; 
                string newText = EditorGUI.TextField(rect, label, oldText);
                EditorGUI.EndDisabledGroup();

                if (EditorGUI.EndChangeCheck())
                {
                    string undoName = GetTextOrKey("runios-editor:undo.modify.property_in_object");
                    undoName = string.Format(undoName, typeof(List<string>).Name, $"[{index}]");
                        
                    UndoHandler.instance.Record(() => nameSpaces[index] = oldText, () => nameSpaces[index] = newText, undoName, UndoHandler.instance.GetTokenForCurrentUnityGroup());
                    nameSpaces[index] = newText;
                    
                    SetDirty();
                }
            };
            reorderableList.onCanRemoveCallback = x => x.selectedIndices.All(t => t >= orgCount);
            
            reorderableList.onAddCallback = x =>
            {
                int index = x.selectedIndices.Any() && x.onCanRemoveCallback(x) ? Min(x.selectedIndices.Max() + 1, x.count) : x.count;   
                
                bool oldDirty = isDirty;
                const bool newDirty = true;
                
                string newValue = string.Empty;
                nameSpaces.Insert(index, newValue);
                
                UndoHandler.instance.Record
                (
                    () =>
                    {
                        nameSpaces.RemoveAt(index);
                        isDirty = oldDirty;
                    },
                    () =>
                    {
                        nameSpaces.Insert(index, newValue);
                        isDirty = newDirty;
                    },
                    UndoHandler.GetAddElementUndoName(nameSpaces),
                    UndoHandler.instance.GetTokenForCurrentUnityGroup()
                );
                
                x.Select(index);
                SetDirty();
            };

            reorderableList.onRemoveCallback = x =>
            {
                if (x.selectedIndices.Count > 0)
                {
                    foreach (var index in x.selectedIndices.OrderByDescending(i => i))
                        OnRemoveCallback(index);
                    
                    x.Select((x.selectedIndices.Min() - 1).Clamp(0));
                }
                else
                {
                    int count = x.count;
                    OnRemoveCallback(count - 1);
                    x.Select(count - 2);
                }
                
                void OnRemoveCallback(int index)
                {
                    bool oldDirty = isDirty;
                    const bool newDirty = true;
                    
                    string lastValue = nameSpaces[index]; 
                    nameSpaces.RemoveAt(index);
            
                    UndoHandler.instance.Record
                    (
                        () =>
                        {
                            nameSpaces.Insert(index, lastValue);
                            isDirty = oldDirty;
                        },
                        () =>
                        {
                            nameSpaces.RemoveAt(index);
                            isDirty = newDirty;
                        },
                        UndoHandler.GetRemoveElementUndoName(nameSpaces),
                        UndoHandler.instance.GetTokenForCurrentUnityGroup()
                    );
                }
                
                SetDirty();
            };

            reorderableList.DoLayoutList();
        }

        // 프로젝트 StreamingAssets가 아니라 편집 중인 각 assets 폴더에 만들고, 쓸 수 없는 위치는 건너뜁니다.
        public override async UniTask SaveChanges()
        {
            ImmutableArray<string> createdNameSpaces = [..nameSpaces.Skip(orgCount)];
            foreach (IONode target in targets)
            {
                if (target.AsWriteNode() is not { } directory)
                    continue;

                foreach (string nameSpace in createdNameSpaces)
                    await directory.CreateChild(nameSpace).dir.Create();
            }

            AssetDatabase.Refresh();
            await Load();

            await base.SaveChanges();
        }

        // 실행 취소 기록을 지금 Unity 실행 취소 그룹에 넣어야 하므로, 다시 읽지 않고 마지막으로 읽어 온 목록으로 돌아갑니다.
        public override UniTask DiscardChanges()
        {
            bool oldDirty = isDirty;
            const bool newDirty = false;

            List<string> oldList = nameSpaces;
            List<string> newList = [..loadedNameSpaces];

            int oldOrgCount = orgCount;
            int newOrgCount = newList.Count;
            
            UndoHandler.instance.Record(() =>
            {
                nameSpaces = oldList;
                orgCount = oldOrgCount;
                isDirty = oldDirty;
            }, () =>
            {
                nameSpaces = newList;
                orgCount = newOrgCount;
                isDirty = newDirty;
            }, UndoHandler.GetDiscardUndoName(nameSpaces), UndoHandler.instance.GetTokenForCurrentUnityGroup());

            nameSpaces = newList;
            orgCount = newOrgCount;

            return base.DiscardChanges();
        }
    }
}