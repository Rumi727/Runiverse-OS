#nullable enable
using RuniOS.IO;
using RuniOS.Resource;
using System.IO;
using UnityEditorInternal;

namespace RuniOS.Editor.Resource
{
    public sealed class NamespacePackDrawer : PackDrawer
    {
        public override string title => "pack_drawer.namespace.title";

        public override bool needsApplyRevert => true;

        public override bool IsMatch(IEnumerable<FilePath> relativePaths) => relativePaths.All(x => x == ResourcePack.assetsFolderName);

        public override void OnEnable(IEnumerable<FilePath> relativePaths)
        {
            relativeExistsPaths = relativePaths
                .Select(x => (Application.streamingAssetsPath + x).value)
                .Where(Directory.Exists)
                .ToArray();

            UpdateNamespaceList();
        }

        string[] relativeExistsPaths = Array.Empty<string>();
        
        List<string> nameSpaces = new List<string>();
        int orgCount;

        ReorderableList? reorderableList;
        
        public override void OnGUI(IEnumerable<FilePath> relativePaths, bool isDebug = false)
        {
            reorderableList ??= new ReorderableList(nameSpaces, typeof(string), false, false, true, true)
            { multiSelect = true };

            reorderableList.list = nameSpaces;

            reorderableList.drawElementCallback = (rect, index, _, _) =>
            {
                EditorGUI.BeginChangeCheck();

                string label = GetTextOrKey("gui.element.index");
                label = new PlaceholderReplacePair("index", index.ToString()).ReplaceAsPlaceholder(label);
                
                EditorGUI.BeginDisabledGroup(index < orgCount);
                string oldText = nameSpaces[index]; 
                string newText = EditorGUI.TextField(rect, label, oldText);
                EditorGUI.EndDisabledGroup();

                if (EditorGUI.EndChangeCheck())
                {
                    string undoName = GetTextOrKey("undo.modify.property_in_object");
                    undoName = new PlaceholderReplacePair("object", typeof(List<string>).Name).ReplaceAsPlaceholder(undoName);
                    undoName = new PlaceholderReplacePair("property", $"[{index}]").ReplaceAsPlaceholder(undoName);
                        
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
                AssetDatabase.CreateFolder(PackInspectorSystem.packRootPath + ResourcePack.assetsFolderName, nameSpaces[i]);
            
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
                .Select(x => ((FilePath)x).GetFileName())
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
                .Select(x => ((FilePath)x).GetFileName())
                .Distinct()
                .ToList();

            orgCount = nameSpaces.Count;
        }
    }
}