#nullable enable
using RuniOS.IO;
using RuniOS.Resource;
using System.Collections.Immutable;
using System.IO;
using UnityEditorInternal;

namespace RuniOS.Editor.Resource
{
    public sealed class NamespacePackDrawer : PackDrawer
    {
        public NamespacePackDrawer(ImmutableArray<PathPair> targets) : base(targets)
        {
            relativeExistsPaths = targets
                .Select<PathPair, string>(x => x.rootPath / x.relativePath)
                .Where(Directory.Exists)
                .ToArray();

            UpdateNamespaceList();
        }

        public override string targetTypeName => targetTitle;
        public override string targetTitle => GetTextOrKey("runios-editor:gui.namespace");

        public override bool needsApplyRevert => true;

        public override bool IsMatch(IEnumerable<RuniPath> relativePaths) => relativePaths.All(x => x == ResourcePack.assetsFolderName);

        protected internal override void OnEnable()
        {

        }

        readonly string[] relativeExistsPaths = [];

        List<string> nameSpaces = [];
        int orgCount;

        ReorderableList? reorderableList;

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

                string label = string.Format(GetTextOrKey("gui.element.index"), index);
                
                EditorGUI.BeginDisabledGroup(index < orgCount);
                string oldText = nameSpaces[index]; 
                string newText = EditorGUI.TextField(rect, label, oldText);
                EditorGUI.EndDisabledGroup();

                if (EditorGUI.EndChangeCheck())
                {
                    string undoName = GetTextOrKey("undo.modify.property_in_object");
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

        public override void SaveChanges()
        {
            for (int i = orgCount; i < nameSpaces.Count; i++)
                AssetDatabase.CreateFolder((PackInspectorSystem.packRootPath / ResourcePack.assetsFolderName).value, nameSpaces[i]);
            
            UpdateNamespaceList();
            base.SaveChanges();
        }

        public override void DiscardChanges()
        {
            bool oldDirty = isDirty;
            const bool newDirty = false;
            
            List<string> oldList = nameSpaces;
            List<string> newList = relativeExistsPaths
                .SelectMany(Directory.GetDirectories)
                .Select(x => ((RuniPath)x).GetFileName())
                .Distinct()
                .ToList();

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

            base.DiscardChanges();
        }

        void UpdateNamespaceList()
        {
            nameSpaces = relativeExistsPaths
                .SelectMany(Directory.GetDirectories)
                .Select(x => ((RuniPath)x).GetFileName())
                .Distinct()
                .ToList();

            orgCount = nameSpaces.Count;
        }
    }
}