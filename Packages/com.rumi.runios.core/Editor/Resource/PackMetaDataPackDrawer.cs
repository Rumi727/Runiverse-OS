#nullable enable
using Newtonsoft.Json;
using RuniOS.Editor.Inspectors;
using RuniOS.Inspectors;
using RuniOS.Inspectors.Csharp;
using RuniOS.IO;
using RuniOS.Resource;
using System.IO;

namespace RuniOS.Editor.Resource
{
    public sealed class PackMetaDataPackDrawer : PackDrawer
    {
        public override int order => int.MinValue;

        public override bool needsApplyRevert => true;

        public override bool IsMatch(IEnumerable<FilePath> relativePaths) => relativePaths.All(x => x.IsEmpty() || x == ResourcePack.infoPath);

        public override void OnEnable(IEnumerable<FilePath> relativePaths)
        {
            relativeExistsPaths = relativePaths
                .Select(x =>
                {
                    if (x.IsEmpty())
                        return (Application.streamingAssetsPath + x + ResourcePack.infoPath).value;
                    
                    return (Application.streamingAssetsPath + x).value;
                })
                .Where(File.Exists)
                .ToArray();

            DiscardChanges();
        }

        string[] relativeExistsPaths = Array.Empty<string>();
        PackMetaData[] packMetaDatas = Array.Empty<PackMetaData>();
        static readonly InspectableObject inspectableObject = new InspectableObject(typeof(PackMetaData));
        static readonly Inspector inspector = new Inspector(UndoHandler.instance);
        public override void OnGUI(IEnumerable<FilePath> relativePaths, bool isDebug = false)
        {
            InspectorFlags flags = InspectorFlags.InstanceAccess | InspectorFlags.Variable;
            if (isDebug)
                flags |= InspectorFlags.Debug;
                
            if (inspector.inspectorFlags != flags)
                inspector.Rebuild(inspectableObject, flags);
            
            GUILayout.Label(GetTextOrKey("pack_drawer.generic.title"), largeLabel);

            if (packMetaDatas.Length == 0)
            {
                if (GUILayout.Button(GetTextOrKey("pack_drawer.pack_meta_data.create")))
                {
                    string json = JsonConvert.SerializeObject(new PackMetaData(), Formatting.Indented);
                    File.WriteAllText(Application.streamingAssetsPath.ToPath() + ResourcePack.infoPath, json);
                    
                    OnEnable(relativePaths);
                }
                
                return;
            }
            
            inspectableObject.SetInstances(packMetaDatas);
            
            inspectableObject.onValueChanged = instances =>
            {
                int index = 0;
                foreach (var instance in instances.Cast<PackMetaData>())
                {
                    packMetaDatas[index] = instance;
                    index++;
                }
                
                SetDirty();
            };

            inspector.DrawLayout();
        }

        public override void SaveChanges()
        {
            foreach ((string path, PackMetaData instance) in relativeExistsPaths.Zip(packMetaDatas, (path, instance) => (path, instance)))
            {
                string json = JsonConvert.SerializeObject(instance, Formatting.Indented);
                File.WriteAllText(path, json);
            }
            
            base.SaveChanges();
        }

        public override void DiscardChanges()
        {
            packMetaDatas = relativeExistsPaths
                .Select(File.ReadAllText)
                .Select(JsonConvert.DeserializeObject<PackMetaData>)
                .ToArray();
            
            base.DiscardChanges();
        }
    }
}